using NUnit.Framework;
using SimpleSummon.Domain;
using System.Numerics;

namespace SimpleSummon.Tests.Domain
{
    public sealed class SummonRitualModelTests
    {
        [Test]
        public void Finish_RequiresOwnerAndPoint()
        {
            SummonRitualModel model = new();
            Assert.That(model.TryClaim(7), Is.True);
            Assert.That(model.TryFinish(7), Is.False);
            model.Add(new SummonStrokePoint(Vector2.Zero, true));
            Assert.That(model.TryFinish(8), Is.False);
            Assert.That(model.TryFinish(7), Is.True);
            Assert.That(model.State, Is.EqualTo(SummonRitualState.Finished));
        }
    }
}
