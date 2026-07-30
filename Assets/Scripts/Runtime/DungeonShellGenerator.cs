using UnityEngine;

namespace SimpleSummon.Runtime
{
    [DisallowMultipleComponent]
    public sealed class DungeonShellGenerator : MonoBehaviour
    {
        [Header("Scene roots")]
        [SerializeField] private Transform floorRoot;
        [SerializeField] private Transform wallsRoot;
        [SerializeField] private Transform ceilingRoot;

        [Header("Prefabs")]
        [SerializeField] private GameObject wallPrefab;
        [SerializeField] private GameObject ceilingPrefab;

        [Header("Grid")]
        [SerializeField, Min(0.01f)] private float gridSize = 4f;
        [SerializeField, Min(0.01f)] private float wallLevelHeight = 4f;
        [SerializeField, Min(1)] private int wallLevels = 2;

        public Transform FloorRoot => floorRoot;
        public Transform WallsRoot => wallsRoot;
        public Transform CeilingRoot => ceilingRoot;
        public GameObject WallPrefab => wallPrefab;
        public GameObject CeilingPrefab => ceilingPrefab;
        public float GridSize => gridSize;
        public float WallLevelHeight => wallLevelHeight;
        public int WallLevels => wallLevels;
    }
}
