using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class TorchView : MonoBehaviour
    {
        [SerializeField] private GameObject container;
        [SerializeField] private RectTransform strengthBar;

        private float originalHeight;

        public void Initialize()
        {
            originalHeight = strengthBar.sizeDelta.y;
            container.SetActive(false);
        }

        public void Show(float strength)
        {
            container.SetActive(true);
            Vector2 size = strengthBar.sizeDelta;
            size.y = originalHeight * Mathf.Clamp01(strength / 100f);
            strengthBar.sizeDelta = size;
        }

        public void Hide() => container.SetActive(false);
    }
}
