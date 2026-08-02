using UnityEngine;
using UnityEngine.UI;

namespace SimpleSummon.Runtime
{
    public sealed class PlayerNameplateView : MonoBehaviour
    {
        [SerializeField] private Text nicknameText;

        private Camera viewerCamera;

        public void SetNickname(string value) => nicknameText.text = value;

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
    }
}
