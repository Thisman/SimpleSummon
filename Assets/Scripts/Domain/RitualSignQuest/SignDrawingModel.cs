using System.Collections.Generic;

namespace SimpleSummon.Domain
{
    public sealed class SignDrawingModel
    {
        private readonly List<SignStrokePoint> points = new();

        public SignDrawingState State { get; private set; }
        public ulong? OwnerId { get; private set; }
        public IReadOnlyList<SignStrokePoint> Points => points;

        public bool TryClaim(ulong actorId)
        {
            if (State != SignDrawingState.Available)
            {
                return false;
            }

            OwnerId = actorId;
            State = SignDrawingState.Claimed;
            return true;
        }

        public bool IsOwnedBy(ulong actorId) =>
            State == SignDrawingState.Claimed && OwnerId == actorId;

        public bool TryRelease(ulong actorId)
        {
            if (!IsOwnedBy(actorId))
            {
                return false;
            }

            OwnerId = null;
            State = SignDrawingState.Available;
            return true;
        }

        public bool TryFinish(ulong actorId)
        {
            if (!IsOwnedBy(actorId) || points.Count == 0)
            {
                return false;
            }

            OwnerId = null;
            State = SignDrawingState.Finished;
            return true;
        }

        public void Add(SignStrokePoint point) => points.Add(point);

        public void ReplacePoints(IEnumerable<SignStrokePoint> values)
        {
            points.Clear();
            points.AddRange(values);
        }
    }
}
