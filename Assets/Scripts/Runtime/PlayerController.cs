using SimpleSummon.Application;
using SimpleSummon.Domain;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SimpleSummon.Runtime
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerSettings))]
    public sealed class PlayerController : MonoBehaviour
    {
        private static readonly int MovementSpeedId = Animator.StringToHash("MovementSpeed");
        private static readonly int AttackId = Animator.StringToHash("Attack");

        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Animator animator;
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference jumpAction;
        [SerializeField] private InputActionReference attackAction;
        [SerializeField, Min(0f)] private float rotationSpeed = 12f;
        [SerializeField, Min(0f)] private float momentumDuration = 0.2f;

        private CharacterController characterController;
        private UnitModel model;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();

            PlayerSettings settings = GetComponent<PlayerSettings>();
            model = new UnitModel(settings.MovementSpeed, settings.JumpHeight, settings.AttackDelay);
        }

        private void OnEnable()
        {
            moveAction.action.Enable();
            jumpAction.action.Enable();
            attackAction.action.Enable();
        }

        private void OnDisable()
        {
            moveAction.action.Disable();
            jumpAction.action.Disable();
            attackAction.action.Disable();
        }

        private void Update()
        {
            Vector2 input = moveAction.action.ReadValue<Vector2>();
            Vector3 direction = UnitMovementService.GetCameraRelativeDirection(
                input,
                cameraTransform.forward,
                cameraTransform.right);

            UpdateVerticalVelocity();
            Move(direction);
            Rotate(horizontalVelocity);
            UpdateAttack();

            float normalizedMovementSpeed = model.MovementSpeed > 0f
                ? horizontalVelocity.magnitude / model.MovementSpeed
                : 0f;
            animator.SetFloat(MovementSpeedId, normalizedMovementSpeed, 0.1f, Time.deltaTime);
        }

        private void UpdateAttack()
        {
            bool attackRequested = attackAction.action.WasPressedThisFrame();

            if (UnitAttackService.TryAttack(model, Time.deltaTime, attackRequested))
            {
                animator.SetTrigger(AttackId);
            }
        }

        private void UpdateVerticalVelocity()
        {
            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            if (characterController.isGrounded && jumpAction.action.WasPressedThisFrame())
            {
                verticalVelocity = UnitMovementService.GetJumpVelocity(model.JumpHeight, Physics.gravity.y);
            }

            verticalVelocity += Physics.gravity.y * Time.deltaTime;
        }

        private void Move(Vector3 direction)
        {
            if (direction.sqrMagnitude > 0f)
            {
                horizontalVelocity = direction * model.MovementSpeed;
            }
            else if (momentumDuration <= 0f)
            {
                horizontalVelocity = Vector3.zero;
            }
            else
            {
                float deceleration = model.MovementSpeed / momentumDuration;
                horizontalVelocity = Vector3.MoveTowards(
                    horizontalVelocity,
                    Vector3.zero,
                    deceleration * Time.deltaTime);
            }

            Vector3 velocity = horizontalVelocity;
            velocity.y = verticalVelocity;
            characterController.Move(velocity * Time.deltaTime);
        }

        private void Rotate(Vector3 direction)
        {
            if (direction.sqrMagnitude <= 0f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime);
        }

        public void Teleport(Transform destination)
        {
            characterController.enabled = false;
            transform.SetPositionAndRotation(destination.position, destination.rotation);
            characterController.enabled = true;

            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
        }
    }
}
