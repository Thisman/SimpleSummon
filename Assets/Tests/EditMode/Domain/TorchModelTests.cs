using System;
using NUnit.Framework;

namespace SimpleSummon.Domain.Tests
{
    public sealed class TorchModelTests
    {
        [TestCase(-1f, 0f, 0f, 0f)]
        [TestCase(0f, -1f, 0f, 0f)]
        [TestCase(0f, 0f, -1f, 0f)]
        [TestCase(0f, 0f, 0f, -1f)]
        [TestCase(float.NaN, 0f, 0f, 0f)]
        [TestCase(float.PositiveInfinity, 0f, 0f, 0f)]
        public void Constructor_InvalidConfiguration_Throws(
            float fadeDelay,
            float recoveryDelay,
            float fadeRate,
            float recoveryRate)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new TorchModel(fadeDelay, recoveryDelay, fadeRate, recoveryRate));
        }

        [Test]
        public void Constructor_ZeroConfiguration_IsValid()
        {
            TorchModel model = new(0f, 0f, 0f, 0f);

            Assert.That(model.Strength, Is.EqualTo(100f));
            Assert.That(model.Phase, Is.EqualTo(TorchBurnPhase.WaitingToFade));
        }

        [Test]
        public void MovingStartsFadingAfterDelay()
        {
            TorchModel model = CreateModel();

            model.Tick(true, 5f);
            Assert.That(model.Strength, Is.EqualTo(100f));

            model.Tick(true, 1f);
            Assert.That(model.Strength, Is.EqualTo(90f));
            Assert.That(model.Phase, Is.EqualTo(TorchBurnPhase.Fading));
        }

        [Test]
        public void StoppingDuringFadeKeepsFadingForRecoveryDelay()
        {
            TorchModel model = CreateModel();
            model.Tick(true, 6f);

            model.Tick(false, 1f);
            Assert.That(model.Strength, Is.EqualTo(80f));
            Assert.That(model.Phase, Is.EqualTo(TorchBurnPhase.Recovering));
        }

        [Test]
        public void MovingPausesRecoveryUntilPlayerStopsAgain()
        {
            TorchModel model = CreateModel();
            model.Tick(true, 6f);
            model.Tick(false, 1f);

            model.Tick(true, 3f);
            Assert.That(model.Strength, Is.EqualTo(80f));
            Assert.That(model.Phase, Is.EqualTo(TorchBurnPhase.Recovering));

            model.Tick(false, 1f);
            Assert.That(model.Strength, Is.EqualTo(92f));
        }

        [Test]
        public void ReachingFullStrengthResetsFadeTimer()
        {
            TorchModel model = CreateModel();
            model.Tick(true, 6f);
            model.Tick(false, 1f);
            model.Tick(false, 2f);

            Assert.That(model.Strength, Is.EqualTo(100f));
            Assert.That(model.Phase, Is.EqualTo(TorchBurnPhase.WaitingToFade));

            model.Tick(true, 4f);
            Assert.That(model.Strength, Is.EqualTo(100f));
        }

        [Test]
        public void ReachingFullStrengthKeepsCurrentHolder()
        {
            TorchModel model = CreateModel();
            model.TryTake(10);
            model.Tick(true, 6f);
            model.Tick(false, 1f);

            model.Tick(false, 2f);

            Assert.That(model.IsHeldBy(10), Is.True);
            Assert.That(model.IsAvailable, Is.False);
            Assert.That(model.TryTake(20), Is.False);
        }

        [Test]
        public void StrengthNeverDropsBelowZero()
        {
            TorchModel model = CreateModel();
            model.Tick(true, 100f);

            Assert.That(model.Strength, Is.Zero);
            Assert.That(model.IsExtinguished, Is.True);
        }

        [Test]
        public void OnlyOneHolderCanTakeTorchUntilItIsReleased()
        {
            TorchModel model = CreateModel();

            Assert.That(model.TryTake(10), Is.True);
            Assert.That(model.TryTake(20), Is.False);
            Assert.That(model.IsHeldBy(10), Is.True);

            model.Release();

            Assert.That(model.TryTake(20), Is.True);
            Assert.That(model.IsHeldBy(20), Is.True);
            Assert.That(model.Strength, Is.EqualTo(100f));
        }

        [TestCase(-0.1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void Tick_InvalidDeltaTime_Throws(float deltaTime)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CreateModel().Tick(true, deltaTime));
        }

        [Test]
        public void Tick_ZeroDeltaTime_DoesNothing()
        {
            TorchModel model = CreateModel();

            model.Tick(true, 0f);

            Assert.That(model.Strength, Is.EqualTo(100f));
            Assert.That(model.Phase, Is.EqualTo(TorchBurnPhase.WaitingToFade));
        }

        [Test]
        public void Reset_ClearsHolderAndRestoresInitialFlame()
        {
            TorchModel model = CreateModel();
            model.TryTake(10);
            model.Tick(true, 6f);

            model.Reset();

            Assert.That(model.IsAvailable, Is.True);
            Assert.That(model.Strength, Is.EqualTo(100f));
            Assert.That(model.Phase, Is.EqualTo(TorchBurnPhase.WaitingToFade));
        }

        private static TorchModel CreateModel() => new(
            fadeDelay: 5f,
            recoveryDelay: 1f,
            fadeRate: 10f,
            recoveryRate: 12f);
    }
}
