using SimpleSummon.Domain;
using SimpleSummon.Application;
using SimpleSummon.Network;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace SimpleSummon.Runtime
{
    [RequireComponent(typeof(EnemySettings))]
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class EnemyController : MonoBehaviour, IDamageable
    {
        private const float MinimumAttackDirectionDot = 0.70710678f;

        [SerializeField] private Animator animator;
        [SerializeField] private DamageFlash damageFlash;
        [SerializeField] private Renderer[] visualRenderers;
        [SerializeField] private EnemyLootCollectable loot;
        [SerializeField] private NetworkQuestState questState;

        private EnemySettings settings;
        private NavMeshAgent agent;
        private UnitModel model;
        private Vector3 homePosition;
        private EnemyBehaviorState state;
        private NetworkEnemyState networkState;
        private bool bossWeakened;
        private EnemyTargetTracker targetTracker;
        private EnemyNavigation navigation;
        private EnemyPresentation presentation;
        private EnemyBossProgression bossProgression;
        private EnemyReplicationPresenter replicationPresenter;

        public bool IsDead => model.IsDead;
        private float EffectiveAttackRadius => settings.AttackRadius *
                                               Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);

        private void Awake()
        {
            settings = GetComponent<EnemySettings>();
            networkState = GetComponent<NetworkEnemyState>();
            agent = GetComponent<NavMeshAgent>();
            bossProgression = new EnemyBossProgression(settings, questState);
            float statMultiplier = bossProgression.InitialStatMultiplier;
            bossWeakened = bossProgression.IsInitiallyWeakened;
            model = EnemyCombatService.Create(
                settings.MovementSpeed,
                settings.AttackDelay,
                settings.Damage,
                settings.MaximumHealth,
                statMultiplier);

            homePosition = transform.position;
            agent.speed = model.MovementSpeed;
            agent.stoppingDistance = EffectiveAttackRadius;
            targetTracker = new EnemyTargetTracker(transform, ForgetTarget);
            navigation = new EnemyNavigation(transform, agent);
            presentation = new EnemyPresentation(
                animator,
                damageFlash,
                visualRenderers,
                GetComponent<CapsuleCollider>());
            replicationPresenter = new EnemyReplicationPresenter(
                networkState,
                navigation,
                presentation);
        }

        private void OnEnable()
        {
            replicationPresenter.Enable(MarkReplicatedDead);
            bossProgression.Enable(ApplyArtifactWeakening);
            targetTracker.Refresh();
        }

        private void OnDisable()
        {
            replicationPresenter.Disable();
            bossProgression.Disable();
            targetTracker.Clear();
        }

        private void Update()
        {
            if (NetworkManager.Singleton != null &&
                NetworkManager.Singleton.IsListening &&
                !NetworkManager.Singleton.IsServer)
            {
                return;
            }

            if (state == EnemyBehaviorState.Dead || !navigation.IsReady)
            {
                return;
            }

            targetTracker.Refresh();
            model.UpdateAttackCooldown(Time.deltaTime);

            PlayerController player = targetTracker.Current;

            float distanceToPlayer = player != null
                ? Vector3.Distance(transform.position, player.transform.position)
                : float.PositiveInfinity;
            float distanceFromHome = Vector3.Distance(transform.position, homePosition);

            bool hasLivingTarget = player != null && !player.IsDead;
            state = EnemyDecisionService.Decide(new EnemyDecisionContext(
                state,
                hasLivingTarget,
                distanceToPlayer,
                distanceFromHome,
                settings.DetectionRadius,
                EffectiveAttackRadius,
                settings.ReturnRadius,
                navigation.StoppingDistance));

            switch (state)
            {
                case EnemyBehaviorState.Idle:
                    navigation.Stop();
                    break;

                case EnemyBehaviorState.Chase:
                    navigation.MoveTo(player.transform.position);
                    break;

                case EnemyBehaviorState.Attack:
                    navigation.Stop();
                    navigation.Face(player.transform.position);
                    if (model.TryAttack())
                    {
                        presentation.PlayAttack();
                    }
                    break;

                case EnemyBehaviorState.Return:
                    navigation.MoveTo(homePosition);
                    break;
            }

            float normalizedSpeed = model.MovementSpeed > 0f
                ? navigation.Speed / model.MovementSpeed
                : 0f;
            presentation.SetMovementSpeed(normalizedSpeed, Time.deltaTime);
        }

        public void ApplyAttackDamage()
        {
            if (NetworkManager.Singleton != null &&
                NetworkManager.Singleton.IsListening &&
                !NetworkManager.Singleton.IsServer)
            {
                return;
            }

            PlayerController player = targetTracker.Current;
            if (state != EnemyBehaviorState.Attack || player == null || player.IsDead)
            {
                return;
            }

            Vector3 direction = player.transform.position - transform.position;
            direction.y = 0f;

            float attackRadius = EffectiveAttackRadius;
            if (direction.sqrMagnitude <= attackRadius * attackRadius &&
                direction.sqrMagnitude > 0f &&
                Vector3.Dot(transform.forward, direction.normalized) >= MinimumAttackDirectionDot)
            {
                player.TakeDamage(model.Damage);
            }
        }

        public void TakeDamage(float damage)
        {
            if (NetworkManager.Singleton != null &&
                NetworkManager.Singleton.IsListening &&
                !NetworkManager.Singleton.IsServer)
            {
                return;
            }

            if (model.IsDead)
            {
                return;
            }

            bool died = EnemyCombatService.TakeDamage(model, damage);
            presentation.PlayDamage();
            if (!died)
            {
                return;
            }

            state = EnemyBehaviorState.Dead;
            navigation.Disable();
            presentation.PlayDeath();
            networkState?.Publish(true);
        }

        public void CompleteDeathAnimation()
        {
            if (NetworkManager.Singleton != null &&
                NetworkManager.Singleton.IsListening &&
                !NetworkManager.Singleton.IsServer)
            {
                return;
            }

            bossProgression.CollectHeart();

            presentation.Hide();

            networkState?.PublishDeathCompleted(!settings.IsBoss && loot != null);
            loot?.RefreshVisibility();
        }

        private void MarkReplicatedDead() => state = EnemyBehaviorState.Dead;

        private void ApplyArtifactWeakening()
        {
            if (!bossProgression.CanApplyWeakening(model, bossWeakened))
            {
                return;
            }

            model = EnemyCombatService.RemoveStatMultiplier(
                model,
                settings.MovementSpeed,
                settings.AttackDelay,
                settings.Damage,
                settings.MaximumHealth);
            bossWeakened = true;
        }

        private void ForgetTarget()
        {
            if (state != EnemyBehaviorState.Dead)
            {
                state = EnemyBehaviorState.Return;
            }
        }

    }
}
