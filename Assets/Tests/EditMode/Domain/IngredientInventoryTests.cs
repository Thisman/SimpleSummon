using System;
using NUnit.Framework;

namespace SimpleSummon.Domain.Tests
{
    public sealed class IngredientInventoryTests
    {
        [Test]
        public void Add_Green_ChangesOnlyGreenCount()
        {
            IngredientInventory inventory = new();

            Assert.That(inventory.Add(IngredientType.BottleGreen), Is.True);
            Assert.That(inventory.GreenBottleCount, Is.EqualTo(1));
            Assert.That(inventory.BrownBottleCount, Is.Zero);
        }

        [Test]
        public void Add_Brown_ChangesOnlyBrownCount()
        {
            IngredientInventory inventory = new();

            Assert.That(inventory.Add(IngredientType.BottleBrown), Is.True);
            Assert.That(inventory.GreenBottleCount, Is.Zero);
            Assert.That(inventory.BrownBottleCount, Is.EqualTo(1));
        }

        [TestCase(IngredientType.None)]
        [TestCase((IngredientType)999)]
        public void Add_UnsupportedType_DoesNotChangeState(IngredientType ingredient)
        {
            IngredientInventory inventory = new();

            Assert.That(inventory.Add(ingredient), Is.False);
            Assert.That(inventory.GreenBottleCount, Is.Zero);
            Assert.That(inventory.BrownBottleCount, Is.Zero);
        }

        [Test]
        public void Apply_ReplacesBothCounts()
        {
            IngredientInventory inventory = new();
            inventory.Apply(2, 3);

            inventory.Apply(5, 7);

            Assert.That(inventory.GreenBottleCount, Is.EqualTo(5));
            Assert.That(inventory.BrownBottleCount, Is.EqualTo(7));
        }

        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        public void Apply_NegativeCount_ThrowsAndPreservesState(int green, int brown)
        {
            IngredientInventory inventory = new();
            inventory.Apply(2, 3);

            Assert.Throws<ArgumentOutOfRangeException>(() => inventory.Apply(green, brown));
            Assert.That(inventory.GreenBottleCount, Is.EqualTo(2));
            Assert.That(inventory.BrownBottleCount, Is.EqualTo(3));
        }

        [TestCase(IngredientType.BottleGreen)]
        [TestCase(IngredientType.BottleBrown)]
        public void Add_AtMaximumCount_ReturnsFalseWithoutOverflow(IngredientType ingredient)
        {
            IngredientInventory inventory = new();
            inventory.Apply(
                ingredient == IngredientType.BottleGreen ? int.MaxValue : 0,
                ingredient == IngredientType.BottleBrown ? int.MaxValue : 0);

            Assert.That(inventory.Add(ingredient), Is.False);
            Assert.That(inventory.GreenBottleCount, Is.GreaterThanOrEqualTo(0));
            Assert.That(inventory.BrownBottleCount, Is.GreaterThanOrEqualTo(0));
        }
    }
}
