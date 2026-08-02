using System;
using SimpleSummon.Application;
using SimpleSummon.Domain;
using Unity.Netcode;
using UnityEngine;

namespace SimpleSummon.Network
{
    public sealed class NetworkTorchState : NetworkBehaviour
    {
        private const ulong NoHolder = ulong.MaxValue;

        [SerializeField, Min(0f)] private float fadeDelay = 5f;
        [SerializeField, Min(0f)] private float recoveryDelay = 1f;
        [SerializeField, Min(0f)] private float fadeRate = 8f;
        [SerializeField, Min(0f)] private float recoveryRate = 10f;

        private readonly NetworkVariable<ulong> holderClientId = new(NoHolder);
        private readonly NetworkVariable<float> strength = new(100f);
        private TorchService service;
        private NetworkPlayer offlineHolder;

        public static NetworkTorchState Active { get; private set; }

        public event Action Changed;
        public bool IsAvailable => IsSpawned && !IsServer
            ? holderClientId.Value == NoHolder
            : service?.IsAvailable ?? true;
        public float Strength => IsSpawned ? strength.Value : service?.Strength ?? 100f;

        private void Awake()
        {
            Active = this;
            service = new TorchService(new TorchModel(
                fadeDelay,
                recoveryDelay,
                fadeRate,
                recoveryRate));
        }

        public override void OnNetworkSpawn()
        {
            holderClientId.OnValueChanged += HandleHolderChanged;
            strength.OnValueChanged += HandleStrengthChanged;
            if (IsServer && NetworkManager != null)
            {
                NetworkManager.OnClientDisconnectCallback += HandleClientDisconnected;
            }
            Changed?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            holderClientId.OnValueChanged -= HandleHolderChanged;
            strength.OnValueChanged -= HandleStrengthChanged;
            if (NetworkManager != null)
            {
                NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (Active == this)
            {
                Active = null;
            }
        }

        private void Update()
        {
            if (IsAvailable || IsSpawned && !IsServer)
            {
                return;
            }

            if (!TryGetHolder(out NetworkPlayer player))
            {
                Release();
                return;
            }

            service.Update(player.TorchMovementActive, Time.deltaTime);
            PublishStrength(player);
            if (service.IsExtinguished)
            {
                Release();
            }
        }

        public bool TryTake(GameObject interactor)
        {
            if (!IsAvailable || IsSpawned && !IsServer || interactor == null)
            {
                return false;
            }

            NetworkPlayer player = interactor.GetComponent<NetworkPlayer>();
            if (player == null)
            {
                return false;
            }

            if (!service.TryTake(player.OwnerClientId))
            {
                return false;
            }
            offlineHolder = player;
            if (IsSpawned)
            {
                holderClientId.Value = player.OwnerClientId;
                strength.Value = service.Strength;
            }
            player.PublishTorch(true, service.Strength);
            Changed?.Invoke();
            return true;
        }

        public bool IsHeldBy(NetworkPlayer player) =>
            player != null && service.IsHeldBy(player.OwnerClientId) &&
            (IsSpawned || offlineHolder == player);

        public void Drop(NetworkPlayer player)
        {
            if ((!IsSpawned || IsServer) && IsHeldBy(player))
            {
                Release();
            }
        }

        private void Release()
        {
            if (TryGetHolder(out NetworkPlayer player))
            {
                player.PublishTorch(false, 100f);
            }

            service.Release();
            offlineHolder = null;
            if (IsSpawned)
            {
                strength.Value = 100f;
                holderClientId.Value = NoHolder;
            }
            Changed?.Invoke();
        }

        private void PublishStrength(NetworkPlayer player)
        {
            if (IsSpawned)
            {
                strength.Value = service.Strength;
            }
            player.PublishTorch(true, service.Strength);
        }

        private bool TryGetHolder(out NetworkPlayer player)
        {
            player = null;
            if (IsAvailable)
            {
                return false;
            }

            if (!IsSpawned)
            {
                player = offlineHolder;
                return player != null;
            }

            if (NetworkManager.ConnectedClients.TryGetValue(
                    holderClientId.Value,
                    out NetworkClient client) &&
                client.PlayerObject != null)
            {
                player = client.PlayerObject.GetComponent<NetworkPlayer>();
            }

            return player != null;
        }

        private void HandleHolderChanged(ulong _, ulong __) => Changed?.Invoke();
        private void HandleStrengthChanged(float _, float __) => Changed?.Invoke();

        private void HandleClientDisconnected(ulong clientId)
        {
            if (holderClientId.Value == clientId)
            {
                Release();
            }
        }
    }
}
