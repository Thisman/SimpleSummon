namespace SimpleSummon.Network
{
    public readonly struct SessionPlayerInfo
    {
        public SessionPlayerInfo(string id, string nickname, bool isHost)
        {
            Id = id;
            Nickname = nickname;
            IsHost = isHost;
        }

        public string Id { get; }
        public string Nickname { get; }
        public bool IsHost { get; }
    }
}
