using System;
using System.Collections.Generic;
using Unity.Services.Multiplayer;

namespace SimpleSummon.Network
{
    internal static class SessionPlayerMapper
    {
        public static IReadOnlyList<SessionPlayerInfo> Map(ISession session)
        {
            if (session == null)
            {
                return Array.Empty<SessionPlayerInfo>();
            }

            List<SessionPlayerInfo> result = new(session.Players.Count);
            foreach (IReadOnlyPlayer player in session.Players)
            {
                string nickname = player.GetPlayerName();
                result.Add(new SessionPlayerInfo(
                    player.Id,
                    string.IsNullOrWhiteSpace(nickname) ? "Player" : nickname,
                    player.Id == session.Host));
            }

            result.Sort((left, right) =>
            {
                if (left.IsHost != right.IsHost)
                {
                    return left.IsHost ? -1 : 1;
                }
                return string.Compare(left.Id, right.Id, StringComparison.Ordinal);
            });
            return result;
        }
    }
}
