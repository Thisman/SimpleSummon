using System;
using SimpleSummon.Network;
using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private MainMenuView view;

        private NetworkSessionService sessionService;
        private string statusKey;

        private void Awake()
        {
            sessionService = NetworkSessionService.Instance;
            view.SetInitialInput(
                NicknameStorage.Load(),
                NetworkSessionService.NormalizeCode(view.RoomCode));
            statusKey = GetSessionCloseKey(sessionService.ConsumeLastCloseReason());
            view.InputChanged += HandleInputChanged;
            view.CreateRoomRequested += CreateRoom;
            view.JoinRoomRequested += JoinRoom;
            sessionService.SessionChanged += Refresh;
            GameLocalizationController.AddLocaleChangedListener(RefreshStatus);
            Refresh();
        }

        private void OnDestroy()
        {
            view.InputChanged -= HandleInputChanged;
            view.CreateRoomRequested -= CreateRoom;
            view.JoinRoomRequested -= JoinRoom;
            if (sessionService != null)
            {
                sessionService.SessionChanged -= Refresh;
            }
            GameLocalizationController.RemoveLocaleChangedListener(RefreshStatus);
        }

        private void HandleInputChanged()
        {
            string normalized = NetworkSessionService.NormalizeCode(view.RoomCode);
            if (normalized != view.RoomCode)
            {
                view.SetRoomCode(normalized);
            }
            Refresh();
        }

        private async void CreateRoom()
        {
            ClearStatus();
            try
            {
                await sessionService.CreateRoomAsync(view.Nickname);
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
                await sessionService.JoinRoomAsync(view.RoomCode, view.Nickname);
            }
            catch (Exception exception)
            {
                SetStatus(GetUserMessageKey(exception));
            }
        }

        private void Refresh()
        {
            bool hasNickname = !string.IsNullOrWhiteSpace(view.Nickname);
            bool hasCode = !string.IsNullOrWhiteSpace(view.RoomCode);
            bool isBusy = sessionService.IsBusy;
            view.SetInteractionState(
                !isBusy,
                hasNickname && !isBusy,
                hasNickname && !isBusy,
                hasNickname && hasCode && !isBusy,
                isBusy);
        }

        private void SetStatus(string key)
        {
            statusKey = key;
            RefreshStatus();
        }

        private void ClearStatus()
        {
            statusKey = null;
            view.SetStatus(string.Empty);
        }

        private void RefreshStatus() => view.SetStatus(
            string.IsNullOrEmpty(statusKey)
                ? string.Empty
                : GameLocalizationController.Get(statusKey));

        private static string GetSessionCloseKey(SessionCloseReason reason) => reason switch
        {
            SessionCloseReason.HostClosed => "session.host_closed",
            SessionCloseReason.HostLost => "session.host_lost",
            _ => null
        };

        private static string GetUserMessageKey(Exception exception)
        {
            string message = exception.Message.ToLowerInvariant();
            if (message.Contains("full")) return "error.room_full";
            if (message.Contains("locked")) return "error.game_started";
            if (message.Contains("not found") || message.Contains("code"))
            {
                return "error.room_not_found";
            }
            return "error.connection_failed";
        }
    }
}
