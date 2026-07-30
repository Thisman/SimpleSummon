using System;
using NUnit.Framework;
using SimpleSummon.Domain;

namespace SimpleSummon.Application.Tests
{
    public sealed class UnitAttackServiceTests
    {
        [Test]
        public void TryAttack_RequestWhenReady_ReturnsTrue()
        {
            UnitModel unit = CreateUnit();

            bool result = UnitAttackService.TryAttack(unit, 0.1f, true);

            Assert.That(result, Is.True);
        }

        [Test]
        public void TryAttack_WithoutRequest_StillAdvancesCooldown()
        {
            UnitModel unit = CreateUnit();
            unit.TryAttack();

            bool result = UnitAttackService.TryAttack(unit, 0.25f, false);

            Assert.That(result, Is.False);
            Assert.That(unit.AttackCooldownRemaining, Is.EqualTo(0.25f));
        }

        [Test]
        public void TryAttack_RequestDuringCooldown_ReturnsFalse()
        {
            UnitModel unit = CreateUnit();
            unit.TryAttack();

            bool result = UnitAttackService.TryAttack(unit, 0.1f, true);

            Assert.That(result, Is.False);
        }

        [Test]
        public void TryAttack_DeadUnit_ReturnsFalse()
        {
            UnitModel unit = CreateUnit();
            unit.TakeDamage(unit.MaximumHealth);

            bool result = UnitAttackService.TryAttack(unit, 1f, true);

            Assert.That(result, Is.False);
        }

        [Test]
        public void TryAttack_NullUnit_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => UnitAttackService.TryAttack(null, 0f, true));
        }

        private static UnitModel CreateUnit()
        {
            return new UnitModel(4f, 2f, 0.5f, 10f, 100f);
        }
    }
}
