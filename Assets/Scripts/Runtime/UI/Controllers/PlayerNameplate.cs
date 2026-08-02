using SimpleSummon.Network;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleSummon.Runtime
{
    public sealed class PlayerNameplate : MonoBehaviour
    {
        [SerializeField] private NetworkPlayer networkPlayer;
        [SerializeField] private Text nicknameText;

        private Camera viewerCamera;

        private void OnEnable()
        {
            networkPlayer.NicknameChanged += HandleNicknameChanged;
            HandleNicknameChanged(networkPlayer.Nickname);
        }

        private void OnDisable()
        {
            networkPlayer.NicknameChanged -= HandleNicknameChanged;
        }

        private void LateUpdate()
        {
            if (viewerCamera == null || !viewerCamera.isActiveAndEnabled)
            {
                viewerCamera = Camera.main;
            }

            if (viewerCamera != null)
            {
                transform.rotation = viewerCamera.transform.rotation;
            }
        }

        private void HandleNicknameChanged(string value)
        {
            nicknameText.text = value;
        }
    }
}
