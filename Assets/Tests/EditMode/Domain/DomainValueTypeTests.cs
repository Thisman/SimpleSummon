using System.Numerics;
using NUnit.Framework;

namespace SimpleSummon.Domain.Tests
{
    public sealed class DomainValueTypeTests
    {
        [Test]
        public void SignStrokePoint_StartStroke_PreservesPositionAndOriginalValue()
        {
            SignStrokePoint original = new(new Vector2(0.2f, 0.7f), false);

            SignStrokePoint started = original.StartStroke();

            Assert.That(started.Position, Is.EqualTo(original.Position));
            Assert.That(started.StartsStroke, Is.True);
            Assert.That(original.StartsStroke, Is.False);
        }

        [Test]
        public void RitualSignPlateAssignment_StoresActorAndPlate()
        {
            RitualSignPlateAssignment assignment = new(42, 8);

            Assert.That(assignment.ActorId, Is.EqualTo(42));
            Assert.That(assignment.PlateIndex, Is.EqualTo(8));
        }
    }
}
