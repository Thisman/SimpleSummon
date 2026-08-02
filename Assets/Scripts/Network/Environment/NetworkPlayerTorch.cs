using System;
using Unity.Netcode;
using UnityEngine;

namespace SimpleSummon.Network
{
    public sealed class NetworkPlayerTorch : NetworkBehaviour
    {
        [SerializeField] private GameObject torchVisual;

        private readonly NetworkVariable<bool> held = new();
        private readonly NetworkVariable<float> strength = new(100f);
        private bool offlineHeld;
        private float offlineStrength = 100f;

        public event Action Changed;
        public bool IsHeld => IsSpawned ? held.Value : offlineHeld;
        public float Strength => IsSpawned ? strength.Value : offlineStrength;

        private void Awake()
        {
            SetVisual(false);
        }

        public override void OnNetworkSpawn()
        {
            held.OnValueChanged += HandleHeldChanged;
            strength.OnValueChanged += HandleStrengthChanged;
            SetVisual(held.Value);
            Changed?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            held.OnValueChanged -= HandleHeldChanged;
            strength.OnValueChanged -= HandleStrengthChanged;
            SetVisual(false);
        }

        internal void Publish(bool isHeld, float currentStrength)
        {
            if (IsSpawned && !IsServer)
            {
                return;
            }

            if (IsSpawned)
            {
                held.Value = isHeld;
                strength.Value = currentStrength;
            }
            else
            {
                offlineHeld = isHeld;
                offlineStrength = currentStrength;
                SetVisual(isHeld);
                Changed?.Invoke();
            }
        }

        private void HandleHeldChanged(bool _, bool value)
        {
            SetVisual(value);
            Changed?.Invoke();
        }

        private void HandleStrengthChanged(float _, float __) => Changed?.Invoke();

        private void SetVisual(bool visible)
        {
            if (torchVisual != null)
            {
                torchVisual.SetActive(visible);
            }
        }
    }
}
