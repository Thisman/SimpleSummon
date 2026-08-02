namespace SimpleSummon.Domain
{
    public sealed class UnitModel
    {
        public UnitModel(
            float movementSpeed,
            float jumpHeight,
            float attackDelay,
            float damage,
            float maximumHealth)
        {
            ValidateNonNegative(movementSpeed, nameof(movementSpeed));
            ValidateNonNegative(jumpHeight, nameof(jumpHeight));
            ValidateNonNegative(attackDelay, nameof(attackDelay));
            ValidateNonNegative(damage, nameof(damage));
            if (maximumHealth <= 0f)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(maximumHealth),
                    "Maximum health must be positive.");
            }

            MovementSpeed = movementSpeed;
            JumpHeight = jumpHeight;
            AttackDelay = attackDelay;
            Damage = damage;
            MaximumHealth = maximumHealth;
            CurrentHealth = maximumHealth;
        }

        public float MovementSpeed { get; }
        public float JumpHeight { get; }
        public float AttackDelay { get; }
        public float Damage { get; }
        public float MaximumHealth { get; }
        public float CurrentHealth { get; private set; }
        public float AttackCooldownRemaining { get; private set; }
        public bool IsDead => CurrentHealth <= 0f;

        public void UpdateAttackCooldown(float deltaTime)
        {
            ValidateNonNegative(deltaTime, nameof(deltaTime));
            AttackCooldownRemaining = System.Math.Max(0f, AttackCooldownRemaining - deltaTime);
        }

        public bool TryAttack()
        {
            if (IsDead || AttackCooldownRemaining > 0f)
            {
                return false;
            }

            AttackCooldownRemaining = AttackDelay;
            return true;
        }

        public void TakeDamage(float damage)
        {
            if (IsDead || !float.IsFinite(damage) || damage <= 0f)
            {
                return;
            }

            CurrentHealth = System.Math.Max(0f, CurrentHealth - damage);
        }

        public void RestoreHealth()
        {
            CurrentHealth = MaximumHealth;
            AttackCooldownRemaining = 0f;
        }

        public void SetCurrentHealth(float currentHealth)
        {
            if (!float.IsFinite(currentHealth))
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(currentHealth),
                    "Current health must be finite.");
            }

            CurrentHealth = System.Math.Clamp(currentHealth, 0f, MaximumHealth);
        }

        private static void ValidateNonNegative(float value, string parameterName)
        {
            if (!float.IsFinite(value) || value < 0f)
            {
                throw new System.ArgumentOutOfRangeException(
                    parameterName,
                    "Value cannot be negative.");
            }
        }
    }
}
