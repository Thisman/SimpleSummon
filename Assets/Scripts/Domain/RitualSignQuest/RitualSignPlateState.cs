using System.Collections.Generic;

namespace SimpleSummon.Domain
{
    public sealed class RitualSignPlateState
    {
        public const int PlateCount = 9;

        private readonly Dictionary<ulong, int> assignments = new();

        public IReadOnlyDictionary<ulong, int> Assignments => assignments;

        public bool Replace(IReadOnlyList<RitualSignPlateAssignment> nextAssignments)
        {
            Dictionary<ulong, int> next = new();
            if (nextAssignments != null)
            {
                for (int i = 0; i < nextAssignments.Count; i++)
                {
                    RitualSignPlateAssignment assignment = nextAssignments[i];
                    if (assignment.PlateIndex >= 0 && assignment.PlateIndex < PlateCount)
                    {
                        next[assignment.ActorId] = assignment.PlateIndex;
                    }
                }
            }

            if (Matches(next))
            {
                return false;
            }

            assignments.Clear();
            foreach (KeyValuePair<ulong, int> assignment in next)
            {
                assignments.Add(assignment.Key, assignment.Value);
            }
            return true;
        }

        public bool TryGetPlate(ulong actorId, out int plateIndex) =>
            assignments.TryGetValue(actorId, out plateIndex);

        public bool IsOccupied(int plateIndex)
        {
            foreach (int assignedPlate in assignments.Values)
            {
                if (assignedPlate == plateIndex)
                {
                    return true;
                }
            }
            return false;
        }

        public ushort GetOccupiedMask()
        {
            ushort mask = 0;
            foreach (int plateIndex in assignments.Values)
            {
                mask |= (ushort)(1 << plateIndex);
            }
            return mask;
        }

        private bool Matches(Dictionary<ulong, int> other)
        {
            if (assignments.Count != other.Count)
            {
                return false;
            }

            foreach (KeyValuePair<ulong, int> assignment in assignments)
            {
                if (!other.TryGetValue(assignment.Key, out int plateIndex) ||
                    plateIndex != assignment.Value)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
