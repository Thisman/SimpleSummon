using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class CraftingInteraction : MonoBehaviour, IInteractable
    {
        [SerializeField] private CraftingView craftingView;

        public void Interact(GameObject interactor)
        {
            craftingView.Open(interactor);
        }
    }
}
