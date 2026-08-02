using NUnit.Framework;
using SimpleSummon.Application;
using SimpleSummon.Domain;
using System.Numerics;

namespace SimpleSummon.Tests.Application
{
    public sealed class SummonRitualServiceTests
    {
        [Test]
        public void SubmitAndErase_KeepStrokeBoundary()
        {
            SummonRitualModel model = new();
            SummonRitualService service = new(model);
            service.Claim(1);
            service.Submit(new SubmitSummonPointsCommand(1, new[]
            {
                new SummonStrokePoint(new Vector2(0.1f, 0.1f), true),
                new SummonStrokePoint(new Vector2(0.5f, 0.5f), false),
                new SummonStrokePoint(new Vector2(0.9f, 0.9f), false)
            }));

            Assert.That(service.Erase(new EraseSummonPointsCommand(
                1,
                new Vector2(0.5f, 0.5f),
                0.05f)), Is.True);
            Assert.That(model.Points, Has.Count.EqualTo(2));
            Assert.That(model.Points[1].StartsStroke, Is.True);
        }
    }
}
