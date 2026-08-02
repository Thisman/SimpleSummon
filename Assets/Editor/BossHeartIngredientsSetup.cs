using System;
using System.Collections.Generic;
using System.Linq;
using SimpleSummon.Domain;
using SimpleSummon.Network;
using SimpleSummon.Runtime;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BossHeartIngredientsSetup
{
    private const string ScenePath = "Assets/Scenes/Game.unity";
    private const string GreenBottlePath = "Assets/Resources/KayKit_Dungeon_Pack_1.1_FREE/Assets/fbx(unity)/bottle_C_green.fbx";
    private const string BrownBottlePath = "Assets/Resources/KayKit_Dungeon_Pack_1.1_FREE/Assets/fbx(unity)/bottle_C_brown.fbx";

    [MenuItem("Simple Summon/Setup Boss Heart Ingredients")]
    public static void Setup()
    {
        ConfigureEnemyPrefabs();

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        NetworkQuestState questState = FindInScene<NetworkQuestState>(scene).Single();
        ConfigureHud(scene, questState);
        ConfigureEnemies(scene, questState);
        ConfigurePlaneTop(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static void ConfigureEnemyPrefabs()
    {
        foreach (string path in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Enemies" })
                     .Select(AssetDatabase.GUIDToAssetPath))
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (root.GetComponent<EnemyController>() == null)
                {
                    continue;
                }

                Transform loot = root.transform.Find("IngredientLoot");
                if (loot == null)
                {
                    loot = new GameObject("IngredientLoot").transform;
                    loot.SetParent(root.transform, false);
                    loot.localPosition = new Vector3(0f, 0.4f, 0f);
                }

                SphereCollider collider = GetOrAdd<SphereCollider>(loot.gameObject);
                collider.isTrigger = true;
                collider.radius = 0.8f;
                collider.enabled = false;

                GameObject green = EnsureModel(loot, "bottle_C_green", GreenBottlePath);
                GameObject brown = EnsureModel(loot, "bottle_C_brown", BrownBottlePath);
                EnemyIngredientCollectable collectable = GetOrAdd<EnemyIngredientCollectable>(loot.gameObject);

                SerializedObject serialized = new(collectable);
                serialized.FindProperty("enemyState").objectReferenceValue = root.GetComponent<NetworkEnemyState>();
                serialized.FindProperty("pickupCollider").objectReferenceValue = collider;
                serialized.FindProperty("greenBottle").objectReferenceValue = green;
                serialized.FindProperty("brownBottle").objectReferenceValue = brown;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    private static GameObject EnsureModel(Transform parent, string name, string assetPath)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            return existing.gameObject;
        }

        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
        instance.name = name;
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;
        instance.SetActive(false);
        return instance;
    }

    private static void ConfigureEnemies(Scene scene, NetworkQuestState questState)
    {
        List<EnemyController> enemies = FindInScene<EnemyController>(scene)
            .OrderBy(enemy => GetHierarchyPath(enemy.transform), StringComparer.Ordinal)
            .ToList();
        List<EnemyController> regularEnemies = enemies
            .Where(enemy => !enemy.GetComponent<EnemySettings>().IsBoss)
            .ToList();
        System.Random random = new();

        for (int index = 0; index < enemies.Count; index++)
        {
            EnemyController enemy = enemies[index];
            EnemyIngredientCollectable collectable = enemy.GetComponentInChildren<EnemyIngredientCollectable>(true);
            if (collectable == null)
            {
                throw new InvalidOperationException($"{enemy.name} has no IngredientLoot.");
            }

            IngredientType ingredient = IngredientType.None;
            int regularIndex = regularEnemies.IndexOf(enemy);
            if (regularIndex >= 0)
            {
                ingredient = regularIndex < 2
                    ? IngredientType.BottleGreen
                    : regularIndex < 5
                        ? IngredientType.BottleBrown
                        : random.Next(2) == 0
                            ? IngredientType.BottleGreen
                            : IngredientType.BottleBrown;
            }

            SerializedObject serialized = new(collectable);
            serialized.FindProperty("ingredient").enumValueIndex = (int)ingredient;
            serialized.FindProperty("questState").objectReferenceValue = questState;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(collectable);
        }
    }

    private static void ConfigureHud(Scene scene, NetworkQuestState questState)
    {
        GameObject playerStats = FindByName(scene, "PlayerStatsContainer");
        GameObject ingredients = FindByName(scene, "IngredientsContainer");
        LocalPlayerHudView hudView = FindInScene<LocalPlayerHudView>(scene).Single();

        SerializedObject hud = new(hudView);
        hud.FindProperty("playerStatsContainer").objectReferenceValue = playerStats;
        hud.FindProperty("ingredientsContainer").objectReferenceValue = ingredients;
        hud.ApplyModifiedPropertiesWithoutUndo();

        TMP_Text label = ingredients.GetComponentInChildren<TMP_Text>(true);
        if (label == null)
        {
            GameObject labelObject = new("IngredientsText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(ingredients.transform, false);
            RectTransform rect = (RectTransform)labelObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(22f, 18f);
            rect.offsetMax = new Vector2(-18f, -18f);
            label = labelObject.GetComponent<TextMeshProUGUI>();
            label.fontSize = 28f;
            label.alignment = TextAlignmentOptions.BottomLeft;
            label.color = Color.white;
        }

        IngredientsView view = GetOrAdd<IngredientsView>(ingredients);
        IngredientsController controller = GetOrAdd<IngredientsController>(ingredients);
        SerializedObject viewData = new(view);
        viewData.FindProperty("ingredientsText").objectReferenceValue = label;
        viewData.ApplyModifiedPropertiesWithoutUndo();
        SerializedObject controllerData = new(controller);
        controllerData.FindProperty("questState").objectReferenceValue = questState;
        controllerData.FindProperty("view").objectReferenceValue = view;
        controllerData.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigurePlaneTop(Scene scene)
    {
        GameObject planeTop = FindByName(scene, "PlaneTop");
        MeshCollider collider = planeTop.GetComponent<MeshCollider>();
        if (collider != null)
        {
            UnityEngine.Object.DestroyImmediate(collider);
        }

        NavMeshModifier modifier = GetOrAdd<NavMeshModifier>(planeTop);
        modifier.ignoreFromBuild = true;
    }

    private static T GetOrAdd<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component == null ? gameObject.AddComponent<T>() : component;
    }

    private static IEnumerable<T> FindInScene<T>(Scene scene) where T : Component =>
        scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true));

    private static GameObject FindByName(Scene scene, string name) =>
        scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Single(transform => transform.name == name)
            .gameObject;

    private static string GetHierarchyPath(Transform transform) =>
        transform.parent == null
            ? transform.name
            : $"{GetHierarchyPath(transform.parent)}/{transform.name}";
}
