using System.Numerics;

namespace SimpleSummon.Application
{
    public readonly struct EraseSummonPointsCommand
    {
        public EraseSummonPointsCommand(ulong actorId, Vector2 position, float radius)
        {
            ActorId = actorId;
            Position = position;
            Radius = radius;
        }

        public ulong ActorId { get; }
        public Vector2 Position { get; }
        public float Radius { get; }
    }
}
