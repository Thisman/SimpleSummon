using System;
using SimpleSummon.Network;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleSummon.Runtime
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private InputField nicknameInput;
        [SerializeField] private InputField roomCodeInput;
        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button joinRoomButton;
        [SerializeField] private Text statusText;
        [SerializeField] private GameObject progressIndicator;

        private NetworkSessionService sessionService;
        private string statusKey;
        private string statusRaw;

        private void Awake()
        {
            sessionService = NetworkSessionService.Instance;
            nicknameInput.text = NicknameStorage.Load();
            roomCodeInput.text = NetworkSessionService.NormalizeCode(roomCodeInput.text);
            statusRaw = sessionService.ConsumeLastMessage();
            RefreshStatus();

            nicknameInput.onValueChanged.AddListener(HandleInputChanged);
            roomCodeInput.onValueChanged.AddListener(HandleRoomCodeChanged);
            createRoomButton.onClick.AddListener(CreateRoom);
            joinRoomButton.onClick.AddListener(JoinRoom);
            sessionService.SessionChanged += Refresh;
            GameLocalization.LocaleChanged += RefreshStatus;
            Refresh();
        }

        private void OnDestroy()
        {
            nicknameInput.onValueChanged.RemoveListener(HandleInputChanged);
            roomCodeInput.onValueChanged.RemoveListener(HandleRoomCodeChanged);
            createRoomButton.onClick.RemoveListener(CreateRoom);
            joinRoomButton.onClick.RemoveListener(JoinRoom);
            if (sessionService != null)
            {
                sessionService.SessionChanged -= Refresh;
            }
            GameLocalization.LocaleChanged -= RefreshStatus;
        }

        private void HandleInputChanged(string _)
        {
            Refresh();
        }

        private void HandleRoomCodeChanged(string value)
        {
            string normalized = NetworkSessionService.NormalizeCode(value);
            if (normalized != value)
            {
                roomCodeInput.SetTextWithoutNotify(normalized);
            }

            Refresh();
        }

        private async void CreateRoom()
        {
            ClearStatus();
            try
            {
                await sessionService.CreateRoomAsync(nicknameInput.text);
            }
            catch (Exception exception)
            {
                SetStatus(GetUserMessageKey(exception));
            }
        }

        private async void JoinRoom()
        {
            ClearStatus();
            try
            {
                await sessionService.JoinRoomAsync(roomCodeInput.text, nicknameInput.text);
            }
            catch (Exception exception)
            {
                SetStatus(GetUserMessageKey(exception));
            }
        }

        private void Refresh()
        {
            bool hasNickname = !string.IsNullOrWhiteSpace(nicknameInput.text);
            bool hasCode = !string.IsNullOrWhiteSpace(roomCodeInput.text);
            bool isBusy = sessionService.IsBusy;

            nicknameInput.interactable = !isBusy;
            roomCodeInput.interactable = hasNickname && !isBusy;
            createRoomButton.interactable = hasNickname && !isBusy;
            joinRoomButton.interactable = hasNickname && hasCode && !isBusy;
            progressIndicator.SetActive(isBusy);
        }

        private void SetStatus(string key)
        {
            statusKey = key;
            statusRaw = null;
            RefreshStatus();
        }

        private void ClearStatus()
        {
            statusKey = null;
            statusRaw = null;
            statusText.text = string.Empty;
        }

        private void RefreshStatus()
        {
            statusText.text = !string.IsNullOrEmpty(statusKey)
                ? GameLocalization.Get(statusKey)
                : GameLocalization.TranslateRaw(statusRaw ?? string.Empty);
        }

        private static string GetUserMessageKey(Exception exception)
        {
            string message = exception.Message.ToLowerInvariant();
            if (message.Contains("full"))
            {
                return "error.room_full";
            }

            if (message.Contains("locked"))
            {
                return "error.game_started";
            }

            if (message.Contains("not found") || message.Contains("code"))
            {
                return "error.room_not_found";
            }

            return "error.connection_failed";
        }
    }
}
