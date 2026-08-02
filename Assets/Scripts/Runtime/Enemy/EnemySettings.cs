using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class EnemySettings : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float movementSpeed = 3.5f;
        [SerializeField, Min(0f)] private float attackDelay = 1.25f;
        [SerializeField, Min(0f)] private float damage = 10f;
        [SerializeField, Min(0f)] private float maximumHealth = 50f;
        [SerializeField, Min(0f)] private float detectionRadius = 8f;
        [SerializeField, Min(0f)] private float attackRadius = 1.5f;
        [SerializeField, Min(0f)] private float returnRadius = 12f;
        [SerializeField] private bool boss;

        public float MovementSpeed => movementSpeed;
        public float AttackDelay => attackDelay;
        public float Damage => damage;
        public float MaximumHealth => maximumHealth;
        public float DetectionRadius => detectionRadius;
        public float AttackRadius => attackRadius;
        public float ReturnRadius => returnRadius;
        public bool IsBoss => boss;
    }
}
