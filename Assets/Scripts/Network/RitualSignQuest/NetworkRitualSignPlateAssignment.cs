using System;
using Unity.Netcode;

namespace SimpleSummon.Network
{
    public struct NetworkRitualSignPlateAssignment :
        INetworkSerializable,
        IEquatable<NetworkRitualSignPlateAssignment>
    {
        public ulong ActorId;
        public byte PlateIndex;

        public NetworkRitualSignPlateAssignment(ulong actorId, int plateIndex)
        {
            ActorId = actorId;
            PlateIndex = (byte)plateIndex;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer)
            where T : IReaderWriter
        {
            serializer.SerializeValue(ref ActorId);
            serializer.SerializeValue(ref PlateIndex);
        }

        public bool Equals(NetworkRitualSignPlateAssignment other) =>
            ActorId == other.ActorId && PlateIndex == other.PlateIndex;
    }
}
