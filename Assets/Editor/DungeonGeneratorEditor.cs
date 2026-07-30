using System;
using System.Collections.Generic;
using SimpleSummon.Runtime;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

namespace SimpleSummon.Editor
{
    [CustomEditor(typeof(DungeonGenerator))]
    public sealed class DungeonGeneratorEditor : UnityEditor.Editor
    {
        private const float GridSize = 4f;
        private const int CorridorWidth = 3;
        private const int MinimumRoomSize = 5;
        private const int MaximumRoomSize = 9;
        private const int PlacementAttemptsPerRoom = 100;
        private const float LightSurfaceOffset = 1f;

        private static readonly string[] GeneratedRootNames =
        {
            "Plane", "Walls", "Floor", "Cell", "Pillars", "Lights", "Ligths"
        };

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
                if (GUILayout.Button("Generate Dungeon"))
                {
                    Generate(generator);
                }
            }

            if (GUILayout.Button("Clear Generated Dungeon"))
            {
                Clear(generator);
            }
        }

        private static void Generate(DungeonGenerator generator)
        {
            Undo.SetCurrentGroupName("Generate Dungeon");
            int undoGroup = Undo.GetCurrentGroup();

            Random random = new(Environment.TickCount);
            List<RectInt> rooms = CreateRooms(generator, random);
            if (rooms.Count != generator.RoomCount)
            {
                Undo.CollapseUndoOperations(undoGroup);
                Debug.LogError(
                    $"Could place only {rooms.Count} of {generator.RoomCount} rooms. " +
                    "Increase dungeon Width/Length or reduce Room Count.",
                    generator);
                return;
            }

            ClearGeneratedRoots(generator.transform);
            HashSet<Vector2Int> floorCells = new();
            foreach (RectInt room in rooms)
            {
                FillRoom(room, floorCells);
            }

            for (int i = 1; i < rooms.Count; i++)
            {
                ConnectRooms(rooms[i - 1], rooms[i], floorCells, random);
            }

            CreatePlane(generator);
            Transform wallsRoot = CreateRoot(generator.transform, "Walls");
            Transform floorRoot = CreateRoot(generator.transform, "Floor");
            Transform ceilingRoot = CreateRoot(generator.transform, "Cell");
            Transform pillarsRoot = CreateRoot(generator.transform, "Pillars");
            Transform lightsRoot = CreateRoot(generator.transform, "Lights");

            CreateFloor(generator, floorRoot, floorCells);
            int wallCount = CreateWalls(generator, wallsRoot, floorCells);
            CreateCeiling(generator, ceilingRoot, floorCells);
            Dictionary<Vector2Int, Transform> pillars =
                CreatePillars(generator, pillarsRoot, rooms, floorCells);
            int lightCount = CreateLights(
                generator,
                lightsRoot,
                rooms,
                pillars,
                random);

            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log(
                $"Generated dungeon: {rooms.Count} rooms, {floorCells.Count} floor tiles, " +
                $"{wallCount} walls, {pillars.Count} pillar stacks, {lightCount} lights.",
                generator);
        }

        private static List<RectInt> CreateRooms(
            DungeonGenerator generator,
            Random random)
        {
            List<RectInt> rooms = new();
            int attempts = generator.RoomCount * PlacementAttemptsPerRoom;
            while (rooms.Count < generator.RoomCount && attempts-- > 0)
            {
                int roomWidth = random.Next(
                    MinimumRoomSize,
                    Mathf.Min(MaximumRoomSize, generator.Width - 2) + 1);
                int roomLength = random.Next(
                    MinimumRoomSize,
                    Mathf.Min(MaximumRoomSize, generator.Length - 2) + 1);
                int x = random.Next(1, generator.Width - roomWidth);
                int y = random.Next(1, generator.Length - roomLength);
                RectInt candidate = new(x, y, roomWidth, roomLength);

                bool overlaps = false;
                RectInt padded = new(
                    candidate.xMin - 1,
                    candidate.yMin - 1,
                    candidate.width + 2,
                    candidate.height + 2);
                foreach (RectInt room in rooms)
                {
                    if (padded.Overlaps(room))
                    {
                        overlaps = true;
                        break;
                    }
                }

                if (!overlaps)
                {
                    rooms.Add(candidate);
                }
            }

            return rooms;
        }

        private static void FillRoom(RectInt room, HashSet<Vector2Int> floorCells)
        {
            foreach (Vector2Int position in room.allPositionsWithin)
            {
                floorCells.Add(position);
            }
        }

        private static void ConnectRooms(
            RectInt first,
            RectInt second,
            HashSet<Vector2Int> floorCells,
            Random random)
        {
            Vector2Int start = new(
                Mathf.RoundToInt(first.center.x),
                Mathf.RoundToInt(first.center.y));
            Vector2Int end = new(
                Mathf.RoundToInt(second.center.x),
                Mathf.RoundToInt(second.center.y));

            if (random.Next(0, 2) == 0)
            {
                CarveHorizontal(start.x, end.x, start.y, floorCells);
                CarveVertical(start.y, end.y, end.x, floorCells);
            }
            else
            {
                CarveVertical(start.y, end.y, start.x, floorCells);
                CarveHorizontal(start.x, end.x, end.y, floorCells);
            }
        }

        private static void CarveHorizontal(
            int fromX,
            int toX,
            int centerY,
            HashSet<Vector2Int> floorCells)
        {
            for (int x = Mathf.Min(fromX, toX); x <= Mathf.Max(fromX, toX); x++)
            {
                for (int offset = -CorridorWidth / 2;
                     offset <= CorridorWidth / 2;
                     offset++)
                {
                    floorCells.Add(new Vector2Int(x, centerY + offset));
                }
            }
        }

        private static void CarveVertical(
            int fromY,
            int toY,
            int centerX,
            HashSet<Vector2Int> floorCells)
        {
            for (int y = Mathf.Min(fromY, toY); y <= Mathf.Max(fromY, toY); y++)
            {
                for (int offset = -CorridorWidth / 2;
                     offset <= CorridorWidth / 2;
                     offset++)
                {
                    floorCells.Add(new Vector2Int(centerX + offset, y));
                }
            }
        }

        private static Transform CreatePlane(DungeonGenerator generator)
        {
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "Plane";
            plane.transform.SetParent(generator.transform, false);
            plane.transform.localScale =
                new Vector3(generator.Width * 0.4f, 1f, generator.Length * 0.4f);
            Undo.RegisterCreatedObjectUndo(plane, "Generate Dungeon");
            return plane.transform;
        }

        private static Transform CreateRoot(Transform parent, string name)
        {
            GameObject root = new(name);
            root.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(root, "Generate Dungeon");
            return root.transform;
        }

        private static void CreateFloor(
            DungeonGenerator generator,
            Transform root,
            HashSet<Vector2Int> cells)
        {
            foreach (Vector2Int cell in cells)
            {
                CreatePrefab(
                    generator.FloorPrefab,
                    root,
                    $"Floor {cell.x}, {cell.y}",
                    CellPosition(generator, cell, 0f),
                    Quaternion.identity);
            }
        }

        private static int CreateWalls(
            DungeonGenerator generator,
            Transform root,
            HashSet<Vector2Int> cells)
        {
            int count = 0;
            foreach (Vector2Int cell in cells)
            {
                for (int side = 0; side < NeighborDirections.Length; side++)
                {
                    Vector2Int direction = NeighborDirections[side];
                    if (cells.Contains(cell + direction))
                    {
                        continue;
                    }

                    for (int level = 0; level < generator.DungeonHeight; level++)
                    {
                        Vector3 position = CellPosition(
                            generator,
                            cell,
                            level * GridSize);
                        position += new Vector3(
                            direction.x * GridSize * 0.5f,
                            0f,
                            direction.y * GridSize * 0.5f);
                        Quaternion rotation = direction.x == 0
                            ? Quaternion.identity
                            : Quaternion.Euler(0f, 90f, 0f);
                        CreatePrefab(
                            generator.WallPrefab,
                            root,
                            $"Wall {cell.x}, {cell.y} - {side} - {level}",
                            position,
                            rotation);
                        count++;
                    }
                }
            }

            return count;
        }

        private static void CreateCeiling(
            DungeonGenerator generator,
            Transform root,
            HashSet<Vector2Int> cells)
        {
            float height = generator.DungeonHeight * GridSize;
            foreach (Vector2Int cell in cells)
            {
                CreatePrefab(
                    generator.CeilingPrefab,
                    root,
                    $"Ceiling {cell.x}, {cell.y}",
                    CellPosition(generator, cell, height),
                    Quaternion.identity);
            }
        }

        private static Dictionary<Vector2Int, Transform> CreatePillars(
            DungeonGenerator generator,
            Transform root,
            List<RectInt> rooms,
            HashSet<Vector2Int> floorCells)
        {
            Dictionary<Vector2Int, Transform> result = new();
            if (generator.PillarPrefab == null)
            {
                return result;
            }

            foreach (RectInt room in rooms)
            {
                if (room.width < 7 || room.height < 7)
                {
                    continue;
                }

                Vector2Int[] candidates =
                {
                    new(room.xMin + 2, room.yMin + 2),
                    new(room.xMax - 3, room.yMin + 2),
                    new(room.xMin + 2, room.yMax - 3),
                    new(room.xMax - 3, room.yMax - 3)
                };
                foreach (Vector2Int cell in candidates)
                {
                    if (!floorCells.Contains(cell) || result.ContainsKey(cell))
                    {
                        continue;
                    }

                    for (int level = 0; level < generator.DungeonHeight; level++)
                    {
                        Transform pillar = CreatePrefab(
                            generator.PillarPrefab,
                            root,
                            $"Pillar {cell.x}, {cell.y} - {level}",
                            CellPosition(generator, cell, level * GridSize),
                            Quaternion.identity);
                        if (level == 0)
                        {
                            result.Add(cell, pillar);
                        }
                    }
                }
            }

            return result;
        }

        private static int CreateLights(
            DungeonGenerator generator,
            Transform root,
            List<RectInt> rooms,
            Dictionary<Vector2Int, Transform> pillars,
            Random random)
        {
            if (generator.LightPrefabs == null || generator.LightPrefabs.Length == 0)
            {
                return 0;
            }

            List<GameObject> usablePrefabs = new();
            foreach (GameObject prefab in generator.LightPrefabs)
            {
                if (prefab != null)
                {
                    usablePrefabs.Add(prefab);
                }
            }

            if (usablePrefabs.Count == 0)
            {
                return 0;
            }

            int count = 0;
            foreach (RectInt room in rooms)
            {
                List<LightPlacement> corners =
                    CreateCornerLightPositions(generator, room);
                Shuffle(corners, random);
                int cornerCount = random.Next(3, 5);
                for (int i = 0; i < cornerCount; i++)
                {
                    CreatePositionedLight(
                        usablePrefabs,
                        root,
                        $"Corner Light {count + 1}",
                        corners[i],
                        random);
                    count++;
                }

                foreach (KeyValuePair<Vector2Int, Transform> pillar in pillars)
                {
                    if (!room.Contains(pillar.Key))
                    {
                        continue;
                    }

                    List<LightPlacement> positions =
                        CreatePillarLightPositions(generator.transform, pillar.Value);
                    Shuffle(positions, random);
                    int pillarLightCount = random.Next(1, 3);
                    for (int i = 0; i < pillarLightCount; i++)
                    {
                        CreatePositionedLight(
                            usablePrefabs,
                            root,
                            $"Pillar Light {count + 1}",
                            positions[i],
                            random);
                        count++;
                    }
                }

                if (random.NextDouble() < 0.35)
                {
                    List<LightPlacement> wallPositions =
                        CreateWallLightPositions(generator, room);
                    LightPlacement position =
                        wallPositions[random.Next(wallPositions.Count)];
                    CreatePositionedLight(
                        usablePrefabs,
                        root,
                        $"Wall Light {count + 1}",
                        position,
                        random);
                    count++;
                }
            }

            return count;
        }

        private static List<LightPlacement> CreateCornerLightPositions(
            DungeonGenerator generator,
            RectInt room)
        {
            Vector3 minimum = CellPosition(
                generator,
                new Vector2Int(room.xMin, room.yMin),
                0f);
            Vector3 maximum = CellPosition(
                generator,
                new Vector2Int(room.xMax - 1, room.yMax - 1),
                0f);
            float minX = minimum.x - GridSize * 0.5f;
            float minZ = minimum.z - GridSize * 0.5f;
            float maxX = maximum.x + GridSize * 0.5f;
            float maxZ = maximum.z + GridSize * 0.5f;
            return new List<LightPlacement>
            {
                new(new Vector3(minX, 0f, minZ), new Vector2Int(1, 1)),
                new(new Vector3(maxX, 0f, minZ), new Vector2Int(-1, 1)),
                new(new Vector3(minX, 0f, maxZ), new Vector2Int(1, -1)),
                new(new Vector3(maxX, 0f, maxZ), new Vector2Int(-1, -1))
            };
        }

        private static List<LightPlacement> CreateWallLightPositions(
            DungeonGenerator generator,
            RectInt room)
        {
            Vector3 minimum = CellPosition(
                generator,
                new Vector2Int(room.xMin, room.yMin),
                0f);
            Vector3 maximum = CellPosition(
                generator,
                new Vector2Int(room.xMax - 1, room.yMax - 1),
                0f);
            float minX = minimum.x - GridSize * 0.5f;
            float minZ = minimum.z - GridSize * 0.5f;
            float maxX = maximum.x + GridSize * 0.5f;
            float maxZ = maximum.z + GridSize * 0.5f;
            float centerX = (minX + maxX) * 0.5f;
            float centerZ = (minZ + maxZ) * 0.5f;
            return new List<LightPlacement>
            {
                new(new Vector3(centerX, 0f, minZ), Vector2Int.up),
                new(new Vector3(centerX, 0f, maxZ), Vector2Int.down),
                new(new Vector3(minX, 0f, centerZ), Vector2Int.right),
                new(new Vector3(maxX, 0f, centerZ), Vector2Int.left)
            };
        }

        private static List<LightPlacement> CreatePillarLightPositions(
            Transform dungeonRoot,
            Transform pillar)
        {
            Bounds bounds = GetBoundsInRootSpace(dungeonRoot, pillar);
            return new List<LightPlacement>
            {
                new(
                    new Vector3(bounds.max.x, 0f, bounds.center.z),
                    Vector2Int.right),
                new(
                    new Vector3(bounds.min.x, 0f, bounds.center.z),
                    Vector2Int.left),
                new(
                    new Vector3(bounds.center.x, 0f, bounds.max.z),
                    Vector2Int.up),
                new(
                    new Vector3(bounds.center.x, 0f, bounds.min.z),
                    Vector2Int.down)
            };
        }

        private static Bounds GetBoundsInRootSpace(
            Transform dungeonRoot,
            Transform target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(target.localPosition, Vector3.one);
            }

            Bounds result = new(
                dungeonRoot.InverseTransformPoint(renderers[0].bounds.center),
                Vector3.zero);
            foreach (Renderer renderer in renderers)
            {
                Bounds worldBounds = renderer.bounds;
                for (int x = -1; x <= 1; x += 2)
                {
                    for (int y = -1; y <= 1; y += 2)
                    {
                        for (int z = -1; z <= 1; z += 2)
                        {
                            Vector3 corner = worldBounds.center +
                                             Vector3.Scale(
                                                 worldBounds.extents,
                                                 new Vector3(x, y, z));
                            result.Encapsulate(
                                dungeonRoot.InverseTransformPoint(corner));
                        }
                    }
                }
            }

            return result;
        }

        private static void CreatePositionedLight(
            IReadOnlyList<GameObject> prefabs,
            Transform root,
            string name,
            LightPlacement placement,
            Random random)
        {
            GameObject selected = prefabs[random.Next(prefabs.Count)];
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                AssetDatabase.GetAssetPath(selected));
            Transform light = CreatePrefab(
                prefab,
                root,
                name,
                placement.SurfacePoint,
                Quaternion.Euler(0f, random.Next(0, 4) * 90f, 0f));
            AlignLightToSurface(root, light, placement);
        }

        private static void AlignLightToSurface(
            Transform dungeonRoot,
            Transform light,
            LightPlacement placement)
        {
            Bounds bounds = GetBoundsInRootSpace(dungeonRoot, light);
            Vector3 adjustment = Vector3.zero;
            if (placement.Direction.x > 0)
            {
                adjustment.x =
                    placement.SurfacePoint.x + LightSurfaceOffset - bounds.min.x;
            }
            else if (placement.Direction.x < 0)
            {
                adjustment.x =
                    placement.SurfacePoint.x - LightSurfaceOffset - bounds.max.x;
            }

            if (placement.Direction.y > 0)
            {
                adjustment.z =
                    placement.SurfacePoint.z + LightSurfaceOffset - bounds.min.z;
            }
            else if (placement.Direction.y < 0)
            {
                adjustment.z =
                    placement.SurfacePoint.z - LightSurfaceOffset - bounds.max.z;
            }

            light.localPosition += adjustment;
        }

        private static void Shuffle<T>(IList<T> values, Random random)
        {
            for (int i = values.Count - 1; i > 0; i--)
            {
                int other = random.Next(i + 1);
                (values[i], values[other]) = (values[other], values[i]);
            }
        }

        private static Vector3 CellPosition(
            DungeonGenerator generator,
            Vector2Int cell,
            float y)
        {
            float xOffset = (generator.Width - 1) * 0.5f;
            float zOffset = (generator.Length - 1) * 0.5f;
            return new Vector3(
                (cell.x - xOffset) * GridSize,
                y,
                (cell.y - zOffset) * GridSize);
        }

        private static Transform CreatePrefab(
            GameObject prefab,
            Transform parent,
            string name,
            Vector3 localPosition,
            Quaternion localRotation)
        {
            GameObject instance =
                (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            Undo.RegisterCreatedObjectUndo(instance, "Generate Dungeon");
            instance.name = name;
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = localRotation;
            instance.transform.localScale = Vector3.one;
            return instance.transform;
        }

        private static void Clear(DungeonGenerator generator)
        {
            Undo.SetCurrentGroupName("Clear Generated Dungeon");
            int undoGroup = Undo.GetCurrentGroup();
            ClearGeneratedRoots(generator.transform);
            Undo.CollapseUndoOperations(undoGroup);
        }

        private static void ClearGeneratedRoots(Transform dungeonRoot)
        {
            for (int i = dungeonRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = dungeonRoot.GetChild(i);
                foreach (string generatedName in GeneratedRootNames)
                {
                    if (child.name == generatedName)
                    {
                        Undo.DestroyObjectImmediate(child.gameObject);
                        break;
                    }
                }
            }
        }

        private static string GetValidationError(DungeonGenerator generator)
        {
            if (generator.FloorPrefab == null ||
                generator.WallPrefab == null ||
                generator.CeilingPrefab == null)
            {
                return "Assign Floor, Wall, and Ceiling prefabs.";
            }

            if (generator.Width < 7 || generator.Length < 7)
            {
                return "Width and Length must be at least 7 cells.";
            }

            if (generator.RoomCount < 1)
            {
                return "Room Count must be at least 1.";
            }

            return null;
        }

        private readonly struct LightPlacement
        {
            public LightPlacement(Vector3 surfacePoint, Vector2Int direction)
            {
                SurfacePoint = surfacePoint;
                Direction = direction;
            }

            public Vector3 SurfacePoint { get; }
            public Vector2Int Direction { get; }
        }
    }
}
