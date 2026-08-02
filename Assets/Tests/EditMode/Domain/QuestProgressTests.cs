using System;
using NUnit.Framework;

namespace SimpleSummon.Domain.Tests
{
    public sealed class QuestProgressTests
    {
        [Test]
        public void CollectSignFragment_CollectsEachIdOnce()
        {
            QuestProgress progress = new();

            Assert.That(progress.CollectSignFragment(3), Is.True);
            Assert.That(progress.CollectSignFragment(3), Is.False);
            Assert.That(progress.IsSignFragmentCollected(3), Is.True);
            Assert.That(progress.IsSignFragmentCollected(4), Is.False);
            Assert.That(progress.CollectedSignFragmentCount, Is.EqualTo(1));
        }

        [TestCase(-1)]
        [TestCase(QuestProgress.SignFragmentCount)]
        public void CollectSignFragment_RejectsInvalidId(int id)
        {
            QuestProgress progress = new();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => progress.CollectSignFragment(id));
        }

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

            progress.Apply(5, true, true, 4, false);

            Assert.That(progress.IsSignFragmentCollected(0), Is.True);
            Assert.That(progress.IsSignFragmentCollected(1), Is.False);
            Assert.That(progress.IsSignFragmentCollected(2), Is.True);
            Assert.That(progress.BossHeartCollected, Is.True);
            Assert.That(progress.SignDrawn, Is.True);
            Assert.That(progress.ArtifactResourceCount, Is.EqualTo(4));
            Assert.That(progress.ArtifactCrafted, Is.False);
        }

        [Test]
        public void Artifact_RequiresFiveResourcesAndCraftsOnce()
        {
            QuestProgress progress = new();

            for (int i = 0; i < QuestProgress.ArtifactResourceRequirement - 1; i++)
            {
                Assert.That(progress.CollectArtifactResource(), Is.True);
            }

            Assert.That(progress.CraftArtifact(), Is.False);
            Assert.That(progress.CollectArtifactResource(), Is.True);
            Assert.That(progress.CollectArtifactResource(), Is.False);
            Assert.That(progress.CraftArtifact(), Is.True);
            Assert.That(progress.CraftArtifact(), Is.False);
            Assert.That(progress.CollectArtifactResource(), Is.False);
            Assert.That(progress.ArtifactResourceCount, Is.Zero);
            Assert.That(progress.ArtifactCrafted, Is.True);
        }
    }
}
