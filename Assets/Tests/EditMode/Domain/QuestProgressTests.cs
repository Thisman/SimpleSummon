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

            progress.Apply(true, true, 2, 3);

            Assert.That(progress.BossHeartCollected, Is.True);
            Assert.That(progress.SignDrawn, Is.True);
            Assert.That(progress.Ingredients.GreenBottleCount, Is.EqualTo(2));
            Assert.That(progress.Ingredients.BrownBottleCount, Is.EqualTo(3));
        }

        [Test]
        public void Ingredients_AreCountedByType()
        {
            QuestProgress progress = new();

            Assert.That(progress.Ingredients.Add(IngredientType.BottleGreen), Is.True);
            Assert.That(progress.Ingredients.Add(IngredientType.BottleBrown), Is.True);
            Assert.That(progress.Ingredients.Add(IngredientType.None), Is.False);
            Assert.That(progress.Ingredients.GreenBottleCount, Is.EqualTo(1));
            Assert.That(progress.Ingredients.BrownBottleCount, Is.EqualTo(1));
        }

        [Test]
        public void Apply_CanResetFlagsAndContinueFromReplicatedCounts()
        {
            QuestProgress progress = new();
            progress.Apply(true, true, 2, 3);

            progress.Apply(false, false, 4, 5);
            progress.Ingredients.Add(IngredientType.BottleGreen);

            Assert.That(progress.BossHeartCollected, Is.False);
            Assert.That(progress.SignDrawn, Is.False);
            Assert.That(progress.Ingredients.GreenBottleCount, Is.EqualTo(5));
            Assert.That(progress.Ingredients.BrownBottleCount, Is.EqualTo(5));
        }
    }
}
