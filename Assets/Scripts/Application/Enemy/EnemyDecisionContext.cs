using SimpleSummon.Domain;

namespace SimpleSummon.Application
{
    public readonly struct EnemyDecisionContext
    {
        public EnemyDecisionContext(
            EnemyBehaviorState state,
            bool hasLivingTarget,
            float distanceToTarget,
            float distanceFromHome,
            float detectionRadius,
            float attackRadius,
            float returnRadius,
            float stoppingDistance)
        {
            State = state;
            HasLivingTarget = hasLivingTarget;
            DistanceToTarget = distanceToTarget;
            DistanceFromHome = distanceFromHome;
            DetectionRadius = detectionRadius;
            AttackRadius = attackRadius;
            ReturnRadius = returnRadius;
            StoppingDistance = stoppingDistance;
        }

        public EnemyBehaviorState State { get; }
        public bool HasLivingTarget { get; }
        public float DistanceToTarget { get; }
        public float DistanceFromHome { get; }
        public float DetectionRadius { get; }
        public float AttackRadius { get; }
        public float ReturnRadius { get; }
        public float StoppingDistance { get; }
    }
}
