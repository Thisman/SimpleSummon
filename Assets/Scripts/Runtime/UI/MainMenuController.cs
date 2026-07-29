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

        private void Awake()
        {
            sessionService = NetworkSessionService.Instance;
            nicknameInput.text = NicknameStorage.Load();
            roomCodeInput.text = NetworkSessionService.NormalizeCode(roomCodeInput.text);
            statusText.text = sessionService.ConsumeLastMessage();

            nicknameInput.onValueChanged.AddListener(HandleInputChanged);
            roomCodeInput.onValueChanged.AddListener(HandleRoomCodeChanged);
            createRoomButton.onClick.AddListener(CreateRoom);
            joinRoomButton.onClick.AddListener(JoinRoom);
            sessionService.SessionChanged += Refresh;
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
            statusText.text = string.Empty;
            try
            {
                await sessionService.CreateRoomAsync(nicknameInput.text);
            }
            catch (Exception exception)
            {
                statusText.text = GetUserMessage(exception);
            }
        }

        private async void JoinRoom()
        {
            statusText.text = string.Empty;
            try
            {
                await sessionService.JoinRoomAsync(roomCodeInput.text, nicknameInput.text);
            }
            catch (Exception exception)
            {
                statusText.text = GetUserMessage(exception);
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

        private static string GetUserMessage(Exception exception)
        {
            string message = exception.Message.ToLowerInvariant();
            if (message.Contains("full"))
            {
                return "Комната заполнена.";
            }

            if (message.Contains("locked"))
            {
                return "Игра уже началась.";
            }

            if (message.Contains("not found") || message.Contains("code"))
            {
                return "Комната с таким кодом не найдена.";
            }

            return "Не удалось подключиться. Проверьте соединение и повторите попытку.";
        }
    }
}
