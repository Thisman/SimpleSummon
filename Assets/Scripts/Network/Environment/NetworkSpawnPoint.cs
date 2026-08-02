using System.Collections.Generic;
using UnityEngine;

namespace SimpleSummon.Network
{
    public sealed class NetworkSpawnPoint : MonoBehaviour
    {
        private static readonly List<NetworkSpawnPoint> points = new();

        [SerializeField, Min(0)] private int index;

        private void OnEnable()
        {
            if (!points.Contains(this))
            {
                points.Add(this);
            }
        }

        private void OnDisable()
        {
            points.Remove(this);
        }

        public static bool TryGet(int requestedIndex, out NetworkSpawnPoint point)
        {
            foreach (NetworkSpawnPoint candidate in points)
            {
                if (candidate.index == requestedIndex)
                {
                    point = candidate;
                    return true;
                }
            }

            point = null;
            return false;
        }
    }
}
