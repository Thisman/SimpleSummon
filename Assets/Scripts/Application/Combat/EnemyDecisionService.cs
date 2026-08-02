using SimpleSummon.Domain;

namespace SimpleSummon.Application
{
    public static class EnemyDecisionService
    {
        public static EnemyBehaviorState Decide(EnemyDecisionContext context)
        {
            if (context.State == EnemyBehaviorState.Dead)
            {
                return EnemyBehaviorState.Dead;
            }

            if (context.DistanceFromHome > context.ReturnRadius ||
                !context.HasLivingTarget &&
                context.DistanceFromHome > context.StoppingDistance)
            {
                return EnemyBehaviorState.Return;
            }

            if (!context.HasLivingTarget)
            {
                return context.State == EnemyBehaviorState.Return &&
                       context.DistanceFromHome > context.StoppingDistance
                    ? EnemyBehaviorState.Return
                    : EnemyBehaviorState.Idle;
            }

            if (context.State == EnemyBehaviorState.Return &&
                context.DistanceFromHome > context.ReturnRadius)
            {
                return EnemyBehaviorState.Return;
            }

            if (context.DistanceToTarget <= context.AttackRadius)
            {
                return EnemyBehaviorState.Attack;
            }

            if (context.DistanceToTarget <= context.DetectionRadius ||
                context.State == EnemyBehaviorState.Chase ||
                context.State == EnemyBehaviorState.Attack)
            {
                return EnemyBehaviorState.Chase;
            }

            return EnemyBehaviorState.Idle;
        }
    }
}
