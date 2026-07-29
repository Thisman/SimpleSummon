using System;
using SimpleSummon.Application;
using SimpleSummon.Domain;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SimpleSummon.Runtime
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerSettings))]
    public sealed class PlayerController : MonoBehaviour, IDamageable
    {
        private static readonly int MovementSpeedId = Animator.StringToHash("MovementSpeed");
        private static readonly int AttackId = Animator.StringToHash("Attack");
        private static readonly int DeathId = Animator.StringToHash("Death");
        private static readonly int RespawnId = Animator.StringToHash("Respawn");

        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private DamageFlash damageFlash;
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference jumpAction;
        [SerializeField] private InputActionReference attackAction;
        [SerializeField, Min(0f)] private float rotationSpeed = 12f;
        [SerializeField, Min(0f)] private float momentumDuration = 0.2f;

        private CharacterController characterController;
        private UnitModel model;
        private PlayerSettings settings;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;

        public event Action Respawned;

        public bool IsDead => model.IsDead;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();

            settings = GetComponent<PlayerSettings>();
            model = new UnitModel(
                settings.MovementSpeed,
                settings.JumpHeight,
                settings.AttackDelay,
                settings.Damage,
                settings.MaximumHealth);
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
            if (model.IsDead)
            {
                return;
            }

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
                FaceAimedTarget();
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

            StopHorizontalMovement();
            verticalVelocity = 0f;
        }

        public void StopHorizontalMovement()
        {
            horizontalVelocity = Vector3.zero;
            animator.SetFloat(MovementSpeedId, 0f);
        }

        public void ApplyAttackDamage()
        {
            if (model.IsDead)
            {
                return;
            }

            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            if (TryGetAimedTarget(ray, out IDamageable target, out RaycastHit hit) &&
                Vector3.Distance(transform.position, hit.point) <= settings.AttackRange)
            {
                target.TakeDamage(model.Damage);
            }
        }

        public void TakeDamage(float damage)
        {
            if (model.IsDead)
            {
                return;
            }

            model.TakeDamage(damage);
            damageFlash.Play();
            if (!model.IsDead)
            {
                return;
            }

            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
            animator.SetFloat(MovementSpeedId, 0f);
            animator.SetTrigger(DeathId);
        }

        public void CompleteDeathAnimation()
        {
            if (!model.IsDead)
            {
                return;
            }

            Teleport(spawnPoint);
            model.RestoreHealth();
            animator.SetTrigger(RespawnId);
            Respawned?.Invoke();
        }

        private void FaceAimedTarget()
        {
            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            if (!TryGetAimedTarget(ray, out IDamageable target, out _))
            {
                return;
            }

            Component targetComponent = (Component)target;
            Vector3 direction = targetComponent.transform.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        private bool TryGetAimedTarget(
            Ray ray,
            out IDamageable target,
            out RaycastHit targetHit)
        {
            RaycastHit[] hits = Physics.RaycastAll(
                ray,
                settings.AimRayDistance,
                settings.AttackMask,
                QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

            foreach (RaycastHit hit in hits)
            {
                if (hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                target = hit.collider.GetComponentInParent<IDamageable>();
                targetHit = hit;
                return target != null && !target.IsDead;
            }

            target = null;
            targetHit = default;
            return false;
        }
    }
}
