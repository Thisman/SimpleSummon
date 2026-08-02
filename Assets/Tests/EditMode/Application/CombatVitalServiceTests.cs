using System;
using NUnit.Framework;
using SimpleSummon.Domain;

namespace SimpleSummon.Application.Tests
{
    public sealed class CombatVitalServiceTests
    {
        [Test]
        public void EnemyCreate_MapsConfigurationAndUsesZeroJumpHeight()
        {
            UnitModel model = EnemyCombatService.Create(3f, 0.7f, 12f, 80f);

            Assert.That(model.MovementSpeed, Is.EqualTo(3f));
            Assert.That(model.JumpHeight, Is.Zero);
            Assert.That(model.AttackDelay, Is.EqualTo(0.7f));
            Assert.That(model.Damage, Is.EqualTo(12f));
            Assert.That(model.MaximumHealth, Is.EqualTo(80f));
        }

        [Test]
        public void EnemyCreate_InvalidConfiguration_UsesDomainValidation()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => EnemyCombatService.Create(-1f, 0f, 0f, 1f));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TakeDamage_ReturnsTrueOnlyOnFirstLethalTransition(bool playerService)
        {
            UnitModel model = Unit();

            Assert.That(TakeDamage(playerService, model, 5f), Is.False);
            Assert.That(TakeDamage(playerService, model, 5f), Is.True);
            Assert.That(TakeDamage(playerService, model, 5f), Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TakeDamage_InvalidDamage_DoesNotKill(bool playerService)
        {
            UnitModel model = Unit();

            Assert.That(TakeDamage(playerService, model, float.NaN), Is.False);
            Assert.That(model.CurrentHealth, Is.EqualTo(10f));
        }

        [Test]
        public void PlayerRestore_ResetsHealthAndCooldown()
        {
            UnitModel model = Unit();
            model.TryAttack();
            model.TakeDamage(5f);

            PlayerVitalService.Restore(model);

            Assert.That(model.CurrentHealth, Is.EqualTo(10f));
            Assert.That(model.AttackCooldownRemaining, Is.Zero);
        }

        [TestCase(-5f, 0f)]
        [TestCase(5f, 5f)]
        [TestCase(50f, 10f)]
        public void ApplyReplicatedHealth_UsesDomainClamp(float health, float expected)
        {
            UnitModel model = Unit();

            PlayerVitalService.ApplyReplicatedHealth(model, health);

            Assert.That(model.CurrentHealth, Is.EqualTo(expected));
        }

        [Test]
        public void ApplicationServices_NullModel_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => EnemyCombatService.TakeDamage(null, 1f));
            Assert.Throws<ArgumentNullException>(() => PlayerVitalService.TakeDamage(null, 1f));
            Assert.Throws<ArgumentNullException>(() => PlayerVitalService.Restore(null));
            Assert.Throws<ArgumentNullException>(() => PlayerVitalService.ApplyReplicatedHealth(null, 1f));
        }

        private static bool TakeDamage(bool playerService, UnitModel model, float damage) =>
            playerService
                ? PlayerVitalService.TakeDamage(model, damage)
                : EnemyCombatService.TakeDamage(model, damage);

        private static UnitModel Unit() => new(1f, 1f, 0.5f, 1f, 10f);
    }
}
