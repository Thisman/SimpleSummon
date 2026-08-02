using UnityEngine;
using UnityEngine.UI;

namespace SimpleSummon.Runtime
{
    public sealed class HandDrawnStrikeGraphic : MaskableGraphic
    {
        private const int PointCount = 6;

        private readonly float[] verticalOffsets = new float[PointCount];

        public void Configure(int seed)
        {
            System.Random random = new(seed);
            for (int i = 0; i < verticalOffsets.Length; i++)
            {
                verticalOffsets[i] = Mathf.Lerp(-0.16f, 0.16f, (float)random.NextDouble());
            }

            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();

            Rect rect = rectTransform.rect;
            float halfThickness = rect.height * 0.275f;

            for (int i = 0; i < verticalOffsets.Length; i++)
            {
                float progress = i / (float)(verticalOffsets.Length - 1);
                float x = Mathf.Lerp(rect.xMin, rect.xMax, progress);
                float y = verticalOffsets[i] * rect.height;
                AddVertex(vertexHelper, x, y - halfThickness);
                AddVertex(vertexHelper, x, y + halfThickness);

                if (i == 0)
                {
                    continue;
                }

                int current = i * 2;
                vertexHelper.AddTriangle(current - 2, current - 1, current + 1);
                vertexHelper.AddTriangle(current - 2, current + 1, current);
            }
        }

        private void AddVertex(VertexHelper vertexHelper, float x, float y)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;
            vertex.position = new Vector3(x, y);
            vertexHelper.AddVert(vertex);
        }
    }
}
