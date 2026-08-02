using System.Numerics;

namespace SimpleSummon.Domain
{
    public readonly struct SignStrokePoint
    {
        public SignStrokePoint(Vector2 position, bool startsStroke)
        {
            Position = position;
            StartsStroke = startsStroke;
        }

        public Vector2 Position { get; }
        public bool StartsStroke { get; }

        public SignStrokePoint StartStroke() => new(Position, true);
    }
}
