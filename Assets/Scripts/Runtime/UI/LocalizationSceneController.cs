using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SimpleSummon.Runtime
{
    public static class LocalizationSceneController
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            GameLocalizationController.AddLocaleChangedListener(RefreshActiveScene);
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode _) => Bind(scene);
        private static void RefreshActiveScene() => Bind(SceneManager.GetActiveScene());

        private static void Bind(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            foreach (Text text in roots.SelectMany(x => x.GetComponentsInChildren<Text>(true)))
            {
                if (GameLocalizationController.TryGetKey(text.text, out string key))
                {
                    text.text = GameLocalizationController.Get(key);
                }
            }

            foreach (TMP_Text text in roots.SelectMany(x => x.GetComponentsInChildren<TMP_Text>(true)))
            {
                if (GameLocalizationController.TryGetKey(text.text, out string key))
                {
                    text.text = GameLocalizationController.Get(key);
                }
            }

            if (scene.name == "MainMenu")
            {
                BindLanguageButtonsController(roots);
            }
        }

        private static void BindLanguageButtonsController(GameObject[] roots)
        {
            Button russianButton = FindButton(roots, "RuButton");
            Button englishButton = FindButton(roots, "EngButton");
            MainMenuView menu = roots
                .SelectMany(x => x.GetComponentsInChildren<MainMenuView>(true))
                .FirstOrDefault();

            if (russianButton != null && englishButton != null && menu != null &&
                menu.GetComponent<LanguageButtonsView>() == null)
            {
                LanguageButtonsView view = menu.gameObject.AddComponent<LanguageButtonsView>();
                LanguageButtonsController controller =
                    menu.gameObject.AddComponent<LanguageButtonsController>();
                view.Configure(russianButton, englishButton);
                controller.Configure(view);
            }
        }

        private static Button FindButton(GameObject[] roots, string objectName) =>
            roots.SelectMany(x => x.GetComponentsInChildren<Button>(true))
                .FirstOrDefault(x => x.name == objectName);
    }
}
