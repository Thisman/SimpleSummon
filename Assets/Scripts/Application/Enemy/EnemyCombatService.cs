using SimpleSummon.Domain;

namespace SimpleSummon.Application
{
    public static class EnemyCombatService
    {
        public static UnitModel Create(
            float movementSpeed,
            float attackDelay,
            float damage,
            float maximumHealth,
            float statMultiplier)
        {
            return new UnitModel(
                movementSpeed,
                0f,
                attackDelay,
                damage * statMultiplier,
                maximumHealth * statMultiplier);
        }

        public static UnitModel RemoveStatMultiplier(
            UnitModel current,
            float movementSpeed,
            float attackDelay,
            float damage,
            float maximumHealth)
        {
            UnitModel result = Create(
                movementSpeed,
                attackDelay,
                damage,
                maximumHealth,
                1f);
            result.SetCurrentHealth(System.MathF.Min(
                current.CurrentHealth,
                result.MaximumHealth));
            return result;
        }

        public static bool TakeDamage(UnitModel model, float damage)
        {
            bool wasDead = model.IsDead;
            model.TakeDamage(damage);
            return !wasDead && model.IsDead;
        }
    }
}
