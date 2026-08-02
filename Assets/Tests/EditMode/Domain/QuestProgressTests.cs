using NUnit.Framework;

namespace SimpleSummon.Domain.Tests
{
    public sealed class QuestProgressTests
    {
        [Test]
        public void BossHeartAndSign_AreRecordedOnce()
        {
            QuestProgress progress = new();

            Assert.That(progress.CollectBossHeart(), Is.True);
            Assert.That(progress.CollectBossHeart(), Is.False);
            Assert.That(progress.DrawSign(), Is.True);
            Assert.That(progress.DrawSign(), Is.False);
            Assert.That(progress.BossHeartCollected, Is.True);
            Assert.That(progress.SignDrawn, Is.True);
        }

        [Test]
        public void Apply_RestoresSharedState()
        {
            QuestProgress progress = new();

            progress.Apply(true, true);

            Assert.That(progress.BossHeartCollected, Is.True);
            Assert.That(progress.SignDrawn, Is.True);
        }
    }
}
