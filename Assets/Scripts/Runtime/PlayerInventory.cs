using System.Collections.Generic;
using SimpleSummon.Domain;
using SimpleSummon.Network;
using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class PlayerInventory : MonoBehaviour
    {
        private readonly InventoryModel model = new();
        private NetworkPlayer networkPlayer;

        public IReadOnlyDictionary<string, int> Items => model.Items;

        private void Awake()
        {
            networkPlayer = GetComponent<NetworkPlayer>();
        }

        private void OnEnable()
        {
            if (networkPlayer != null)
            {
                networkPlayer.InventoryChanged += RefreshFromNetwork;
            }
        }

        private void OnDisable()
        {
            if (networkPlayer != null)
            {
                networkPlayer.InventoryChanged -= RefreshFromNetwork;
            }
        }

        public void Add(string itemName, int quantity)
        {
            model.Add(itemName, quantity);
            networkPlayer?.SetInventoryQuantity(itemName, model.GetQuantity(itemName));
        }

        public int GetQuantity(string itemName)
        {
            return model.GetQuantity(itemName);
        }

        private void RefreshFromNetwork()
        {
            networkPlayer.CopyInventoryTo(model.SetQuantity);
        }
    }
}
