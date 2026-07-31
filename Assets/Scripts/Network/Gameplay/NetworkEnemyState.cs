using System;
using Unity.Netcode;

namespace SimpleSummon.Network
{
    public sealed class NetworkEnemyState : NetworkBehaviour
    {
        private readonly NetworkVariable<bool> dead = new();

        public event Action<bool> StateChanged;

        public override void OnNetworkSpawn()
        {
            dead.OnValueChanged += HandleDeadChanged;
            StateChanged?.Invoke(dead.Value);
        }

        public override void OnNetworkDespawn()
        {
            dead.OnValueChanged -= HandleDeadChanged;
        }

        public void Publish(bool isDead)
        {
            if (!IsSpawned || !IsServer)
            {
                return;
            }

            dead.Value = isDead;
        }

        private void HandleDeadChanged(bool _, bool value)
        {
            StateChanged?.Invoke(value);
        }
    }
}
