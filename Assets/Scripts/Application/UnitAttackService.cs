using SimpleSummon.Domain;

namespace SimpleSummon.Application
{
    public static class UnitAttackService
    {
        public static bool TryAttack(UnitModel unit, float deltaTime, bool attackRequested)
        {
            unit.UpdateAttackCooldown(deltaTime);
            return attackRequested && unit.TryAttack();
        }
    }
}
