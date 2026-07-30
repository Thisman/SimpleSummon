using System;
using System.Linq;
using System.Reflection;
using SimpleSummon.Network;
using SimpleSummon.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SimpleSummon.Editor
{
    [InitializeOnLoad]
    public static class PixelFontSetup
    {
        private const string SetupVersion = "SimpleSummon.PixelFont.v2";
        private const string FontPath =
            "Assets/Fonts/PressStart2P/PressStart2P-Regular.ttf";
        private const string TmpFontPath =
            "Assets/Fonts/PressStart2P/PressStart2P-Regular SDF.asset";
        private const string PlayerPrefabPath =
            "Assets/Prefabs/Network/NetworkPlayer.prefab";

        static PixelFontSetup()
        {
            EditorApplication.delayCall += TryApplyAutomatically;
        }

        [MenuItem("Tools/Simple Summon/Apply Pixel Font")]
        public static void Apply()
        {
            AssetDatabase.ImportAsset(FontPath, ImportAssetOptions.ForceUpdate);
            Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font == null)
            {
                throw new InvalidOperationException("Pixel font was not imported.");
            }

            Type tmpFontType = Type.GetType("TMPro.TMP_FontAsset, Unity.TextMeshPro");
            MethodInfo createFontAsset = tmpFontType
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(method =>
                {
                    ParameterInfo[] parameters = method.GetParameters();
                    return method.Name == "CreateFontAsset" &&
                           parameters.Length == 1 &&
                           parameters[0].ParameterType == typeof(Font);
                });

            Object tmpFont = AssetDatabase.LoadAssetAtPath(TmpFontPath, tmpFontType);
            if (tmpFont == null)
            {
                tmpFont = (Object)createFontAsset.Invoke(null, new object[] { font });
                tmpFont.name = "PressStart2P-Regular SDF";
                AssetDatabase.CreateAsset(tmpFont, TmpFontPath);
            }

            EnsureFontResources(tmpFont, font, createFontAsset);
            SerializedObject tmpFontSerialized = new(tmpFont);
            SerializedProperty atlasPopulationMode =
                tmpFontSerialized.FindProperty("m_AtlasPopulationMode");
            if (atlasPopulationMode != null)
            {
                atlasPopulationMode.enumValueIndex = 1;
                tmpFontSerialized.ApplyModifiedPropertiesWithoutUndo();
            }
            ApplyToPlayerPrefab(font);
            ApplyToPrefab("Assets/Prefabs/UI/LobbyPlayerEntry.prefab", font, tmpFont);
            ApplyToScene("Assets/Scenes/MainMenu.unity", font, tmpFont);
            ApplyToScene("Assets/Scenes/Lobby.unity", font, tmpFont);
            ApplyToScene("Assets/Scenes/Game.unity", font, tmpFont);
            ApplyTmpDefault(tmpFont);
            AssetImporter fontImporter = AssetImporter.GetAtPath(FontPath);
            fontImporter.userData = SetupVersion;
            fontImporter.SaveAndReimport();
            AssetDatabase.SaveAssets();
            Debug.Log("Press Start 2P is now the project UI font.");
        }

        private static void EnsureFontResources(
            Object tmpFont,
            Font font,
            MethodInfo createFontAsset)
        {
            SerializedObject serializedFont = new(tmpFont);
            SerializedProperty materialProperty =
                serializedFont.FindProperty("m_Material");
            SerializedProperty atlasTexturesProperty =
                serializedFont.FindProperty("m_AtlasTextures");
            bool hasAtlas =
                atlasTexturesProperty.arraySize > 0 &&
                atlasTexturesProperty.GetArrayElementAtIndex(0).objectReferenceValue != null;
            if (materialProperty.objectReferenceValue != null && hasAtlas)
            {
                return;
            }

            Object generatedFont =
                (Object)createFontAsset.Invoke(null, new object[] { font });
            SerializedObject generatedSerialized = new(generatedFont);
            Object material = generatedSerialized
                .FindProperty("m_Material")
                .objectReferenceValue;
            Object atlasTexture = generatedSerialized
                .FindProperty("m_AtlasTextures")
                .GetArrayElementAtIndex(0)
                .objectReferenceValue;

            material.name = $"{tmpFont.name} Material";
            atlasTexture.name = $"{tmpFont.name} Atlas";
            AssetDatabase.AddObjectToAsset(material, tmpFont);
            AssetDatabase.AddObjectToAsset(atlasTexture, tmpFont);

            materialProperty.objectReferenceValue = material;
            atlasTexturesProperty.arraySize = 1;
            atlasTexturesProperty
                .GetArrayElementAtIndex(0)
                .objectReferenceValue = atlasTexture;
            serializedFont.ApplyModifiedPropertiesWithoutUndo();
            Object.DestroyImmediate(generatedFont);
            EditorUtility.SetDirty(tmpFont);
        }

        private static void TryApplyAutomatically()
        {
            AssetImporter importer = AssetImporter.GetAtPath(FontPath);
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling ||
                importer == null ||
                importer.userData == SetupVersion)
            {
                return;
            }

            Apply();
        }

        private static void ApplyToPlayerPrefab(Font font)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                PlayerNameplate existing =
                    root.GetComponentInChildren<PlayerNameplate>(true);
                if (existing == null)
                {
                    GameObject nameplate = new(
                        "Player Nameplate",
                        typeof(RectTransform),
                        typeof(Canvas),
                        typeof(PlayerNameplate));
                    RectTransform rect = nameplate.GetComponent<RectTransform>();
                    rect.SetParent(root.transform, false);
                    rect.localPosition = new Vector3(0f, 2.65f, 0f);
                    rect.sizeDelta = new Vector2(380f, 55f);
                    rect.localScale = Vector3.one * 0.01f;

                    Canvas canvas = nameplate.GetComponent<Canvas>();
                    canvas.renderMode = RenderMode.WorldSpace;
                    canvas.sortingOrder = 20;

                    GameObject label = new("Nickname", typeof(RectTransform), typeof(Text));
                    RectTransform labelRect = label.GetComponent<RectTransform>();
                    labelRect.SetParent(rect, false);
                    labelRect.anchorMin = Vector2.zero;
                    labelRect.anchorMax = Vector2.one;
                    labelRect.offsetMin = Vector2.zero;
                    labelRect.offsetMax = Vector2.zero;
                    Text text = label.GetComponent<Text>();
                    text.font = font;
                    text.fontSize = 28;
                    text.alignment = TextAnchor.MiddleCenter;
                    text.color = Color.white;
                    text.horizontalOverflow = HorizontalWrapMode.Overflow;
                    text.raycastTarget = false;
                    text.text = "Player";

                    PlayerNameplate nameplateComponent =
                        nameplate.GetComponent<PlayerNameplate>();
                    SetReference(nameplateComponent, "networkPlayer",
                        root.GetComponent<NetworkPlayer>());
                    SetReference(nameplateComponent, "nicknameText", text);
                }
                else
                {
                    existing.GetComponentInChildren<Text>(true).font = font;
                }

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ApplyToPrefab(string path, Font font, Object tmpFont)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                ApplyFonts(root, font, tmpFont);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ApplyToScene(string path, Font font, Object tmpFont)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                ApplyFonts(root, font, tmpFont);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ApplyFonts(GameObject root, Font font, Object tmpFont)
        {
            foreach (Text text in root.GetComponentsInChildren<Text>(true))
            {
                text.font = font;
                EditorUtility.SetDirty(text);
            }

            Type tmpTextType = Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro");
            foreach (Component text in root.GetComponentsInChildren(tmpTextType, true))
            {
                SerializedObject serializedText = new(text);
                serializedText.FindProperty("m_fontAsset").objectReferenceValue = tmpFont;
                serializedText.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(text);
            }
        }

        private static void ApplyTmpDefault(Object tmpFont)
        {
            Object settings = AssetDatabase.LoadMainAssetAtPath(
                "Assets/TextMesh Pro/Resources/TMP Settings.asset");
            SerializedObject serialized = new(settings);
            serialized.FindProperty("m_defaultFontAsset").objectReferenceValue = tmpFont;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }

        private static void SetReference(Object target, string property, Object value)
        {
            SerializedObject serialized = new(target);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
