using SimpleSummon.Domain;
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

        [SerializeField] private PlayerController player;
        [SerializeField] private Animator animator;
        [SerializeField] private DamageFlash damageFlash;
        [SerializeField] private GameObject deathInteractionRoot;

        private EnemySettings settings;
        private NavMeshAgent agent;
        private UnitModel model;
        private Vector3 homePosition;
        private State state;

        public bool IsDead => model.IsDead;

        private void Awake()
        {
            settings = GetComponent<EnemySettings>();
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
            deathInteractionRoot.SetActive(false);
        }

        private void OnEnable()
        {
            player.Respawned += ForgetPlayer;
        }

        private void OnDisable()
        {
            player.Respawned -= ForgetPlayer;
        }

        private void Update()
        {
            if (state == State.Dead || !agent.isOnNavMesh)
            {
                return;
            }

            model.UpdateAttackCooldown(Time.deltaTime);

            float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
            float distanceFromHome = Vector3.Distance(transform.position, homePosition);

            if (player.IsDead || distanceFromHome > settings.ReturnRadius)
            {
                BeginReturn();
            }

            switch (state)
            {
                case State.Idle:
                    StopMoving();
                    if (!player.IsDead && distanceToPlayer <= settings.DetectionRadius)
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
            if (state != State.Attack || player.IsDead)
            {
                return;
            }

            if (Vector3.Distance(transform.position, player.transform.position) <= settings.AttackRadius)
            {
                player.TakeDamage(model.Damage);
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

            state = State.Dead;
            agent.isStopped = true;
            agent.enabled = false;
            GetComponent<CapsuleCollider>().enabled = false;
            animator.SetFloat(MovementSpeedId, 0f);
            animator.SetTrigger(DeathId);
        }

        public void CompleteDeathAnimation()
        {
            if (state == State.Dead)
            {
                deathInteractionRoot.SetActive(true);
            }
        }

        private void ForgetPlayer()
        {
            if (state != State.Dead)
            {
                BeginReturn();
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
