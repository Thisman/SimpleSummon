using System;
using NUnit.Framework;
using SimpleSummon.Domain;

namespace SimpleSummon.Application.Tests
{
    public sealed class QuestProgressServiceTests
    {
        [Test]
        public void Constructor_NullProgress_Throws() =>
            Assert.Throws<ArgumentNullException>(() => new QuestProgressService(null));

        [Test]
        public void QuestFlags_AreRecordedOnce()
        {
            QuestProgress progress = new();
            QuestProgressService service = new(progress);

            Assert.That(service.CollectBossHeart(), Is.True);
            Assert.That(service.CollectBossHeart(), Is.False);
            Assert.That(service.RecordSignDrawn(), Is.True);
            Assert.That(service.RecordSignDrawn(), Is.False);
            Assert.That(progress.BossHeartCollected, Is.True);
            Assert.That(progress.SignDrawn, Is.True);
        }

        [TestCase(IngredientType.BottleGreen, 1, 0, true)]
        [TestCase(IngredientType.BottleBrown, 0, 1, true)]
        [TestCase(IngredientType.None, 0, 0, false)]
        [TestCase((IngredientType)999, 0, 0, false)]
        public void CollectIngredient_UsesDomainInventory(
            IngredientType ingredient,
            int green,
            int brown,
            bool expected)
        {
            QuestProgress progress = new();
            QuestProgressService service = new(progress);

            Assert.That(service.CollectIngredient(ingredient), Is.EqualTo(expected));
            Assert.That(progress.Ingredients.GreenBottleCount, Is.EqualTo(green));
            Assert.That(progress.Ingredients.BrownBottleCount, Is.EqualTo(brown));
        }
    }
}
