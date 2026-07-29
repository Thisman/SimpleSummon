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

        private void Awake()
        {
            sessionService = NetworkSessionService.Instance;
            copyCodeButton.onClick.AddListener(CopyCode);
            startGameButton.onClick.AddListener(StartGame);
            leaveButton.onClick.AddListener(Leave);
            sessionService.SessionChanged += Refresh;
            sessionService.SessionClosed += HandleSessionClosed;
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
        }

        private void CopyCode()
        {
            GUIUtility.systemCopyBuffer = sessionService.JoinCode;
            statusText.text = "Код комнаты скопирован.";
        }

        private async void StartGame()
        {
            statusText.text = string.Empty;
            try
            {
                await sessionService.StartGameAsync();
            }
            catch (Exception)
            {
                statusText.text = "Не удалось запустить игру.";
            }
        }

        private async void Leave()
        {
            statusText.text = string.Empty;
            try
            {
                await sessionService.LeaveAsync();
            }
            catch (Exception)
            {
                statusText.text = "Не удалось корректно покинуть комнату.";
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

        private void HandleSessionClosed(string message)
        {
            statusText.text = message;
        }
    }
}
