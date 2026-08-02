using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace SimpleSummon.Network
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class NetworkPlayer : NetworkBehaviour
    {
        private const float InputSendInterval = 1f / 30f;

        private readonly NetworkVariable<float> health = new();
        private readonly NetworkVariable<bool> dead = new();
        private readonly NetworkVariable<int> damageSequence = new();
        private readonly NetworkVariable<FixedString64Bytes> nickname = new("Player");

        private Vector3 serverMoveDirection;
        private bool serverJumpRequested;
        private bool serverAttackRequested;
        private Vector3 pendingMoveDirection;
        private bool pendingJumpRequested;
        private bool pendingAttackRequested;
        private float nextInputSendTime;

        public event Action RoleChanged;
        public event Action<float, bool> VitalStateChanged;
        public event Action DamageReceived;
        public event Action<string> NicknameChanged;
        public static event Action LocalPlayerChanged;
        public static NetworkPlayer LocalPlayer { get; private set; }

        public float CurrentHealth => health.Value;

        private bool IsOffline =>
            NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.IsListening;

        public bool CanReadLocalInput => IsOffline || IsSpawned && IsOwner;
        public bool CanRunSimulation => IsOffline || IsSpawned && IsServer;
        public string Nickname => IsOffline
            ? NormalizeNickname(NicknameStorage.Load())
            : nickname.Value.ToString();

        public override void OnNetworkSpawn()
        {
            health.OnValueChanged += HandleHealthChanged;
            dead.OnValueChanged += HandleDeadChanged;
            damageSequence.OnValueChanged += HandleDamageSequenceChanged;
            nickname.OnValueChanged += HandleNicknameChanged;
            if (IsOwner)
            {
                LocalPlayer = this;
                LocalPlayerChanged?.Invoke();
                SetNickname(NicknameStorage.Load());
            }
            RoleChanged?.Invoke();
            VitalStateChanged?.Invoke(health.Value, dead.Value);
            NicknameChanged?.Invoke(Nickname);
        }

        public override void OnNetworkDespawn()
        {
            health.OnValueChanged -= HandleHealthChanged;
            dead.OnValueChanged -= HandleDeadChanged;
            damageSequence.OnValueChanged -= HandleDamageSequenceChanged;
            nickname.OnValueChanged -= HandleNicknameChanged;
            if (LocalPlayer == this)
            {
                LocalPlayer = null;
                LocalPlayerChanged?.Invoke();
            }
            RoleChanged?.Invoke();
        }

        private void SetNickname(string value)
        {
            string normalized = NormalizeNickname(value);
            if (IsServer)
            {
                nickname.Value = normalized;
            }
            else
            {
                SetNicknameRpc(normalized);
            }
        }

        [Rpc(SendTo.Server)]
        private void SetNicknameRpc(FixedString64Bytes value)
        {
            nickname.Value = NormalizeNickname(value.ToString());
        }

        public void SubmitInput(Vector3 moveDirection, bool jumpRequested, bool attackRequested)
        {
            if (!IsSpawned)
            {
                SetServerInput(moveDirection, jumpRequested, attackRequested);
                return;
            }

            if (!IsOwner)
            {
                return;
            }

            if (IsServer)
            {
                SetServerInput(moveDirection, jumpRequested, attackRequested);
            }
            else
            {
                pendingMoveDirection = moveDirection;
                pendingJumpRequested |= jumpRequested;
                pendingAttackRequested |= attackRequested;

                if (Time.unscaledTime < nextInputSendTime)
                {
                    return;
                }

                nextInputSendTime = Time.unscaledTime + InputSendInterval;
                SubmitInputRpc(
                    pendingMoveDirection,
                    pendingJumpRequested,
                    pendingAttackRequested);
                pendingJumpRequested = false;
                pendingAttackRequested = false;
            }
        }

        public void ReadServerInput(
            out Vector3 moveDirection,
            out bool jumpRequested,
            out bool attackRequested)
        {
            moveDirection = serverMoveDirection;
            jumpRequested = serverJumpRequested;
            attackRequested = serverAttackRequested;
            serverJumpRequested = false;
            serverAttackRequested = false;
        }

        public void PublishVitalState(float currentHealth, bool isDead)
        {
            if (!IsSpawned || !IsServer)
            {
                return;
            }

            health.Value = currentHealth;
            dead.Value = isDead;
        }

        public void PublishDamage()
        {
            if (IsSpawned && IsServer)
            {
                damageSequence.Value++;
            }
        }

        public void RequestInteraction(NetworkObject target, float maximumDistance)
        {
            if (target == null || (!IsOwner && IsSpawned))
            {
                return;
            }

            if (!IsSpawned || IsServer)
            {
                ExecuteInteraction(target, maximumDistance);
            }
            else
            {
                RequestInteractionRpc(target, maximumDistance);
            }
        }

        public void HideSceneObject(NetworkObject target)
        {
            if (!IsServer || target == null || !target.InScenePlaced)
            {
                return;
            }

            SetSceneObjectVisible(target, false);
            SetSceneObjectVisibleRpc(target, false);
        }

        [Rpc(SendTo.Server)]
        private void SubmitInputRpc(
            Vector3 moveDirection,
            bool jumpRequested,
            bool attackRequested)
        {
            SetServerInput(moveDirection, jumpRequested, attackRequested);
        }

        [Rpc(SendTo.Server)]
        private void RequestInteractionRpc(
            NetworkObjectReference targetReference,
            float maximumDistance)
        {
            if (targetReference.TryGet(out NetworkObject target))
            {
                ExecuteInteraction(target, maximumDistance);
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void SetSceneObjectVisibleRpc(
            NetworkObjectReference targetReference,
            bool visible)
        {
            if (targetReference.TryGet(out NetworkObject target))
            {
                SetSceneObjectVisible(target, visible);
            }
        }

        private void ExecuteInteraction(NetworkObject target, float maximumDistance)
        {
            if (target == null ||
                maximumDistance <= 0f ||
                !IsInteractionTargetInRange(target, maximumDistance))
            {
                return;
            }

            MonoBehaviour[] behaviours = target.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is INetworkInteractionTarget interactionTarget)
                {
                    interactionTarget.InteractOnServer(gameObject);
                    return;
                }
            }
        }

        private bool IsInteractionTargetInRange(
            NetworkObject target,
            float maximumDistance)
        {
            float maximumSqrDistance = maximumDistance * maximumDistance;
            Collider[] targetColliders = target.GetComponentsInChildren<Collider>(true);

            foreach (Collider targetCollider in targetColliders)
            {
                if (!targetCollider.enabled)
                {
                    continue;
                }

                Vector3 closestPoint = targetCollider is MeshCollider { convex: false }
                    ? targetCollider.bounds.ClosestPoint(transform.position)
                    : targetCollider.ClosestPoint(transform.position);
                if ((closestPoint - transform.position).sqrMagnitude <= maximumSqrDistance)
                {
                    return true;
                }
            }

            return targetColliders.Length == 0 &&
                   (target.transform.position - transform.position).sqrMagnitude <=
                   maximumSqrDistance;
        }

        private void SetServerInput(
            Vector3 moveDirection,
            bool jumpRequested,
            bool attackRequested)
        {
            serverMoveDirection = Vector3.ClampMagnitude(moveDirection, 1f);
            serverJumpRequested |= jumpRequested;
            serverAttackRequested |= attackRequested;
        }

        private static void SetSceneObjectVisible(NetworkObject target, bool visible)
        {
            foreach (Renderer targetRenderer in target.GetComponentsInChildren<Renderer>(true))
            {
                targetRenderer.enabled = visible;
            }

            foreach (Collider targetCollider in target.GetComponentsInChildren<Collider>(true))
            {
                targetCollider.enabled = visible;
            }
        }

        private void HandleHealthChanged(float _, float value)
        {
            VitalStateChanged?.Invoke(value, dead.Value);
        }

        private void HandleDeadChanged(bool _, bool value)
        {
            VitalStateChanged?.Invoke(health.Value, value);
        }

        private void HandleDamageSequenceChanged(int _, int value)
        {
            if (value > 0)
            {
                DamageReceived?.Invoke();
            }
        }

        private void HandleNicknameChanged(FixedString64Bytes _, FixedString64Bytes value)
        {
            NicknameChanged?.Invoke(value.ToString());
        }

        private static string NormalizeNickname(string value)
        {
            string normalized = string.IsNullOrWhiteSpace(value) ? "Player" : value.Trim();
            return normalized.Length <= 32 ? normalized : normalized.Substring(0, 32);
        }
    }
}
