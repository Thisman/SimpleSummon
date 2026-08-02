using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;

namespace SimpleSummon.Network
{
    internal sealed class MultiplayerSessionClient : IMultiplayerSessionClient
    {
        public async Task<ISession> CreateAsync(string nickname, int maximumPlayers)
        {
            string normalized = await PreparePlayerAsync(nickname);
            SessionOptions options = new SessionOptions
            {
                MaxPlayers = maximumPlayers,
                IsPrivate = true,
                Name = $"{normalized}'s room"
            }
            .WithRelayNetwork()
            .WithPlayerName(VisibilityPropertyOptions.Member);
            return await MultiplayerService.Instance.CreateSessionAsync(options);
        }

        public async Task<ISession> JoinAsync(string code, string nickname)
        {
            await PreparePlayerAsync(nickname);
            JoinSessionOptions options = new JoinSessionOptions()
                .WithPlayerName(VisibilityPropertyOptions.Member);
            return await MultiplayerService.Instance.JoinSessionByCodeAsync(code, options);
        }

        public async Task LeaveAsync(ISession session)
        {
            if (session == null)
            {
                return;
            }

            if (session.IsHost)
            {
                await session.AsHost().DeleteAsync();
            }
            else
            {
                await session.LeaveAsync();
            }
        }

        private static async Task<string> PreparePlayerAsync(string nickname)
        {
            string normalized = nickname?.Trim() ?? string.Empty;
            if (normalized.Length == 0)
            {
                throw new ArgumentException("Nickname is required.", nameof(nickname));
            }

            NicknameStorage.Save(normalized);
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            await AuthenticationService.Instance.UpdatePlayerNameAsync(normalized);
            return normalized;
        }
    }
}
