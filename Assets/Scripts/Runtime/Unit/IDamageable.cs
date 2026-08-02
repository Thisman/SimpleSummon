namespace SimpleSummon.Runtime
{
    public interface IDamageable
    {
        bool IsDead { get; }

        void TakeDamage(float damage);
    }
}
