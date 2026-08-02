using UnityEngine;

namespace SimpleSummon.Runtime
{
    [DisallowMultipleComponent]
    public sealed class DungeonGenerator : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject floorPrefab;
        [SerializeField] private GameObject wallPrefab;
        [SerializeField] private GameObject ceilingPrefab;
        [SerializeField] private GameObject pillarPrefab;
        [SerializeField] private GameObject[] lightPrefabs;

        [Header("Dungeon")]
        [SerializeField, Min(1)] private int dungeonHeight = 2;
        [SerializeField, Min(7)] private int width = 30;
        [SerializeField, Min(7)] private int length = 20;
        [SerializeField, Min(1)] private int roomCount = 5;

        public GameObject FloorPrefab => floorPrefab;
        public GameObject WallPrefab => wallPrefab;
        public GameObject CeilingPrefab => ceilingPrefab;
        public GameObject PillarPrefab => pillarPrefab;
        public GameObject[] LightPrefabs => lightPrefabs;
        public int DungeonHeight => dungeonHeight;
        public int Width => width;
        public int Length => length;
        public int RoomCount => roomCount;
    }
}
