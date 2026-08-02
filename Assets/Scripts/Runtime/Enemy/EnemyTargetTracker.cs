using UnityEngine;

namespace SimpleSummon.Runtime
{
    internal sealed class EnemyTargetTracker
    {
        private readonly Transform owner;
        private readonly System.Action targetRespawned;
        private PlayerController current;

        public EnemyTargetTracker(Transform owner, System.Action targetRespawned)
        {
            this.owner = owner;
            this.targetRespawned = targetRespawned;
        }

        public PlayerController Current => current;

        public void Refresh()
        {
            PlayerController closest = PlayerRegistry.GetClosestLiving(owner.position);
            if (closest != current)
            {
                Set(closest);
            }
        }

        public void Clear()
        {
            Set(null);
        }

        private void Set(PlayerController value)
        {
            if (current != null)
            {
                current.Respawned -= targetRespawned;
            }

            current = value;
            if (current != null)
            {
                current.Respawned += targetRespawned;
            }
        }
    }
}
