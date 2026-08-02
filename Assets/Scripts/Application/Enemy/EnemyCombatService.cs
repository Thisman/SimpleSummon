using SimpleSummon.Domain;

namespace SimpleSummon.Application
{
    public static class EnemyCombatService
    {
        public static UnitModel Create(
            float movementSpeed,
            float attackDelay,
            float damage,
            float maximumHealth)
        {
            return new UnitModel(
                movementSpeed,
                0f,
                attackDelay,
                damage,
                maximumHealth);
        }

        public static bool TakeDamage(UnitModel model, float damage)
        {
            bool wasDead = model.IsDead;
            model.TakeDamage(damage);
            return !wasDead && model.IsDead;
        }
    }
}
