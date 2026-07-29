using System;
using Unity.Netcode;

namespace SimpleSummon.Network
{
    public sealed class NetworkEnemyState : NetworkBehaviour
    {
        private readonly NetworkVariable<bool> dead = new();
        private readonly NetworkVariable<bool> lootVisible = new();

        public event Action<bool, bool> StateChanged;

        public override void OnNetworkSpawn()
        {
            dead.OnValueChanged += HandleDeadChanged;
            lootVisible.OnValueChanged += HandleLootChanged;
            StateChanged?.Invoke(dead.Value, lootVisible.Value);
        }

        public override void OnNetworkDespawn()
        {
            dead.OnValueChanged -= HandleDeadChanged;
            lootVisible.OnValueChanged -= HandleLootChanged;
        }

        public void Publish(bool isDead, bool isLootVisible)
        {
            if (!IsSpawned || !IsServer)
            {
                return;
            }

            dead.Value = isDead;
            lootVisible.Value = isLootVisible;
        }

        private void HandleDeadChanged(bool _, bool value)
        {
            StateChanged?.Invoke(value, lootVisible.Value);
        }

        private void HandleLootChanged(bool _, bool value)
        {
            StateChanged?.Invoke(dead.Value, value);
        }
    }
}
