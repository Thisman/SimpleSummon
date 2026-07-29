using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SimpleSummon.Network
{
    [DefaultExecutionOrder(-1000)]
    [RequireComponent(typeof(NetworkManager))]
    public sealed class NetworkSessionService : MonoBehaviour
    {
        public const int MaximumPlayers = 5;
        public const string MenuSceneName = "MainMenu";
        public const string LobbySceneName = "Lobby";
        public const string GameSceneName = "Game";

        private static NetworkSessionService instance;

        private NetworkManager networkManager;
        private ISession session;
        private bool operationInProgress;

        public static NetworkSessionService Instance => instance;
        public event Action SessionChanged;
        public event Action<string> SessionClosed;

        public bool HasSession => session != null;
        public bool IsHost => session?.IsHost == true;
        public bool IsBusy => operationInProgress;
        public string JoinCode => session?.Code ?? string.Empty;
        public string LocalPlayerId => session?.CurrentPlayer?.Id ?? string.Empty;
        public string LastMessage { get; private set; }

        public IReadOnlyList<SessionPlayerInfo> Players
        {
            get
            {
                if (session == null)
                {
                    return Array.Empty<SessionPlayerInfo>();
                }

                List<SessionPlayerInfo> result = new(session.Players.Count);
                foreach (IReadOnlyPlayer player in session.Players)
                {
                    string nickname = player.GetPlayerName();
                    if (string.IsNullOrWhiteSpace(nickname))
                    {
                        nickname = "Player";
                    }

                    result.Add(new SessionPlayerInfo(
                        player.Id,
                        nickname,
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

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            networkManager = GetComponent<NetworkManager>();
            networkManager.NetworkConfig.ConnectionApproval = true;
            networkManager.ConnectionApprovalCallback = HandleConnectionApproval;
            networkManager.OnServerStarted += HandleServerStarted;
            DontDestroyOnLoad(gameObject);
            networkManager.OnClientDisconnectCallback += HandleClientDisconnected;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                Unsubscribe();
                if (networkManager != null)
                {
                    networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
                    networkManager.OnServerStarted -= HandleServerStarted;
                    networkManager.ConnectionApprovalCallback = null;
                    if (networkManager.SceneManager != null)
                    {
                        networkManager.SceneManager.OnLoadEventCompleted -=
                            HandleLoadEventCompleted;
                    }
                }
                instance = null;
            }
        }

        public async Task CreateRoomAsync(string nickname)
        {
            await RunSessionOperationAsync(async () =>
            {
                LastMessage = string.Empty;
                await PreparePlayerAsync(nickname);
                SessionOptions options = new SessionOptions
                {
                    MaxPlayers = MaximumPlayers,
                    IsPrivate = true,
                    Name = $"{nickname}'s room"
                }
                .WithRelayNetwork()
                .WithPlayerName(VisibilityPropertyOptions.Member);

                SetSession(await MultiplayerService.Instance.CreateSessionAsync(options));
                SceneManager.LoadScene(LobbySceneName);
            });
        }

        public async Task JoinRoomAsync(string code, string nickname)
        {
            await RunSessionOperationAsync(async () =>
            {
                LastMessage = string.Empty;
                await PreparePlayerAsync(nickname);
                JoinSessionOptions options = new JoinSessionOptions()
                    .WithPlayerName(VisibilityPropertyOptions.Member);
                SetSession(await MultiplayerService.Instance.JoinSessionByCodeAsync(
                    NormalizeCode(code),
                    options));
                SceneManager.LoadScene(LobbySceneName);
            });
        }

        public async Task StartGameAsync()
        {
            if (!IsHost || operationInProgress)
            {
                return;
            }

            operationInProgress = true;
            SessionChanged?.Invoke();
            try
            {
                session.AsHost().IsLocked = true;
                await session.AsHost().SavePropertiesAsync();
                networkManager.SceneManager.LoadScene(
                    GameSceneName,
                    LoadSceneMode.Single);
            }
            finally
            {
                operationInProgress = false;
                SessionChanged?.Invoke();
            }
        }

        public async Task LeaveAsync()
        {
            if (operationInProgress)
            {
                return;
            }

            operationInProgress = true;
            SessionChanged?.Invoke();
            try
            {
                ISession leavingSession = session;
                bool wasHost = leavingSession?.IsHost == true;
                ClearSession();

                if (leavingSession != null)
                {
                    if (wasHost)
                    {
                        await leavingSession.AsHost().DeleteAsync();
                    }
                    else
                    {
                        await leavingSession.LeaveAsync();
                    }
                }

                ShutdownNetwork();
                SceneManager.LoadScene(MenuSceneName);
            }
            finally
            {
                operationInProgress = false;
                SessionChanged?.Invoke();
            }
        }

        public static string NormalizeCode(string code)
        {
            return string.IsNullOrWhiteSpace(code)
                ? string.Empty
                : code.Replace(" ", string.Empty).Trim().ToUpperInvariant();
        }

        public string ConsumeLastMessage()
        {
            string message = LastMessage;
            LastMessage = string.Empty;
            return message;
        }

        private async Task PreparePlayerAsync(string nickname)
        {
            string normalizedNickname = nickname.Trim();
            if (normalizedNickname.Length == 0)
            {
                throw new ArgumentException("Nickname is required.", nameof(nickname));
            }

            NicknameStorage.Save(normalizedNickname);
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            await AuthenticationService.Instance.UpdatePlayerNameAsync(normalizedNickname);
        }

        private async Task RunSessionOperationAsync(Func<Task> operation)
        {
            if (operationInProgress)
            {
                return;
            }

            operationInProgress = true;
            SessionChanged?.Invoke();
            try
            {
                await operation();
            }
            finally
            {
                operationInProgress = false;
                SessionChanged?.Invoke();
            }
        }

        private void SetSession(ISession value)
        {
            ClearSession();
            session = value;
            session.PlayerJoined += HandlePlayerChanged;
            session.PlayerHasLeft += HandlePlayerChanged;
            session.PlayerPropertiesChanged += HandlePlayerPropertiesChanged;
            session.RemovedFromSession += HandleRemovedFromSession;
            SessionChanged?.Invoke();
        }

        private void ClearSession()
        {
            Unsubscribe();
            session = null;
            SessionChanged?.Invoke();
        }

        private void Unsubscribe()
        {
            if (session == null)
            {
                return;
            }

            session.PlayerJoined -= HandlePlayerChanged;
            session.PlayerHasLeft -= HandlePlayerChanged;
            session.PlayerPropertiesChanged -= HandlePlayerPropertiesChanged;
            session.RemovedFromSession -= HandleRemovedFromSession;
        }

        private void HandlePlayerChanged(string _)
        {
            SessionChanged?.Invoke();
        }

        private void HandlePlayerPropertiesChanged()
        {
            SessionChanged?.Invoke();
        }

        private void HandleRemovedFromSession()
        {
            ClearSession();
            ShutdownNetwork();
            LastMessage = "Хост закрыл комнату.";
            SessionClosed?.Invoke(LastMessage);
            SceneManager.LoadScene(MenuSceneName);
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            NetworkManager manager = networkManager;
            if (manager == null ||
                clientId != manager.LocalClientId ||
                session == null ||
                session.IsHost)
            {
                return;
            }

            LastMessage = "Соединение с хостом потеряно.";
            ClearSession();
            manager.Shutdown();
            SceneManager.LoadScene(MenuSceneName);
        }

        private static void HandleConnectionApproval(
            NetworkManager.ConnectionApprovalRequest _,
            NetworkManager.ConnectionApprovalResponse response)
        {
            response.Approved = true;
            response.CreatePlayerObject = false;
            response.Pending = false;
        }

        private void HandleServerStarted()
        {
            networkManager.SceneManager.OnLoadEventCompleted += HandleLoadEventCompleted;
        }

        private void HandleLoadEventCompleted(
            string sceneName,
            LoadSceneMode _,
            List<ulong> __,
            List<ulong> ___)
        {
            if (!networkManager.IsServer || sceneName != GameSceneName)
            {
                return;
            }

            foreach (ulong clientId in networkManager.ConnectedClientsIds)
            {
                NetworkClient client = networkManager.ConnectedClients[clientId];
                if (client.PlayerObject != null ||
                    !NetworkSpawnPoint.TryGet(
                        (int)(clientId % MaximumPlayers),
                        out NetworkSpawnPoint spawnPoint))
                {
                    continue;
                }

                GameObject player = Instantiate(
                    networkManager.NetworkConfig.PlayerPrefab,
                    spawnPoint.transform.position,
                    spawnPoint.transform.rotation);
                player.GetComponent<NetworkObject>()
                    .SpawnAsPlayerObject(clientId, true);
            }
        }

        private static void ShutdownNetwork()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }
        }
    }
}
