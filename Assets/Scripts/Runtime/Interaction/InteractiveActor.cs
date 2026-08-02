using SimpleSummon.Network;
using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class InteractiveActor : MonoBehaviour, INetworkInteractionTarget
    {
        [SerializeField] private string interactionText;
        [SerializeField] private MonoBehaviour interactionTarget;

        private IInteractable interactable;

        public string InteractionText => interactionText;
        public bool IsLocalPresentation =>
            interactable is InstructionInteraction or SignDrawingInteraction or CraftingInteraction;

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
