using System;
using System.Collections.Generic;
using System.Linq;
using SimpleSummon.Network;
using SimpleSummon.Runtime;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SimpleSummon.Editor
{
    public static class ArtifactQuestSetup
    {
        private const string GameScenePath = "Assets/Scenes/Game.unity";

        [MenuItem("Simple Summon/Setup Artifact Quest")]
        public static void Run()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != GameScenePath)
            {
                scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            }

            NetworkQuestState questState = UnityEngine.Object.FindAnyObjectByType<NetworkQuestState>();
            LocalPlayerHudView hud = UnityEngine.Object.FindAnyObjectByType<LocalPlayerHudView>();
            if (questState == null || hud == null)
            {
                Debug.LogError("Artifact quest setup requires NetworkQuestState and Player HUD in Game.unity.");
                return;
            }

            EnsureBossTag();
            EnsureEventSystem();
            TMP_FontAsset font = LoadFont();
            CraftingController craftingController = SetupHud(hud, questState, font);
            SetupEnemies(questState);
            SetupCraftingStation(craftingController);
            FixStaticCollectables(questState);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Artifact quest setup completed.");
        }

        private static CraftingController SetupHud(
            LocalPlayerHudView hud,
            NetworkQuestState questState,
            TMP_FontAsset font)
        {
            Transform root = hud.transform;
            DestroyChild(root, "Artifact Quest HUD");
            DestroyChild(root, "Crafting Container");
            GameObject questPanel = CreateUiObject("Artifact Quest HUD", root);
            RectTransform panelRect = questPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0.5f);
            panelRect.anchorMax = new Vector2(0f, 0.5f);
            panelRect.pivot = new Vector2(0f, 0.5f);
            panelRect.anchoredPosition = new Vector2(24f, 0f);
            panelRect.sizeDelta = new Vector2(390f, 250f);
            Image panelImage = questPanel.AddComponent<Image>();
            panelImage.color = new Color(0.04f, 0.035f, 0.05f, 0.82f);

            VerticalLayoutGroup layout = questPanel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(22, 18, 18, 18);
            layout.spacing = 10f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;

            TMP_Text health = CreateText("Health", questPanel.transform, font, 20);
            TMP_Text heart = CreateText("Boss Heart", questPanel.transform, font, 20);
            TMP_Text resources = CreateText("Artifact Resources", questPanel.transform, font, 20);
            TMP_Text artifact = CreateText("Artifact", questPanel.transform, font, 20);
            TMP_Text fragments = CreateText("Fragments", questPanel.transform, font, 20);

            QuestHudView questView = questPanel.AddComponent<QuestHudView>();
            QuestHudController questController = questPanel.AddComponent<QuestHudController>();
            SetReference(questView, "healthText", health);
            SetReference(questView, "bossHeartText", heart);
            SetReference(questView, "resourcesText", resources);
            SetReference(questView, "artifactText", artifact);
            SetReference(questView, "fragmentsText", fragments);
            SetReference(questController, "questState", questState);
            SetReference(questController, "view", questView);
            SetReference(hud, "questProgressContainer", questPanel);

            CraftingView craftingView = root.gameObject.GetComponent<CraftingView>() ??
                                        root.gameObject.AddComponent<CraftingView>();
            CraftingController craftingController =
                root.gameObject.GetComponent<CraftingController>() ??
                root.gameObject.AddComponent<CraftingController>();
            GameObject container = CreateUiObject("Crafting Container", root);
            RectTransform containerRect = container.GetComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;
            Image overlay = container.AddComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0.78f);

            GameObject window = CreateUiObject("Crafting Window", container.transform);
            RectTransform windowRect = window.GetComponent<RectTransform>();
            windowRect.anchorMin = windowRect.anchorMax = new Vector2(0.5f, 0.5f);
            windowRect.sizeDelta = new Vector2(820f, 470f);
            Image windowImage = window.AddComponent<Image>();
            windowImage.color = new Color(0.12f, 0.095f, 0.14f, 1f);

            TMP_Text title = CreateText("Title", window.transform, font, 30);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -45f), new Vector2(720f, 55f));
            title.alignment = TextAlignmentOptions.Center;
            GameObject exitHintPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/UI/ExitHint.prefab");
            GameObject exitHint = (GameObject)PrefabUtility.InstantiatePrefab(exitHintPrefab, container.transform);
            exitHint.name = "ExitHint";
            Text exitHintText = exitHint.GetComponent<Text>();

            var slots = new List<CraftingSlotView>();
            for (int i = 0; i < 5; i++)
            {
                GameObject slotObject = CreateUiObject($"Resource Slot {i + 1}", window.transform);
                RectTransform slotRect = slotObject.GetComponent<RectTransform>();
                SetRect(slotRect, new Vector2(0.5f, 0.5f), new Vector2(-264f + i * 132f, 10f), new Vector2(105f, 105f));
                Image slotBackground = slotObject.AddComponent<Image>();
                slotBackground.color = new Color(0.28f, 0.24f, 0.31f, 1f);

                GameObject iconObject = CreateUiObject("Silver Nugget Icon", slotObject.transform);
                RectTransform iconRect = iconObject.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.15f, 0.15f);
                iconRect.anchorMax = new Vector2(0.85f, 0.85f);
                iconRect.offsetMin = iconRect.offsetMax = Vector2.zero;
                Image icon = iconObject.AddComponent<Image>();
                icon.color = new Color(0.78f, 0.83f, 0.9f, 1f);

                TMP_Text count = CreateText("Count", slotObject.transform, font, 22);
                count.alignment = TextAlignmentOptions.BottomRight;
                count.raycastTarget = false;
                count.rectTransform.anchorMin = Vector2.zero;
                count.rectTransform.anchorMax = Vector2.one;
                count.rectTransform.offsetMin = new Vector2(5f, 5f);
                count.rectTransform.offsetMax = new Vector2(-7f, -5f);

                CraftingSlotView slot = slotObject.AddComponent<CraftingSlotView>();
                SetReference(slot, "icon", icon);
                SetReference(slot, "countText", count);
                slots.Add(slot);
            }

            TMP_Text status = CreateText("Status", window.transform, font, 19);
            SetRect(status.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 105f), new Vector2(700f, 40f));
            status.alignment = TextAlignmentOptions.Center;

            GameObject buttonObject = CreateUiObject("Craft Button", window.transform);
            SetRect(buttonObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 47f), new Vector2(300f, 60f));
            Image buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new Color(0.52f, 0.25f, 0.18f, 1f);
            Button button = buttonObject.AddComponent<Button>();
            TMP_Text buttonText = CreateText("Text", buttonObject.transform, font, 21);
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.rectTransform.anchorMin = Vector2.zero;
            buttonText.rectTransform.anchorMax = Vector2.one;
            buttonText.rectTransform.offsetMin = buttonText.rectTransform.offsetMax = Vector2.zero;

            SetReference(craftingView, "canvas", root.GetComponent<Canvas>());
            SetReference(craftingView, "container", container);
            SetReference(craftingView, "titleText", title);
            SetReference(craftingView, "exitHintText", exitHintText);
            SetReference(craftingView, "statusText", status);
            SetReference(craftingView, "craftButton", button);
            SetObjectList(craftingView, "slots", slots.Cast<UnityEngine.Object>().ToArray());
            SetReference(craftingController, "questState", questState);
            SetReference(craftingController, "view", craftingView);
            container.SetActive(false);
            return craftingController;
        }

        private static void SetupEnemies(NetworkQuestState questState)
        {
            EnemyController[] enemies = UnityEngine.Object.FindObjectsByType<EnemyController>(
                FindObjectsInactive.Include);
            EnemyController boss = enemies.FirstOrDefault(x => x.name == "Skeleton_Warrior") ??
                                   enemies.FirstOrDefault(x => x.name.Contains("Skeleton_Warrior")) ??
                                   enemies.First();

            foreach (EnemyController enemy in enemies)
            {
                EnemySettings settings = enemy.GetComponent<EnemySettings>();
                NetworkEnemyState state = enemy.GetComponent<NetworkEnemyState>();
                bool isBoss = enemy == boss;

                SetReference(enemy, "questState", questState);
                SetBool(settings, "boss", isBoss);
                SetFloat(settings, "bossStatMultiplier", 5f);

                if (isBoss)
                {
                    enemy.tag = "Boss";
                    enemy.transform.localScale = Vector3.one * 1.25f;
                    SetReference(enemy, "loot", null);
                    SetEnemyVisualRenderers(enemy, null);
                    continue;
                }

                enemy.tag = "Untagged";
                enemy.transform.localScale = Vector3.one;

                Transform oldLoot = enemy.transform.Find("Silver Nugget Drop");
                if (oldLoot != null)
                {
                    UnityEngine.Object.DestroyImmediate(oldLoot.gameObject);
                }

                GameObject lootObject = new("Silver Nugget Drop");
                lootObject.transform.SetParent(enemy.transform, false);
                lootObject.transform.localPosition = new Vector3(0f, 0.45f, 0f);
                SphereCollider collider = lootObject.AddComponent<SphereCollider>();
                collider.radius = 0.65f;
                collider.isTrigger = true;

                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Resources/KayKit_Dungeon_Pack_1.1_FREE/Assets/fbx(unity)/bottle_C_green.fbx");
                GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model, lootObject.transform);
                visual.name = "bottle_C_green";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one;

                EnemyLootCollectable loot = lootObject.AddComponent<EnemyLootCollectable>();
                SetReference(loot, "enemyState", state);
                SetReference(loot, "questState", questState);
                SetReference(enemy, "loot", loot);
                SetEnemyVisualRenderers(enemy, loot.transform);
            }
        }

        private static void SetEnemyVisualRenderers(EnemyController enemy, Transform lootRoot)
        {
            Renderer[] renderers = enemy.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => lootRoot == null || !renderer.transform.IsChildOf(lootRoot))
                .ToArray();
            SetObjectList(enemy, "visualRenderers", renderers);
        }

        private static void SetupCraftingStation(CraftingController craftingController)
        {
            Transform station = UnityEngine.Object.FindObjectsByType<Transform>(
                    FindObjectsInactive.Include)
                .FirstOrDefault(IsDecoratedKeg);
            if (station == null)
            {
                Debug.LogError("keg_decorated was not found in the active Game scene.");
                return;
            }

            station.gameObject.tag = "Interactive";
            Collider collider = station.GetComponent<Collider>();
            if (collider == null)
            {
                collider = station.gameObject.AddComponent<BoxCollider>();
            }
            collider.isTrigger = false;
            CraftingInteraction interaction = station.GetComponent<CraftingInteraction>() ??
                                              station.gameObject.AddComponent<CraftingInteraction>();
            InteractiveActor actor = station.GetComponent<InteractiveActor>() ??
                                     station.gameObject.AddComponent<InteractiveActor>();
            SetReference(interaction, "craftingController", craftingController);
            SetString(actor, "interactionText", "Зажмите E, чтобы создать артефакт");
            SetReference(actor, "interactionTarget", interaction);
        }

        private static bool IsDecoratedKeg(Transform transform)
        {
            if (transform.name.Contains("keg_decorated", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            UnityEngine.Object source = PrefabUtility.GetCorrespondingObjectFromSource(transform.gameObject);
            string path = source != null ? AssetDatabase.GetAssetPath(source) : string.Empty;
            return path.Contains("keg_decorated", StringComparison.OrdinalIgnoreCase);
        }

        private static void FixStaticCollectables(NetworkQuestState questState)
        {
            foreach (CollectableItemController collectable in UnityEngine.Object.FindObjectsByType<CollectableItemController>(
                         FindObjectsInactive.Include))
            {
                SetReference(collectable, "questState", questState);
                SetFloat(collectable, "bobAmplitude", 0.15f);
                SetFloat(collectable, "bobFrequency", 1f);
                SetVector(collectable, "rotationAxes", Vector3.up);
                SetFloat(collectable, "rotationSpeed", 45f);
                Collider collider = collectable.GetComponent<Collider>();
                if (collider != null)
                {
                    collider.isTrigger = true;
                }
            }
        }

        private static TMP_FontAsset LoadFont()
        {
            string guid = AssetDatabase.FindAssets("PressStart2P-Regular SDF t:TMP_FontAsset").FirstOrDefault();
            return string.IsNullOrEmpty(guid)
                ? TMP_Settings.defaultFontAsset
                : AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject value = new(name, typeof(RectTransform));
            value.layer = LayerMask.NameToLayer("UI");
            value.transform.SetParent(parent, false);
            return value;
        }

        private static void DestroyChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null)
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }

        private static TMP_Text CreateText(string name, Transform parent, TMP_FontAsset font, float size)
        {
            GameObject value = CreateUiObject(name, parent);
            TextMeshProUGUI text = value.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = size;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void EnsureBossTag()
        {
            UnityEngine.Object tagManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
            SerializedObject serialized = new(tagManager);
            SerializedProperty tags = serialized.FindProperty("tags");
            for (int i = 0; i < tags.arraySize; i++)
            {
                if (tags.GetArrayElementAtIndex(i).stringValue == "Boss")
                {
                    return;
                }
            }
            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = "Boss";
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem != null)
            {
                if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                {
                    eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                }
                return;
            }

            GameObject eventSystemObject = new("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }

        private static void SetReference(UnityEngine.Object target, string name, UnityEngine.Object value)
        {
            SerializedObject serialized = new(target);
            serialized.FindProperty(name).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectList(UnityEngine.Object target, string name, UnityEngine.Object[] values)
        {
            SerializedObject serialized = new(target);
            SerializedProperty property = serialized.FindProperty(name);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBool(UnityEngine.Object target, string name, bool value) => SetValue(target, name, x => x.boolValue = value);
        private static void SetFloat(UnityEngine.Object target, string name, float value) => SetValue(target, name, x => x.floatValue = value);
        private static void SetString(UnityEngine.Object target, string name, string value) => SetValue(target, name, x => x.stringValue = value);
        private static void SetVector(UnityEngine.Object target, string name, Vector3 value) => SetValue(target, name, x => x.vector3Value = value);

        private static void SetValue(UnityEngine.Object target, string name, Action<SerializedProperty> set)
        {
            SerializedObject serialized = new(target);
            set(serialized.FindProperty(name));
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
