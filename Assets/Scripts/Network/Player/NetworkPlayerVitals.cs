using System;
using Unity.Netcode;

namespace SimpleSummon.Network
{
    public sealed class NetworkPlayerVitals : NetworkBehaviour
    {
        private readonly NetworkVariable<float> health = new();
        private readonly NetworkVariable<bool> dead = new();
        private readonly NetworkVariable<int> damageSequence = new();

        public event Action<float, bool> Changed;
        public event Action DamageReceived;
        public float CurrentHealth => health.Value;

        public override void OnNetworkSpawn()
        {
            health.OnValueChanged += HandleHealthChanged;
            dead.OnValueChanged += HandleDeadChanged;
            damageSequence.OnValueChanged += HandleDamageChanged;
            Changed?.Invoke(health.Value, dead.Value);
        }

        public override void OnNetworkDespawn()
        {
            health.OnValueChanged -= HandleHealthChanged;
            dead.OnValueChanged -= HandleDeadChanged;
            damageSequence.OnValueChanged -= HandleDamageChanged;
        }

        public void Publish(float currentHealth, bool isDead)
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

        private void HandleHealthChanged(float _, float value) =>
            Changed?.Invoke(value, dead.Value);

        private void HandleDeadChanged(bool _, bool value) =>
            Changed?.Invoke(health.Value, value);

        private void HandleDamageChanged(int _, int value)
        {
            if (value > 0)
            {
                DamageReceived?.Invoke();
            }
        }
    }
}
