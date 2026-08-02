using System;
using Unity.Netcode;
using UnityEngine;

namespace SimpleSummon.Network
{
    public struct NetworkSummonPoint : INetworkSerializable, IEquatable<NetworkSummonPoint>
    {
        public Vector2 Position;
        public bool StartsStroke;

        public NetworkSummonPoint(Vector2 position, bool startsStroke)
        {
            Position = position;
            StartsStroke = startsStroke;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer)
            where T : IReaderWriter
        {
            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref StartsStroke);
        }

        public bool Equals(NetworkSummonPoint other)
        {
            return Position == other.Position && StartsStroke == other.StartsStroke;
        }
    }
}
