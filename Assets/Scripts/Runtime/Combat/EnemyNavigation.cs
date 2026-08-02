using UnityEngine;
using UnityEngine.AI;

namespace SimpleSummon.Runtime
{
    internal sealed class EnemyNavigation
    {
        private readonly Transform transform;
        private readonly NavMeshAgent agent;

        public EnemyNavigation(Transform transform, NavMeshAgent agent)
        {
            this.transform = transform;
            this.agent = agent;
        }

        public bool IsReady => agent.isOnNavMesh;
        public float Speed => agent.velocity.magnitude;
        public float StoppingDistance => agent.stoppingDistance;

        public void MoveTo(Vector3 destination)
        {
            agent.isStopped = false;
            agent.SetDestination(destination);
        }

        public void Stop()
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        public void Disable()
        {
            if (!agent.enabled)
            {
                return;
            }

            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }
            agent.enabled = false;
        }

        public void Face(Vector3 targetPosition)
        {
            Vector3 direction = targetPosition - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }
}
