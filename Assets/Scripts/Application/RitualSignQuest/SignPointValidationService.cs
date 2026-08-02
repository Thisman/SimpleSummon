using System;
using System.Numerics;

namespace SimpleSummon.Application
{
    public static class SignPointValidationService
    {
        public static bool TryValidate(
            Vector2? previousPoint,
            Vector2 submittedPoint,
            bool startsStroke,
            float minimumPointDistance,
            out Vector2 validatedPoint)
        {
            if (!float.IsFinite(minimumPointDistance) ||
                minimumPointDistance < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minimumPointDistance),
                    "Minimum point distance must be finite and non-negative.");
            }

            if (!float.IsFinite(submittedPoint.X) ||
                !float.IsFinite(submittedPoint.Y))
            {
                validatedPoint = default;
                return false;
            }

            validatedPoint = new Vector2(
                Math.Clamp(submittedPoint.X, 0f, 1f),
                Math.Clamp(submittedPoint.Y, 0f, 1f));

            if (!startsStroke &&
                previousPoint.HasValue &&
                Vector2.DistanceSquared(previousPoint.Value, validatedPoint) <
                minimumPointDistance * minimumPointDistance)
            {
                validatedPoint = default;
                return false;
            }

            return true;
        }
    }
}
