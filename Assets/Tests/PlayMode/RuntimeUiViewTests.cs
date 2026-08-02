using System.Reflection;
using NUnit.Framework;
using SimpleSummon.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleSummon.Tests.PlayMode
{
    public sealed class RuntimeUiViewTests
    {
        [Test]
        public void MainMenuView_RaisesInputAndButtonEventsAndAppliesState()
        {
            GameObject root = new("Main Menu");
            root.SetActive(false);
            InputField nickname = CreateInputField(root, "Nickname");
            InputField roomCode = CreateInputField(root, "Room Code");
            Button create = CreateButton(root, "Create");
            Button join = CreateButton(root, "Join");
            Text status = CreateText(root, "Status");
            GameObject progress = Child(root, "Progress");
            MainMenuView view = root.AddComponent<MainMenuView>();
            SetField(view, "nicknameInput", nickname);
            SetField(view, "roomCodeInput", roomCode);
            SetField(view, "createRoomButton", create);
            SetField(view, "joinRoomButton", join);
            SetField(view, "statusText", status);
            SetField(view, "progressIndicator", progress);
            int inputChanges = 0;
            int createRequests = 0;
            int joinRequests = 0;
            view.InputChanged += () => inputChanges++;
            view.CreateRoomRequested += () => createRequests++;
            view.JoinRoomRequested += () => joinRequests++;

            try
            {
                root.SetActive(true);
                view.SetInitialInput("Mage", "ABCD");
                Assert.That(inputChanges, Is.Zero);
                Assert.That(view.Nickname, Is.EqualTo("Mage"));
                Assert.That(view.RoomCode, Is.EqualTo("ABCD"));

                nickname.text = "Wizard";
                create.onClick.Invoke();
                join.onClick.Invoke();
                Assert.That(inputChanges, Is.EqualTo(1));
                Assert.That(createRequests, Is.EqualTo(1));
                Assert.That(joinRequests, Is.EqualTo(1));

                view.SetInteractionState(false, true, false, true, true);
                Assert.That(nickname.interactable, Is.False);
                Assert.That(roomCode.interactable, Is.True);
                Assert.That(create.interactable, Is.False);
                Assert.That(join.interactable, Is.True);
                Assert.That(progress.activeSelf, Is.True);

                view.SetStatus("Failed");
                Assert.That(status.text, Is.EqualTo("Failed"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void LobbyView_BindsRosterHostStateBusyStateAndEvents()
        {
            GameObject root = new("Lobby");
            root.SetActive(false);
            Text code = CreateText(root, "Code");
            Text count = CreateText(root, "Count");
            Text status = CreateText(root, "Status");
            GameObject listRoot = Child(root, "Players");
            LobbyPlayerEntryView prefab = CreateLobbyEntry(root);
            prefab.gameObject.SetActive(false);
            Button copy = CreateButton(root, "Copy");
            Button start = CreateButton(root, "Start");
            Button leave = CreateButton(root, "Leave");
            LobbyView view = root.AddComponent<LobbyView>();
            SetField(view, "roomCodeText", code);
            SetField(view, "playerCountText", count);
            SetField(view, "statusText", status);
            SetField(view, "playerListRoot", listRoot.transform);
            SetField(view, "playerEntryPrefab", prefab);
            SetField(view, "copyCodeButton", copy);
            SetField(view, "startGameButton", start);
            SetField(view, "leaveButton", leave);
            int copies = 0;
            int starts = 0;
            int leaves = 0;
            view.CopyCodeRequested += () => copies++;
            view.StartGameRequested += () => starts++;
            view.LeaveRequested += () => leaves++;

            try
            {
                root.SetActive(true);
                view.SetSession(
                    "ROOM",
                    new[] { "Host", "Client" },
                    new[] { true, false },
                    4,
                    true,
                    false);

                Assert.That(code.text, Is.EqualTo("ROOM"));
                Assert.That(count.text, Is.EqualTo("2 / 4"));
                Assert.That(start.gameObject.activeSelf, Is.True);
                Assert.That(start.interactable, Is.True);
                Assert.That(leave.interactable, Is.True);
                Assert.That(listRoot.transform.childCount, Is.EqualTo(2));

                copy.onClick.Invoke();
                start.onClick.Invoke();
                leave.onClick.Invoke();
                Assert.That(copies, Is.EqualTo(1));
                Assert.That(starts, Is.EqualTo(1));
                Assert.That(leaves, Is.EqualTo(1));

                view.SetSession("ROOM", new[] { "Client" }, new[] { false }, 4, false, true);
                Assert.That(start.gameObject.activeSelf, Is.False);
                Assert.That(leave.interactable, Is.False);
                Assert.That(listRoot.transform.GetChild(1).gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void TorchView_InitializeShowClampAndHide_UpdateVisibilityAndBar()
        {
            GameObject root = new("Torch View");
            GameObject container = new("Container");
            RectTransform bar = new GameObject("Bar", typeof(RectTransform))
                .GetComponent<RectTransform>();
            container.transform.SetParent(root.transform);
            bar.SetParent(container.transform);
            bar.sizeDelta = new Vector2(10f, 200f);
            TorchView view = root.AddComponent<TorchView>();
            SetField(view, "container", container);
            SetField(view, "strengthBar", bar);

            try
            {
                view.Initialize();
                Assert.That(container.activeSelf, Is.False);

                view.Show(25f);
                Assert.That(container.activeSelf, Is.True);
                Assert.That(bar.sizeDelta.y, Is.EqualTo(50f));

                view.Show(200f);
                Assert.That(bar.sizeDelta.y, Is.EqualTo(200f));
                view.Show(-10f);
                Assert.That(bar.sizeDelta.y, Is.Zero);

                view.Hide();
                Assert.That(container.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void IngredientsView_SetCounts_UpdatesBothLines()
        {
            GameObject root = new("Ingredients View");
            TextMeshProUGUI text = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI))
                .GetComponent<TextMeshProUGUI>();
            text.transform.SetParent(root.transform);
            IngredientsView view = root.AddComponent<IngredientsView>();
            SetField(view, "ingredientsText", text);

            try
            {
                view.SetCounts(2, 3);

                Assert.That(text.text, Is.EqualTo("bottle_C_green - 2\nbottle_C_brown - 3"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PlayerStatsView_SetHealth_UsesCurrentLocaleAndRoundsUp()
        {
            GameObject root = new("Stats View");
            TextMeshProUGUI text = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI))
                .GetComponent<TextMeshProUGUI>();
            text.transform.SetParent(root.transform);
            PlayerStatsView view = root.AddComponent<PlayerStatsView>();
            SetField(view, "healthText", text);
            GameLocalizationController.SelectLocale(GameLocalizationController.EnglishLocaleCode, false);

            try
            {
                view.SetHealth(9.1f, 20f);

                Assert.That(text.text, Is.EqualTo("Health: 10 / 20"));
            }
            finally
            {
                GameLocalizationController.SelectLocale(GameLocalizationController.RussianLocaleCode, false);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void LocalPlayerHudView_ControlsOnlyRequestedContainers()
        {
            GameObject root = new("HUD");
            GameObject summon = Child(root, "Summon");
            GameObject stats = Child(root, "Stats");
            GameObject ingredients = Child(root, "Ingredients");
            LocalPlayerHudView view = root.AddComponent<LocalPlayerHudView>();
            SetField(view, "summonContainer", summon);
            SetField(view, "playerStatsContainer", stats);
            SetField(view, "ingredientsContainer", ingredients);

            try
            {
                view.Initialize();
                Assert.That(summon.activeSelf, Is.False);

                view.SetGameplayHudVisible(false);
                Assert.That(stats.activeSelf, Is.False);
                Assert.That(ingredients.activeSelf, Is.False);

                view.SetGameplayHudVisible(true);
                Assert.That(stats.activeSelf, Is.True);
                Assert.That(ingredients.activeSelf, Is.True);
                Assert.That(summon.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void LanguageButtonsView_ClicksRaiseOnceAcrossEnableCyclesAndUpdateColors()
        {
            GameObject root = new("Language View");
            LanguageButtonsView view = root.AddComponent<LanguageButtonsView>();
            Button russian = CreateButton(root, "Russian");
            Button english = CreateButton(root, "English");
            int russianRequests = 0;
            int englishRequests = 0;
            view.RussianRequested += () => russianRequests++;
            view.EnglishRequested += () => englishRequests++;
            view.Configure(russian, english);

            try
            {
                russian.onClick.Invoke();
                english.onClick.Invoke();
                root.SetActive(false);
                root.SetActive(true);
                russian.onClick.Invoke();

                Assert.That(russianRequests, Is.EqualTo(2));
                Assert.That(englishRequests, Is.EqualTo(1));

                view.SetSelected(true, false);
                Assert.That(russian.targetGraphic.color, Is.EqualTo(Color.white));
                Assert.That(english.targetGraphic.color, Is.Not.EqualTo(Color.white));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void LanguageButtonsController_SelectsImmediatelyAndSurvivesEnableCycle()
        {
            GameObject root = new("Language Controller");
            root.SetActive(false);
            LanguageButtonsView view = root.AddComponent<LanguageButtonsView>();
            Button russian = CreateButton(root, "Russian");
            Button english = CreateButton(root, "English");
            view.Configure(russian, english);
            LanguageButtonsController controller = root.AddComponent<LanguageButtonsController>();
            controller.Configure(view);
            GameLocalizationController.SelectLocale(GameLocalizationController.RussianLocaleCode, false);

            try
            {
                root.SetActive(true);
                Assert.That(russian.targetGraphic.color, Is.EqualTo(Color.white));

                english.onClick.Invoke();
                Assert.That(GameLocalizationController.IsSelected(
                    GameLocalizationController.EnglishLocaleCode), Is.True);
                Assert.That(english.targetGraphic.color, Is.EqualTo(Color.white));

                root.SetActive(false);
                root.SetActive(true);
                russian.onClick.Invoke();
                Assert.That(GameLocalizationController.IsSelected(
                    GameLocalizationController.RussianLocaleCode), Is.True);
            }
            finally
            {
                GameLocalizationController.SelectLocale(GameLocalizationController.RussianLocaleCode, false);
                Object.DestroyImmediate(root);
            }
        }

        private static Button CreateButton(GameObject parent, string name)
        {
            GameObject gameObject = new(name, typeof(RectTransform), typeof(Image), typeof(Button));
            gameObject.transform.SetParent(parent.transform);
            return gameObject.GetComponent<Button>();
        }

        private static InputField CreateInputField(GameObject parent, string name)
        {
            GameObject gameObject = new(name, typeof(RectTransform), typeof(Image), typeof(InputField));
            gameObject.transform.SetParent(parent.transform);
            Text text = CreateText(gameObject, "Text");
            InputField input = gameObject.GetComponent<InputField>();
            input.textComponent = text;
            return input;
        }

        private static Text CreateText(GameObject parent, string name)
        {
            GameObject gameObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            gameObject.transform.SetParent(parent.transform);
            Text text = gameObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return text;
        }

        private static LobbyPlayerEntryView CreateLobbyEntry(GameObject parent)
        {
            GameObject gameObject = Child(parent, "Entry Prefab");
            Text nickname = CreateText(gameObject, "Nickname");
            GameObject hostMarker = Child(gameObject, "Host");
            LobbyPlayerEntryView view = gameObject.AddComponent<LobbyPlayerEntryView>();
            SetField(view, "nicknameText", nickname);
            SetField(view, "hostMarker", hostMarker);
            return view;
        }

        private static GameObject Child(GameObject parent, string name)
        {
            GameObject child = new(name);
            child.transform.SetParent(parent.transform);
            return child;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {name}");
            field.SetValue(target, value);
        }
    }
}
