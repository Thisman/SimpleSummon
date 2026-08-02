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
        private DamageVignette damageVignette;
        private PlayerCombatTargeting combatTargeting;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;
        private bool inputEnabled;
        private InputAction moveInput;
        private InputAction jumpInput;
        private InputAction attackInput;
        private Vector3 fallbackSpawnPosition;
        private Quaternion fallbackSpawnRotation;
        private bool replicatedDead;
        private bool replicatedStateInitialized;

        public event Action Respawned;
        public event Action<float, float> VitalStateChanged;

        public bool IsDead => model.IsDead;
        public float CurrentHealth => model.CurrentHealth;
        public float MaximumHealth => model.MaximumHealth;

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
            damageVignette = new DamageVignette(
                damageVignetteDuration,
                damageVignetteOpacity);
            combatTargeting = new PlayerCombatTargeting(transform);

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
            VitalStateChanged?.Invoke(model.CurrentHealth, model.MaximumHealth);
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
            damageVignette.Dispose();
        }

        private void Update()
        {
            damageVignette.Tick(Time.unscaledDeltaTime);

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

            if (combatTargeting.TryGetClosestAttackTarget(
                settings.AttackRange,
                settings.AttackMask,
                out IDamageable target))
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
            VitalStateChanged?.Invoke(model.CurrentHealth, model.MaximumHealth);
            networkPlayer?.PublishDamage();
            networkPlayer?.PublishVitalState(model.CurrentHealth, model.IsDead);
            damageFlash.Play();
            orbitCamera.PlayDamageShake();
            damageVignette.Play();
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
            damageVignette.Draw();
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
            VitalStateChanged?.Invoke(model.CurrentHealth, model.MaximumHealth);
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
            VitalStateChanged?.Invoke(model.CurrentHealth, model.MaximumHealth);
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
                damageVignette.Play();
            }
        }

        private static NumericsVector3 ToNumerics(Vector3 value)
        {
            return new NumericsVector3(value.x, value.y, value.z);
        }

        private void FaceAimedTarget()
        {
            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            if (!combatTargeting.TryGetAimedTarget(
                ray,
                settings.AimRayDistance,
                settings.AttackMask,
                out IDamageable target))
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

    }
}
