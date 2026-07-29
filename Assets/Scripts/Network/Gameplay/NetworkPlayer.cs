using System;
using Unity.Netcode;
using UnityEngine;

namespace SimpleSummon.Network
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class NetworkPlayer : NetworkBehaviour
    {
        private readonly NetworkVariable<float> health = new();
        private readonly NetworkVariable<bool> dead = new();
        private readonly NetworkVariable<int> damageSequence = new();
        private readonly NetworkList<NetworkInventoryEntry> inventory = new();

        private Vector3 serverMoveDirection;
        private bool serverJumpRequested;
        private bool serverAttackRequested;

        public event Action RoleChanged;
        public event Action<float, bool> VitalStateChanged;
        public event Action InventoryChanged;
        public event Action DamageReceived;

        private bool IsOffline =>
            NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.IsListening;

        public bool CanReadLocalInput => IsOffline || IsSpawned && IsOwner;
        public bool CanRunSimulation => IsOffline || IsSpawned && IsServer;

        public override void OnNetworkSpawn()
        {
            health.OnValueChanged += HandleHealthChanged;
            dead.OnValueChanged += HandleDeadChanged;
            damageSequence.OnValueChanged += HandleDamageSequenceChanged;
            inventory.OnListChanged += HandleInventoryChanged;
            RoleChanged?.Invoke();
            VitalStateChanged?.Invoke(health.Value, dead.Value);
            InventoryChanged?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            health.OnValueChanged -= HandleHealthChanged;
            dead.OnValueChanged -= HandleDeadChanged;
            damageSequence.OnValueChanged -= HandleDamageSequenceChanged;
            inventory.OnListChanged -= HandleInventoryChanged;
            RoleChanged?.Invoke();
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
                SubmitInputRpc(moveDirection, jumpRequested, attackRequested);
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

        public void SetInventoryQuantity(string itemName, int quantity)
        {
            if (IsSpawned && !IsServer)
            {
                return;
            }

            for (int i = 0; i < inventory.Count; i++)
            {
                if (inventory[i].ItemName.ToString() != itemName)
                {
                    continue;
                }

                inventory[i] = new NetworkInventoryEntry(itemName, quantity);
                return;
            }

            inventory.Add(new NetworkInventoryEntry(itemName, quantity));
        }

        public int GetInventoryQuantity(string itemName)
        {
            foreach (NetworkInventoryEntry entry in inventory)
            {
                if (entry.ItemName.ToString() == itemName)
                {
                    return entry.Quantity;
                }
            }

            return 0;
        }

        public void CopyInventoryTo(Action<string, int> receiveEntry)
        {
            foreach (NetworkInventoryEntry entry in inventory)
            {
                receiveEntry(entry.ItemName.ToString(), entry.Quantity);
            }
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

        private void ExecuteInteraction(NetworkObject target, float maximumDistance)
        {
            if (target == null ||
                maximumDistance <= 0f ||
                (target.transform.position - transform.position).sqrMagnitude >
                maximumDistance * maximumDistance)
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

        private void SetServerInput(
            Vector3 moveDirection,
            bool jumpRequested,
            bool attackRequested)
        {
            serverMoveDirection = Vector3.ClampMagnitude(moveDirection, 1f);
            serverJumpRequested |= jumpRequested;
            serverAttackRequested |= attackRequested;
        }

        private void HandleHealthChanged(float _, float value)
        {
            VitalStateChanged?.Invoke(value, dead.Value);
        }

        private void HandleDeadChanged(bool _, bool value)
        {
            VitalStateChanged?.Invoke(health.Value, value);
        }

        private void HandleInventoryChanged(
            NetworkListEvent<NetworkInventoryEntry> _)
        {
            InventoryChanged?.Invoke();
        }

        private void HandleDamageSequenceChanged(int _, int value)
        {
            if (value > 0)
            {
                DamageReceived?.Invoke();
            }
        }
    }
}
