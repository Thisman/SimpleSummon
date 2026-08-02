using System;
using System.Collections.Generic;
using SimpleSummon.Network;
using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class LobbyController : MonoBehaviour
    {
        [SerializeField] private LobbyView view;

        private NetworkSessionService sessionService;
        private string statusKey;

        private void Awake()
        {
            sessionService = NetworkSessionService.Instance;
            view.CopyCodeRequested += CopyCode;
            view.StartGameRequested += StartGame;
            view.LeaveRequested += Leave;
            sessionService.SessionChanged += Refresh;
            sessionService.SessionClosed += HandleSessionClosed;
            GameLocalizationController.AddLocaleChangedListener(RefreshStatus);
            Refresh();
        }

        private void OnDestroy()
        {
            view.CopyCodeRequested -= CopyCode;
            view.StartGameRequested -= StartGame;
            view.LeaveRequested -= Leave;
            if (sessionService != null)
            {
                sessionService.SessionChanged -= Refresh;
                sessionService.SessionClosed -= HandleSessionClosed;
            }
            GameLocalizationController.RemoveLocaleChangedListener(RefreshStatus);
        }

        private void CopyCode()
        {
            GUIUtility.systemCopyBuffer = sessionService.JoinCode;
            SetStatus("lobby.code_copied");
        }

        private async void StartGame()
        {
            ClearStatus();
            try { await sessionService.StartGameAsync(); }
            catch (Exception) { SetStatus("error.start_game"); }
        }

        private async void Leave()
        {
            ClearStatus();
            try { await sessionService.LeaveAsync(); }
            catch (Exception) { SetStatus("error.leave_room"); }
        }

        private void Refresh()
        {
            IReadOnlyList<SessionPlayerInfo> players = sessionService.Players;
            string[] nicknames = new string[players.Count];
            bool[] hostFlags = new bool[players.Count];
            for (int i = 0; i < players.Count; i++)
            {
                nicknames[i] = players[i].Nickname;
                hostFlags[i] = players[i].IsHost;
            }
            view.SetSession(
                sessionService.JoinCode,
                nicknames,
                hostFlags,
                NetworkSessionService.MaximumPlayers,
                sessionService.IsHost,
                sessionService.IsBusy);
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
            view.SetStatus(string.Empty);
        }

        private void RefreshStatus() => view.SetStatus(
            string.IsNullOrEmpty(statusKey)
                ? string.Empty
                : GameLocalizationController.Get(statusKey));
    }
}
