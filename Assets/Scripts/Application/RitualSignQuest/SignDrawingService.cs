using System;
using System.Collections.Generic;
using System.Numerics;
using SimpleSummon.Domain;

namespace SimpleSummon.Application
{
    public sealed class SignDrawingService
    {
        public const int MaximumBatchSize = 32;
        public const float MinimumPointDistance = 0.001f;

        private readonly SignDrawingModel model;

        public SignDrawingService(SignDrawingModel model)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public bool Claim(ulong actorId) => model.TryClaim(actorId);
        public bool Release(ulong actorId) => model.TryRelease(actorId);
        public bool Finish(ulong actorId) => model.TryFinish(actorId);

        public bool Submit(SubmitSignPointsCommand command)
        {
            if (!model.IsOwnedBy(command.ActorId) || command.Points == null)
            {
                return false;
            }

            bool changed = false;
            int count = Math.Min(command.Points.Count, MaximumBatchSize);
            for (int i = 0; i < count; i++)
            {
                SignStrokePoint point = command.Points[i];
                Vector2? previous = model.Points.Count > 0
                    ? model.Points[model.Points.Count - 1].Position
                    : null;
                if (!SignPointValidationService.TryValidate(
                        previous,
                        point.Position,
                        point.StartsStroke,
                        MinimumPointDistance,
                        out Vector2 validated))
                {
                    continue;
                }

                model.Add(new SignStrokePoint(validated, point.StartsStroke));
                changed = true;
            }

            return changed;
        }

        public bool Erase(EraseSignPointsCommand command)
        {
            if (!model.IsOwnedBy(command.ActorId) ||
                !float.IsFinite(command.Radius) ||
                command.Radius <= 0f)
            {
                return false;
            }

            float radius = Math.Clamp(command.Radius, 0.005f, 0.2f);
            float squareRadius = radius * radius;
            List<SignStrokePoint> remaining = new(model.Points);
            bool removedPrevious = false;
            bool changed = false;

            for (int i = remaining.Count - 1; i >= 0; i--)
            {
                if (Vector2.DistanceSquared(remaining[i].Position, command.Position) <= squareRadius)
                {
                    remaining.RemoveAt(i);
                    removedPrevious = true;
                    changed = true;
                }
                else if (removedPrevious)
                {
                    if (i + 1 < remaining.Count)
                    {
                        remaining[i + 1] = remaining[i + 1].StartStroke();
                    }
                    removedPrevious = false;
                }
            }

            if (remaining.Count > 0 && removedPrevious)
            {
                remaining[0] = remaining[0].StartStroke();
            }

            if (changed)
            {
                model.ReplacePoints(remaining);
            }

            return changed;
        }
    }
}
