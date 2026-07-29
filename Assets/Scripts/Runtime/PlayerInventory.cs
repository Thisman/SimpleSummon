using System.Collections.Generic;
using SimpleSummon.Domain;
using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class PlayerInventory : MonoBehaviour
    {
        private readonly InventoryModel model = new();

        public IReadOnlyDictionary<string, int> Items => model.Items;

        public void Add(string itemName, int quantity)
        {
            model.Add(itemName, quantity);
        }

        public int GetQuantity(string itemName)
        {
            return model.GetQuantity(itemName);
        }
    }
}
