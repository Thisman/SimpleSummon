using System;
using System.Numerics;
using NUnit.Framework;

namespace SimpleSummon.Application.Tests
{
    public sealed class SummonPointValidationServiceTests
    {
        [Test]
        public void TryValidate_PointInsideBounds_ReturnsUnchangedPoint()
        {
            Vector2 submitted = new(0.25f, 0.75f);

            bool result = SummonPointValidationService.TryValidate(
                null,
                submitted,
                true,
                0.01f,
                out Vector2 validated);

            Assert.That(result, Is.True);
            Assert.That(validated, Is.EqualTo(submitted));
        }

        [Test]
        public void TryValidate_PointOutsideBounds_ClampsPoint()
        {
            bool result = SummonPointValidationService.TryValidate(
                null,
                new Vector2(-1f, 2f),
                true,
                0.01f,
                out Vector2 validated);

            Assert.That(result, Is.True);
            Assert.That(validated, Is.EqualTo(new Vector2(0f, 1f)));
        }

        [TestCase(float.NaN, 0f)]
        [TestCase(0f, float.PositiveInfinity)]
        public void TryValidate_NonFinitePoint_ReturnsFalse(float x, float y)
        {
            bool result = SummonPointValidationService.TryValidate(
                null,
                new Vector2(x, y),
                true,
                0.01f,
                out _);

            Assert.That(result, Is.False);
        }

        [Test]
        public void TryValidate_ContinuationTooClose_ReturnsFalse()
        {
            bool result = SummonPointValidationService.TryValidate(
                new Vector2(0.5f, 0.5f),
                new Vector2(0.505f, 0.5f),
                false,
                0.01f,
                out _);

            Assert.That(result, Is.False);
        }

        [Test]
        public void TryValidate_NewStrokeTooClose_ReturnsTrue()
        {
            bool result = SummonPointValidationService.TryValidate(
                new Vector2(0.5f, 0.5f),
                new Vector2(0.505f, 0.5f),
                true,
                0.01f,
                out _);

            Assert.That(result, Is.True);
        }

        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void TryValidate_InvalidMinimumDistance_Throws(float distance)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => SummonPointValidationService.TryValidate(
                    null,
                    Vector2.Zero,
                    true,
                    distance,
                    out _));
        }
    }
}
