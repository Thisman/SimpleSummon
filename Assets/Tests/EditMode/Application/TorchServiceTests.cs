using System;
using NUnit.Framework;
using SimpleSummon.Domain;

namespace SimpleSummon.Application.Tests
{
    public sealed class TorchServiceTests
    {
        [Test]
        public void Constructor_NullModel_Throws() =>
            Assert.Throws<ArgumentNullException>(() => new TorchService(null));

        [Test]
        public void PropertiesAndOwnership_ReflectDomainModel()
        {
            TorchService service = CreateService();

            Assert.That(service.IsAvailable, Is.True);
            Assert.That(service.TryTake(10), Is.True);
            Assert.That(service.IsHeldBy(10), Is.True);
            Assert.That(service.IsAvailable, Is.False);

            service.Release();

            Assert.That(service.IsAvailable, Is.True);
            Assert.That(service.Strength, Is.EqualTo(100f));
        }

        [Test]
        public void Update_AdvancesDomainBurnState()
        {
            TorchService service = CreateService();
            service.TryTake(10);

            service.Update(true, 6f);

            Assert.That(service.Strength, Is.EqualTo(90f));
            Assert.That(service.IsExtinguished, Is.False);
        }

        private static TorchService CreateService() =>
            new(new TorchModel(5f, 1f, 10f, 12f));
    }
}
