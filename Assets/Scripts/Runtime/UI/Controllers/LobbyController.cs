using System;
using System.Collections.Generic;
using SimpleSummon.Network;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleSummon.Runtime
{
    public sealed class LobbyController : MonoBehaviour
    {
        [SerializeField] private Text roomCodeText;
        [SerializeField] private Text playerCountText;
        [SerializeField] private Text statusText;
        [SerializeField] private Transform playerListRoot;
        [SerializeField] private LobbyPlayerEntryView playerEntryPrefab;
        [SerializeField] private Button copyCodeButton;
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button leaveButton;

        private readonly List<LobbyPlayerEntryView> entries = new();
        private NetworkSessionService sessionService;
        private string statusKey;

        private void Awake()
        {
            sessionService = NetworkSessionService.Instance;
            copyCodeButton.onClick.AddListener(CopyCode);
            startGameButton.onClick.AddListener(StartGame);
            leaveButton.onClick.AddListener(Leave);
            sessionService.SessionChanged += Refresh;
            sessionService.SessionClosed += HandleSessionClosed;
            GameLocalization.LocaleChanged += RefreshStatus;
            Refresh();
        }

        private void OnDestroy()
        {
            copyCodeButton.onClick.RemoveListener(CopyCode);
            startGameButton.onClick.RemoveListener(StartGame);
            leaveButton.onClick.RemoveListener(Leave);
            if (sessionService != null)
            {
                sessionService.SessionChanged -= Refresh;
                sessionService.SessionClosed -= HandleSessionClosed;
            }
            GameLocalization.LocaleChanged -= RefreshStatus;
        }

        private void CopyCode()
        {
            GUIUtility.systemCopyBuffer = sessionService.JoinCode;
            SetStatus("lobby.code_copied");
        }

        private async void StartGame()
        {
            ClearStatus();
            try
            {
                await sessionService.StartGameAsync();
            }
            catch (Exception)
            {
                SetStatus("error.start_game");
            }
        }

        private async void Leave()
        {
            ClearStatus();
            try
            {
                await sessionService.LeaveAsync();
            }
            catch (Exception)
            {
                SetStatus("error.leave_room");
            }
        }

        private void Refresh()
        {
            IReadOnlyList<SessionPlayerInfo> players = sessionService.Players;
            roomCodeText.text = sessionService.JoinCode;
            playerCountText.text = $"{players.Count} / {NetworkSessionService.MaximumPlayers}";
            startGameButton.gameObject.SetActive(sessionService.IsHost);
            startGameButton.interactable = sessionService.IsHost &&
                                           players.Count > 0 &&
                                           !sessionService.IsBusy;
            leaveButton.interactable = !sessionService.IsBusy;

            while (entries.Count < players.Count)
            {
                entries.Add(Instantiate(playerEntryPrefab, playerListRoot));
            }

            for (int i = 0; i < entries.Count; i++)
            {
                bool active = i < players.Count;
                entries[i].gameObject.SetActive(active);
                if (active)
                {
                    entries[i].Bind(players[i].Nickname, players[i].IsHost);
                }
            }
        }

        private void HandleSessionClosed(SessionCloseReason reason)
        {
            statusKey = reason == SessionCloseReason.HostClosed
                ? "session.host_closed"
                : "session.host_lost";
            RefreshStatus();
        }

        private void SetStatus(string key)
        {
            statusKey = key;
            RefreshStatus();
        }

        private void ClearStatus()
        {
            statusKey = null;
            statusText.text = string.Empty;
        }

        private void RefreshStatus()
        {
            statusText.text = string.IsNullOrEmpty(statusKey)
                ? string.Empty
                : GameLocalization.Get(statusKey);
        }
    }
}
