using System;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleSummon.Runtime
{
    public sealed class LanguageButtonsView : MonoBehaviour
    {
        [SerializeField] private Button russianButton;
        [SerializeField] private Button englishButton;
        [SerializeField] private Color activeColor = Color.white;
        [SerializeField] private Color inactiveColor = new(0.45f, 0.45f, 0.45f, 1f);

        public event Action RussianRequested;
        public event Action EnglishRequested;

        public void Configure(Button ruButton, Button enButton)
        {
            Detach();
            russianButton = ruButton;
            englishButton = enButton;
            Attach();
        }

        private void OnEnable() => Attach();
        private void OnDisable() => Detach();

        public void SetSelected(bool russianSelected, bool englishSelected)
        {
            SetColor(russianButton, russianSelected);
            SetColor(englishButton, englishSelected);
        }

        private void Attach()
        {
            if (russianButton == null || englishButton == null)
            {
                return;
            }
            russianButton.onClick.RemoveListener(HandleRussianRequested);
            englishButton.onClick.RemoveListener(HandleEnglishRequested);
            russianButton.onClick.AddListener(HandleRussianRequested);
            englishButton.onClick.AddListener(HandleEnglishRequested);
        }

        private void Detach()
        {
            russianButton?.onClick.RemoveListener(HandleRussianRequested);
            englishButton?.onClick.RemoveListener(HandleEnglishRequested);
        }

        private void SetColor(Button button, bool active)
        {
            if (button == null || button.targetGraphic == null)
            {
                return;
            }
            Color color = active ? activeColor : inactiveColor;
            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.selectedColor = color;
            button.colors = colors;
            button.targetGraphic.color = color;
        }

        private void HandleRussianRequested() => RussianRequested?.Invoke();
        private void HandleEnglishRequested() => EnglishRequested?.Invoke();
    }
}
