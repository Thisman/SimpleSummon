using System;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using SimpleSummon.Domain;

namespace SimpleSummon.Application.Tests
{
    public sealed class SignDrawingServiceTests
    {
        [Test]
        public void Constructor_NullModel_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new SignDrawingService(null));
        }

        [Test]
        public void Lifecycle_DelegatesOwnershipAndFinishRules()
        {
            SignDrawingModel model = new();
            SignDrawingService service = new(model);

            Assert.That(service.Claim(1), Is.True);
            Assert.That(service.Claim(2), Is.False);
            Assert.That(service.Finish(1), Is.False);
            Assert.That(service.Release(2), Is.False);
            Assert.That(service.Release(1), Is.True);
        }

        [Test]
        public void Submit_ByNonOwnerOrWithNullOrEmptyPoints_ReturnsFalse()
        {
            SignDrawingModel model = ClaimedModel();
            SignDrawingService service = new(model);

            Assert.That(service.Submit(new SubmitSignPointsCommand(2, OnePoint())), Is.False);
            Assert.That(service.Submit(new SubmitSignPointsCommand(1, null)), Is.False);
            Assert.That(service.Submit(new SubmitSignPointsCommand(
                1,
                Array.Empty<SignStrokePoint>())), Is.False);
            Assert.That(model.Points, Is.Empty);
        }

        [Test]
        public void Submit_ValidatesClampsAndSkipsOnlyInvalidPoints()
        {
            SignDrawingModel model = ClaimedModel();
            SignDrawingService service = new(model);

            bool changed = service.Submit(new SubmitSignPointsCommand(1, new[]
            {
                new SignStrokePoint(new Vector2(-1f, 2f), true),
                new SignStrokePoint(new Vector2(float.NaN, 0.5f), false),
                new SignStrokePoint(new Vector2(0f, 1f), false),
                new SignStrokePoint(new Vector2(0f, 1f), true)
            }));

            Assert.That(changed, Is.True);
            Assert.That(model.Points, Has.Count.EqualTo(2));
            Assert.That(model.Points[0].Position, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(model.Points[1].StartsStroke, Is.True);
        }

        [Test]
        public void Submit_LimitsEachBatchButDoesNotLimitTotalDrawing()
        {
            SignDrawingModel model = ClaimedModel();
            SignDrawingService service = new(model);
            SignStrokePoint[] first = Enumerable.Range(0, SignDrawingService.MaximumBatchSize + 5)
                .Select(index => Point(index, true))
                .ToArray();
            SignStrokePoint[] second = Enumerable.Range(0, SignDrawingService.MaximumBatchSize)
                .Select(index => Point(index + 100, true))
                .ToArray();

            Assert.That(service.Submit(new SubmitSignPointsCommand(1, first)), Is.True);
            Assert.That(model.Points, Has.Count.EqualTo(SignDrawingService.MaximumBatchSize));
            Assert.That(service.Submit(new SubmitSignPointsCommand(1, second)), Is.True);
            Assert.That(model.Points, Has.Count.EqualTo(SignDrawingService.MaximumBatchSize * 2));
        }

        [Test]
        public void Submit_ManyBatches_PreservesUnlimitedCenterlineOrder()
        {
            SignDrawingModel model = ClaimedModel();
            SignDrawingService service = new(model);
            const int batchCount = 100;

            for (int batch = 0; batch < batchCount; batch++)
            {
                SignStrokePoint[] points = Enumerable.Range(0, SignDrawingService.MaximumBatchSize)
                    .Select(index => Point(batch * SignDrawingService.MaximumBatchSize + index, true))
                    .ToArray();
                Assert.That(service.Submit(new SubmitSignPointsCommand(1, points)), Is.True);
            }

            Assert.That(
                model.Points,
                Has.Count.EqualTo(batchCount * SignDrawingService.MaximumBatchSize));
        }

        [Test]
        public void Submit_AfterFinish_IsRejected()
        {
            SignDrawingModel model = ClaimedModel();
            SignDrawingService service = new(model);
            service.Submit(new SubmitSignPointsCommand(1, OnePoint()));
            service.Finish(1);

            Assert.That(service.Submit(new SubmitSignPointsCommand(1, OnePoint())), Is.False);
            Assert.That(model.Points, Has.Count.EqualTo(1));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void Erase_InvalidRadius_IsRejected(float radius)
        {
            SignDrawingModel model = ModelWithLine();
            SignDrawingService service = new(model);

            Assert.That(service.Erase(new EraseSignPointsCommand(1, Vector2.Zero, radius)), Is.False);
            Assert.That(model.Points, Has.Count.EqualTo(3));
        }

        [Test]
        public void Erase_ByNonOwnerOrOutsideDrawing_PreservesPoints()
        {
            SignDrawingModel model = ModelWithLine();
            SignDrawingService service = new(model);

            Assert.That(service.Erase(new EraseSignPointsCommand(2, new Vector2(0.5f), 0.1f)), Is.False);
            Assert.That(service.Erase(new EraseSignPointsCommand(1, Vector2.One, 0.01f)), Is.False);
            Assert.That(model.Points, Has.Count.EqualTo(3));
        }

        [Test]
        public void Erase_MiddlePoint_StartsFollowingStroke()
        {
            SignDrawingModel model = ModelWithLine();
            SignDrawingService service = new(model);

            Assert.That(service.Erase(new EraseSignPointsCommand(
                1,
                new Vector2(0.5f, 0.5f),
                0.05f)), Is.True);
            Assert.That(model.Points, Has.Count.EqualTo(2));
            Assert.That(model.Points[1].StartsStroke, Is.True);
        }

        [Test]
        public void Erase_FirstPoint_StartsNewFirstStroke()
        {
            SignDrawingModel model = ModelWithLine();
            SignDrawingService service = new(model);

            service.Erase(new EraseSignPointsCommand(1, new Vector2(0.1f), 0.05f));

            Assert.That(model.Points, Has.Count.EqualTo(2));
            Assert.That(model.Points[0].StartsStroke, Is.True);
        }

        [Test]
        public void Erase_AllPoints_LeavesEmptyDrawing()
        {
            SignDrawingModel model = ClaimedModel();
            model.Add(new SignStrokePoint(new Vector2(0.4f), true));
            model.Add(new SignStrokePoint(new Vector2(0.5f), false));
            model.Add(new SignStrokePoint(new Vector2(0.6f), false));
            SignDrawingService service = new(model);

            Assert.That(service.Erase(new EraseSignPointsCommand(1, new Vector2(0.5f), 1f)), Is.True);
            Assert.That(model.Points, Is.Empty);
        }

        [Test]
        public void Erase_NonFinitePosition_DoesNotChangeDrawing()
        {
            SignDrawingModel model = ModelWithLine();
            SignDrawingService service = new(model);

            Assert.That(service.Erase(new EraseSignPointsCommand(
                1,
                new Vector2(float.NaN, 0f),
                0.1f)), Is.False);
            Assert.That(model.Points, Has.Count.EqualTo(3));
        }

        private static SignDrawingModel ClaimedModel()
        {
            SignDrawingModel model = new();
            model.TryClaim(1);
            return model;
        }

        private static SignDrawingModel ModelWithLine()
        {
            SignDrawingModel model = ClaimedModel();
            model.Add(new SignStrokePoint(new Vector2(0.1f), true));
            model.Add(new SignStrokePoint(new Vector2(0.5f), false));
            model.Add(new SignStrokePoint(new Vector2(0.9f), false));
            return model;
        }

        private static SignStrokePoint[] OnePoint() =>
            new[] { new SignStrokePoint(new Vector2(0.25f), true) };

        private static SignStrokePoint Point(int index, bool startsStroke) =>
            new(new Vector2((index % 10) / 10f, (index % 9) / 9f), startsStroke);
    }
}
