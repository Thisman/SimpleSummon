using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SimpleSummon.Network;
using SimpleSummon.Runtime;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SimpleSummon.Editor
{
    [InitializeOnLoad]
    public static class MultiplayerProjectSetup
    {
        private const string GameScenePath = "Assets/Scenes/Game.unity";
        private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string LobbyScenePath = "Assets/Scenes/Lobby.unity";
        private const string PlayerPrefabPath = "Assets/Prefabs/Network/NetworkPlayer.prefab";
        private const string BootstrapPrefabPath = "Assets/Prefabs/Network/NetworkBootstrap.prefab";
        private const string PlayerEntryPrefabPath = "Assets/Prefabs/UI/LobbyPlayerEntry.prefab";
        private const string SetupVersion = "SimpleSummon.MultiplayerSetup.v2";

        static MultiplayerProjectSetup()
        {
            EditorApplication.delayCall += TryRunAutomaticSetup;
        }

        [MenuItem("Tools/Simple Summon/Rebuild Multiplayer Scenes")]
        public static void Rebuild()
        {
            BuildAll();
        }

        private static void TryRunAutomaticSetup()
        {
            GameObject existingPlayerPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            AssetImporter bootstrapImporter =
                AssetImporter.GetAtPath(BootstrapPrefabPath);
            bool setupCurrent = existingPlayerPrefab != null &&
                                existingPlayerPrefab.GetComponent<NetworkAnimator>() != null &&
                                bootstrapImporter?.userData == SetupVersion;

            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling ||
                setupCurrent &&
                File.Exists(MenuScenePath) &&
                File.Exists(LobbyScenePath) &&
                File.Exists(PlayerPrefabPath) &&
                File.Exists(BootstrapPrefabPath))
            {
                return;
            }

            BuildAll();
        }

        private static void BuildAll()
        {
            try
            {
                Directory.CreateDirectory("Assets/Prefabs/Network");
                Directory.CreateDirectory("Assets/Prefabs/UI");

                GameObject playerPrefab = ConfigureGameScene();
                GameObject bootstrapPrefab = CreateBootstrapPrefab(playerPrefab);
                CreateMenuScene(bootstrapPrefab);
                CreateLobbyScene();
                ConfigureBuildSettings();
                AssetImporter bootstrapImporter =
                    AssetImporter.GetAtPath(BootstrapPrefabPath);
                bootstrapImporter.userData = SetupVersion;
                bootstrapImporter.SaveAndReimport();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("SimpleSummon multiplayer scenes and prefabs were created.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static GameObject ConfigureGameScene()
        {
            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            PlayerController scenePlayer = Object.FindAnyObjectByType<PlayerController>();
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);

            Vector3 initialPosition;
            Quaternion initialRotation;
            if (scenePlayer != null)
            {
                GameObject playerRoot = scenePlayer.gameObject;
                initialPosition = playerRoot.transform.position;
                initialRotation = playerRoot.transform.rotation;
                AddNetworkComponents(playerRoot);
                playerPrefab = PrefabUtility.SaveAsPrefabAsset(playerRoot, PlayerPrefabPath);
                Object.DestroyImmediate(playerRoot);
            }
            else
            {
                if (playerPrefab == null)
                {
                    throw new InvalidOperationException("Game scene player was not found.");
                }

                Transform firstSpawn = Object.FindObjectsByType<NetworkSpawnPoint>()
                    .OrderBy(point => point.name)
                    .Select(point => point.transform)
                    .FirstOrDefault();
                initialPosition = firstSpawn != null ? firstSpawn.position : Vector3.zero;
                initialRotation = firstSpawn != null ? firstSpawn.rotation : Quaternion.identity;

                GameObject prefabContents = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
                AddNetworkComponents(prefabContents);
                PrefabUtility.SaveAsPrefabAsset(prefabContents, PlayerPrefabPath);
                PrefabUtility.UnloadPrefabContents(prefabContents);
                playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            }

            foreach (EnemyController enemy in
                     Object.FindObjectsByType<EnemyController>())
            {
                AddNetworkComponents(enemy.gameObject);
            }

            foreach (InteractiveActor actor in
                     Object.FindObjectsByType<InteractiveActor>())
            {
                if (actor.GetComponentInParent<NetworkObject>() == null)
                {
                    actor.gameObject.AddComponent<NetworkObject>();
                }
            }

            ConfigureLocalPlayerHud();

            Transform[] existingSpawnRoots = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include);
            foreach (Transform existing in existingSpawnRoots)
            {
                if (existing.name == "Network Spawn Points")
                {
                    Object.DestroyImmediate(existing.gameObject);
                }
            }

            Transform spawnRoot = new GameObject("Network Spawn Points").transform;
            Transform firstSpawnPoint = null;
            for (int i = 0; i < NetworkSessionService.MaximumPlayers; i++)
            {
                GameObject spawnObject = new GameObject($"Spawn Point {i + 1}");
                spawnObject.transform.SetParent(spawnRoot);
                spawnObject.transform.SetPositionAndRotation(
                    initialPosition + Vector3.right * (i - 2) * 1.5f,
                    initialRotation);
                NetworkSpawnPoint spawnPoint = spawnObject.AddComponent<NetworkSpawnPoint>();
                SetSerializedField(spawnPoint, "index", i);
                firstSpawnPoint ??= spawnObject.transform;
            }

            OfflineGameBootstrap offlineBootstrap =
                Object.FindAnyObjectByType<OfflineGameBootstrap>();
            if (offlineBootstrap == null)
            {
                offlineBootstrap = new GameObject("Offline Game Bootstrap")
                    .AddComponent<OfflineGameBootstrap>();
            }

            SetSerializedField(offlineBootstrap, "playerPrefab", playerPrefab);
            SetSerializedField(offlineBootstrap, "spawnPoint", firstSpawnPoint);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return playerPrefab;
        }

        private static void ConfigureLocalPlayerHud()
        {
            InteractionPromptView prompt = Object.FindAnyObjectByType<InteractionPromptView>(
                FindObjectsInactive.Include);
            if (prompt == null)
            {
                throw new InvalidOperationException("Game scene player HUD was not found.");
            }

            LocalPlayerHud hud = prompt.GetComponent<LocalPlayerHud>();
            if (hud == null)
            {
                hud = prompt.gameObject.AddComponent<LocalPlayerHud>();
            }

            Transform exitHint = Object.FindObjectsByType<Transform>(
                    FindObjectsInactive.Include)
                .FirstOrDefault(item => item.name == "Instruction Exit Hint");
            SetSerializedField(hud, "interactionPrompt", prompt);
            SetSerializedField(
                hud,
                "instructionExitHint",
                exitHint != null ? exitHint.gameObject : null);
        }

        private static void AddNetworkComponents(GameObject target)
        {
            if (target.GetComponent<NetworkObject>() == null)
            {
                target.AddComponent<NetworkObject>();
            }

            if (target.GetComponent<NetworkTransform>() == null)
            {
                target.AddComponent<NetworkTransform>();
            }

            Animator animator = target.GetComponent<Animator>();
            if (animator != null && target.GetComponent<NetworkAnimator>() == null)
            {
                NetworkAnimator networkAnimator = target.AddComponent<NetworkAnimator>();
                SetSerializedField(networkAnimator, "m_Animator", animator);
            }

            if (target.GetComponent<PlayerController>() != null &&
                target.GetComponent<NetworkPlayer>() == null)
            {
                target.AddComponent<NetworkPlayer>();
            }

            if (target.GetComponent<EnemyController>() != null &&
                target.GetComponent<NetworkEnemyState>() == null)
            {
                target.AddComponent<NetworkEnemyState>();
            }
        }

        private static GameObject CreateBootstrapPrefab(GameObject playerPrefab)
        {
            GameObject bootstrap = new GameObject("Network Bootstrap");
            NetworkManager networkManager = bootstrap.AddComponent<NetworkManager>();
            UnityTransport transport = bootstrap.AddComponent<UnityTransport>();
            bootstrap.AddComponent<NetworkSessionService>();

            networkManager.NetworkConfig.NetworkTransport = transport;
            networkManager.NetworkConfig.PlayerPrefab = playerPrefab;
            networkManager.NetworkConfig.ConnectionApproval = true;
            networkManager.NetworkConfig.EnableSceneManagement = true;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(
                bootstrap,
                BootstrapPrefabPath);
            Object.DestroyImmediate(bootstrap);
            return prefab;
        }

        private static void CreateMenuScene(GameObject bootstrapPrefab)
        {
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            PrefabUtility.InstantiatePrefab(bootstrapPrefab, scene);
            CreateEventSystem();

            Canvas canvas = CreateCanvas("Main Menu");
            RectTransform panel = CreatePanel(canvas.transform, new Color(0.06f, 0.04f, 0.08f, 0.96f));
            CreateText(panel, "Simple Summon", 38, new Vector2(0f, 220f), new Vector2(700f, 70f));
            InputField nickname = CreateInput(panel, "Никнейм", new Vector2(0f, 110f));
            Button create = CreateButton(panel, "Создать комнату", new Vector2(0f, 35f));
            InputField code = CreateInput(panel, "Код комнаты", new Vector2(0f, -55f));
            Button join = CreateButton(panel, "Присоединиться", new Vector2(0f, -130f));
            Text status = CreateText(panel, string.Empty, 18, new Vector2(0f, -220f), new Vector2(700f, 80f));
            Text progress = CreateText(panel, "Подключение…", 18, new Vector2(0f, -280f), new Vector2(400f, 40f));

            MainMenuController controller = panel.gameObject.AddComponent<MainMenuController>();
            SetSerializedField(controller, "nicknameInput", nickname);
            SetSerializedField(controller, "roomCodeInput", code);
            SetSerializedField(controller, "createRoomButton", create);
            SetSerializedField(controller, "joinRoomButton", join);
            SetSerializedField(controller, "statusText", status);
            SetSerializedField(controller, "progressIndicator", progress.gameObject);

            EditorSceneManager.SaveScene(scene, MenuScenePath);
        }

        private static void CreateLobbyScene()
        {
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            CreateEventSystem();

            LobbyPlayerEntryView entryPrefab = CreatePlayerEntryPrefab();
            Canvas canvas = CreateCanvas("Lobby");
            RectTransform panel = CreatePanel(canvas.transform, new Color(0.06f, 0.04f, 0.08f, 0.96f));
            CreateText(panel, "Комната", 36, new Vector2(0f, 250f), new Vector2(700f, 60f));
            Text code = CreateText(panel, string.Empty, 28, new Vector2(-80f, 180f), new Vector2(420f, 50f));
            Button copy = CreateButton(panel, "Копировать код", new Vector2(230f, 180f), new Vector2(220f, 48f));
            Text count = CreateText(panel, "0 / 5", 22, new Vector2(0f, 120f), new Vector2(300f, 45f));

            RectTransform listRoot = new GameObject(
                "Players",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup)).GetComponent<RectTransform>();
            listRoot.SetParent(panel, false);
            listRoot.anchoredPosition = new Vector2(0f, -20f);
            listRoot.sizeDelta = new Vector2(600f, 230f);
            VerticalLayoutGroup layout = listRoot.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = false;

            Button start = CreateButton(panel, "Начать игру", new Vector2(-150f, -230f), new Vector2(260f, 58f));
            Button leave = CreateButton(panel, "Выйти", new Vector2(150f, -230f), new Vector2(260f, 58f));
            Text status = CreateText(panel, string.Empty, 18, new Vector2(0f, -300f), new Vector2(700f, 55f));

            LobbyController controller = panel.gameObject.AddComponent<LobbyController>();
            SetSerializedField(controller, "roomCodeText", code);
            SetSerializedField(controller, "playerCountText", count);
            SetSerializedField(controller, "statusText", status);
            SetSerializedField(controller, "playerListRoot", listRoot);
            SetSerializedField(controller, "playerEntryPrefab", entryPrefab);
            SetSerializedField(controller, "copyCodeButton", copy);
            SetSerializedField(controller, "startGameButton", start);
            SetSerializedField(controller, "leaveButton", leave);

            EditorSceneManager.SaveScene(scene, LobbyScenePath);
        }

        private static LobbyPlayerEntryView CreatePlayerEntryPrefab()
        {
            GameObject root = new GameObject(
                "Lobby Player Entry",
                typeof(RectTransform),
                typeof(Image));
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(560f, 42f);
            root.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);

            Text nickname = CreateText(rect, "Player", 20, new Vector2(-70f, 0f), new Vector2(380f, 40f));
            Text host = CreateText(rect, "ХОСТ", 16, new Vector2(205f, 0f), new Vector2(110f, 40f));
            LobbyPlayerEntryView view = root.AddComponent<LobbyPlayerEntryView>();
            SetSerializedField(view, "nicknameText", nickname);
            SetSerializedField(view, "hostMarker", host.gameObject);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PlayerEntryPrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<LobbyPlayerEntryView>();
        }

        private static Canvas CreateCanvas(string name)
        {
            GameObject root = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static RectTransform CreatePanel(Transform parent, Color color)
        {
            GameObject panelObject = new GameObject(
                "Panel",
                typeof(RectTransform),
                typeof(Image));
            RectTransform panel = panelObject.GetComponent<RectTransform>();
            panel.SetParent(parent, false);
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.one;
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
            panelObject.GetComponent<Image>().color = color;
            return panel;
        }

        private static Text CreateText(
            Transform parent,
            string value,
            int fontSize,
            Vector2 position,
            Vector2 size)
        {
            GameObject textObject = new GameObject(
                "Text",
                typeof(RectTransform),
                typeof(Text));
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = value;
            return text;
        }

        private static InputField CreateInput(
            Transform parent,
            string placeholderValue,
            Vector2 position)
        {
            GameObject root = new GameObject(
                placeholderValue,
                typeof(RectTransform),
                typeof(Image),
                typeof(InputField));
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(520f, 56f);
            root.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.16f);

            Text value = CreateText(rect, string.Empty, 22, Vector2.zero, new Vector2(480f, 52f));
            value.alignment = TextAnchor.MiddleLeft;
            Text placeholder = CreateText(rect, placeholderValue, 22, Vector2.zero, new Vector2(480f, 52f));
            placeholder.alignment = TextAnchor.MiddleLeft;
            placeholder.color = new Color(1f, 1f, 1f, 0.45f);

            InputField input = root.GetComponent<InputField>();
            input.textComponent = value;
            input.placeholder = placeholder;
            input.characterLimit = 32;
            return input;
        }

        private static Button CreateButton(
            Transform parent,
            string label,
            Vector2 position,
            Vector2? size = null)
        {
            GameObject root = new GameObject(
                label,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchoredPosition = position;
            rect.sizeDelta = size ?? new Vector2(520f, 56f);
            root.GetComponent<Image>().color = new Color(0.43f, 0.18f, 0.52f, 1f);
            CreateText(rect, label, 22, Vector2.zero, rect.sizeDelta);
            return root.GetComponent<Button>();
        }

        private static void CreateEventSystem()
        {
            new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MenuScenePath, true),
                new EditorBuildSettingsScene(LobbyScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true)
            };
        }

        private static void SetSerializedField(
            Object target,
            string propertyName,
            Object value)
        {
            SerializedObject serializedObject = new(target);
            serializedObject.FindProperty(propertyName).objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSerializedField(
            Object target,
            string propertyName,
            int value)
        {
            SerializedObject serializedObject = new(target);
            serializedObject.FindProperty(propertyName).intValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
