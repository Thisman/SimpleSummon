using System.Collections.Generic;
using SimpleSummon.Network;
using UnityEngine;

namespace SimpleSummon.Runtime
{
    internal sealed class SignStrokeBuffer
    {
        public const int MaximumBatchSize = 32;

        private readonly float minimumPointDistance;
        private readonly List<NetworkSignPoint> points = new();
        private Vector2 previousPoint;

        public SignStrokeBuffer(float minimumPointDistance)
        {
            this.minimumPointDistance = minimumPointDistance;
        }

        public int Count => points.Count;

        public bool TryAdd(Vector2 point, bool startsStroke)
        {
            if (!startsStroke &&
                Vector2.Distance(previousPoint, point) < minimumPointDistance)
            {
                return false;
            }

            previousPoint = point;
            points.Add(new NetworkSignPoint(point, startsStroke));
            return true;
        }

        public NetworkSignPoint[] Take()
        {
            NetworkSignPoint[] result = points.ToArray();
            points.Clear();
            return result;
        }
    }
}
