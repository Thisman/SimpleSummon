using UnityEngine;
using UnityEngine.UI;

namespace SimpleSummon.Runtime
{
    public sealed class StrikethroughText : Text
    {
        private const string LineName = "Strikethrough";
        private const float MinimumLineHeight = 12f;
        private const float RelativeLineHeight = 0.3f;
        private const float MinimumLineAngle = -7f;
        private const float MaximumLineAngle = 7f;

        private bool completed;
        private RectTransform line;
        private HandDrawnStrikeGraphic strikeGraphic;
        private float lineAngle;

        public Graphic StrikeGraphic => strikeGraphic;

        public void SetCompleted(bool value)
        {
            completed = value;
            UpdateLine();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            UpdateLine();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            UpdateLine();
        }

        private void UpdateLine()
        {
            EnsureLine();
            if (line == null || strikeGraphic == null)
            {
                return;
            }

            line.gameObject.SetActive(completed && !string.IsNullOrEmpty(text));
            float width = Mathf.Min(preferredWidth, rectTransform.rect.width);
            line.sizeDelta = new Vector2(
                width,
                Mathf.Max(MinimumLineHeight, fontSize * RelativeLineHeight));
            line.anchoredPosition = new Vector2(GetHorizontalOffset(width), GetVerticalOffset());
            line.localRotation = Quaternion.Euler(0f, 0f, lineAngle);
            strikeGraphic.color = Color.white;
        }

        private void EnsureLine()
        {
            if (line != null)
            {
                return;
            }

            Transform existing = transform.Find(LineName);
            if (existing != null)
            {
                strikeGraphic = existing.GetComponent<HandDrawnStrikeGraphic>();
                if (strikeGraphic != null)
                {
                    line = existing.GetComponent<RectTransform>();
                    return;
                }

                DestroyImmediate(existing.gameObject);
            }

            GameObject lineObject = new(
                LineName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(HandDrawnStrikeGraphic));
            lineObject.transform.SetParent(transform, false);
            line = (RectTransform)lineObject.transform;
            line.anchorMin = new Vector2(0.5f, 0.5f);
            line.anchorMax = new Vector2(0.5f, 0.5f);
            line.pivot = new Vector2(0.5f, 0.5f);
            strikeGraphic = lineObject.GetComponent<HandDrawnStrikeGraphic>();
            strikeGraphic.raycastTarget = false;
            strikeGraphic.color = Color.white;

            int seed = GetEntityId().GetHashCode();
            System.Random random = new(seed);
            lineAngle = Mathf.Lerp(
                MinimumLineAngle,
                MaximumLineAngle,
                (float)random.NextDouble());
            strikeGraphic.Configure(seed);
        }

        private float GetHorizontalOffset(float width)
        {
            Rect rect = rectTransform.rect;
            return alignment switch
            {
                TextAnchor.UpperLeft or TextAnchor.MiddleLeft or TextAnchor.LowerLeft =>
                    rect.xMin + width * 0.5f,
                TextAnchor.UpperRight or TextAnchor.MiddleRight or TextAnchor.LowerRight =>
                    rect.xMax - width * 0.5f,
                _ => 0f
            };
        }

        private float GetVerticalOffset()
        {
            Rect rect = rectTransform.rect;
            return alignment switch
            {
                TextAnchor.UpperLeft or TextAnchor.UpperCenter or TextAnchor.UpperRight =>
                    rect.yMax - fontSize * 0.5f,
                TextAnchor.LowerLeft or TextAnchor.LowerCenter or TextAnchor.LowerRight =>
                    rect.yMin + fontSize * 0.5f,
                _ => 0f
            };
        }

    }
}
