using System;
using System.Collections.Generic;
using UnityEngine;

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
            ["game.drawing_controls"] = ("Зажмите Shift + ЛКМ, чтобы стереть\nНажмите Esc чтобы выйти", "Hold Shift + LMB to erase\nPress Esc to exit"),
            ["game.draw_sign"] = ("Нарисуйте знак призыва", "Draw the summoning sign"),
            ["game.instructions"] = ("Для призыва нужен\n\n- Знак призыва\n- Сердце Босса\n- Чаша маны\n- to be continue\n- to be continue ", "The ritual requires\n\n- A summoning sign\n- The Boss's heart\n- A bowl of mana\n- to be continued\n- to be continued"),
            ["game.exit_reading"] = ("Нажмите Esc чтобы выйти из просмотра", "Press Esc to stop reading"),
            ["hud.health"] = ("Здоровье", "Health"),
            ["hud.boss_heart"] = ("Сердце босса", "Boss heart"),
            ["hud.artifact_resources"] = ("Ресурсы артефакта", "Artifact resources"),
            ["hud.artifact"] = ("Артефакт", "Artifact"),
            ["hud.fragments"] = ("Фрагменты", "Fragments"),
            ["hud.yes"] = ("Есть", "Yes"),
            ["hud.no"] = ("Нет", "No"),
            ["craft.title"] = ("Создание артефакта", "Craft the artifact"),
            ["craft.hint"] = ("Перетащите все ресурсы в одну ячейку\nEsc — выйти", "Drag all resources into one slot\nEsc — exit"),
            ["craft.exit_hint"] = ("Нажмите Esc, чтобы выйти из крафта", "Press Esc to exit crafting"),
            ["craft.resources"] = ("Ресурсы", "Resources"),
            ["craft.button"] = ("Скрафтить", "Craft"),
            ["craft.complete"] = ("Артефакт создан", "Artifact crafted"),
            ["interaction.craft"] = ("Зажмите E, чтобы создать артефакт", "Hold E to craft the artifact"),
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

        public static string GetSignPuzzleProgress(int collected, int total) =>
            SelectedCode == EnglishLocaleCode
                ? $"Collected {collected} of {total} fragments\nPress Esc to stop assembling the sign"
                : $"Собрано {collected} из {total} фрагментов\nНажмите Esc чтобы выйти из сборки знака";

        public static string FormatHealth(float current, float maximum) =>
            $"{Get("hud.health")}: {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(maximum)}";

        public static string FormatQuestFlag(string key, bool value) =>
            $"{Get(key)}: {Get(value ? "hud.yes" : "hud.no")}";

        public static string FormatQuestCount(string key, int current, int total) =>
            $"{Get(key)}: {current} / {total}";

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

}
