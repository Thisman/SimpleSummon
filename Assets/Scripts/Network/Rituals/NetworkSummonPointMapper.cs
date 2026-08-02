using System.Collections.Generic;
using SimpleSummon.Application;
using SimpleSummon.Domain;
using UnityEngine;
using NumericsVector2 = System.Numerics.Vector2;

namespace SimpleSummon.Network
{
    internal static class NetworkSummonPointMapper
    {
        public static SubmitSummonPointsCommand ToCommand(
            ulong actorId,
            IReadOnlyList<NetworkSummonPoint> points)
        {
            SummonStrokePoint[] mapped = new SummonStrokePoint[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                NetworkSummonPoint point = points[i];
                mapped[i] = new SummonStrokePoint(
                    new NumericsVector2(point.Position.x, point.Position.y),
                    point.StartsStroke);
            }

            return new SubmitSummonPointsCommand(actorId, mapped);
        }

        public static EraseSummonPointsCommand ToEraseCommand(
            ulong actorId,
            Vector2 position,
            float radius) =>
            new(actorId, new NumericsVector2(position.x, position.y), radius);

        public static NetworkSummonPoint ToNetwork(SummonStrokePoint point) =>
            new(
                new Vector2(point.Position.X, point.Position.Y),
                point.StartsStroke);
    }
}
