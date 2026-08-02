using System.Collections.Generic;
using SimpleSummon.Application;
using SimpleSummon.Domain;
using UnityEngine;
using NumericsVector2 = System.Numerics.Vector2;

namespace SimpleSummon.Network
{
    internal static class NetworkSignPointMapper
    {
        public static SubmitSignPointsCommand ToCommand(
            ulong actorId,
            IReadOnlyList<NetworkSignPoint> points)
        {
            SignStrokePoint[] mapped = new SignStrokePoint[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                NetworkSignPoint point = points[i];
                mapped[i] = new SignStrokePoint(
                    new NumericsVector2(point.Position.x, point.Position.y),
                    point.StartsStroke);
            }

            return new SubmitSignPointsCommand(actorId, mapped);
        }

        public static EraseSignPointsCommand ToEraseCommand(
            ulong actorId,
            Vector2 position,
            float radius) =>
            new(actorId, new NumericsVector2(position.x, position.y), radius);

        public static NetworkSignPoint ToNetwork(SignStrokePoint point) =>
            new(
                new Vector2(point.Position.X, point.Position.Y),
                point.StartsStroke);
    }
}
