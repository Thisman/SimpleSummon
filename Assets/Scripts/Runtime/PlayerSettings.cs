using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class PlayerSettings : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float movementSpeed = 4f;
        [SerializeField, Min(0f)] private float jumpHeight = 1.5f;
        [SerializeField, Min(0f)] private float attackDelay = 0.75f;
        [SerializeField, Min(0f)] private float damage = 25f;
        [SerializeField, Min(0f)] private float maximumHealth = 100f;
        [SerializeField, Min(0f)] private float attackRange = 2.5f;
        [SerializeField, Min(0f)] private float aimRayDistance = 100f;
        [SerializeField] private LayerMask attackMask = ~0;

        public float MovementSpeed => movementSpeed;
        public float JumpHeight => jumpHeight;
        public float AttackDelay => attackDelay;
        public float Damage => damage;
        public float MaximumHealth => maximumHealth;
        public float AttackRange => attackRange;
        public float AimRayDistance => aimRayDistance;
        public LayerMask AttackMask => attackMask;
    }
}
