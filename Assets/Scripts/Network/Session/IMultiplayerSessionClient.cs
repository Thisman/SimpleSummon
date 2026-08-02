using System.Threading.Tasks;
using Unity.Services.Multiplayer;

namespace SimpleSummon.Network
{
    internal interface IMultiplayerSessionClient
    {
        Task<ISession> CreateAsync(string nickname, int maximumPlayers);
        Task<ISession> JoinAsync(string code, string nickname);
        Task LeaveAsync(ISession session);
    }
}
