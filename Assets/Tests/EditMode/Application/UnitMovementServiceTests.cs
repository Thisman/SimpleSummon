using System;
using System.Numerics;
using NUnit.Framework;

namespace SimpleSummon.Application.Tests
{
    public sealed class UnitMovementServiceTests
    {
        [Test]
        public void GetCameraRelativeDirection_ForwardInput_UsesFlatCameraForward()
        {
            Vector3 result = UnitMovementService.GetCameraRelativeDirection(
                Vector2.UnitY,
                new Vector3(0f, 3f, 2f),
                Vector3.UnitX);

            AssertVector(result, new Vector3(0f, 0f, 1f));
        }

        [Test]
        public void GetCameraRelativeDirection_DiagonalInput_IsClamped()
        {
            Vector3 result = UnitMovementService.GetCameraRelativeDirection(
                Vector2.One,
                Vector3.UnitZ,
                Vector3.UnitX);

            Assert.That(result.Length(), Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void GetCameraRelativeDirection_ZeroAxes_ReturnsZero()
        {
            Vector3 result = UnitMovementService.GetCameraRelativeDirection(
                Vector2.One,
                Vector3.Zero,
                Vector3.Zero);

            Assert.That(result, Is.EqualTo(Vector3.Zero));
        }

        [Test]
        public void GetJumpVelocity_ValidValues_ReturnsRequiredVelocity()
        {
            float result = UnitMovementService.GetJumpVelocity(2f, -9.81f);

            Assert.That(result, Is.EqualTo(MathF.Sqrt(39.24f)).Within(0.0001f));
        }

        [Test]
        public void GetJumpVelocity_ZeroHeight_ReturnsZero()
        {
            Assert.That(
                UnitMovementService.GetJumpVelocity(0f, -9.81f),
                Is.Zero);
        }

        [Test]
        public void GetJumpVelocity_NegativeHeight_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => UnitMovementService.GetJumpVelocity(-1f, -9.81f));
        }

        [TestCase(0f)]
        [TestCase(9.81f)]
        public void GetJumpVelocity_NonNegativeGravity_Throws(float gravity)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => UnitMovementService.GetJumpVelocity(1f, gravity));
        }

        private static void AssertVector(Vector3 actual, Vector3 expected)
        {
            Assert.That(actual.X, Is.EqualTo(expected.X).Within(0.0001f));
            Assert.That(actual.Y, Is.EqualTo(expected.Y).Within(0.0001f));
            Assert.That(actual.Z, Is.EqualTo(expected.Z).Within(0.0001f));
        }
    }

}
