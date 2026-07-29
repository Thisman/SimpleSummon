using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class PlayerSettings : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float movementSpeed = 4f;
        [SerializeField, Min(0f)] private float jumpHeight = 1.5f;

        public float MovementSpeed => movementSpeed;
        public float JumpHeight => jumpHeight;
    }
}
