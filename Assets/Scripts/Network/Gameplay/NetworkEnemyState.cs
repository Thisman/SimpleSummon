using System;
using Unity.Netcode;

namespace SimpleSummon.Network
{
    public sealed class NetworkEnemyState : NetworkBehaviour
    {
        private readonly NetworkVariable<bool> dead = new();
        private readonly NetworkVariable<bool> lootAvailable = new();
        private readonly NetworkVariable<bool> disappeared = new();
        private bool offlineDead;
        private bool offlineLootAvailable;
        private bool offlineDisappeared;

        public event Action<bool> StateChanged;
        public event Action LootStateChanged;

        private bool IsOffline => NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening;
        public bool IsDead => IsOffline ? offlineDead : dead.Value;
        public bool LootAvailable => IsOffline ? offlineLootAvailable : lootAvailable.Value;
        public bool Disappeared => IsOffline ? offlineDisappeared : disappeared.Value;

        public override void OnNetworkSpawn()
        {
            dead.OnValueChanged += HandleDeadChanged;
            lootAvailable.OnValueChanged += HandleLootChanged;
            disappeared.OnValueChanged += HandleDisappearedChanged;
            StateChanged?.Invoke(dead.Value);
            LootStateChanged?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            dead.OnValueChanged -= HandleDeadChanged;
            lootAvailable.OnValueChanged -= HandleLootChanged;
            disappeared.OnValueChanged -= HandleDisappearedChanged;
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

        public void PublishDeathCompleted(bool hasLoot)
        {
            if (IsOffline)
            {
                offlineDisappeared = true;
                offlineLootAvailable = hasLoot;
                LootStateChanged?.Invoke();
                return;
            }
            if (!IsSpawned || !IsServer)
            {
                return;
            }

            disappeared.Value = true;
            lootAvailable.Value = hasLoot;
        }

        public bool TryCollectLoot()
        {
            if (IsOffline)
            {
                if (!offlineLootAvailable)
                {
                    return false;
                }
                offlineLootAvailable = false;
                LootStateChanged?.Invoke();
                return true;
            }
            if (!IsSpawned || !IsServer || !lootAvailable.Value)
            {
                return false;
            }

            lootAvailable.Value = false;
            return true;
        }

        private void HandleDeadChanged(bool _, bool value)
        {
            StateChanged?.Invoke(value);
        }

        private void HandleLootChanged(bool _, bool __) => LootStateChanged?.Invoke();
        private void HandleDisappearedChanged(bool _, bool __) => LootStateChanged?.Invoke();
    }
}
