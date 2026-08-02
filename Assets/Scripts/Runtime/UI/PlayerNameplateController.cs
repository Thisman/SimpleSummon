using SimpleSummon.Network;
using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class PlayerNameplateController : MonoBehaviour
    {
        [SerializeField] private NetworkPlayer networkPlayer;
        [SerializeField] private PlayerNameplateView view;

        private void OnEnable()
        {
            networkPlayer.NicknameChanged += HandleNicknameChanged;
            HandleNicknameChanged(networkPlayer.Nickname);
        }

        private void OnDisable() =>
            networkPlayer.NicknameChanged -= HandleNicknameChanged;

        private void HandleNicknameChanged(string value) => view.SetNickname(value);
    }
}
