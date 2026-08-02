using System;
using NUnit.Framework;
using SimpleSummon.Network;

namespace SimpleSummon.Tests.PlayMode
{
    public sealed class NetworkRateLimiterTests
    {
        [TestCase(0d)]
        [TestCase(-1d)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void Constructor_InvalidRate_Throws(double rate)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new NetworkRateLimiter(rate));
        }

        [Test]
        public void TryAcquire_EnforcesBoundaryPerClient()
        {
            NetworkRateLimiter limiter = new(2d);

            Assert.That(limiter.TryAcquire(new NetworkRequestContext(1, 10d)), Is.True);
            Assert.That(limiter.TryAcquire(new NetworkRequestContext(1, 10.499d)), Is.False);
            Assert.That(limiter.TryAcquire(new NetworkRequestContext(1, 10.5d)), Is.True);
            Assert.That(limiter.TryAcquire(new NetworkRequestContext(2, 10.1d)), Is.True);
        }

        [Test]
        public void Forget_RemovesOnlySpecifiedClientCooldown()
        {
            NetworkRateLimiter limiter = new(1d);
            limiter.TryAcquire(new NetworkRequestContext(1, 10d));
            limiter.TryAcquire(new NetworkRequestContext(2, 10d));

            limiter.Forget(1);

            Assert.That(limiter.TryAcquire(new NetworkRequestContext(1, 10.1d)), Is.True);
            Assert.That(limiter.TryAcquire(new NetworkRequestContext(2, 10.1d)), Is.False);
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void TryAcquire_NonFiniteServerTime_Throws(double time)
        {
            NetworkRateLimiter limiter = new(1d);

            Assert.Throws<ArgumentOutOfRangeException>(
                () => limiter.TryAcquire(new NetworkRequestContext(1, time)));
        }
    }
}
