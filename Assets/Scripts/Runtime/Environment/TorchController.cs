using SimpleSummon.Network;
using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class TorchController : MonoBehaviour
    {
        [SerializeField] private NetworkTorchState state;
        [SerializeField] private Collider interactionCollider;

        private Renderer[] torchRenderers;
        private Light[] torchLights;

        private void Awake()
        {
            torchRenderers = GetComponentsInChildren<Renderer>(true);
            torchLights = GetComponentsInChildren<Light>(true);
        }

        private void OnEnable()
        {
            state.Changed += Refresh;
            Refresh();
        }

        private void OnDisable() => state.Changed -= Refresh;

        private void Refresh()
        {
            bool available = state.IsAvailable;
            foreach (Renderer torchRenderer in torchRenderers)
            {
                torchRenderer.enabled = available;
            }
            foreach (Light torchLight in torchLights)
            {
                torchLight.enabled = available;
            }
            interactionCollider.enabled = available;
        }
    }
}
