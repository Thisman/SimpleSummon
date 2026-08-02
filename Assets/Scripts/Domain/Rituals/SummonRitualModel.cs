using System.Collections.Generic;

namespace SimpleSummon.Domain
{
    public sealed class SummonRitualModel
    {
        private readonly List<SummonStrokePoint> points = new();

        public SummonRitualState State { get; private set; }
        public ulong? OwnerId { get; private set; }
        public IReadOnlyList<SummonStrokePoint> Points => points;

        public bool TryClaim(ulong actorId)
        {
            if (State != SummonRitualState.Available)
            {
                return false;
            }

            OwnerId = actorId;
            State = SummonRitualState.Claimed;
            return true;
        }

        public bool IsOwnedBy(ulong actorId) =>
            State == SummonRitualState.Claimed && OwnerId == actorId;

        public bool TryRelease(ulong actorId)
        {
            if (!IsOwnedBy(actorId))
            {
                return false;
            }

            OwnerId = null;
            State = SummonRitualState.Available;
            return true;
        }

        public bool TryFinish(ulong actorId)
        {
            if (!IsOwnedBy(actorId) || points.Count == 0)
            {
                return false;
            }

            OwnerId = null;
            State = SummonRitualState.Finished;
            return true;
        }

        public void Add(SummonStrokePoint point) => points.Add(point);

        public void ReplacePoints(IEnumerable<SummonStrokePoint> values)
        {
            points.Clear();
            points.AddRange(values);
        }
    }
}
