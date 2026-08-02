using System;
using SimpleSummon.Domain;

namespace SimpleSummon.Application
{
    public static class PlayerVitalService
    {
        public static bool TakeDamage(UnitModel model, float damage)
        {
            Validate(model);
            bool wasDead = model.IsDead;
            model.TakeDamage(damage);
            return !wasDead && model.IsDead;
        }

        public static void Restore(UnitModel model)
        {
            Validate(model);
            model.RestoreHealth();
        }

        public static void ApplyReplicatedHealth(UnitModel model, float health)
        {
            Validate(model);
            model.SetCurrentHealth(health);
        }

        private static void Validate(UnitModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }
        }
    }
}
