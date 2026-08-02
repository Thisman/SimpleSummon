using System;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleSummon.Runtime
{
    public sealed class MainMenuView : MonoBehaviour
    {
        [SerializeField] private InputField nicknameInput;
        [SerializeField] private InputField roomCodeInput;
        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button joinRoomButton;
        [SerializeField] private Text statusText;
        [SerializeField] private GameObject progressIndicator;

        public event Action InputChanged;
        public event Action CreateRoomRequested;
        public event Action JoinRoomRequested;

        public string Nickname => nicknameInput.text;
        public string RoomCode => roomCodeInput.text;

        private void Awake()
        {
            nicknameInput.onValueChanged.AddListener(HandleInputChanged);
            roomCodeInput.onValueChanged.AddListener(HandleInputChanged);
            createRoomButton.onClick.AddListener(HandleCreateRoomRequested);
            joinRoomButton.onClick.AddListener(HandleJoinRoomRequested);
        }

        private void OnDestroy()
        {
            nicknameInput.onValueChanged.RemoveListener(HandleInputChanged);
            roomCodeInput.onValueChanged.RemoveListener(HandleInputChanged);
            createRoomButton.onClick.RemoveListener(HandleCreateRoomRequested);
            joinRoomButton.onClick.RemoveListener(HandleJoinRoomRequested);
        }

        public void SetInitialInput(string nickname, string roomCode)
        {
            nicknameInput.SetTextWithoutNotify(nickname);
            roomCodeInput.SetTextWithoutNotify(roomCode);
        }

        public void SetRoomCode(string roomCode) =>
            roomCodeInput.SetTextWithoutNotify(roomCode);

        public void SetInteractionState(
            bool nicknameEnabled,
            bool roomCodeEnabled,
            bool createEnabled,
            bool joinEnabled,
            bool showProgress)
        {
            nicknameInput.interactable = nicknameEnabled;
            roomCodeInput.interactable = roomCodeEnabled;
            createRoomButton.interactable = createEnabled;
            joinRoomButton.interactable = joinEnabled;
            progressIndicator.SetActive(showProgress);
        }

        public void SetStatus(string value) => statusText.text = value;

        private void HandleInputChanged(string _) => InputChanged?.Invoke();
        private void HandleCreateRoomRequested() => CreateRoomRequested?.Invoke();
        private void HandleJoinRoomRequested() => JoinRoomRequested?.Invoke();
    }
}
