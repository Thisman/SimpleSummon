using System;
using NumericsVector2 = System.Numerics.Vector2;
using NumericsVector3 = System.Numerics.Vector3;
using SimpleSummon.Application;
using SimpleSummon.Domain;
using SimpleSummon.Network;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SimpleSummon.Runtime
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerSettings))]
    public sealed class PlayerController : MonoBehaviour, IDamageable
    {
        private const float MinimumAttackDirectionDot = 0.70710678f;
        private const int AttackColliderBufferSize = 32;
        private const int AimHitBufferSize = 32;

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
        private NetworkPlayer networkPlayer;
        private UnitModel model;
        private PlayerSettings settings;
        private Texture2D damageVignette;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;
        private float damageVignetteTime;
        private bool inputEnabled;
        private InputAction moveInput;
        private InputAction jumpInput;
        private InputAction attackInput;
        private Vector3 fallbackSpawnPosition;
        private Quaternion fallbackSpawnRotation;
        private bool replicatedDead;
        private bool replicatedStateInitialized;
        private readonly Collider[] attackColliders =
            new Collider[AttackColliderBufferSize];
        private readonly RaycastHit[] aimHits = new RaycastHit[AimHitBufferSize];

        public event Action Respawned;

        public bool IsDead => model.IsDead;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            moveInput = moveAction.action.Clone();
            jumpInput = jumpAction.action.Clone();
            attackInput = attackAction.action.Clone();
            fallbackSpawnPosition = transform.position;
            fallbackSpawnRotation = transform.rotation;
            networkPlayer = GetComponent<NetworkPlayer>();
            orbitCamera = cameraTransform.GetComponent<OrbitCameraController>();
            cameraTransform.gameObject.SetActive(
                SceneManager.GetActiveScene().name == NetworkSessionService.GameSceneName);
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
            PlayerRegistry.Register(this);
            SceneManager.activeSceneChanged += HandleActiveSceneChanged;
            if (networkPlayer != null)
            {
                networkPlayer.RoleChanged += RefreshLocalRole;
                networkPlayer.VitalStateChanged += ApplyReplicatedVitalState;
                networkPlayer.DamageReceived += ApplyReplicatedDamage;
            }

            RefreshLocalRole();
        }

        private void OnDisable()
        {
            PlayerRegistry.Unregister(this);
            SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
            SetInputEnabled(false);
            if (networkPlayer != null)
            {
                networkPlayer.RoleChanged -= RefreshLocalRole;
                networkPlayer.VitalStateChanged -= ApplyReplicatedVitalState;
                networkPlayer.DamageReceived -= ApplyReplicatedDamage;
            }
        }

        private void OnDestroy()
        {
            moveInput?.Dispose();
            jumpInput?.Dispose();
            attackInput?.Dispose();
            Destroy(damageVignette);
        }

        private void Update()
        {
            damageVignetteTime = Mathf.Max(0f, damageVignetteTime - Time.unscaledDeltaTime);

            if (model.IsDead)
            {
                return;
            }

            Vector3 direction = Vector3.zero;
            bool jumpRequested = false;
            bool attackRequested = false;

            if (networkPlayer == null || networkPlayer.CanReadLocalInput)
            {
                Vector2 input = moveInput.ReadValue<Vector2>();
                NumericsVector3 calculatedDirection =
                    UnitMovementService.GetCameraRelativeDirection(
                        new NumericsVector2(input.x, input.y),
                        ToNumerics(cameraTransform.forward),
                        ToNumerics(cameraTransform.right));
                direction = new Vector3(
                    calculatedDirection.X,
                    calculatedDirection.Y,
                    calculatedDirection.Z);
                jumpRequested = jumpInput.WasPressedThisFrame();
                attackRequested = attackInput.WasPressedThisFrame();
                networkPlayer?.SubmitInput(direction, jumpRequested, attackRequested);
            }

            if (networkPlayer != null && networkPlayer.CanRunSimulation)
            {
                networkPlayer.ReadServerInput(
                    out direction,
                    out jumpRequested,
                    out attackRequested);
            }
            else if (networkPlayer != null)
            {
                return;
            }

            UpdateVerticalVelocity(jumpRequested);
            Move(direction);
            UpdateAttack(attackRequested);

            float normalizedMovementSpeed = model.MovementSpeed > 0f
                ? horizontalVelocity.magnitude / model.MovementSpeed
                : 0f;
            animator.SetFloat(MovementSpeedId, normalizedMovementSpeed, 0.1f, Time.deltaTime);
        }

        private void UpdateAttack(bool attackRequested)
        {
            if (UnitAttackService.TryAttack(model, Time.deltaTime, attackRequested))
            {
                FaceAimedTarget();
                animator.SetTrigger(AttackId);
            }
        }

        private void UpdateVerticalVelocity(bool jumpRequested)
        {
            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            if (characterController.isGrounded && jumpRequested)
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
            if (model.IsDead ||
                networkPlayer != null && !networkPlayer.CanRunSimulation)
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
            networkPlayer?.PublishDamage();
            networkPlayer?.PublishVitalState(model.CurrentHealth, model.IsDead);
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
            if (!model.IsDead ||
                networkPlayer != null && !networkPlayer.CanRunSimulation)
            {
                return;
            }

            if (spawnPoint != null)
            {
                Teleport(spawnPoint);
            }
            else
            {
                characterController.enabled = false;
                transform.SetPositionAndRotation(
                    fallbackSpawnPosition,
                    fallbackSpawnRotation);
                characterController.enabled = true;
                StopHorizontalMovement();
                verticalVelocity = 0f;
            }
            model.RestoreHealth();
            networkPlayer?.PublishVitalState(model.CurrentHealth, model.IsDead);
            animator.SetTrigger(RespawnId);
            Respawned?.Invoke();
        }

        private void RefreshLocalRole()
        {
            bool isLocal = networkPlayer == null || networkPlayer.CanReadLocalInput;
            SetInputEnabled(isLocal);
            cameraTransform.gameObject.SetActive(
                isLocal &&
                SceneManager.GetActiveScene().name == NetworkSessionService.GameSceneName);
            if (networkPlayer != null && networkPlayer.CanRunSimulation)
            {
                networkPlayer.PublishVitalState(model.CurrentHealth, model.IsDead);
            }
        }

        private void HandleActiveSceneChanged(Scene _, Scene __)
        {
            RefreshLocalRole();
        }

        private void SetInputEnabled(bool enabled)
        {
            if (inputEnabled == enabled)
            {
                return;
            }

            inputEnabled = enabled;
            if (enabled)
            {
                moveInput.Enable();
                jumpInput.Enable();
                attackInput.Enable();
            }
            else
            {
                moveInput.Disable();
                jumpInput.Disable();
                attackInput.Disable();
            }
        }

        public void SetLocalInputEnabled(bool enabled)
        {
            if (networkPlayer == null || networkPlayer.CanReadLocalInput)
            {
                SetInputEnabled(enabled);
            }
        }

        private void ApplyReplicatedVitalState(float currentHealth, bool isDead)
        {
            if (networkPlayer == null || networkPlayer.CanRunSimulation)
            {
                return;
            }

            model.SetCurrentHealth(currentHealth);
            if (!replicatedStateInitialized || replicatedDead != isDead)
            {
                if (isDead)
                {
                    animator.SetTrigger(DeathId);
                }
                else if (replicatedStateInitialized)
                {
                    animator.SetTrigger(RespawnId);
                }
            }

            replicatedDead = isDead;
            replicatedStateInitialized = true;
        }

        private void ApplyReplicatedDamage()
        {
            if (networkPlayer == null || networkPlayer.CanRunSimulation)
            {
                return;
            }

            damageFlash.Play();
            if (networkPlayer.CanReadLocalInput)
            {
                orbitCamera.PlayDamageShake();
                damageVignetteTime = damageVignetteDuration;
            }
        }

        private bool TryGetClosestAttackTarget(out IDamageable target)
        {
            int colliderCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                settings.AttackRange,
                attackColliders,
                settings.AttackMask,
                QueryTriggerInteraction.Ignore);

            target = null;
            float closestSqrDistance = float.PositiveInfinity;
            float attackRangeSqr = settings.AttackRange * settings.AttackRange;

            for (int i = 0; i < colliderCount; i++)
            {
                Collider collider = attackColliders[i];
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
                if (candidateComponent is PlayerController)
                {
                    continue;
                }

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

        private static NumericsVector3 ToNumerics(Vector3 value)
        {
            return new NumericsVector3(value.x, value.y, value.z);
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
            int hitCount = Physics.RaycastNonAlloc(
                ray,
                aimHits,
                settings.AimRayDistance,
                settings.AttackMask,
                QueryTriggerInteraction.Ignore);

            target = null;
            targetHit = default;
            float closestDistance = float.PositiveInfinity;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = aimHits[i];
                if (hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                IDamageable candidate =
                    hit.collider.GetComponentInParent<IDamageable>();
                if (candidate == null ||
                    candidate is PlayerController ||
                    candidate.IsDead ||
                    hit.distance >= closestDistance)
                {
                    continue;
                }

                target = candidate;
                targetHit = hit;
                closestDistance = hit.distance;
            }

            return target != null;
        }
    }
}
