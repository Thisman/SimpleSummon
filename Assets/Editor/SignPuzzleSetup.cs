using System.Linq;
using SimpleSummon.Network;
using SimpleSummon.Runtime;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SimpleSummon.Editor
{
    public static class SignPuzzleSetup
    {
        private const string ExitHintPrefabPath = "Assets/Prefabs/UI/ExitHint.prefab";
        private const string PlayerPrefabPath = "Assets/Prefabs/Network/NetworkPlayer.prefab";
        private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";

        [MenuItem("Tools/Simple Summon/Setup Sign Puzzle")]
        public static void Run()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.name != "Game" || !scene.isLoaded)
            {
                Debug.LogWarning("Open the Game scene before setting up the sign puzzle.");
                return;
            }

            LocalPlayerHud hud = Object.FindAnyObjectByType<LocalPlayerHud>(FindObjectsInactive.Include);
            NetworkQuestState questState = Object.FindAnyObjectByType<NetworkQuestState>(FindObjectsInactive.Include);
            GameObject instructionHint = FindGameObject("Instruction Exit Hint");
            GameObject instructionContainer = FindGameObject("InstructionContainer");
            GameObject signContainer = FindGameObject("SignBuilderContainer");
            GameObject signBuilder = FindGameObject("SignBuilder");
            GameObject spellbook = FindGameObject("spellbook_closed");
            if (hud == null || questState == null || instructionHint == null || instructionContainer == null ||
                signContainer == null || signBuilder == null || spellbook == null)
            {
                Debug.LogError("Sign puzzle setup could not find all required Game scene objects.");
                return;
            }

            GameObject exitHintPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ExitHintPrefabPath);
            if (exitHintPrefab == null)
            {
                exitHintPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(
                    instructionHint,
                    ExitHintPrefabPath,
                    InteractionMode.AutomatedAction);
            }

            ReplaceDrawingHint(exitHintPrefab);
            GameObject puzzleHint = FindGameObject("Sign Puzzle Exit Hint") ??
                                    CreateHint(exitHintPrefab, instructionHint.transform.parent,
                                        "Sign Puzzle Exit Hint", "Нажмите Esc чтобы выйти из сборки знака");
            puzzleHint.SetActive(true);

            SignPuzzleView view = signBuilder.GetComponent<SignPuzzleView>() ?? signBuilder.AddComponent<SignPuzzleView>();
            Image[] cells = signBuilder.transform.Cast<Transform>()
                .OrderBy(child => child.GetSiblingIndex())
                .Select(child => child.GetComponent<Image>())
                .Where(image => image != null)
                .ToArray();
            SerializedObject viewObject = new(view);
            SetArray(viewObject.FindProperty("cells"), cells);
            viewObject.ApplyModifiedPropertiesWithoutUndo();

            NetworkObject networkObject = spellbook.GetComponent<NetworkObject>() ?? spellbook.AddComponent<NetworkObject>();
            NetworkSignPuzzle puzzle = spellbook.GetComponent<NetworkSignPuzzle>() ?? spellbook.AddComponent<NetworkSignPuzzle>();
            SignPuzzleInteraction interaction = spellbook.GetComponent<SignPuzzleInteraction>() ?? spellbook.AddComponent<SignPuzzleInteraction>();
            InteractiveActor actor = spellbook.GetComponent<InteractiveActor>() ?? spellbook.AddComponent<InteractiveActor>();
            SetObjectReference(puzzle, "questState", questState);
            SetObjectReference(interaction, "puzzle", puzzle);
            SerializedObject actorObject = new(actor);
            actorObject.FindProperty("interactionText").stringValue = "Нажмите E, чтобы собрать знак";
            actorObject.FindProperty("interactionTarget").objectReferenceValue = interaction;
            actorObject.ApplyModifiedPropertiesWithoutUndo();
            _ = networkObject;

            SerializedObject hudObject = new(hud);
            hudObject.FindProperty("signPuzzleContainer").objectReferenceValue = signContainer;
            hudObject.FindProperty("signPuzzleView").objectReferenceValue = view;
            hudObject.FindProperty("instructionContainer").objectReferenceValue = instructionContainer;
            hudObject.ApplyModifiedPropertiesWithoutUndo();
            signContainer.SetActive(false);

            ConfigureInputActions();
            ConfigurePlayerPrefab();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Sign puzzle setup completed.");
        }

        private static void ConfigurePlayerPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                SignPuzzleMode mode = root.GetComponent<SignPuzzleMode>() ?? root.AddComponent<SignPuzzleMode>();
                SerializedObject modeObject = new(mode);
                modeObject.FindProperty("playerController").objectReferenceValue = root.GetComponent<PlayerController>();
                modeObject.FindProperty("interactionController").objectReferenceValue = root.GetComponent<PlayerInteractionController>();
                modeObject.FindProperty("lookController").objectReferenceValue = root.GetComponentInChildren<OrbitCameraController>(true);
                modeObject.FindProperty("inputActions").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
                modeObject.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureInputActions()
        {
            InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (asset == null || asset.FindActionMap("SignPuzzle") != null) return;

            InputActionMap map = asset.AddActionMap("SignPuzzle");
            map.AddAction("Point", InputActionType.PassThrough).AddBinding("<Mouse>/position");
            map.AddAction("Click", InputActionType.Button).AddBinding("<Mouse>/leftButton");
            map.AddAction("Exit", InputActionType.Button).AddBinding("<Keyboard>/escape");
            map.AddAction("Left", InputActionType.Button).AddBinding("<Keyboard>/leftArrow");
            map.AddAction("Right", InputActionType.Button).AddBinding("<Keyboard>/rightArrow");
            map.AddAction("Up", InputActionType.Button).AddBinding("<Keyboard>/upArrow");
            map.AddAction("Down", InputActionType.Button).AddBinding("<Keyboard>/downArrow");
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
        }

        private static void ReplaceDrawingHint(GameObject prefab)
        {
            GameObject drawingHint = FindGameObject("Hint");
            if (drawingHint == null || drawingHint.transform.parent?.name != "SummonContainer" ||
                PrefabUtility.IsPartOfPrefabInstance(drawingHint)) return;
            Transform parent = drawingHint.transform.parent;
            Object.DestroyImmediate(drawingHint);
            CreateHint(prefab, parent, "Drawing Exit Hint", "Нажмите Esc чтобы выйти");
        }

        private static GameObject CreateHint(
            GameObject prefab,
            Transform parent,
            string name,
            string text)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.gameObject.scene);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            Text label = instance.GetComponent<Text>();
            if (label != null) label.text = text;
            return instance;
        }

        private static GameObject FindGameObject(string name)
        {
            return Resources.FindObjectsOfTypeAll<GameObject>()
                .FirstOrDefault(value => value.scene.IsValid() && value.scene == SceneManager.GetActiveScene() && value.name == name);
        }

        private static void SetObjectReference(Object target, string propertyName, Object value)
        {
            SerializedObject serializedObject = new(target);
            serializedObject.FindProperty(propertyName).objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray<T>(SerializedProperty property, T[] values) where T : Object
        {
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
