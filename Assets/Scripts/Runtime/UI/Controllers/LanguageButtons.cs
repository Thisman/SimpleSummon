using UnityEngine;
using UnityEngine.UI;

namespace SimpleSummon.Runtime
{
    public sealed class LanguageButtons : MonoBehaviour
    {
        [SerializeField] private Button russianButton;
        [SerializeField] private Button englishButton;
        [SerializeField] private Color activeColor = Color.white;
        [SerializeField] private Color inactiveColor = new(0.45f, 0.45f, 0.45f, 1f);
        private bool listening;

        public void Configure(Button ruButton, Button enButton)
        {
            Detach();
            russianButton = ruButton;
            englishButton = enButton;
            if (isActiveAndEnabled)
            {
                Attach();
                Refresh();
            }
        }

        private void OnEnable()
        {
            Attach();
            Refresh();
        }

        private void OnDisable()
        {
            Detach();
        }

        private void SelectRussian() => GameLocalization.SelectLocale(GameLocalization.RussianLocaleCode);
        private void SelectEnglish() => GameLocalization.SelectLocale(GameLocalization.EnglishLocaleCode);

        private void Refresh()
        {
            SetColor(russianButton, GameLocalization.IsSelected(GameLocalization.RussianLocaleCode));
            SetColor(englishButton, GameLocalization.IsSelected(GameLocalization.EnglishLocaleCode));
        }

        private void SetColor(Button button, bool active)
        {
            if (button != null && button.targetGraphic != null)
            {
                Color color = active ? activeColor : inactiveColor;
                ColorBlock colors = button.colors;
                colors.normalColor = color;
                colors.selectedColor = color;
                button.colors = colors;
                button.targetGraphic.color = color;
            }
        }

        private void Attach()
        {
            if (listening || russianButton == null || englishButton == null)
            {
                return;
            }

            russianButton.onClick.AddListener(SelectRussian);
            englishButton.onClick.AddListener(SelectEnglish);
            GameLocalization.LocaleChanged += Refresh;
            listening = true;
        }

        private void Detach()
        {
            if (!listening)
            {
                return;
            }

            russianButton.onClick.RemoveListener(SelectRussian);
            englishButton.onClick.RemoveListener(SelectEnglish);
            GameLocalization.LocaleChanged -= Refresh;
            listening = false;
        }
    }
}
