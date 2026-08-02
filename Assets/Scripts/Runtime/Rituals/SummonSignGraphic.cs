using SimpleSummon.Network;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleSummon.Runtime
{
    public sealed class SummonSignGraphic : MaskableGraphic
    {
        [SerializeField, Min(1f)] private float lineWidth = 24f;

        private NetworkSummonRitual ritual;

        public void SetRitual(NetworkSummonRitual value)
        {
            if (ritual != null)
            {
                ritual.DrawingChanged -= SetVerticesDirty;
            }

            ritual = value;
            if (ritual != null)
            {
                ritual.DrawingChanged += SetVerticesDirty;
            }

            SetVerticesDirty();
        }

        protected override void OnDestroy()
        {
            SetRitual(null);
            base.OnDestroy();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            if (ritual == null)
            {
                return;
            }

            Rect square = GetSquareRect(rectTransform.rect);
            for (int i = 1; i < ritual.PointCount; i++)
            {
                NetworkSummonPoint current = ritual.GetPoint(i);
                if (current.StartsStroke)
                {
                    continue;
                }

                Vector2 start = ToLocalPoint(ritual.GetPoint(i - 1).Position, square);
                Vector2 end = ToLocalPoint(current.Position, square);
                AddSegment(vertexHelper, start, end);
            }
        }

        private void AddSegment(VertexHelper vertexHelper, Vector2 start, Vector2 end)
        {
            Vector2 direction = end - start;
            if (direction.sqrMagnitude <= 0.01f)
            {
                return;
            }

            Vector2 normal = new Vector2(-direction.y, direction.x).normalized * lineWidth * 0.5f;
            int index = vertexHelper.currentVertCount;
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;

            vertex.position = start - normal;
            vertexHelper.AddVert(vertex);
            vertex.position = start + normal;
            vertexHelper.AddVert(vertex);
            vertex.position = end + normal;
            vertexHelper.AddVert(vertex);
            vertex.position = end - normal;
            vertexHelper.AddVert(vertex);

            vertexHelper.AddTriangle(index, index + 1, index + 2);
            vertexHelper.AddTriangle(index, index + 2, index + 3);
        }

        public static Rect GetSquareRect(Rect source)
        {
            float size = Mathf.Min(source.width, source.height);
            return new Rect(
                source.center.x - size * 0.5f,
                source.center.y - size * 0.5f,
                size,
                size);
        }

        private static Vector2 ToLocalPoint(Vector2 normalized, Rect square)
        {
            return new Vector2(
                Mathf.Lerp(square.xMin, square.xMax, normalized.x),
                Mathf.Lerp(square.yMin, square.yMax, normalized.y));
        }
    }
}
