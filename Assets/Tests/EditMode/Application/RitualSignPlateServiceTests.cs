using NUnit.Framework;
using SimpleSummon.Application;
using SimpleSummon.Domain;

namespace SimpleSummon.Tests.Application
{
    public sealed class RitualSignPlateServiceTests
    {
        [Test]
        public void ReplaceOccupancy_ChangesDomainStateOnlyWhenAssignmentsChange()
        {
            RitualSignPlateState state = new();
            RitualSignPlateService service = new(state);
            RitualSignPlateAssignment[] assignments =
            {
                new(1, 2),
                new(2, 5)
            };

            Assert.That(service.ReplaceOccupancy(assignments), Is.True);
            Assert.That(service.ReplaceOccupancy(assignments), Is.False);
            Assert.That(state.GetOccupiedMask(), Is.EqualTo((1 << 2) | (1 << 5)));
        }
    }
}
