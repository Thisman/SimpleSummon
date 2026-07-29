using Unity.Netcode;
using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class BoneCollectionInteraction : MonoBehaviour, IInteractable
    {
        [SerializeField] private string itemName = "Bones";
        [SerializeField, Min(1)] private int quantity = 1;

        public void Interact(GameObject interactor)
        {
            if (!interactor.TryGetComponent(out PlayerInventory inventory))
            {
                return;
            }

            inventory.Add(itemName, quantity);
            NetworkObject networkObject = GetComponentInParent<NetworkObject>();
            if (networkObject != null && networkObject.IsSpawned)
            {
                networkObject.Despawn(true);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
