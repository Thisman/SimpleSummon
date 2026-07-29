using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class InteractiveActor : MonoBehaviour
    {
        [SerializeField] private string interactionText;
        [SerializeField] private MonoBehaviour interactionTarget;

        private IInteractable interactable;

        public string InteractionText => interactionText;

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
    }
}
