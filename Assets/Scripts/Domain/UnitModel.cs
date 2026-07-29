namespace SimpleSummon.Domain
{
    public sealed class UnitModel
    {
        public UnitModel(float movementSpeed, float jumpHeight)
        {
            MovementSpeed = movementSpeed;
            JumpHeight = jumpHeight;
        }

        public float MovementSpeed { get; }
        public float JumpHeight { get; }
    }
}
