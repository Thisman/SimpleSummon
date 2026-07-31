using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SimpleSummon.Runtime
{
    public static class GameLocalization
    {
        public const string RussianLocaleCode = "ru";
        public const string EnglishLocaleCode = "en";

        private const string LocalePreferenceKey = "language";
        private static readonly Dictionary<string, string> RawTextKeys = new()
        {
            ["Комната заполнена."] = "error.room_full",
            ["Игра уже началась."] = "error.game_started",
            ["Комната с таким кодом не найдена."] = "error.room_not_found",
            ["Не удалось подключиться. Проверьте соединение и повторите попытку."] = "error.connection_failed",
            ["Код комнаты скопирован."] = "lobby.code_copied",
            ["Не удалось запустить игру."] = "error.start_game",
            ["Не удалось корректно покинуть комнату."] = "error.leave_room",
            ["Хост закрыл комнату."] = "session.host_closed",
            ["Соединение с хостом потеряно."] = "session.host_lost",
            ["Зажмите E, чтобы начать призыв"] = "interaction.start_summon",
            ["Нажмите Е, чтобы подняться из подземелья"] = "interaction.return_lab",
            ["Нажмите Е, чтобы спуститься в подземелье"] = "interaction.enter_dungeon",
            ["Нажмите E чтобы посмотреть инструкцию"] = "interaction.read"
        };

        private static readonly Dictionary<string, (string Ru, string En)> Entries = new()
        {
            ["menu.create_room"] = ("Создать комнату", "Create room"),
            ["menu.nickname"] = ("Никнейм", "Nickname"),
            ["menu.join"] = ("Присоединиться", "Join"),
            ["menu.room_code"] = ("Код комнаты", "Room code"),
            ["menu.connecting"] = ("Подключение…", "Connecting…"),
            ["lobby.copy_code"] = ("Копировать код", "Copy code"),
            ["lobby.leave"] = ("Выйти", "Leave"),
            ["lobby.start_game"] = ("Начать игру", "Start game"),
            ["lobby.room"] = ("Комната", "Room"),
            ["lobby.host"] = ("ХОСТ", "HOST"),
            ["lobby.code_copied"] = ("Код комнаты скопирован.", "Room code copied."),
            ["game.summon"] = ("Призвать", "Summon"),
            ["game.exit_drawing"] = ("Нажмите Esc чтобы выйти", "Press Esc to exit"),
            ["game.draw_sign"] = ("Нарисуйте знак призыва", "Draw the summoning sign"),
            ["game.instructions"] = ("Для призыва нужен\n\n- Знак призыва\n- Сердце Босса\n- Чаша маны\n- to be continue\n- to be continue ", "The ritual requires\n\n- A summoning sign\n- The Boss's heart\n- A bowl of mana\n- to be continued\n- to be continued"),
            ["game.exit_reading"] = ("Нажмите Esc чтобы выйти из просмотра", "Press Esc to stop reading"),
            ["interaction.start_summon"] = ("Зажмите E, чтобы начать призыв", "Hold E to begin the summoning"),
            ["interaction.return_lab"] = ("Нажмите Е, чтобы подняться из подземелья", "Press E to return to the laboratory"),
            ["interaction.enter_dungeon"] = ("Нажмите Е, чтобы спуститься в подземелье", "Press E to enter the dungeon"),
            ["interaction.read"] = ("Нажмите E чтобы посмотреть инструкцию", "Press E to read the instructions"),
            ["error.room_full"] = ("Комната заполнена.", "The room is full."),
            ["error.game_started"] = ("Игра уже началась.", "The game has already started."),
            ["error.room_not_found"] = ("Комната с таким кодом не найдена.", "No room was found with that code."),
            ["error.connection_failed"] = ("Не удалось подключиться. Проверьте соединение и повторите попытку.", "Could not connect. Check your connection and try again."),
            ["error.start_game"] = ("Не удалось запустить игру.", "Could not start the game."),
            ["error.leave_room"] = ("Не удалось корректно покинуть комнату.", "Could not leave the room correctly."),
            ["session.host_closed"] = ("Хост закрыл комнату.", "The host closed the room."),
            ["session.host_lost"] = ("Соединение с хостом потеряно.", "Connection to the host was lost.")
        };
        private static readonly Dictionary<string, (string Ru, string En)> SignPuzzleEntries = new()
        {
            ["interaction.build_sign"] = ("Нажмите E, чтобы собрать знак", "Press E to assemble the sign"),
            ["game.exit_sign_puzzle"] = ("Нажмите Esc чтобы выйти из сборки знака", "Press Esc to stop assembling the sign"),
            ["game.assemble_sign"] = ("Соберите знак", "Assemble the sign")
        };
        private static string selectedCode;

        public static event Action LocaleChanged;

        public static bool IsSelected(string code) => SelectedCode == code;

        private static string SelectedCode
        {
            get
            {
                selectedCode ??= PlayerPrefs.GetString(LocalePreferenceKey, RussianLocaleCode);
                return selectedCode;
            }
        }

        public static void SelectLocale(string code, bool save = true)
        {
            selectedCode = code == EnglishLocaleCode ? EnglishLocaleCode : RussianLocaleCode;
            if (save)
            {
                PlayerPrefs.SetString(LocalePreferenceKey, selectedCode);
                PlayerPrefs.Save();
            }

            LocaleChanged?.Invoke();
        }

        public static string Get(string key)
        {
            if (!Entries.TryGetValue(key, out var entry) &&
                !SignPuzzleEntries.TryGetValue(key, out entry))
            {
                return key;
            }

            return SelectedCode == EnglishLocaleCode ? entry.En : entry.Ru;
        }

        public static bool TryGetKey(string value, out string key)
        {
            string normalized = value?.Replace("\r\n", "\n").Trim();
            foreach (var pair in SignPuzzleEntries)
            {
                if (pair.Value.Ru.Trim() == normalized || pair.Value.En.Trim() == normalized)
                {
                    key = pair.Key;
                    return true;
                }
            }
            foreach (var pair in Entries)
            {
                if (pair.Value.Ru.Trim() == normalized || pair.Value.En.Trim() == normalized)
                {
                    key = pair.Key;
                    return true;
                }
            }

            key = null;
            return false;
        }

        public static string TranslateRaw(string value)
        {
            if (value == null)
            {
                return null;
            }

            if (RawTextKeys.TryGetValue(value.Trim(), out string key) ||
                TryGetKey(value, out key))
            {
                return Get(key);
            }

            return value;
        }
    }

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
                Button ru = FindButton(roots, "RuButton");
                Button en = FindButton(roots, "EngButton");
                MainMenuController menu = roots.SelectMany(x => x.GetComponentsInChildren<MainMenuController>(true)).FirstOrDefault();
                if (ru != null && en != null && menu != null && menu.GetComponent<LanguageButtons>() == null)
                {
                    var buttons = menu.gameObject.AddComponent<LanguageButtons>();
                    buttons.Configure(ru, en);
                }
            }
        }

        private static Button FindButton(GameObject[] roots, string objectName)
        {
            return roots.SelectMany(x => x.GetComponentsInChildren<Button>(true)).FirstOrDefault(x => x.name == objectName);
        }
    }
}
