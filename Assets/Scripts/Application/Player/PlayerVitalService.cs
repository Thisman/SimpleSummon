using SimpleSummon.Domain;

namespace SimpleSummon.Application
{
    public static class PlayerVitalService
    {
        public static bool TakeDamage(UnitModel model, float damage)
        {
            bool wasDead = model.IsDead;
            model.TakeDamage(damage);
            return !wasDead && model.IsDead;
        }

        public static void Restore(UnitModel model) => model.RestoreHealth();

        public static void ApplyReplicatedHealth(UnitModel model, float health) =>
            model.SetCurrentHealth(health);
    }
}
