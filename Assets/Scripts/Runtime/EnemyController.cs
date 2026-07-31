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

        private EnemySettings settings;
        private NavMeshAgent agent;
        private UnitModel model;
        private Vector3 homePosition;
        private State state;
        private PlayerController player;
        private NetworkEnemyState networkState;

        public bool IsDead => model.IsDead;

        private void Awake()
        {
            settings = GetComponent<EnemySettings>();
            networkState = GetComponent<NetworkEnemyState>();
            agent = GetComponent<NavMeshAgent>();
            model = new UnitModel(
                settings.MovementSpeed,
                0f,
                settings.AttackDelay,
                settings.Damage,
                settings.MaximumHealth);

            homePosition = transform.position;
            agent.speed = model.MovementSpeed;
            agent.stoppingDistance = settings.AttackRadius;
        }

        private void OnEnable()
        {
            if (networkState != null)
            {
                networkState.StateChanged += ApplyReplicatedState;
            }
            TrySelectPlayer();
        }

        private void OnDisable()
        {
            if (networkState != null)
            {
                networkState.StateChanged -= ApplyReplicatedState;
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

            if (player == null || player.IsDead || distanceFromHome > settings.ReturnRadius)
            {
                BeginReturn();
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
                    if (distanceToPlayer <= settings.AttackRadius)
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
                    if (distanceToPlayer > settings.AttackRadius)
                    {
                        state = State.Chase;
                    }
                    else if (model.TryAttack())
                    {
                        animator.SetTrigger(AttackId);
                    }
                    break;

                case State.Return:
                    if (Vector3.Distance(transform.position, homePosition) <= agent.stoppingDistance)
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

            if (direction.sqrMagnitude <= settings.AttackRadius * settings.AttackRadius &&
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

            networkState?.Publish(true);
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
            Gizmos.DrawWireSphere(transform.position, currentSettings.AttackRadius);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(origin, currentSettings.ReturnRadius);
        }
    }
}
