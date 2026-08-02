using System;
using NUnit.Framework;

namespace SimpleSummon.Domain.Tests
{
    public sealed class UnitModelTests
    {
        [Test]
        public void Constructor_InitializesState()
        {
            UnitModel unit = CreateUnit();

            Assert.That(unit.MovementSpeed, Is.EqualTo(4f));
            Assert.That(unit.JumpHeight, Is.EqualTo(2f));
            Assert.That(unit.AttackDelay, Is.EqualTo(0.5f));
            Assert.That(unit.Damage, Is.EqualTo(10f));
            Assert.That(unit.MaximumHealth, Is.EqualTo(100f));
            Assert.That(unit.CurrentHealth, Is.EqualTo(100f));
            Assert.That(unit.AttackCooldownRemaining, Is.Zero);
            Assert.That(unit.IsDead, Is.False);
        }

        [TestCase(-1f, 0f, 0f, 0f, 1f)]
        [TestCase(0f, -1f, 0f, 0f, 1f)]
        [TestCase(0f, 0f, -1f, 0f, 1f)]
        [TestCase(0f, 0f, 0f, -1f, 1f)]
        [TestCase(0f, 0f, 0f, 0f, 0f)]
        [TestCase(0f, 0f, 0f, 0f, -1f)]
        public void Constructor_InvalidConfiguration_Throws(
            float movementSpeed,
            float jumpHeight,
            float attackDelay,
            float damage,
            float maximumHealth)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new UnitModel(
                    movementSpeed,
                    jumpHeight,
                    attackDelay,
                    damage,
                    maximumHealth));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void Constructor_NonFiniteConfiguration_Throws(float value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new UnitModel(value, 0f, 0f, 0f, 1f));
        }

        [Test]
        public void TryAttack_WhenReady_StartsCooldown()
        {
            UnitModel unit = CreateUnit();

            bool attacked = unit.TryAttack();

            Assert.That(attacked, Is.True);
            Assert.That(unit.AttackCooldownRemaining, Is.EqualTo(0.5f));
        }

        [Test]
        public void TryAttack_DuringCooldown_ReturnsFalse()
        {
            UnitModel unit = CreateUnit();
            unit.TryAttack();

            Assert.That(unit.TryAttack(), Is.False);
        }

        [Test]
        public void UpdateAttackCooldown_ClampsAtZero()
        {
            UnitModel unit = CreateUnit();
            unit.TryAttack();

            unit.UpdateAttackCooldown(2f);

            Assert.That(unit.AttackCooldownRemaining, Is.Zero);
        }

        [Test]
        public void UpdateAttackCooldown_NegativeDeltaTime_Throws()
        {
            UnitModel unit = CreateUnit();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => unit.UpdateAttackCooldown(-0.1f));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void UpdateAttackCooldown_NonFiniteDeltaTime_Throws(float deltaTime)
        {
            UnitModel unit = CreateUnit();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => unit.UpdateAttackCooldown(deltaTime));
        }

        [Test]
        public void UpdateAttackCooldown_AtExactDelay_MakesUnitReady()
        {
            UnitModel unit = CreateUnit();
            unit.TryAttack();

            unit.UpdateAttackCooldown(unit.AttackDelay);

            Assert.That(unit.TryAttack(), Is.True);
        }

        [Test]
        public void TakeDamage_PositiveDamage_ReducesHealth()
        {
            UnitModel unit = CreateUnit();

            unit.TakeDamage(25f);

            Assert.That(unit.CurrentHealth, Is.EqualTo(75f));
        }

        [TestCase(0f)]
        [TestCase(-10f)]
        public void TakeDamage_NonPositiveDamage_IsIgnored(float damage)
        {
            UnitModel unit = CreateUnit();

            unit.TakeDamage(damage);

            Assert.That(unit.CurrentHealth, Is.EqualTo(100f));
        }

        [Test]
        public void TakeDamage_NonFiniteDamage_IsIgnored()
        {
            UnitModel unit = CreateUnit();

            unit.TakeDamage(float.NaN);
            unit.TakeDamage(float.PositiveInfinity);

            Assert.That(unit.CurrentHealth, Is.EqualTo(100f));
        }

        [Test]
        public void TakeDamage_LethalDamage_ClampsHealthAndPreventsAttack()
        {
            UnitModel unit = CreateUnit();

            unit.TakeDamage(200f);

            Assert.That(unit.CurrentHealth, Is.Zero);
            Assert.That(unit.IsDead, Is.True);
            Assert.That(unit.TryAttack(), Is.False);
        }

        [Test]
        public void DeadUnit_IgnoresFurtherDamage()
        {
            UnitModel unit = CreateUnit();
            unit.TakeDamage(100f);

            unit.TakeDamage(10f);

            Assert.That(unit.CurrentHealth, Is.Zero);
        }

        [TestCase(-10f, 0f)]
        [TestCase(30f, 30f)]
        [TestCase(200f, 100f)]
        public void SetCurrentHealth_ClampsValue(float value, float expected)
        {
            UnitModel unit = CreateUnit();

            unit.SetCurrentHealth(value);

            Assert.That(unit.CurrentHealth, Is.EqualTo(expected));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void SetCurrentHealth_NonFiniteValue_Throws(float value)
        {
            UnitModel unit = CreateUnit();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => unit.SetCurrentHealth(value));
        }

        [Test]
        public void RestoreHealth_ResetsHealthAndCooldown()
        {
            UnitModel unit = CreateUnit();
            unit.TryAttack();
            unit.TakeDamage(40f);

            unit.RestoreHealth();

            Assert.That(unit.CurrentHealth, Is.EqualTo(100f));
            Assert.That(unit.AttackCooldownRemaining, Is.Zero);
        }

        [Test]
        public void RestoreHealth_AfterDeath_RevivesAndAllowsAttack()
        {
            UnitModel unit = CreateUnit();
            unit.TakeDamage(unit.MaximumHealth);

            unit.RestoreHealth();

            Assert.That(unit.IsDead, Is.False);
            Assert.That(unit.TryAttack(), Is.True);
        }

        [Test]
        public void ZeroAttackDelay_AllowsConsecutiveAttacks()
        {
            UnitModel unit = new(0f, 0f, 0f, 0f, 1f);

            Assert.That(unit.TryAttack(), Is.True);
            Assert.That(unit.TryAttack(), Is.True);
        }

        private static UnitModel CreateUnit()
        {
            return new UnitModel(4f, 2f, 0.5f, 10f, 100f);
        }
    }
}
