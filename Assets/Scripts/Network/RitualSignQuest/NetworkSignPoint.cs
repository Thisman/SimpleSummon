using System;
using Unity.Netcode;
using UnityEngine;

namespace SimpleSummon.Network
{
    public struct NetworkSignPoint : INetworkSerializable, IEquatable<NetworkSignPoint>
    {
        public Vector2 Position;
        public bool StartsStroke;

        public NetworkSignPoint(Vector2 position, bool startsStroke)
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

        public bool Equals(NetworkSignPoint other)
        {
            return Position == other.Position && StartsStroke == other.StartsStroke;
        }
    }
}
