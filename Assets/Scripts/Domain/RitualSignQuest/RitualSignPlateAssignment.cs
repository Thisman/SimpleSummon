namespace SimpleSummon.Domain
{
    public readonly struct RitualSignPlateAssignment
    {
        public RitualSignPlateAssignment(ulong actorId, int plateIndex)
        {
            ActorId = actorId;
            PlateIndex = plateIndex;
        }

        public ulong ActorId { get; }
        public int PlateIndex { get; }
    }
}
