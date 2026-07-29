using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class TeleportInteraction : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform destination;

        public void Interact(GameObject interactor)
        {
            if (interactor.TryGetComponent(out PlayerController playerController))
            {
                playerController.Teleport(destination);
            }
        }
    }
}
