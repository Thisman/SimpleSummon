using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class LanguageButtonsController : MonoBehaviour
    {
        [SerializeField] private LanguageButtonsView view;

        public void Configure(LanguageButtonsView configuredView)
        {
            if (isActiveAndEnabled)
            {
                Detach();
            }
            view = configuredView;
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

        private void OnDisable() => Detach();

        private void Attach()
        {
            if (view == null) return;
            view.RussianRequested += SelectRussian;
            view.EnglishRequested += SelectEnglish;
            GameLocalizationController.AddLocaleChangedListener(Refresh);
        }

        private void Detach()
        {
            if (view != null)
            {
                view.RussianRequested -= SelectRussian;
                view.EnglishRequested -= SelectEnglish;
            }
            GameLocalizationController.RemoveLocaleChangedListener(Refresh);
        }

        private void SelectRussian() =>
            GameLocalizationController.SelectLocale(GameLocalizationController.RussianLocaleCode);

        private void SelectEnglish() =>
            GameLocalizationController.SelectLocale(GameLocalizationController.EnglishLocaleCode);

        private void Refresh()
        {
            view?.SetSelected(
                GameLocalizationController.IsSelected(GameLocalizationController.RussianLocaleCode),
                GameLocalizationController.IsSelected(GameLocalizationController.EnglishLocaleCode));
        }
    }
}
