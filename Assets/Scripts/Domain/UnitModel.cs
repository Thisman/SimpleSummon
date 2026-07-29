namespace SimpleSummon.Domain
{
    public sealed class UnitModel
    {
        public UnitModel(float movementSpeed, float jumpHeight, float attackDelay)
        {
            MovementSpeed = movementSpeed;
            JumpHeight = jumpHeight;
            AttackDelay = attackDelay;
        }

        public float MovementSpeed { get; }
        public float JumpHeight { get; }
        public float AttackDelay { get; }
        public float AttackCooldownRemaining { get; private set; }

        public void UpdateAttackCooldown(float deltaTime)
        {
            AttackCooldownRemaining = System.Math.Max(0f, AttackCooldownRemaining - deltaTime);
        }

        public bool TryAttack()
        {
            if (AttackCooldownRemaining > 0f)
            {
                return false;
            }

            AttackCooldownRemaining = AttackDelay;
            return true;
        }
    }
}
