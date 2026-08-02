using NUnit.Framework;
using SimpleSummon.Application;
using SimpleSummon.Domain;

namespace SimpleSummon.Tests.Application
{
    public sealed class EnemyDecisionServiceTests
    {
        [TestCase(EnemyBehaviorState.Idle, true, 4f, 0f, EnemyBehaviorState.Chase)]
        [TestCase(EnemyBehaviorState.Chase, true, 1f, 0f, EnemyBehaviorState.Attack)]
        [TestCase(EnemyBehaviorState.Attack, true, 4f, 0f, EnemyBehaviorState.Chase)]
        [TestCase(EnemyBehaviorState.Chase, false, 4f, 0f, EnemyBehaviorState.Idle)]
        [TestCase(EnemyBehaviorState.Chase, true, 4f, 20f, EnemyBehaviorState.Return)]
        [TestCase(EnemyBehaviorState.Return, false, 4f, 0.1f, EnemyBehaviorState.Idle)]
        public void Decide_ReturnsExpectedState(
            EnemyBehaviorState state,
            bool hasTarget,
            float targetDistance,
            float homeDistance,
            EnemyBehaviorState expected)
        {
            EnemyDecisionContext context = new(
                state,
                hasTarget,
                targetDistance,
                homeDistance,
                8f,
                2f,
                12f,
                0.5f);

            Assert.That(EnemyDecisionService.Decide(context), Is.EqualTo(expected));
        }

        [Test]
        public void Decide_DeadAlwaysRemainsDead()
        {
            EnemyDecisionContext context = new(
                EnemyBehaviorState.Dead,
                true,
                0f,
                100f,
                8f,
                2f,
                12f,
                0.5f);

            Assert.That(EnemyDecisionService.Decide(context), Is.EqualTo(EnemyBehaviorState.Dead));
        }

        [TestCase(2f, EnemyBehaviorState.Attack)]
        [TestCase(2.001f, EnemyBehaviorState.Chase)]
        [TestCase(8f, EnemyBehaviorState.Chase)]
        [TestCase(8.001f, EnemyBehaviorState.Idle)]
        public void Decide_TargetDistanceBoundaries(
            float targetDistance,
            EnemyBehaviorState expected)
        {
            EnemyDecisionContext context = new(
                EnemyBehaviorState.Idle,
                true,
                targetDistance,
                0f,
                8f,
                2f,
                12f,
                0.5f);

            Assert.That(EnemyDecisionService.Decide(context), Is.EqualTo(expected));
        }

        [TestCase(12f, EnemyBehaviorState.Idle)]
        [TestCase(12.001f, EnemyBehaviorState.Return)]
        public void Decide_ReturnRadiusBoundary(float homeDistance, EnemyBehaviorState expected)
        {
            EnemyDecisionContext context = new(
                EnemyBehaviorState.Idle,
                true,
                20f,
                homeDistance,
                8f,
                2f,
                12f,
                0.5f);

            Assert.That(EnemyDecisionService.Decide(context), Is.EqualTo(expected));
        }
    }
}
