using UnityEngine;

namespace SimpleSummon.Runtime
{
    internal sealed class InteractionTargetScanner
    {
        private const int RaycastBufferSize = 16;
        private readonly Camera camera;
        private readonly Transform player;
        private readonly float interactionDistance;
        private readonly LayerMask interactionLayers;
        private readonly RaycastHit[] hits = new RaycastHit[RaycastBufferSize];

        public InteractionTargetScanner(
            Camera camera,
            Transform player,
            float interactionDistance,
            LayerMask interactionLayers)
        {
            this.camera = camera;
            this.player = player;
            this.interactionDistance = interactionDistance;
            this.interactionLayers = interactionLayers;
        }

        public InteractiveActor Find()
        {
            Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            float cameraOffset = Vector3.Distance(camera.transform.position, player.position);
            int hitCount = Physics.RaycastNonAlloc(
                ray,
                hits,
                cameraOffset + interactionDistance,
                interactionLayers,
                QueryTriggerInteraction.Ignore);

            InteractiveActor nearest = null;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = hits[i];
                InteractiveActor actor = hit.collider.GetComponentInParent<InteractiveActor>();
                if (actor == null || !actor.CompareTag("Interactive") ||
                    Vector3.Distance(player.position, hit.point) > interactionDistance ||
                    hit.distance >= nearestDistance)
                {
                    continue;
                }

                nearest = actor;
                nearestDistance = hit.distance;
            }

            return nearest;
        }
    }
}
