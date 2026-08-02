using System.Numerics;

namespace SimpleSummon.Domain
{
    public readonly struct SummonStrokePoint
    {
        public SummonStrokePoint(Vector2 position, bool startsStroke)
        {
            Position = position;
            StartsStroke = startsStroke;
        }

        public Vector2 Position { get; }
        public bool StartsStroke { get; }

        public SummonStrokePoint StartStroke() => new(Position, true);
    }
}
