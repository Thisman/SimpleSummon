using NUnit.Framework;
using SimpleSummon.Application;
using SimpleSummon.Domain;
using System.Numerics;

namespace SimpleSummon.Tests.Application
{
    public sealed class SignDrawingServiceTests
    {
        [Test]
        public void SubmitAndErase_KeepStrokeBoundary()
        {
            SignDrawingModel model = new();
            SignDrawingService service = new(model);
            service.Claim(1);
            service.Submit(new SubmitSignPointsCommand(1, new[]
            {
                new SignStrokePoint(new Vector2(0.1f, 0.1f), true),
                new SignStrokePoint(new Vector2(0.5f, 0.5f), false),
                new SignStrokePoint(new Vector2(0.9f, 0.9f), false)
            }));

            Assert.That(service.Erase(new EraseSignPointsCommand(
                1,
                new Vector2(0.5f, 0.5f),
                0.05f)), Is.True);
            Assert.That(model.Points, Has.Count.EqualTo(2));
            Assert.That(model.Points[1].StartsStroke, Is.True);
        }
    }
}
