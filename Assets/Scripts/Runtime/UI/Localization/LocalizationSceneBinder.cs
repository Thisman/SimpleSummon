using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SimpleSummon.Runtime
{
    public static class LocalizationSceneBinder
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            GameLocalization.LocaleChanged += RefreshActiveScene;
            Bind(SceneManager.GetActiveScene());
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
                if (GameLocalization.TryGetKey(text.text, out string key))
                {
                    text.text = GameLocalization.Get(key);
                }
            }

            foreach (TMP_Text text in roots.SelectMany(x => x.GetComponentsInChildren<TMP_Text>(true)))
            {
                if (GameLocalization.TryGetKey(text.text, out string key))
                {
                    text.text = GameLocalization.Get(key);
                }
            }

            if (scene.name == "MainMenu")
            {
                BindLanguageButtons(roots);
            }
        }

        private static void BindLanguageButtons(GameObject[] roots)
        {
            Button russianButton = FindButton(roots, "RuButton");
            Button englishButton = FindButton(roots, "EngButton");
            MainMenuController menu = roots
                .SelectMany(x => x.GetComponentsInChildren<MainMenuController>(true))
                .FirstOrDefault();

            if (russianButton != null && englishButton != null && menu != null &&
                menu.GetComponent<LanguageButtons>() == null)
            {
                LanguageButtons buttons = menu.gameObject.AddComponent<LanguageButtons>();
                buttons.Configure(russianButton, englishButton);
            }
        }

        private static Button FindButton(GameObject[] roots, string objectName) =>
            roots.SelectMany(x => x.GetComponentsInChildren<Button>(true))
                .FirstOrDefault(x => x.name == objectName);
    }
}
