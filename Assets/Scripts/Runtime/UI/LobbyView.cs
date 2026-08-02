using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleSummon.Runtime
{
    public sealed class LobbyView : MonoBehaviour
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

        public event Action CopyCodeRequested;
        public event Action StartGameRequested;
        public event Action LeaveRequested;

        private void Awake()
        {
            copyCodeButton.onClick.AddListener(HandleCopyCodeRequested);
            startGameButton.onClick.AddListener(HandleStartGameRequested);
            leaveButton.onClick.AddListener(HandleLeaveRequested);
        }

        private void OnDestroy()
        {
            copyCodeButton.onClick.RemoveListener(HandleCopyCodeRequested);
            startGameButton.onClick.RemoveListener(HandleStartGameRequested);
            leaveButton.onClick.RemoveListener(HandleLeaveRequested);
        }

        public void SetSession(
            string joinCode,
            IReadOnlyList<string> nicknames,
            IReadOnlyList<bool> hostFlags,
            int maximumPlayers,
            bool isHost,
            bool isBusy)
        {
            roomCodeText.text = joinCode;
            playerCountText.text = $"{nicknames.Count} / {maximumPlayers}";
            startGameButton.gameObject.SetActive(isHost);
            startGameButton.interactable = isHost && nicknames.Count > 0 && !isBusy;
            leaveButton.interactable = !isBusy;

            while (entries.Count < nicknames.Count)
            {
                entries.Add(Instantiate(playerEntryPrefab, playerListRoot));
            }

            for (int i = 0; i < entries.Count; i++)
            {
                bool active = i < nicknames.Count;
                entries[i].gameObject.SetActive(active);
                if (active)
                {
                    entries[i].Bind(nicknames[i], hostFlags[i]);
                }
            }
        }

        public void SetStatus(string value) => statusText.text = value;

        private void HandleCopyCodeRequested() => CopyCodeRequested?.Invoke();
        private void HandleStartGameRequested() => StartGameRequested?.Invoke();
        private void HandleLeaveRequested() => LeaveRequested?.Invoke();
    }
}
