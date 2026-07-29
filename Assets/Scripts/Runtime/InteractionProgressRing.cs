using UnityEngine;
using UnityEngine.UI;

namespace SimpleSummon.Runtime
{
    public sealed class InteractionProgressRing : MaskableGraphic
    {
        private const int SegmentCount = 64;

        [SerializeField, Range(0.1f, 0.9f)] private float thickness = 0.18f;
        [SerializeField] private Color backgroundColor = new(1f, 1f, 1f, 0.2f);

        private float progress;

        public void SetProgress(float value)
        {
            progress = Mathf.Clamp01(value);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();

            float outerRadius = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * 0.5f;
            float innerRadius = outerRadius * (1f - thickness);

            for (int i = 0; i < SegmentCount; i++)
            {
                float start = i / (float)SegmentCount;
                float end = (i + 1) / (float)SegmentCount;
                Color segmentColor = end <= progress ? color : backgroundColor;

                AddSegment(vertexHelper, start, end, innerRadius, outerRadius, segmentColor);
            }
        }

        private static void AddSegment(
            VertexHelper vertexHelper,
            float start,
            float end,
            float innerRadius,
            float outerRadius,
            Color segmentColor)
        {
            float startAngle = Mathf.PI * 0.5f - start * Mathf.PI * 2f;
            float endAngle = Mathf.PI * 0.5f - end * Mathf.PI * 2f;
            Vector2 startDirection = new(Mathf.Cos(startAngle), Mathf.Sin(startAngle));
            Vector2 endDirection = new(Mathf.Cos(endAngle), Mathf.Sin(endAngle));
            int vertexIndex = vertexHelper.currentVertCount;

            vertexHelper.AddVert(startDirection * innerRadius, segmentColor, Vector2.zero);
            vertexHelper.AddVert(startDirection * outerRadius, segmentColor, Vector2.zero);
            vertexHelper.AddVert(endDirection * outerRadius, segmentColor, Vector2.zero);
            vertexHelper.AddVert(endDirection * innerRadius, segmentColor, Vector2.zero);
            vertexHelper.AddTriangle(vertexIndex, vertexIndex + 1, vertexIndex + 2);
            vertexHelper.AddTriangle(vertexIndex, vertexIndex + 2, vertexIndex + 3);
        }
    }
}
