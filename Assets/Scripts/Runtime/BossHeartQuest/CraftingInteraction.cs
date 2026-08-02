using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class CraftingInteraction : MonoBehaviour, IInteractable
    {
        [SerializeField] private CraftingController craftingController;

        public void Interact(GameObject interactor)
        {
            craftingController.Open(interactor);
        }
    }
}
