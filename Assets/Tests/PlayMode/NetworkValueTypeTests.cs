using NUnit.Framework;
using SimpleSummon.Network;
using UnityEngine;

namespace SimpleSummon.Tests.PlayMode
{
    public sealed class NetworkValueTypeTests
    {
        [Test]
        public void NetworkSignPoint_EqualityIncludesPositionAndStrokeBoundary()
        {
            NetworkSignPoint point = new(new Vector2(0.2f, 0.7f), true);

            Assert.That(point.Equals(new NetworkSignPoint(new Vector2(0.2f, 0.7f), true)), Is.True);
            Assert.That(point.Equals(new NetworkSignPoint(new Vector2(0.3f, 0.7f), true)), Is.False);
            Assert.That(point.Equals(new NetworkSignPoint(new Vector2(0.2f, 0.7f), false)), Is.False);
        }

        [Test]
        public void RitualPlateAssignment_EqualityIncludesActorAndPlate()
        {
            NetworkRitualSignPlateAssignment assignment = new(10, 3);

            Assert.That(assignment.Equals(new NetworkRitualSignPlateAssignment(10, 3)), Is.True);
            Assert.That(assignment.Equals(new NetworkRitualSignPlateAssignment(20, 3)), Is.False);
            Assert.That(assignment.Equals(new NetworkRitualSignPlateAssignment(10, 4)), Is.False);
        }
    }
}
