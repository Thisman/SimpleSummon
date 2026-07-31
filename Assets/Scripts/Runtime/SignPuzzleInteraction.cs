using SimpleSummon.Network;
using Unity.Netcode;
using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class SignPuzzleInteraction : MonoBehaviour, IInteractable
    {
        [SerializeField] private NetworkSignPuzzle puzzle;

        private void OnEnable()
        {
            puzzle.StateChanged += Refresh;
            puzzle.BoardChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            puzzle.StateChanged -= Refresh;
            puzzle.BoardChanged -= Refresh;
        }

        public void Interact(GameObject interactor)
        {
            if (!interactor.TryGetComponent(out NetworkPlayer player)) return;
            puzzle.TryClaim(player.IsSpawned ? player.OwnerClientId : 0);
        }

        private void Refresh()
        {
            gameObject.tag = puzzle.ControllingClientId == NetworkSignPuzzle.NoOwner
                ? "Interactive" : "Untagged";
            NetworkManager manager = NetworkManager.Singleton;
            ulong localId = manager != null && manager.IsListening ? manager.LocalClientId : 0;
            if (puzzle.ControllingClientId == localId &&
                PlayerRegistry.GetLocalPlayer() is PlayerController player &&
                player.TryGetComponent(out SignPuzzleMode mode)) mode.Enter(puzzle);
        }
    }
}
