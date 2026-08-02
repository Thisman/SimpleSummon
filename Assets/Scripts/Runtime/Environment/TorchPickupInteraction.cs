using SimpleSummon.Network;
using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class TorchPickupInteraction : MonoBehaviour, IInteractable
    {
        [SerializeField] private NetworkTorchState state;

        public void Interact(GameObject interactor) => state.TryTake(interactor);
    }
}
