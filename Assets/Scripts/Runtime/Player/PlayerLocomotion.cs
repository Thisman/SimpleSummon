using SimpleSummon.Application;
using SimpleSummon.Domain;
using UnityEngine;

namespace SimpleSummon.Runtime
{
    internal sealed class PlayerLocomotion
    {
        private readonly Transform transform;
        private readonly CharacterController controller;
        private readonly float momentumDuration;
        private readonly float rotationSpeed;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;

        public PlayerLocomotion(
            Transform transform,
            CharacterController controller,
            float momentumDuration,
            float rotationSpeed)
        {
            this.transform = transform;
            this.controller = controller;
            this.momentumDuration = momentumDuration;
            this.rotationSpeed = rotationSpeed;
        }

        public float HorizontalSpeed => horizontalVelocity.magnitude;
        public bool IsGrounded => controller.isGrounded;

        public void Tick(
            UnitModel model,
            Vector3 direction,
            bool jumpRequested,
            float movementSpeedBonus,
            float deltaTime)
        {
            UpdateVerticalVelocity(model.JumpHeight, jumpRequested, deltaTime);
            UpdateHorizontalVelocity(
                model.MovementSpeed + movementSpeedBonus,
                direction,
                deltaTime);

            Vector3 velocity = horizontalVelocity;
            velocity.y = verticalVelocity;
            controller.Move(velocity * deltaTime);

            if (horizontalVelocity.sqrMagnitude > 0f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(horizontalVelocity);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * deltaTime);
            }
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            controller.enabled = true;
            Stop();
        }

        public void Stop()
        {
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
        }

        private void UpdateVerticalVelocity(
            float jumpHeight,
            bool jumpRequested,
            float deltaTime)
        {
            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            if (controller.isGrounded && jumpRequested)
            {
                verticalVelocity = UnitMovementService.GetJumpVelocity(
                    jumpHeight,
                    Physics.gravity.y);
            }

            verticalVelocity += Physics.gravity.y * deltaTime;
        }

        private void UpdateHorizontalVelocity(
            float movementSpeed,
            Vector3 direction,
            float deltaTime)
        {
            if (direction.sqrMagnitude > 0f)
            {
                horizontalVelocity = direction * movementSpeed;
            }
            else if (momentumDuration <= 0f)
            {
                horizontalVelocity = Vector3.zero;
            }
            else
            {
                float deceleration = movementSpeed / momentumDuration;
                horizontalVelocity = Vector3.MoveTowards(
                    horizontalVelocity,
                    Vector3.zero,
                    deceleration * deltaTime);
            }
        }
    }
}
