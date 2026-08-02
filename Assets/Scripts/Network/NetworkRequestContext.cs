namespace SimpleSummon.Network
{
    public readonly struct NetworkRequestContext
    {
        public NetworkRequestContext(ulong senderClientId, double serverTime)
        {
            SenderClientId = senderClientId;
            ServerTime = serverTime;
        }

        public ulong SenderClientId { get; }
        public double ServerTime { get; }
    }
}
