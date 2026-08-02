using NUnit.Framework;
using SimpleSummon.Domain;
using System.Numerics;

namespace SimpleSummon.Tests.Domain
{
    public sealed class SignDrawingModelTests
    {
        [Test]
        public void Claim_AvailableDrawing_AssignsOwner()
        {
            SignDrawingModel model = new();

            Assert.That(model.TryClaim(7), Is.True);
            Assert.That(model.State, Is.EqualTo(SignDrawingState.Claimed));
            Assert.That(model.OwnerId, Is.EqualTo(7));
            Assert.That(model.IsOwnedBy(7), Is.True);
        }

        [Test]
        public void Claim_WhenAlreadyClaimed_IsRejectedForEveryActor()
        {
            SignDrawingModel model = ClaimedDrawing();

            Assert.That(model.TryClaim(7), Is.False);
            Assert.That(model.TryClaim(8), Is.False);
            Assert.That(model.OwnerId, Is.EqualTo(7));
        }

        [Test]
        public void Release_ByOwner_MakesDrawingAvailableAndPreservesPoints()
        {
            SignDrawingModel model = ClaimedDrawing();
            model.Add(new SignStrokePoint(Vector2.Zero, true));

            Assert.That(model.TryRelease(7), Is.True);
            Assert.That(model.State, Is.EqualTo(SignDrawingState.Available));
            Assert.That(model.OwnerId, Is.Null);
            Assert.That(model.Points, Has.Count.EqualTo(1));
        }

        [TestCase(8ul)]
        [TestCase(ulong.MaxValue)]
        public void Release_ByNonOwner_IsRejected(ulong actorId)
        {
            SignDrawingModel model = ClaimedDrawing();

            Assert.That(model.TryRelease(actorId), Is.False);
            Assert.That(model.IsOwnedBy(7), Is.True);
        }

        [Test]
        public void Finish_RequiresPointAndOwner()
        {
            SignDrawingModel model = ClaimedDrawing();

            Assert.That(model.TryFinish(7), Is.False);
            model.Add(new SignStrokePoint(Vector2.Zero, true));
            Assert.That(model.TryFinish(8), Is.False);
            Assert.That(model.TryFinish(7), Is.True);
            Assert.That(model.State, Is.EqualTo(SignDrawingState.Finished));
            Assert.That(model.OwnerId, Is.Null);
        }

        [Test]
        public void FinishedDrawing_RejectsFurtherLifecycleTransitions()
        {
            SignDrawingModel model = ClaimedDrawing();
            model.Add(new SignStrokePoint(Vector2.Zero, true));
            model.TryFinish(7);

            Assert.That(model.TryClaim(7), Is.False);
            Assert.That(model.TryRelease(7), Is.False);
            Assert.That(model.TryFinish(7), Is.False);
            Assert.That(model.Points, Has.Count.EqualTo(1));
        }

        [Test]
        public void Add_AppendsPointsInOrder()
        {
            SignDrawingModel model = new();
            SignStrokePoint first = new(new Vector2(0.1f, 0.2f), true);
            SignStrokePoint second = new(new Vector2(0.3f, 0.4f), false);

            model.Add(first);
            model.Add(second);

            Assert.That(model.Points, Has.Count.EqualTo(2));
            Assert.That(model.Points[0].Position, Is.EqualTo(first.Position));
            Assert.That(model.Points[1].Position, Is.EqualTo(second.Position));
        }

        [Test]
        public void ReplacePoints_ReplacesExistingPointsAndCanClear()
        {
            SignDrawingModel model = new();
            model.Add(new SignStrokePoint(Vector2.Zero, true));

            model.ReplacePoints(new[]
            {
                new SignStrokePoint(Vector2.One, true),
                new SignStrokePoint(Vector2.UnitX, false)
            });

            Assert.That(model.Points, Has.Count.EqualTo(2));
            Assert.That(model.Points[0].Position, Is.EqualTo(Vector2.One));

            model.ReplacePoints(System.Array.Empty<SignStrokePoint>());
            Assert.That(model.Points, Is.Empty);
        }

        [Test]
        public void ReplacePoints_Null_ThrowsAndPreservesExistingPoints()
        {
            SignDrawingModel model = new();
            model.Add(new SignStrokePoint(Vector2.Zero, true));

            Assert.Throws<System.ArgumentNullException>(() => model.ReplacePoints(null));
            Assert.That(model.Points, Has.Count.EqualTo(1));
        }

        private static SignDrawingModel ClaimedDrawing()
        {
            SignDrawingModel model = new();
            model.TryClaim(7);
            return model;
        }
    }
}
