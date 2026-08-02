using System;
using Unity.Netcode;

namespace SimpleSummon.Network
{
    public sealed class NetworkEnemyState : NetworkBehaviour
    {
        private readonly NetworkVariable<bool> dead = new();
        private readonly NetworkVariable<bool> disappeared = new();
        private readonly NetworkVariable<bool> lootCollected = new();
        private bool offlineDead;
        private bool offlineDisappeared;
        private bool offlineLootCollected;

        public event Action<bool> StateChanged;
        public event Action DisappearedChanged;
        public event Action LootChanged;

        private bool IsOffline => NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening;
        public bool IsDead => IsOffline ? offlineDead : dead.Value;
        public bool Disappeared => IsOffline ? offlineDisappeared : disappeared.Value;
        public bool LootCollected => IsOffline ? offlineLootCollected : lootCollected.Value;

        public override void OnNetworkSpawn()
        {
            dead.OnValueChanged += HandleDeadChanged;
            disappeared.OnValueChanged += HandleDisappearedChanged;
            lootCollected.OnValueChanged += HandleLootChanged;
            StateChanged?.Invoke(dead.Value);
            DisappearedChanged?.Invoke();
            LootChanged?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            dead.OnValueChanged -= HandleDeadChanged;
            disappeared.OnValueChanged -= HandleDisappearedChanged;
            lootCollected.OnValueChanged -= HandleLootChanged;
        }

        public void Publish(bool isDead)
        {
            if (IsOffline)
            {
                offlineDead = isDead;
                StateChanged?.Invoke(isDead);
                return;
            }
            if (!IsSpawned || !IsServer)
            {
                return;
            }

            dead.Value = isDead;
        }

        public void PublishDeathCompleted()
        {
            if (IsOffline)
            {
                offlineDisappeared = true;
                DisappearedChanged?.Invoke();
                return;
            }
            if (!IsSpawned || !IsServer)
            {
                return;
            }

            disappeared.Value = true;
        }

        public void PublishLootCollected()
        {
            if (IsOffline)
            {
                offlineLootCollected = true;
                LootChanged?.Invoke();
                return;
            }
            if (!IsSpawned || !IsServer)
            {
                return;
            }

            lootCollected.Value = true;
        }

        private void HandleDeadChanged(bool _, bool value)
        {
            StateChanged?.Invoke(value);
        }

        private void HandleDisappearedChanged(bool _, bool __) => DisappearedChanged?.Invoke();
        private void HandleLootChanged(bool _, bool __) => LootChanged?.Invoke();
    }
}
