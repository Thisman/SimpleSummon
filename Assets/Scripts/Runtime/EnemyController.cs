using SimpleSummon.Domain;
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

        private enum State
        {
            Idle,
            Chase,
            Attack,
            Return,
            Dead
        }

        private static readonly int MovementSpeedId = Animator.StringToHash("MovementSpeed");
        private static readonly int AttackId = Animator.StringToHash("Attack");
        private static readonly int DeathId = Animator.StringToHash("Death");

        [SerializeField] private Animator animator;
        [SerializeField] private DamageFlash damageFlash;
        [SerializeField] private Renderer[] visualRenderers;
        [SerializeField] private EnemyLootCollectable loot;
        [SerializeField] private NetworkQuestState questState;

        private EnemySettings settings;
        private NavMeshAgent agent;
        private UnitModel model;
        private Vector3 homePosition;
        private State state;
        private PlayerController player;
        private NetworkEnemyState networkState;
        private bool bossWeakened;

        public bool IsDead => model.IsDead;
        private float EffectiveAttackRadius => settings.AttackRadius *
                                               Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);

        private void Awake()
        {
            settings = GetComponent<EnemySettings>();
            networkState = GetComponent<NetworkEnemyState>();
            agent = GetComponent<NavMeshAgent>();
            float statMultiplier = settings.IsBoss &&
                                   (questState == null || !questState.ArtifactCrafted)
                ? settings.BossStatMultiplier
                : 1f;
            bossWeakened = settings.IsBoss && statMultiplier <= 1f;
            model = new UnitModel(
                settings.MovementSpeed,
                0f,
                settings.AttackDelay,
                settings.Damage * statMultiplier,
                settings.MaximumHealth * statMultiplier);

            homePosition = transform.position;
            agent.speed = model.MovementSpeed;
            agent.stoppingDistance = EffectiveAttackRadius;
        }

        private void OnEnable()
        {
            if (networkState != null)
            {
                networkState.StateChanged += ApplyReplicatedState;
                networkState.LootStateChanged += ApplyReplicatedLootState;
            }
            if (settings.IsBoss && questState != null)
            {
                questState.Changed += ApplyArtifactWeakening;
                ApplyArtifactWeakening();
            }
            TrySelectPlayer();
        }

        private void OnDisable()
        {
            if (networkState != null)
            {
                networkState.StateChanged -= ApplyReplicatedState;
                networkState.LootStateChanged -= ApplyReplicatedLootState;
            }
            if (questState != null)
            {
                questState.Changed -= ApplyArtifactWeakening;
            }
            SetPlayer(null);
        }

        private void Update()
        {
            if (NetworkManager.Singleton != null &&
                NetworkManager.Singleton.IsListening &&
                !NetworkManager.Singleton.IsServer)
            {
                return;
            }

            if (state == State.Dead || !agent.isOnNavMesh)
            {
                return;
            }

            TrySelectPlayer();
            model.UpdateAttackCooldown(Time.deltaTime);

            float distanceToPlayer = player != null
                ? Vector3.Distance(transform.position, player.transform.position)
                : float.PositiveInfinity;
            float distanceFromHome = Vector3.Distance(transform.position, homePosition);

            bool hasLivingTarget = player != null && !player.IsDead;
            if (distanceFromHome > settings.ReturnRadius ||
                !hasLivingTarget && distanceFromHome > agent.stoppingDistance)
            {
                BeginReturn();
            }
            else if (!hasLivingTarget && state != State.Idle)
            {
                state = State.Idle;
                StopMoving();
            }

            switch (state)
            {
                case State.Idle:
                    StopMoving();
                    if (player != null &&
                        !player.IsDead &&
                        distanceToPlayer <= settings.DetectionRadius)
                    {
                        state = State.Chase;
                    }
                    break;

                case State.Chase:
                    if (distanceToPlayer <= EffectiveAttackRadius)
                    {
                        state = State.Attack;
                        StopMoving();
                    }
                    else
                    {
                        MoveTo(player.transform.position);
                    }
                    break;

                case State.Attack:
                    FacePlayer();
                    if (distanceToPlayer > EffectiveAttackRadius)
                    {
                        state = State.Chase;
                    }
                    else if (model.TryAttack())
                    {
                        animator.SetTrigger(AttackId);
                    }
                    break;

                case State.Return:
                    if (hasLivingTarget &&
                        distanceFromHome <= settings.ReturnRadius &&
                        distanceToPlayer <= settings.DetectionRadius)
                    {
                        state = State.Chase;
                    }
                    else if (distanceFromHome <= agent.stoppingDistance)
                    {
                        state = State.Idle;
                        StopMoving();
                    }
                    else
                    {
                        MoveTo(homePosition);
                    }
                    break;
            }

            float normalizedSpeed = model.MovementSpeed > 0f
                ? agent.velocity.magnitude / model.MovementSpeed
                : 0f;
            animator.SetFloat(MovementSpeedId, normalizedSpeed, 0.1f, Time.deltaTime);
        }

        public void ApplyAttackDamage()
        {
            if (NetworkManager.Singleton != null &&
                NetworkManager.Singleton.IsListening &&
                !NetworkManager.Singleton.IsServer)
            {
                return;
            }

            if (state != State.Attack || player == null || player.IsDead)
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

            model.TakeDamage(damage);
            damageFlash.Play();
            if (!model.IsDead)
            {
                return;
            }

            state = State.Dead;
            agent.isStopped = true;
            agent.enabled = false;
            GetComponent<CapsuleCollider>().enabled = false;
            animator.SetFloat(MovementSpeedId, 0f);
            animator.SetTrigger(DeathId);
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

            if (settings.IsBoss)
            {
                questState?.Collect(QuestCollectableType.BossHeart, 0);
            }

            foreach (Renderer visualRenderer in visualRenderers)
            {
                if (visualRenderer != null)
                {
                    visualRenderer.enabled = false;
                }
            }

            networkState?.PublishDeathCompleted(!settings.IsBoss && loot != null);
            loot?.RefreshVisibility();
        }

        private void ApplyReplicatedState(bool isDead)
        {
            if (networkState == null || networkState.IsServer)
            {
                return;
            }

            if (isDead && state != State.Dead)
            {
                state = State.Dead;
                agent.enabled = false;
                GetComponent<CapsuleCollider>().enabled = false;
                animator.SetFloat(MovementSpeedId, 0f);
                animator.SetTrigger(DeathId);
            }

            if (networkState.Disappeared)
            {
                HideVisuals();
            }
        }

        private void ApplyReplicatedLootState()
        {
            if (networkState != null && networkState.Disappeared)
            {
                HideVisuals();
            }
        }

        private void ApplyArtifactWeakening()
        {
            if (!settings.IsBoss || bossWeakened || !questState.ArtifactCrafted || model.IsDead)
            {
                return;
            }

            float currentHealth = model.CurrentHealth;
            model = new UnitModel(
                settings.MovementSpeed,
                0f,
                settings.AttackDelay,
                settings.Damage,
                settings.MaximumHealth);
            model.SetCurrentHealth(Mathf.Min(currentHealth, model.MaximumHealth));
            bossWeakened = true;
        }

        private void HideVisuals()
        {
            foreach (Renderer visualRenderer in visualRenderers)
            {
                if (visualRenderer != null)
                {
                    visualRenderer.enabled = false;
                }
            }
        }

        private void ForgetPlayer()
        {
            if (state != State.Dead)
            {
                BeginReturn();
            }
        }

        private void TrySelectPlayer()
        {
            PlayerController closest = PlayerRegistry.GetClosestLiving(transform.position);
            if (closest != player)
            {
                SetPlayer(closest);
            }
        }

        private void SetPlayer(PlayerController value)
        {
            if (player != null)
            {
                player.Respawned -= ForgetPlayer;
            }

            player = value;
            if (player != null)
            {
                player.Respawned += ForgetPlayer;
            }
        }

        private void BeginReturn()
        {
            if (state == State.Return || state == State.Dead)
            {
                return;
            }

            state = State.Return;
            MoveTo(homePosition);
        }

        private void MoveTo(Vector3 destination)
        {
            agent.isStopped = false;
            agent.SetDestination(destination);
        }

        private void StopMoving()
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        private void FacePlayer()
        {
            Vector3 direction = player.transform.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        private void OnDrawGizmosSelected()
        {
            EnemySettings currentSettings = settings != null ? settings : GetComponent<EnemySettings>();
            if (currentSettings == null)
            {
                return;
            }

            Vector3 origin = UnityEngine.Application.isPlaying ? homePosition : transform.position;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(origin, currentSettings.DetectionRadius);
            Gizmos.color = Color.red;
            float scale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
            Gizmos.DrawWireSphere(transform.position, currentSettings.AttackRadius * scale);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(origin, currentSettings.ReturnRadius);
        }
    }
}
