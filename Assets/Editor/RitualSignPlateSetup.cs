using System;
using System.Collections.Generic;
using System.Linq;
using SimpleSummon.Network;
using SimpleSummon.Runtime;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SimpleSummon.Editor
{
    public static class RitualSignPlateSetup
    {
        private const string GameScenePath = "Assets/Scenes/Game.unity";
        private const string SignTexturePath =
            "Assets/Textures/RitualSignQuest/RitualSign.png";

        [MenuItem("Tools/Simple Summon/Setup Ritual Sign Plates")]
        public static void Setup()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != GameScenePath)
            {
                throw new InvalidOperationException(
                    $"Open {GameScenePath} before setting up ritual sign plates.");
            }

            ConfigureTexture();
            RitualSignPlateController[] plates = ConfigurePlates(scene);
            NetworkRitualSignPlates networkState = ConfigureNetworkState(scene);
            ConfigureCoordinator(scene, networkState, plates);
            ConfigureSign(scene, networkState);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Ritual sign plates configured and Game.unity saved.");
        }

        private static RitualSignPlateController[] ConfigurePlates(Scene scene)
        {
            RitualSignPlateController[] existing = FindInScene<RitualSignPlateController>(scene);
            if (existing.Length > 0)
            {
                if (existing.Length != 9)
                {
                    throw new InvalidOperationException(
                        $"Expected 9 configured ritual plates, found {existing.Length}.");
                }
                return existing.OrderBy(plate => plate.PlateIndex).ToArray();
            }

            GameObject[] visuals = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Select(transform => transform.gameObject)
                .Where(gameObject =>
                    gameObject.name.StartsWith(
                        "floor_tile_small_broken_A",
                        StringComparison.Ordinal))
                .ToArray();
            if (visuals.Length != 9)
            {
                throw new InvalidOperationException(
                    $"Expected 9 floor_tile_small_broken_A objects, found {visuals.Length}.");
            }

            visuals = visuals
                .OrderByDescending(gameObject => gameObject.transform.position.z)
                .ThenBy(gameObject => gameObject.transform.position.x)
                .ToArray();

            RitualSignPlateController[] plates = new RitualSignPlateController[9];
            for (int i = 0; i < visuals.Length; i++)
            {
                GameObject visual = visuals[i];
                Transform oldParent = visual.transform.parent;
                int siblingIndex = visual.transform.GetSiblingIndex();

                GameObject plateObject = new($"RitualSignPlate {i}");
                plateObject.transform.SetParent(oldParent, false);
                plateObject.transform.SetSiblingIndex(siblingIndex);
                plateObject.transform.SetPositionAndRotation(
                    visual.transform.position,
                    visual.transform.rotation);
                visual.transform.SetParent(plateObject.transform, true);

                BoxCollider volume = plateObject.AddComponent<BoxCollider>();
                volume.isTrigger = true;
                volume.center = new Vector3(0f, 1f, 0f);
                volume.size = new Vector3(2f, 2f, 2f);

                RitualSignPlateController plate =
                    plateObject.AddComponent<RitualSignPlateController>();
                SerializedObject serializedPlate = new(plate);
                serializedPlate.FindProperty("plateIndex").intValue = i;
                serializedPlate.FindProperty("activationVolume").objectReferenceValue = volume;
                serializedPlate.FindProperty("visualTransform").objectReferenceValue = visual.transform;
                serializedPlate.FindProperty("standingHeightTolerance").floatValue = 0.35f;
                serializedPlate.FindProperty("pressedOffset").floatValue = 0.7f;
                serializedPlate.FindProperty("movementDuration").floatValue = 0.15f;
                serializedPlate.ApplyModifiedPropertiesWithoutUndo();
                plates[i] = plate;
            }
            return plates;
        }

        private static NetworkRitualSignPlates ConfigureNetworkState(Scene scene)
        {
            NetworkRitualSignPlates[] existing = FindInScene<NetworkRitualSignPlates>(scene);
            if (existing.Length > 1)
            {
                throw new InvalidOperationException(
                    $"Expected at most one NetworkRitualSignPlates, found {existing.Length}.");
            }
            if (existing.Length == 1)
            {
                return existing[0];
            }

            NetworkQuestState questState = FindInScene<NetworkQuestState>(scene).Single();
            if (questState.GetComponent<NetworkObject>() == null)
            {
                throw new InvalidOperationException("NetworkQuestState has no NetworkObject.");
            }
            return questState.gameObject.AddComponent<NetworkRitualSignPlates>();
        }

        private static void ConfigureCoordinator(
            Scene scene,
            NetworkRitualSignPlates networkState,
            RitualSignPlateController[] plates)
        {
            RitualSignPlateCoordinator[] existing =
                FindInScene<RitualSignPlateCoordinator>(scene);
            RitualSignPlateCoordinator coordinator;
            if (existing.Length == 0)
            {
                GameObject coordinatorObject = new("RitualSignPlateSystem");
                coordinator = coordinatorObject.AddComponent<RitualSignPlateCoordinator>();
            }
            else if (existing.Length == 1)
            {
                coordinator = existing[0];
            }
            else
            {
                throw new InvalidOperationException(
                    $"Expected at most one RitualSignPlateCoordinator, found {existing.Length}.");
            }

            SerializedObject serializedCoordinator = new(coordinator);
            serializedCoordinator.FindProperty("networkState").objectReferenceValue = networkState;
            SetObjectArray(serializedCoordinator.FindProperty("plates"), plates);
            serializedCoordinator.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureSign(Scene scene, NetworkRitualSignPlates networkState)
        {
            Canvas signCanvas = FindInScene<Canvas>(scene)
                .Single(canvas => canvas.gameObject.name == "Sign");
            Transform fragmentsRoot = signCanvas.transform.Find("SignFragments");
            if (fragmentsRoot == null)
            {
                throw new InvalidOperationException("Sign/SignFragments was not found.");
            }

            Image[] images = new Image[9];
            for (int i = 0; i < images.Length; i++)
            {
                images[i] = fragmentsRoot.GetChild(i).GetComponent<Image>();
                if (images[i] == null)
                {
                    throw new InvalidOperationException(
                        $"Sign fragment {i} has no Image component.");
                }
                images[i].enabled = false;
                images[i].raycastTarget = false;
                images[i].color = Color.white;
                images[i].sprite = null;
            }

            RitualSignFragmentView view =
                signCanvas.GetComponent<RitualSignFragmentView>() ??
                signCanvas.gameObject.AddComponent<RitualSignFragmentView>();
            SerializedObject serializedView = new(view);
            serializedView.FindProperty("signTexture").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Texture2D>(SignTexturePath);
            SetObjectArray(serializedView.FindProperty("fragmentImages"), images);
            serializedView.ApplyModifiedPropertiesWithoutUndo();

            RitualSignFragmentController controller =
                signCanvas.GetComponent<RitualSignFragmentController>() ??
                signCanvas.gameObject.AddComponent<RitualSignFragmentController>();
            SerializedObject serializedController = new(controller);
            serializedController.FindProperty("networkState").objectReferenceValue = networkState;
            serializedController.FindProperty("view").objectReferenceValue = view;
            serializedController.FindProperty("shuffleInterval").floatValue = 1f;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureTexture()
        {
            AssetDatabase.ImportAsset(SignTexturePath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(SignTexturePath);
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }

        private static T[] FindInScene<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .ToArray();

        private static void SetObjectArray<T>(SerializedProperty property, T[] values)
            where T : UnityEngine.Object
        {
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }
    }
}
