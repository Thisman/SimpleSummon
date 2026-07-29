using UnityEngine;

namespace SimpleSummon.Application
{
    public static class UnitMovementService
    {
        public static Vector3 GetCameraRelativeDirection(Vector2 input, Vector3 cameraForward, Vector3 cameraRight)
        {
            cameraForward.y = 0f;
            cameraRight.y = 0f;

            cameraForward.Normalize();
            cameraRight.Normalize();

            return Vector3.ClampMagnitude(cameraForward * input.y + cameraRight * input.x, 1f);
        }

        public static float GetJumpVelocity(float jumpHeight, float gravity)
        {
            return Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }
}
