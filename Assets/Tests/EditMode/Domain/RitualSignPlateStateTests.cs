using NUnit.Framework;

namespace SimpleSummon.Domain.Tests
{
    public sealed class RitualSignPlateStateTests
    {
        [Test]
        public void Replace_AssignsOnlyOnePlatePerActor()
        {
            RitualSignPlateState state = new();

            state.Replace(new[]
            {
                new RitualSignPlateAssignment(10, 1),
                new RitualSignPlateAssignment(10, 4)
            });

            Assert.That(state.TryGetPlate(10, out int plateIndex), Is.True);
            Assert.That(plateIndex, Is.EqualTo(4));
            Assert.That(state.IsOccupied(1), Is.False);
            Assert.That(state.IsOccupied(4), Is.True);
        }

        [Test]
        public void Replace_CombinesDifferentPlayersPlates()
        {
            RitualSignPlateState state = new();

            state.Replace(new[]
            {
                new RitualSignPlateAssignment(10, 1),
                new RitualSignPlateAssignment(20, 7)
            });

            Assert.That(state.GetOccupiedMask(), Is.EqualTo((1 << 1) | (1 << 7)));
        }

        [Test]
        public void Replace_KeepsPlateOccupiedUntilLastActorLeaves()
        {
            RitualSignPlateState state = new();
            state.Replace(new[]
            {
                new RitualSignPlateAssignment(10, 3),
                new RitualSignPlateAssignment(20, 3)
            });

            state.Replace(new[] { new RitualSignPlateAssignment(20, 3) });

            Assert.That(state.IsOccupied(3), Is.True);
            Assert.That(state.TryGetPlate(10, out _), Is.False);
        }

        [Test]
        public void Replace_RemovesPlateWhenLastActorLeaves()
        {
            RitualSignPlateState state = new();
            state.Replace(new[] { new RitualSignPlateAssignment(10, 3) });

            state.Replace(System.Array.Empty<RitualSignPlateAssignment>());

            Assert.That(state.GetOccupiedMask(), Is.Zero);
        }

        [Test]
        public void Replace_IgnoresInvalidPlateIndices()
        {
            RitualSignPlateState state = new();

            state.Replace(new[]
            {
                new RitualSignPlateAssignment(10, -1),
                new RitualSignPlateAssignment(20, RitualSignPlateState.PlateCount)
            });

            Assert.That(state.Assignments, Is.Empty);
        }
    }
}
