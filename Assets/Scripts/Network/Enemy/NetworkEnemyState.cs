using System;
using Unity.Netcode;

namespace SimpleSummon.Network
{
    public sealed class NetworkEnemyState : NetworkBehaviour
    {
        private readonly NetworkVariable<bool> dead = new();
        private readonly NetworkVariable<bool> disappeared = new();
        private bool offlineDead;
        private bool offlineDisappeared;

        public event Action<bool> StateChanged;
        public event Action DisappearedChanged;

        private bool IsOffline => NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening;
        public bool IsDead => IsOffline ? offlineDead : dead.Value;
        public bool Disappeared => IsOffline ? offlineDisappeared : disappeared.Value;

        public override void OnNetworkSpawn()
        {
            dead.OnValueChanged += HandleDeadChanged;
            disappeared.OnValueChanged += HandleDisappearedChanged;
            StateChanged?.Invoke(dead.Value);
            DisappearedChanged?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            dead.OnValueChanged -= HandleDeadChanged;
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

        private void HandleDeadChanged(bool _, bool value)
        {
            StateChanged?.Invoke(value);
        }

        private void HandleDisappearedChanged(bool _, bool __) => DisappearedChanged?.Invoke();
    }
}
