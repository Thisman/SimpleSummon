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

        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Animator animator;
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference jumpAction;
        [SerializeField, Min(0f)] private float rotationSpeed = 12f;

        private CharacterController characterController;
        private UnitModel model;
        private float verticalVelocity;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();

            PlayerSettings settings = GetComponent<PlayerSettings>();
            model = new UnitModel(settings.MovementSpeed, settings.JumpHeight);
        }

        private void OnEnable()
        {
            moveAction.action.Enable();
            jumpAction.action.Enable();
        }

        private void OnDisable()
        {
            moveAction.action.Disable();
            jumpAction.action.Disable();
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
            Rotate(direction);

            animator.SetFloat(MovementSpeedId, direction.magnitude, 0.1f, Time.deltaTime);
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
            Vector3 velocity = direction * model.MovementSpeed;
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
    }
}
