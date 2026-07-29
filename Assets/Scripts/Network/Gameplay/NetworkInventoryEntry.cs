using System;
using Unity.Collections;
using Unity.Netcode;

namespace SimpleSummon.Network
{
    public struct NetworkInventoryEntry :
        INetworkSerializable,
        IEquatable<NetworkInventoryEntry>
    {
        public FixedString64Bytes ItemName;
        public int Quantity;

        public NetworkInventoryEntry(string itemName, int quantity)
        {
            ItemName = itemName;
            Quantity = quantity;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer)
            where T : IReaderWriter
        {
            serializer.SerializeValue(ref ItemName);
            serializer.SerializeValue(ref Quantity);
        }

        public bool Equals(NetworkInventoryEntry other)
        {
            return ItemName.Equals(other.ItemName) && Quantity == other.Quantity;
        }
    }
}
