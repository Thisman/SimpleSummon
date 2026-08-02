using Unity.Netcode;
using UnityEngine;

namespace SimpleSummon.Network
{
    public sealed class NetworkPlayerInteraction : NetworkBehaviour
    {
        public void Request(NetworkObject target, float maximumDistance)
        {
            if (target == null || IsSpawned && !IsOwner)
            {
                return;
            }

            if (!IsSpawned || IsServer)
            {
                Execute(target, maximumDistance);
            }
            else
            {
                RequestRpc(target, maximumDistance);
            }
        }

        [Rpc(SendTo.Server)]
        private void RequestRpc(
            NetworkObjectReference targetReference,
            float maximumDistance)
        {
            if (targetReference.TryGet(out NetworkObject target))
            {
                Execute(target, maximumDistance);
            }
        }

        private void Execute(NetworkObject target, float maximumDistance)
        {
            if (target == null || maximumDistance <= 0f ||
                !IsInRange(target, maximumDistance))
            {
                return;
            }

            foreach (MonoBehaviour behaviour in
                     target.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour is INetworkInteractionTarget interactionTarget)
                {
                    interactionTarget.InteractOnServer(gameObject);
                    return;
                }
            }
        }

        private bool IsInRange(NetworkObject target, float maximumDistance)
        {
            float maximumSqrDistance = maximumDistance * maximumDistance;
            Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
            foreach (Collider targetCollider in colliders)
            {
                if (!targetCollider.enabled)
                {
                    continue;
                }

                Vector3 closestPoint = targetCollider is MeshCollider { convex: false }
                    ? targetCollider.bounds.ClosestPoint(transform.position)
                    : targetCollider.ClosestPoint(transform.position);
                if ((closestPoint - transform.position).sqrMagnitude <= maximumSqrDistance)
                {
                    return true;
                }
            }

            return colliders.Length == 0 &&
                   (target.transform.position - transform.position).sqrMagnitude <=
                   maximumSqrDistance;
        }
    }
}
