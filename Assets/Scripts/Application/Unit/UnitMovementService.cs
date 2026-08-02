using System;
using System.Numerics;

namespace SimpleSummon.Application
{
    public static class UnitMovementService
    {
        public static Vector3 GetCameraRelativeDirection(Vector2 input, Vector3 cameraForward, Vector3 cameraRight)
        {
            cameraForward.Y = 0f;
            cameraRight.Y = 0f;

            cameraForward = NormalizeOrZero(cameraForward);
            cameraRight = NormalizeOrZero(cameraRight);

            Vector3 direction = cameraForward * input.Y + cameraRight * input.X;
            float lengthSquared = direction.LengthSquared();
            return lengthSquared > 1f
                ? direction / MathF.Sqrt(lengthSquared)
                : direction;
        }

        public static float GetJumpVelocity(float jumpHeight, float gravity)
        {
            if (jumpHeight < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(jumpHeight),
                    "Jump height cannot be negative.");
            }

            if (gravity >= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(gravity),
                    "Gravity must be negative.");
            }

            return MathF.Sqrt(jumpHeight * -2f * gravity);
        }

        private static Vector3 NormalizeOrZero(Vector3 value)
        {
            float lengthSquared = value.LengthSquared();
            return lengthSquared > 0f
                ? value / MathF.Sqrt(lengthSquared)
                : Vector3.Zero;
        }
    }

}
