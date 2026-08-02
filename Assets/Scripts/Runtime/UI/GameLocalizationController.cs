using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleSummon.Runtime
{
    public static class GameLocalizationController
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
            ["Нажмите E, чтобы спуститься в подземелье"] = "interaction.enter_dungeon",
            ["Нажмите Е чтобы взять факел"] = "interaction.take_torch",
            ["Нажмите E чтобы взять факел"] = "interaction.take_torch"
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
            ["game.submit_ritual_sign"] = ("Готово", "Done"),
            ["game.exit_drawing"] = ("Нажмите Esc чтобы выйти", "Press Esc to exit"),
            ["game.drawing_controls"] = ("Зажмите Shift + ЛКМ, чтобы стереть\nНажмите Esc чтобы выйти", "Hold Shift + LMB to erase\nPress Esc to exit"),
            ["game.draw_sign"] = ("Нарисуйте знак призыва", "Draw the summoning sign"),
            ["hud.health"] = ("Здоровье", "Health"),
            ["instruction.sign"] = ("Нарисовать знак", "Draw the sign"),
            ["instruction.boss"] = ("Достань сердце", "Obtain the heart"),
            ["interaction.start_summon"] = ("Зажмите E, чтобы начать призыв", "Hold E to begin the summoning"),
            ["interaction.return_lab"] = ("Нажмите Е, чтобы подняться из подземелья", "Press E to return to the laboratory"),
            ["interaction.enter_dungeon"] = ("Нажмите Е, чтобы спуститься в подземелье", "Press E to enter the dungeon"),
            ["interaction.take_torch"] = ("Нажмите Е чтобы взять факел", "Press E to take the torch"),
            ["error.room_full"] = ("Комната заполнена.", "The room is full."),
            ["error.game_started"] = ("Игра уже началась.", "The game has already started."),
            ["error.room_not_found"] = ("Комната с таким кодом не найдена.", "No room was found with that code."),
            ["error.connection_failed"] = ("Не удалось подключиться. Проверьте соединение и повторите попытку.", "Could not connect. Check your connection and try again."),
            ["error.start_game"] = ("Не удалось запустить игру.", "Could not start the game."),
            ["error.leave_room"] = ("Не удалось корректно покинуть комнату.", "Could not leave the room correctly."),
            ["session.host_closed"] = ("Хост закрыл комнату.", "The host closed the room."),
            ["session.host_lost"] = ("Соединение с хостом потеряно.", "Connection to the host was lost.")
        };
        private static string selectedCode;

        private static event Action LocaleChanged;

        public static bool IsSelected(string code) => SelectedCode == code;

        public static void AddLocaleChangedListener(Action listener)
        {
            if (listener == null)
            {
                return;
            }

            LocaleChanged -= listener;
            LocaleChanged += listener;
            listener();
        }

        public static void RemoveLocaleChangedListener(Action listener) => LocaleChanged -= listener;

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
            if (!Entries.TryGetValue(key, out var entry))
            {
                return key;
            }

            return SelectedCode == EnglishLocaleCode ? entry.En : entry.Ru;
        }

        public static string FormatHealth(float current, float maximum) =>
            $"{Get("hud.health")}: {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(maximum)}";

        public static bool TryGetKey(string value, out string key)
        {
            string normalized = value?.Replace("\r\n", "\n").Trim();
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
