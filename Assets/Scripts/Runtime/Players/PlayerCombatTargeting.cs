using UnityEngine;

namespace SimpleSummon.Runtime
{
    internal sealed class PlayerCombatTargeting
    {
        private const float MinimumAttackDirectionDot = 0.70710678f;
        private const int AttackColliderBufferSize = 32;
        private const int AimHitBufferSize = 32;

        private readonly Transform playerTransform;
        private readonly Collider[] attackColliders = new Collider[AttackColliderBufferSize];
        private readonly RaycastHit[] aimHits = new RaycastHit[AimHitBufferSize];

        public PlayerCombatTargeting(Transform playerTransform)
        {
            this.playerTransform = playerTransform;
        }

        public bool TryGetClosestAttackTarget(
            float attackRange,
            LayerMask attackMask,
            out IDamageable target)
        {
            int colliderCount = Physics.OverlapSphereNonAlloc(
                playerTransform.position,
                attackRange,
                attackColliders,
                attackMask,
                QueryTriggerInteraction.Ignore);

            target = null;
            float closestSqrDistance = float.PositiveInfinity;
            float attackRangeSqr = attackRange * attackRange;

            for (int i = 0; i < colliderCount; i++)
            {
                Collider collider = attackColliders[i];
                if (collider.transform.IsChildOf(playerTransform))
                {
                    continue;
                }

                IDamageable candidate = collider.GetComponentInParent<IDamageable>();
                if (!IsValidEnemy(candidate))
                {
                    continue;
                }

                Component candidateComponent = (Component)candidate;
                Vector3 direction = candidateComponent.transform.position - playerTransform.position;
                direction.y = 0f;

                float sqrDistance = direction.sqrMagnitude;
                if (sqrDistance <= 0f || sqrDistance > attackRangeSqr ||
                    Vector3.Dot(playerTransform.forward, direction.normalized) < MinimumAttackDirectionDot ||
                    sqrDistance >= closestSqrDistance)
                {
                    continue;
                }

                target = candidate;
                closestSqrDistance = sqrDistance;
            }

            return target != null;
        }

        public bool TryGetAimedTarget(
            Ray ray,
            float maximumDistance,
            LayerMask attackMask,
            out IDamageable target)
        {
            int hitCount = Physics.RaycastNonAlloc(
                ray,
                aimHits,
                maximumDistance,
                attackMask,
                QueryTriggerInteraction.Ignore);

            target = null;
            float closestDistance = float.PositiveInfinity;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = aimHits[i];
                if (hit.transform.IsChildOf(playerTransform))
                {
                    continue;
                }

                IDamageable candidate = hit.collider.GetComponentInParent<IDamageable>();
                if (!IsValidEnemy(candidate) || hit.distance >= closestDistance)
                {
                    continue;
                }

                target = candidate;
                closestDistance = hit.distance;
            }

            return target != null;
        }

        private static bool IsValidEnemy(IDamageable candidate) =>
            candidate != null && candidate is not PlayerController && !candidate.IsDead;
    }
}
