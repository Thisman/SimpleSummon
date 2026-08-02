using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
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
        private IMultiplayerSessionClient sessionClient;
        private NetworkGameSpawner gameSpawner;
        private bool operationInProgress;

        public static NetworkSessionService Instance => instance;
        public event Action SessionChanged;
        public event Action<SessionCloseReason> SessionClosed;

        public bool HasSession => session != null;
        public bool IsHost => session?.IsHost == true;
        public bool IsBusy => operationInProgress;
        public string JoinCode => session?.Code ?? string.Empty;
        public string LocalPlayerId => session?.CurrentPlayer?.Id ?? string.Empty;
        public SessionCloseReason LastCloseReason { get; private set; }

        public IReadOnlyList<SessionPlayerInfo> Players => SessionPlayerMapper.Map(session);

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            networkManager = GetComponent<NetworkManager>();
            sessionClient = new MultiplayerSessionClient();
            gameSpawner = new NetworkGameSpawner(
                networkManager,
                GameSceneName,
                MaximumPlayers);
            gameSpawner.ConfigureApproval();
            networkManager.OnServerStarted += HandleServerStarted;
            networkManager.OnTransportFailure += HandleTransportFailure;
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
                    networkManager.OnTransportFailure -= HandleTransportFailure;
                    gameSpawner.Unsubscribe();
                }
                instance = null;
            }
        }

        public async Task CreateRoomAsync(string nickname)
        {
            await RunSessionOperationAsync(async () =>
            {
                LastCloseReason = SessionCloseReason.None;
                SetSession(await sessionClient.CreateAsync(nickname, MaximumPlayers));
                SceneManager.LoadScene(LobbySceneName);
            });
        }

        public async Task JoinRoomAsync(string code, string nickname)
        {
            await RunSessionOperationAsync(async () =>
            {
                LastCloseReason = SessionCloseReason.None;
                SetSession(await sessionClient.JoinAsync(NormalizeCode(code), nickname));
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
                ClearSession();
                await sessionClient.LeaveAsync(leavingSession);

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

        public SessionCloseReason ConsumeLastCloseReason()
        {
            SessionCloseReason reason = LastCloseReason;
            LastCloseReason = SessionCloseReason.None;
            return reason;
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
            LastCloseReason = SessionCloseReason.HostClosed;
            SessionClosed?.Invoke(LastCloseReason);
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

            LastCloseReason = SessionCloseReason.HostLost;
            ClearSession();
            manager.Shutdown();
            SceneManager.LoadScene(MenuSceneName);
        }

        private void HandleServerStarted()
        {
            gameSpawner.Subscribe();
        }

        private void HandleTransportFailure()
        {
            ClearSession();
            SceneManager.LoadScene(MenuSceneName);
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
