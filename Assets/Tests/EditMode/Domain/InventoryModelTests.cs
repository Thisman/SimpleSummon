using System;
using NUnit.Framework;

namespace SimpleSummon.Domain.Tests
{
    public sealed class InventoryModelTests
    {
        [Test]
        public void NewInventory_IsEmpty()
        {
            InventoryModel inventory = new();

            Assert.That(inventory.Items, Is.Empty);
            Assert.That(inventory.GetQuantity("Bones"), Is.Zero);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void GetQuantity_InvalidName_Throws(string itemName)
        {
            InventoryModel inventory = new();

            Assert.Throws<ArgumentException>(
                () => inventory.GetQuantity(itemName));
        }

        [Test]
        public void Add_NewItem_StoresQuantity()
        {
            InventoryModel inventory = new();

            inventory.Add("Bones", 2);

            Assert.That(inventory.GetQuantity("Bones"), Is.EqualTo(2));
        }

        [Test]
        public void Add_ExistingItem_AccumulatesQuantity()
        {
            InventoryModel inventory = new();
            inventory.Add("Bones", 2);

            inventory.Add("Bones", 3);

            Assert.That(inventory.GetQuantity("Bones"), Is.EqualTo(5));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void Add_InvalidName_Throws(string itemName)
        {
            InventoryModel inventory = new();

            Assert.Throws<ArgumentException>(() => inventory.Add(itemName, 1));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Add_NonPositiveQuantity_Throws(int quantity)
        {
            InventoryModel inventory = new();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => inventory.Add("Bones", quantity));
        }

        [Test]
        public void Add_QuantityOverflow_Throws()
        {
            InventoryModel inventory = new();
            inventory.SetQuantity("Bones", int.MaxValue);

            Assert.Throws<OverflowException>(
                () => inventory.Add("Bones", 1));
        }

        [Test]
        public void SetQuantity_PositiveValue_ReplacesQuantity()
        {
            InventoryModel inventory = new();
            inventory.Add("Bones", 2);

            inventory.SetQuantity("Bones", 7);

            Assert.That(inventory.GetQuantity("Bones"), Is.EqualTo(7));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void SetQuantity_NonPositiveValue_RemovesItem(int quantity)
        {
            InventoryModel inventory = new();
            inventory.Add("Bones", 2);

            inventory.SetQuantity("Bones", quantity);

            Assert.That(inventory.Items.ContainsKey("Bones"), Is.False);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void SetQuantity_InvalidName_Throws(string itemName)
        {
            InventoryModel inventory = new();

            Assert.Throws<ArgumentException>(
                () => inventory.SetQuantity(itemName, 1));
        }
    }
}
