using UnityEngine;

namespace SimpleSummon.Runtime
{
    [DisallowMultipleComponent]
    public sealed class DungeonGenerator : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject wallPrefab;
        [SerializeField] private GameObject ceilingPrefab;

        [Header("Dungeon")]
        [SerializeField, Min(1)] private int dungeonHeight = 2;
        [SerializeField] private Transform floorRoot;
        [SerializeField] private Transform wallsRoot;
        [SerializeField] private Transform cellRoot;

        public GameObject WallPrefab => wallPrefab;
        public GameObject CeilingPrefab => ceilingPrefab;
        public int DungeonHeight => dungeonHeight;
        public Transform FloorRoot => floorRoot;
        public Transform WallsRoot => wallsRoot;
        public Transform CellRoot => cellRoot;
    }
}
