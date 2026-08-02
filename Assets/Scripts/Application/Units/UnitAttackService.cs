using System;
using SimpleSummon.Domain;

namespace SimpleSummon.Application
{
    public static class UnitAttackService
    {
        public static bool TryAttack(UnitModel unit, float deltaTime, bool attackRequested)
        {
            if (unit == null)
            {
                throw new ArgumentNullException(nameof(unit));
            }
            unit.UpdateAttackCooldown(deltaTime);
            return attackRequested && unit.TryAttack();
        }
    }
}
