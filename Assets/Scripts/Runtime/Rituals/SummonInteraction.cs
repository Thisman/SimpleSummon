using SimpleSummon.Network;
using SimpleSummon.Domain;
using Unity.Netcode;
using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class SummonInteraction : MonoBehaviour, IInteractable
    {
        [SerializeField] private NetworkSummonRitual ritual;

        private void OnEnable()
        {
            ritual.StateChanged += HandleStateChanged;
            HandleStateChanged();
        }

        private void OnDisable()
        {
            ritual.StateChanged -= HandleStateChanged;
        }

        public void Interact(GameObject interactor)
        {
            if (!interactor.TryGetComponent(out NetworkPlayer player) ||
                !player.CanReadLocalInput)
            {
                return;
            }

            ritual.RequestClaim();
        }

        private void HandleStateChanged()
        {
            gameObject.tag = ritual.State == SummonRitualState.Available
                ? "Interactive"
                : "Untagged";

            NetworkManager manager = NetworkManager.Singleton;
            ulong localClientId = manager != null && manager.IsListening
                ? manager.LocalClientId
                : 0;

            if (ritual.State == SummonRitualState.Claimed &&
                ritual.DrawingClientId == localClientId &&
                PlayerRegistry.GetLocalPlayer() is PlayerController player &&
                player.TryGetComponent(out SummonDrawingMode drawingMode))
            {
                drawingMode.Enter(ritual);
            }
        }
    }
}
