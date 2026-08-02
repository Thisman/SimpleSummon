using System;
using System.Collections.Generic;
using SimpleSummon.Domain;

namespace SimpleSummon.Application
{
    public sealed class RitualSignPlateService
    {
        private readonly RitualSignPlateState state;

        public RitualSignPlateService(RitualSignPlateState state)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public bool ReplaceOccupancy(IReadOnlyList<RitualSignPlateAssignment> assignments) =>
            state.Replace(assignments);
    }
}
