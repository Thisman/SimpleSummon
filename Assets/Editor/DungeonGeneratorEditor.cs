using System.Collections.Generic;
using SimpleSummon.Runtime;
using UnityEditor;
using UnityEngine;

namespace SimpleSummon.Editor
{
    [CustomEditor(typeof(DungeonGenerator))]
    public sealed class DungeonGeneratorEditor : UnityEditor.Editor
    {
        private const float GridSize = 4f;

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

            DungeonGenerator generator = (DungeonGenerator)target;
            string validationError = GetValidationError(generator);
            if (validationError != null)
            {
                EditorGUILayout.HelpBox(validationError, MessageType.Warning);
            }

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(validationError != null))
            {
                if (GUILayout.Button("Generate Walls and Ceiling"))
                {
                    Generate(generator);
                }
            }

            if (GUILayout.Button("Clear Walls and Ceiling"))
            {
                Clear(generator);
            }
        }

        private static void Generate(DungeonGenerator generator)
        {
            Undo.SetCurrentGroupName("Generate Dungeon Walls and Ceiling");
            int undoGroup = Undo.GetCurrentGroup();

            Dictionary<Vector2Int, Vector3> floorCells = ReadFloorCells(generator);
            ClearChildren(generator.WallsRoot);
            ClearChildren(generator.CellRoot);

            int wallCount = CreateWalls(generator, floorCells);
            CreateCeiling(generator, floorCells);

            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log(
                $"Generated {wallCount} walls and {floorCells.Count} ceiling tiles " +
                $"from the existing floor.",
                generator);
        }

        private static Dictionary<Vector2Int, Vector3> ReadFloorCells(
            DungeonGenerator generator)
        {
            Dictionary<Vector2Int, Vector3> cells = new();
            Vector3 origin = generator.transform.InverseTransformPoint(
                generator.FloorRoot.GetChild(0).position);

            foreach (Transform floorTile in generator.FloorRoot)
            {
                Vector3 position = generator.transform.InverseTransformPoint(
                    floorTile.position);
                Vector2Int cell = new(
                    Mathf.RoundToInt((position.x - origin.x) / GridSize),
                    Mathf.RoundToInt((position.z - origin.z) / GridSize));
                cells[cell] = position;
            }

            return cells;
        }

        private static int CreateWalls(
            DungeonGenerator generator,
            IReadOnlyDictionary<Vector2Int, Vector3> cells)
        {
            int count = 0;
            foreach (KeyValuePair<Vector2Int, Vector3> floorCell in cells)
            {
                for (int side = 0; side < NeighborDirections.Length; side++)
                {
                    Vector2Int direction = NeighborDirections[side];
                    if (cells.ContainsKey(floorCell.Key + direction))
                    {
                        continue;
                    }

                    for (int level = 0; level < generator.DungeonHeight; level++)
                    {
                        Vector3 position = floorCell.Value + new Vector3(
                            direction.x * GridSize * 0.5f,
                            level * GridSize,
                            direction.y * GridSize * 0.5f);
                        Quaternion rotation = direction.x == 0
                            ? Quaternion.identity
                            : Quaternion.Euler(0f, 90f, 0f);
                        CreatePrefab(
                            generator.WallPrefab,
                            generator.WallsRoot,
                            $"Wall {floorCell.Key.x}, {floorCell.Key.y} - {side} - {level}",
                            position,
                            rotation,
                            generator.transform);
                        count++;
                    }
                }
            }

            return count;
        }

        private static void CreateCeiling(
            DungeonGenerator generator,
            IReadOnlyDictionary<Vector2Int, Vector3> cells)
        {
            float height = generator.DungeonHeight * GridSize;
            foreach (KeyValuePair<Vector2Int, Vector3> floorCell in cells)
            {
                CreatePrefab(
                    generator.CeilingPrefab,
                    generator.CellRoot,
                    $"Ceiling {floorCell.Key.x}, {floorCell.Key.y}",
                    floorCell.Value + Vector3.up * height,
                    Quaternion.identity,
                    generator.transform);
            }
        }

        private static void CreatePrefab(
            GameObject prefab,
            Transform parent,
            string name,
            Vector3 dungeonLocalPosition,
            Quaternion dungeonLocalRotation,
            Transform dungeonRoot)
        {
            GameObject instance =
                (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            Undo.RegisterCreatedObjectUndo(instance, "Generate Dungeon Walls and Ceiling");
            instance.name = name;
            instance.transform.SetPositionAndRotation(
                dungeonRoot.TransformPoint(dungeonLocalPosition),
                dungeonRoot.rotation * dungeonLocalRotation);
            instance.transform.localScale = Vector3.one;
        }

        private static void Clear(DungeonGenerator generator)
        {
            Undo.SetCurrentGroupName("Clear Dungeon Walls and Ceiling");
            int undoGroup = Undo.GetCurrentGroup();
            if (generator.WallsRoot != null)
            {
                ClearChildren(generator.WallsRoot);
            }
            if (generator.CellRoot != null)
            {
                ClearChildren(generator.CellRoot);
            }
            Undo.CollapseUndoOperations(undoGroup);
        }

        private static void ClearChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Undo.DestroyObjectImmediate(root.GetChild(i).gameObject);
            }
        }

        private static string GetValidationError(DungeonGenerator generator)
        {
            if (generator.WallPrefab == null || generator.CeilingPrefab == null)
            {
                return "Assign Wall and Ceiling prefabs.";
            }
            if (generator.FloorRoot == null ||
                generator.WallsRoot == null ||
                generator.CellRoot == null)
            {
                return "Assign Floor, Walls, and Cell roots.";
            }
            if (generator.FloorRoot.childCount == 0)
            {
                return "Floor must contain at least one floor tile.";
            }

            return null;
        }
    }
}
