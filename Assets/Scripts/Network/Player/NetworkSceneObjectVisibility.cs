using Unity.Netcode;
using UnityEngine;

namespace SimpleSummon.Network
{
    public sealed class NetworkSceneObjectVisibility : NetworkBehaviour
    {
        public void Hide(NetworkObject target)
        {
            if (!IsServer || target == null || !target.InScenePlaced)
            {
                return;
            }

            SetVisible(target, false);
            SetVisibleRpc(target, false);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void SetVisibleRpc(
            NetworkObjectReference targetReference,
            bool visible)
        {
            if (targetReference.TryGet(out NetworkObject target))
            {
                SetVisible(target, visible);
            }
        }

        private static void SetVisible(NetworkObject target, bool visible)
        {
            foreach (Renderer targetRenderer in
                     target.GetComponentsInChildren<Renderer>(true))
            {
                targetRenderer.enabled = visible;
            }

            foreach (Collider targetCollider in
                     target.GetComponentsInChildren<Collider>(true))
            {
                targetCollider.enabled = visible;
            }
        }
    }
}
