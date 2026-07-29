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
        private const float MinimumAttackDirectionDot = 0.70710678f;

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
        [SerializeField, Min(0f)] private float momentumDuration = 0.2f;
        [SerializeField, Min(0f)] private float rotationSpeed = 720f;
        [SerializeField, Min(0f)] private float damageVignetteDuration = 0.35f;
        [SerializeField, Range(0f, 1f)] private float damageVignetteOpacity = 0.65f;

        private CharacterController characterController;
        private OrbitCameraController orbitCamera;
        private UnitModel model;
        private PlayerSettings settings;
        private Texture2D damageVignette;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;
        private float damageVignetteTime;

        public event Action Respawned;

        public bool IsDead => model.IsDead;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            orbitCamera = cameraTransform.GetComponent<OrbitCameraController>();
            damageVignette = CreateDamageVignette();

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

        private void OnDestroy()
        {
            Destroy(damageVignette);
        }

        private void Update()
        {
            damageVignetteTime = Mathf.Max(0f, damageVignetteTime - Time.unscaledDeltaTime);

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

            if (horizontalVelocity.sqrMagnitude > 0f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(horizontalVelocity);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime);
            }
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

            if (TryGetClosestAttackTarget(out IDamageable target))
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
            orbitCamera.PlayDamageShake();
            damageVignetteTime = damageVignetteDuration;
            if (!model.IsDead)
            {
                return;
            }

            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
            animator.SetFloat(MovementSpeedId, 0f);
            animator.SetTrigger(DeathId);
        }

        private void OnGUI()
        {
            if (damageVignetteTime <= 0f || damageVignetteDuration <= 0f)
            {
                return;
            }

            float normalizedTime = damageVignetteTime / damageVignetteDuration;
            float pulse = Mathf.Sin(normalizedTime * Mathf.PI * 0.5f);
            GUI.color = new Color(1f, 1f, 1f, pulse * damageVignetteOpacity);
            GUI.DrawTexture(
                new Rect(0f, 0f, Screen.width, Screen.height),
                damageVignette,
                ScaleMode.StretchToFill);
            GUI.color = Color.white;
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

        private bool TryGetClosestAttackTarget(out IDamageable target)
        {
            Collider[] colliders = Physics.OverlapSphere(
                transform.position,
                settings.AttackRange,
                settings.AttackMask,
                QueryTriggerInteraction.Ignore);

            target = null;
            float closestSqrDistance = float.PositiveInfinity;
            float attackRangeSqr = settings.AttackRange * settings.AttackRange;

            foreach (Collider collider in colliders)
            {
                if (collider.transform.IsChildOf(transform))
                {
                    continue;
                }

                IDamageable candidate = collider.GetComponentInParent<IDamageable>();
                if (candidate == null || candidate.IsDead)
                {
                    continue;
                }

                Component candidateComponent = (Component)candidate;
                Vector3 direction = candidateComponent.transform.position - transform.position;
                direction.y = 0f;

                float sqrDistance = direction.sqrMagnitude;
                if (sqrDistance <= 0f ||
                    sqrDistance > attackRangeSqr ||
                    Vector3.Dot(transform.forward, direction.normalized) < MinimumAttackDirectionDot ||
                    sqrDistance >= closestSqrDistance)
                {
                    continue;
                }

                target = candidate;
                closestSqrDistance = sqrDistance;
            }

            return target != null;
        }

        private static Texture2D CreateDamageVignette()
        {
            const int textureSize = 128;
            Texture2D texture = new Texture2D(
                textureSize,
                textureSize,
                TextureFormat.RGBA32,
                false);
            Color[] pixels = new Color[textureSize * textureSize];

            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    float normalizedX = (x + 0.5f) / textureSize * 2f - 1f;
                    float normalizedY = (y + 0.5f) / textureSize * 2f - 1f;
                    float distance = Mathf.Max(
                        Mathf.Abs(normalizedX),
                        Mathf.Abs(normalizedY));
                    float alpha = Mathf.SmoothStep(
                        0f,
                        1f,
                        Mathf.InverseLerp(0.82f, 1f, distance));
                    pixels[y * textureSize + x] = new Color(0.75f, 0f, 0f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
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
