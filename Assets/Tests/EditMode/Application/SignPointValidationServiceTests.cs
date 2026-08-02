using System;
using System.Numerics;
using NUnit.Framework;

namespace SimpleSummon.Application.Tests
{
    public sealed class SignPointValidationServiceTests
    {
        [Test]
        public void TryValidate_PointInsideBounds_ReturnsUnchangedPoint()
        {
            Vector2 submitted = new(0.25f, 0.75f);

            bool result = SignPointValidationService.TryValidate(
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
            bool result = SignPointValidationService.TryValidate(
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
            bool result = SignPointValidationService.TryValidate(
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
            bool result = SignPointValidationService.TryValidate(
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
            bool result = SignPointValidationService.TryValidate(
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
                () => SignPointValidationService.TryValidate(
                    null,
                    Vector2.Zero,
                    true,
                    distance,
                    out _));
        }

        [Test]
        public void TryValidate_ContinuationAtExactMinimumDistance_IsAccepted()
        {
            Assert.That(SignPointValidationService.TryValidate(
                Vector2.Zero,
                new Vector2(0.01f, 0f),
                false,
                0.01f,
                out _), Is.True);
        }

        [Test]
        public void TryValidate_ZeroMinimumDistance_AcceptsDuplicate()
        {
            Assert.That(SignPointValidationService.TryValidate(
                Vector2.One,
                Vector2.One,
                false,
                0f,
                out _), Is.True);
        }

        [TestCase(float.NaN, 0f)]
        [TestCase(float.PositiveInfinity, 0f)]
        public void TryValidate_NonFinitePreviousPoint_ReturnsFalse(float x, float y)
        {
            Assert.That(SignPointValidationService.TryValidate(
                new Vector2(x, y),
                Vector2.Zero,
                false,
                0.01f,
                out _), Is.False);
        }
    }
}
