using System.Collections.Generic;
using SimpleSummon.Domain;

namespace SimpleSummon.Application
{
    public readonly struct SubmitSummonPointsCommand
    {
        public SubmitSummonPointsCommand(
            ulong actorId,
            IReadOnlyList<SummonStrokePoint> points)
        {
            ActorId = actorId;
            Points = points;
        }

        public ulong ActorId { get; }
        public IReadOnlyList<SummonStrokePoint> Points { get; }
    }
}
