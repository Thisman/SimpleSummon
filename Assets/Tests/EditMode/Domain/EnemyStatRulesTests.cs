using NUnit.Framework;
using SimpleSummon.Domain;

namespace SimpleSummon.Tests.Domain
{
    public sealed class EnemyStatRulesTests
    {
        [TestCase(false, false, 3f, 1f)]
        [TestCase(false, true, 3f, 1f)]
        [TestCase(true, true, 3f, 1f)]
        [TestCase(true, false, 3f, 3f)]
        public void GetStatMultiplier_ReturnsExpectedValue(
            bool isBoss,
            bool artifactCrafted,
            float bossMultiplier,
            float expected)
        {
            Assert.That(
                EnemyStatRules.GetStatMultiplier(
                    isBoss,
                    artifactCrafted,
                    bossMultiplier),
                Is.EqualTo(expected));
        }
    }
}
