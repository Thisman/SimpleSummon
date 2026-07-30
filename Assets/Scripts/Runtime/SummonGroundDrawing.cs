using SimpleSummon.Network;
using UnityEngine;

namespace SimpleSummon.Runtime
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class SummonGroundDrawing : MonoBehaviour
    {
        [SerializeField] private NetworkSummonRitual ritual;
        [SerializeField] private BoxCollider drawingBounds;
        [SerializeField] private Material lineMaterial;
        [SerializeField] private Color lineColor = Color.white;
        [SerializeField, Min(0.001f)] private float lineWidth = 0.16f;
        [SerializeField, Min(0.01f)] private float widthMultiplier = 1f;
        [SerializeField, Min(0f)] private float surfaceOffset = 0.01f;

        private Mesh mesh;
        private Material runtimeMaterial;
        private bool rebuildRequested;

        private void Awake()
        {
            mesh = new Mesh { name = "Summon Ground Drawing" };
            GetComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            if (lineMaterial != null)
            {
                meshRenderer.sharedMaterial = lineMaterial;
            }
            else
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader != null)
                {
                    runtimeMaterial = new Material(shader)
                    {
                        name = "Summon Ground Drawing Material",
                        color = lineColor
                    };
                    meshRenderer.sharedMaterial = runtimeMaterial;
                }
            }
        }

        private void OnEnable()
        {
            ritual.DrawingChanged += RequestRebuild;
            RequestRebuild();
        }

        private void OnDisable()
        {
            ritual.DrawingChanged -= RequestRebuild;
        }

        private void OnDestroy()
        {
            Destroy(mesh);
            Destroy(runtimeMaterial);
        }

        private void LateUpdate()
        {
            if (!rebuildRequested)
            {
                return;
            }

            rebuildRequested = false;
            Rebuild();
        }

        private void RequestRebuild()
        {
            rebuildRequested = true;
        }

        private void Rebuild()
        {
            if (mesh == null)
            {
                return;
            }

            int segmentCount = 0;
            for (int i = 1; i < ritual.PointCount; i++)
            {
                if (!ritual.GetPoint(i).StartsStroke)
                {
                    segmentCount++;
                }
            }

            Vector3[] vertices = new Vector3[segmentCount * 4];
            int[] triangles = new int[segmentCount * 6];
            Vector2[] uv = new Vector2[vertices.Length];
            int segmentIndex = 0;

            for (int i = 1; i < ritual.PointCount; i++)
            {
                NetworkSummonPoint point = ritual.GetPoint(i);
                if (point.StartsStroke)
                {
                    continue;
                }

                Vector3 start = ToLocalPoint(ritual.GetPoint(i - 1).Position);
                Vector3 end = ToLocalPoint(point.Position);
                Vector3 direction = end - start;
                if (direction.sqrMagnitude <= Mathf.Epsilon)
                {
                    continue;
                }

                Vector3 normal = Vector3.Cross(Vector3.up, direction).normalized *
                                 lineWidth * widthMultiplier * 0.5f;
                int vertexIndex = segmentIndex * 4;
                vertices[vertexIndex] = start - normal;
                vertices[vertexIndex + 1] = start + normal;
                vertices[vertexIndex + 2] = end + normal;
                vertices[vertexIndex + 3] = end - normal;

                int triangleIndex = segmentIndex * 6;
                triangles[triangleIndex] = vertexIndex;
                triangles[triangleIndex + 1] = vertexIndex + 2;
                triangles[triangleIndex + 2] = vertexIndex + 1;
                triangles[triangleIndex + 3] = vertexIndex;
                triangles[triangleIndex + 4] = vertexIndex + 3;
                triangles[triangleIndex + 5] = vertexIndex + 2;
                segmentIndex++;
            }

            if (segmentIndex * 4 != vertices.Length)
            {
                System.Array.Resize(ref vertices, segmentIndex * 4);
                System.Array.Resize(ref triangles, segmentIndex * 6);
                System.Array.Resize(ref uv, segmentIndex * 4);
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;
            mesh.RecalculateBounds();
        }

        private Vector3 ToLocalPoint(Vector2 normalized)
        {
            Vector3 center = drawingBounds.center;
            Vector3 size = drawingBounds.size;
            return new Vector3(
                center.x + (normalized.x - 0.5f) * size.x,
                center.y + size.y * 0.5f + surfaceOffset,
                center.z + (normalized.y - 0.5f) * size.z);
        }
    }
}
