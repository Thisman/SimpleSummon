using SimpleSummon.Application;
using UnityEngine;

namespace SimpleSummon.Network
{
    internal sealed class UnityRandomSource : IRandomSource
    {
        public int Next() => Random.Range(0, int.MaxValue);
    }
}
