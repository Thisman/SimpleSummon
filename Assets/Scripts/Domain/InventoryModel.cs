using System;
using System.Collections.Generic;

namespace SimpleSummon.Domain
{
    public sealed class InventoryModel
    {
        private readonly Dictionary<string, int> items = new();

        public IReadOnlyDictionary<string, int> Items => items;

        public void Add(string itemName, int quantity)
        {
            ValidateItemName(itemName);
            if (quantity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
            }

            items.TryGetValue(itemName, out int currentQuantity);
            items[itemName] = checked(currentQuantity + quantity);
        }

        public int GetQuantity(string itemName)
        {
            ValidateItemName(itemName);
            return items.TryGetValue(itemName, out int quantity) ? quantity : 0;
        }

        public void SetQuantity(string itemName, int quantity)
        {
            ValidateItemName(itemName);

            if (quantity <= 0)
            {
                items.Remove(itemName);
            }
            else
            {
                items[itemName] = quantity;
            }
        }

        private static void ValidateItemName(string itemName)
        {
            if (string.IsNullOrWhiteSpace(itemName))
            {
                throw new ArgumentException(
                    "Item name cannot be empty.",
                    nameof(itemName));
            }
        }
    }
}
