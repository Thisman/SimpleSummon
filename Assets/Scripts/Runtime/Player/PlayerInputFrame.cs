using UnityEngine;

namespace SimpleSummon.Runtime
{
    internal readonly struct PlayerInputFrame
    {
        public PlayerInputFrame(
            Vector2 movement,
            bool jumpRequested,
            bool attackRequested)
        {
            Movement = movement;
            JumpRequested = jumpRequested;
            AttackRequested = attackRequested;
        }

        public Vector2 Movement { get; }
        public bool JumpRequested { get; }
        public bool AttackRequested { get; }
    }
}
