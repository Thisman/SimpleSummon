using SimpleSummon.Network;
using Unity.Netcode;
using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class InteractiveActor : MonoBehaviour, INetworkInteractionTarget
    {
        [SerializeField] private string interactionText;
        [SerializeField] private MonoBehaviour interactionTarget;
        [SerializeField] private NetworkObject networkTargetOverride;

        private IInteractable interactable;

        public string InteractionText => interactionText;
        public bool IsLocalPresentation =>
            interactable is SignDrawingInteraction;
        public NetworkObject NetworkTarget => networkTargetOverride != null
            ? networkTargetOverride
            : GetComponentInParent<NetworkObject>();

        private void Awake()
        {
            interactable = interactionTarget as IInteractable;

            if (interactable == null)
            {
                Debug.LogError(
                    $"{name}: interaction target must implement {nameof(IInteractable)}.",
                    this);
            }
        }

        public void Interact(GameObject interactor)
        {
            interactable?.Interact(interactor);
        }

        public void InteractOnServer(GameObject interactor)
        {
            Interact(interactor);
        }
    }
}
