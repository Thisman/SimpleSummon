using UnityEngine;
using UnityEngine.UI;

namespace SimpleSummon.Runtime
{
    public sealed class InteractionPromptView : MonoBehaviour
    {
        [SerializeField] private Text promptText;
        [SerializeField] private InteractionProgressRing progressRing;

        private void Awake()
        {
            Hide();
        }

        public void Show(string text)
        {
            promptText.text = text;
            promptText.gameObject.SetActive(true);
        }

        public void SetProgress(float progress)
        {
            progressRing.gameObject.SetActive(progress > 0f);
            progressRing.SetProgress(progress);
        }

        public void Hide()
        {
            if (promptText != null)
            {
                promptText.gameObject.SetActive(false);
            }

            if (progressRing != null)
            {
                progressRing.gameObject.SetActive(false);
                progressRing.SetProgress(0f);
            }
        }
    }
}
