using System;
using Unity.Netcode;
using UnityEngine;

namespace SimpleSummon.Network
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(NetworkPlayerInput))]
    [RequireComponent(typeof(NetworkPlayerVitals))]
    [RequireComponent(typeof(NetworkPlayerIdentity))]
    [RequireComponent(typeof(NetworkPlayerInteraction))]
    [RequireComponent(typeof(NetworkSceneObjectVisibility))]
    [RequireComponent(typeof(NetworkPlayerTorch))]
    public sealed class NetworkPlayer : NetworkBehaviour
    {
        private NetworkPlayerInput input;
        private NetworkPlayerVitals vitals;
        private NetworkPlayerIdentity identity;
        private NetworkPlayerInteraction interaction;
        private NetworkSceneObjectVisibility visibility;
        private NetworkPlayerTorch torch;
        private bool torchMovementActive;

        public event Action RoleChanged;
        public event Action<float, bool> VitalStateChanged
        {
            add => Vitals.Changed += value;
            remove => Vitals.Changed -= value;
        }
        public event Action DamageReceived
        {
            add => Vitals.DamageReceived += value;
            remove => Vitals.DamageReceived -= value;
        }
        public event Action<string> NicknameChanged
        {
            add => Identity.Changed += value;
            remove => Identity.Changed -= value;
        }

        public static event Action LocalPlayerChanged;
        public static NetworkPlayer LocalPlayer { get; private set; }
        public float CurrentHealth => Vitals.CurrentHealth;
        public bool CanReadLocalInput => IsOffline || IsSpawned && IsOwner;
        public bool CanRunSimulation => IsOffline || IsSpawned && IsServer;
        public string Nickname => Identity.Nickname;
        public bool HasTorch => Torch.IsHeld;
        public float TorchStrength => Torch.Strength;
        public bool TorchMovementActive => torchMovementActive;
        public event Action TorchChanged
        {
            add => Torch.Changed += value;
            remove => Torch.Changed -= value;
        }

        private NetworkPlayerVitals Vitals =>
            vitals != null ? vitals : vitals = GetComponent<NetworkPlayerVitals>();

        private NetworkPlayerIdentity Identity =>
            identity != null ? identity : identity = GetComponent<NetworkPlayerIdentity>();

        private NetworkPlayerTorch Torch =>
            torch != null ? torch : torch = GetComponent<NetworkPlayerTorch>();

        private bool IsOffline =>
            NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.IsListening;

        private void Awake()
        {
            input = GetComponent<NetworkPlayerInput>();
            vitals = GetComponent<NetworkPlayerVitals>();
            identity = GetComponent<NetworkPlayerIdentity>();
            interaction = GetComponent<NetworkPlayerInteraction>();
            visibility = GetComponent<NetworkSceneObjectVisibility>();
            torch = GetComponent<NetworkPlayerTorch>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                LocalPlayer = this;
                LocalPlayerChanged?.Invoke();
            }
            RoleChanged?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            if (LocalPlayer == this)
            {
                LocalPlayer = null;
                LocalPlayerChanged?.Invoke();
            }
            RoleChanged?.Invoke();
        }

        public void SubmitInput(
            Vector3 moveDirection,
            bool jumpRequested,
            bool attackRequested) =>
            input.Submit(moveDirection, jumpRequested, attackRequested);

        public void ReadServerInput(
            out Vector3 moveDirection,
            out bool jumpRequested,
            out bool attackRequested) =>
            input.Read(out moveDirection, out jumpRequested, out attackRequested);

        public void PublishVitalState(float currentHealth, bool isDead) =>
            Vitals.Publish(currentHealth, isDead);

        public void PublishDamage() => Vitals.PublishDamage();

        public void RequestInteraction(NetworkObject target, float maximumDistance) =>
            interaction.Request(target, maximumDistance);

        public void HideSceneObject(NetworkObject target) => visibility.Hide(target);

        public void SetTorchMovementActive(bool value)
        {
            if (CanRunSimulation)
            {
                torchMovementActive = value;
            }
        }

        public void PublishTorch(bool held, float strength) =>
            Torch.Publish(held, strength);

        public void DropTorch() => NetworkTorchState.Active?.Drop(this);
    }
}
