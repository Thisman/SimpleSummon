using System.Collections.Generic;
using SimpleSummon.Runtime;
using UnityEditor;
using UnityEngine;

namespace SimpleSummon.Editor
{
    [CustomEditor(typeof(DungeonShellGenerator))]
    public sealed class DungeonShellGeneratorEditor : UnityEditor.Editor
    {
        private static readonly Vector2Int[] NeighborDirections =
        {
            Vector2Int.up,
            Vector2Int.right,
            Vector2Int.down,
            Vector2Int.left
        };

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            DungeonShellGenerator generator = (DungeonShellGenerator)target;
            string validationError = GetValidationError(generator);
            if (validationError != null)
            {
                EditorGUILayout.HelpBox(validationError, MessageType.Warning);
            }

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(validationError != null))
            {
                if (GUILayout.Button("Generate Shell"))
                {
                    Generate(generator);
                }
            }

            using (new EditorGUI.DisabledScope(
                       generator.WallsRoot == null && generator.CeilingRoot == null))
            {
                if (GUILayout.Button("Clear Generated Shell"))
                {
                    Clear(generator);
                }
            }
        }

        private static void Generate(DungeonShellGenerator generator)
        {
            Undo.SetCurrentGroupName("Generate Dungeon Shell");
            int undoGroup = Undo.GetCurrentGroup();

            ClearChildren(generator.WallsRoot);
            ClearChildren(generator.CeilingRoot);

            Dictionary<Vector2Int, float> floorCells = CollectFloorCells(generator);
            int wallCount = 0;
            foreach (KeyValuePair<Vector2Int, float> cell in floorCells)
            {
                CreateCeiling(generator, cell.Key, cell.Value);

                for (int side = 0; side < NeighborDirections.Length; side++)
                {
                    Vector2Int direction = NeighborDirections[side];
                    if (floorCells.ContainsKey(cell.Key + direction))
                    {
                        continue;
                    }

                    for (int level = 0; level < generator.WallLevels; level++)
                    {
                        CreateWall(generator, cell.Key, cell.Value, direction, side, level);
                        wallCount++;
                    }
                }
            }

            EditorUtility.SetDirty(generator);
            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log(
                $"Generated {wallCount} walls and {floorCells.Count} ceiling tiles.",
                generator);
        }

        private static Dictionary<Vector2Int, float> CollectFloorCells(
            DungeonShellGenerator generator)
        {
            Dictionary<Vector2Int, float> cells = new();
            foreach (Transform floorTile in generator.FloorRoot)
            {
                Vector3 localPosition =
                    generator.transform.InverseTransformPoint(floorTile.position);
                Vector2Int cell = new(
                    Mathf.RoundToInt(localPosition.x / generator.GridSize),
                    Mathf.RoundToInt(localPosition.z / generator.GridSize));
                cells[cell] = localPosition.y;
            }

            return cells;
        }

        private static void CreateCeiling(
            DungeonShellGenerator generator,
            Vector2Int cell,
            float floorY)
        {
            GameObject instance = Instantiate(
                generator.CeilingPrefab,
                generator.CeilingRoot);
            instance.name = $"Ceiling {cell.x}, {cell.y}";

            Vector3 rootLocalPosition = new(
                cell.x * generator.GridSize,
                floorY + generator.WallLevels * generator.WallLevelHeight,
                cell.y * generator.GridSize);
            SetPositionFromGeneratorSpace(
                instance.transform,
                generator.transform,
                rootLocalPosition,
                Quaternion.identity);
        }

        private static void CreateWall(
            DungeonShellGenerator generator,
            Vector2Int cell,
            float floorY,
            Vector2Int direction,
            int side,
            int level)
        {
            GameObject instance = Instantiate(generator.WallPrefab, generator.WallsRoot);
            instance.name = $"Wall {cell.x}, {cell.y} - {side} - {level}";

            float halfGrid = generator.GridSize * 0.5f;
            Vector3 rootLocalPosition = new(
                cell.x * generator.GridSize + direction.x * halfGrid,
                floorY + level * generator.WallLevelHeight,
                cell.y * generator.GridSize + direction.y * halfGrid);
            Quaternion rootLocalRotation =
                direction.x == 0
                    ? Quaternion.identity
                    : Quaternion.Euler(0f, 90f, 0f);
            SetPositionFromGeneratorSpace(
                instance.transform,
                generator.transform,
                rootLocalPosition,
                rootLocalRotation);
        }

        private static GameObject Instantiate(GameObject prefab, Transform parent)
        {
            GameObject instance =
                (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            Undo.RegisterCreatedObjectUndo(instance, "Generate Dungeon Shell");
            return instance;
        }

        private static void SetPositionFromGeneratorSpace(
            Transform instance,
            Transform generatorRoot,
            Vector3 localPosition,
            Quaternion localRotation)
        {
            instance.position = generatorRoot.TransformPoint(localPosition);
            instance.rotation = generatorRoot.rotation * localRotation;
            instance.localScale = Vector3.one;
        }

        private static void Clear(DungeonShellGenerator generator)
        {
            Undo.SetCurrentGroupName("Clear Dungeon Shell");
            int undoGroup = Undo.GetCurrentGroup();
            ClearChildren(generator.WallsRoot);
            ClearChildren(generator.CeilingRoot);
            Undo.CollapseUndoOperations(undoGroup);
        }

        private static void ClearChildren(Transform root)
        {
            if (root == null)
            {
                return;
            }

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Undo.DestroyObjectImmediate(root.GetChild(i).gameObject);
            }
        }

        private static string GetValidationError(DungeonShellGenerator generator)
        {
            if (generator.FloorRoot == null ||
                generator.WallsRoot == null ||
                generator.CeilingRoot == null)
            {
                return "Assign Floor, Walls, and Ceiling roots.";
            }

            if (generator.WallPrefab == null || generator.CeilingPrefab == null)
            {
                return "Assign wall and ceiling prefabs.";
            }

            if (generator.FloorRoot.childCount == 0)
            {
                return "Floor Root has no direct child tiles.";
            }

            if (generator.FloorRoot == generator.WallsRoot ||
                generator.FloorRoot == generator.CeilingRoot ||
                generator.WallsRoot == generator.CeilingRoot)
            {
                return "Floor, Walls, and Ceiling must be different objects.";
            }

            return null;
        }
    }
}
