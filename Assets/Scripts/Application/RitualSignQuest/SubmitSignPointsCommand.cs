using System.Collections.Generic;
using SimpleSummon.Domain;

namespace SimpleSummon.Application
{
    public readonly struct SubmitSignPointsCommand
    {
        public SubmitSignPointsCommand(
            ulong actorId,
            IReadOnlyList<SignStrokePoint> points)
        {
            ActorId = actorId;
            Points = points;
        }

        public ulong ActorId { get; }
        public IReadOnlyList<SignStrokePoint> Points { get; }
    }
}
