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
        [SerializeField, Min(0f)] private float torchMovementSpeedBonus = 2f;

        private NetworkPlayer networkPlayer;
        private UnitModel model;
        private PlayerSettings settings;
        private LocalPlayerPresentation localPresentation;
        private PlayerCombatTargeting combatTargeting;
        private PlayerInputReader inputReader;
        private PlayerLocomotion locomotion;
        private PlayerPresentation presentation;
        private PlayerVitalController vitals;

        public event Action Respawned
        {
            add => vitals.Respawned += value;
            remove => vitals.Respawned -= value;
        }
        public event Action<float, float> VitalStateChanged
        {
            add => vitals.Changed += value;
            remove => vitals.Changed -= value;
        }

        public bool IsDead => model.IsDead;
        public bool IsGrounded => locomotion.IsGrounded;
        public float CurrentHealth => model.CurrentHealth;
        public float MaximumHealth => model.MaximumHealth;

        private void Awake()
        {
            CharacterController characterController = GetComponent<CharacterController>();
            inputReader = new PlayerInputReader(moveAction, jumpAction, attackAction);
            locomotion = new PlayerLocomotion(
                transform,
                characterController,
                momentumDuration,
                rotationSpeed);
            presentation = new PlayerPresentation(animator, damageFlash);
            Vector3 fallbackSpawnPosition = transform.position;
            Quaternion fallbackSpawnRotation = transform.rotation;
            networkPlayer = GetComponent<NetworkPlayer>();
            localPresentation = new LocalPlayerPresentation(
                cameraTransform,
                damageVignetteDuration,
                damageVignetteOpacity);
            localPresentation.SetCameraActive(
                SceneManager.GetActiveScene().name == NetworkSessionService.GameSceneName);
            combatTargeting = new PlayerCombatTargeting(transform);

            settings = GetComponent<PlayerSettings>();
            model = new UnitModel(
                settings.MovementSpeed,
                settings.JumpHeight,
                settings.AttackDelay,
                settings.Damage,
                settings.MaximumHealth);
            vitals = new PlayerVitalController(
                model,
                networkPlayer,
                locomotion,
                presentation,
                localPresentation,
                spawnPoint,
                fallbackSpawnPosition,
                fallbackSpawnRotation);
        }

        private void OnEnable()
        {
            PlayerRegistry.Register(this);
            SceneManager.activeSceneChanged += HandleActiveSceneChanged;
            if (networkPlayer != null)
            {
                networkPlayer.RoleChanged += RefreshLocalRole;
                networkPlayer.VitalStateChanged += vitals.ApplyReplicatedState;
                networkPlayer.DamageReceived += vitals.ApplyReplicatedDamage;
            }

            RefreshLocalRole();
            vitals.NotifyInitialState();
        }

        private void OnDisable()
        {
            PlayerRegistry.Unregister(this);
            SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
            inputReader.SetEnabled(false);
            if (networkPlayer != null)
            {
                networkPlayer.RoleChanged -= RefreshLocalRole;
                networkPlayer.VitalStateChanged -= vitals.ApplyReplicatedState;
                networkPlayer.DamageReceived -= vitals.ApplyReplicatedDamage;
            }
        }

        private void OnDestroy()
        {
            inputReader.Dispose();
            localPresentation.Dispose();
        }

        private void Update()
        {
            localPresentation.Tick(Time.unscaledDeltaTime);

            if (model.IsDead)
            {
                return;
            }

            Vector3 direction = Vector3.zero;
            bool jumpRequested = false;
            bool attackRequested = false;

            if (networkPlayer == null || networkPlayer.CanReadLocalInput)
            {
                PlayerInputFrame input = inputReader.Read();
                NumericsVector3 calculatedDirection =
                    UnitMovementService.GetCameraRelativeDirection(
                        new NumericsVector2(input.Movement.x, input.Movement.y),
                        ToNumerics(cameraTransform.forward),
                        ToNumerics(cameraTransform.right));
                direction = new Vector3(
                    calculatedDirection.X,
                    calculatedDirection.Y,
                    calculatedDirection.Z);
                jumpRequested = input.JumpRequested;
                attackRequested = input.AttackRequested;
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

            bool hasTorch = networkPlayer != null && networkPlayer.HasTorch;
            locomotion.Tick(
                model,
                direction,
                jumpRequested,
                hasTorch ? torchMovementSpeedBonus : 0f,
                Time.deltaTime);
            networkPlayer?.SetTorchMovementActive(locomotion.HorizontalSpeed > 0.01f);
            UpdateAttack(attackRequested);

            float normalizedMovementSpeed = model.MovementSpeed > 0f
                ? locomotion.HorizontalSpeed / model.MovementSpeed
                : 0f;
            presentation.SetMovementSpeed(normalizedMovementSpeed, Time.deltaTime);
        }

        private void UpdateAttack(bool attackRequested)
        {
            bool attackAllowed = networkPlayer == null || !networkPlayer.HasTorch;
            if (UnitAttackService.TryAttack(
                model,
                Time.deltaTime,
                attackAllowed && attackRequested))
            {
                FaceAimedTarget();
                presentation.PlayAttack();
            }
        }

        public void Teleport(Transform destination)
        {
            locomotion.Teleport(destination.position, destination.rotation);
            presentation.StopMovement();
        }

        public void StopHorizontalMovement()
        {
            locomotion.Stop();
            presentation.StopMovement();
        }

        public void ApplyAttackDamage()
        {
            if (model.IsDead ||
                networkPlayer != null && networkPlayer.HasTorch ||
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
            float healthBefore = model.CurrentHealth;
            vitals.TakeDamage(damage);
            if (model.CurrentHealth < healthBefore)
            {
                networkPlayer?.DropTorch();
            }
        }

        private void OnGUI()
        {
            localPresentation.Draw();
        }

        public void CompleteDeathAnimation()
        {
            vitals.CompleteDeathAnimation();
        }

        private void RefreshLocalRole()
        {
            bool isLocal = networkPlayer == null || networkPlayer.CanReadLocalInput;
            inputReader.SetEnabled(isLocal);
            localPresentation.SetCameraActive(
                isLocal &&
                SceneManager.GetActiveScene().name == NetworkSessionService.GameSceneName);
            if (networkPlayer != null && networkPlayer.CanRunSimulation)
            {
                vitals.Publish();
            }
        }

        private void HandleActiveSceneChanged(Scene _, Scene __)
        {
            RefreshLocalRole();
        }

        public void SetLocalInputEnabled(bool enabled)
        {
            if (networkPlayer == null || networkPlayer.CanReadLocalInput)
            {
                inputReader.SetEnabled(enabled);
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
