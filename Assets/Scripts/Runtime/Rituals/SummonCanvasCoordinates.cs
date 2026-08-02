using UnityEngine;

namespace SimpleSummon.Runtime
{
    internal sealed class SummonCanvasCoordinates
    {
        private readonly RectTransform signContainer;

        public SummonCanvasCoordinates(RectTransform signContainer)
        {
            this.signContainer = signContainer;
        }

        public bool TryNormalize(Vector2 screenPoint, out Vector2 normalized)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    signContainer,
                    screenPoint,
                    GetEventCamera(),
                    out Vector2 localPoint))
            {
                normalized = default;
                return false;
            }

            Rect square = SummonSignGraphic.GetSquareRect(signContainer.rect);
            if (!square.Contains(localPoint))
            {
                normalized = default;
                return false;
            }

            normalized = new Vector2(
                Mathf.InverseLerp(square.xMin, square.xMax, localPoint.x),
                Mathf.InverseLerp(square.yMin, square.yMax, localPoint.y));
            return true;
        }

        public bool Contains(RectTransform target, Vector2 screenPoint) =>
            RectTransformUtility.RectangleContainsScreenPoint(
                target,
                screenPoint,
                GetEventCamera());

        private Camera GetEventCamera()
        {
            Canvas canvas = signContainer.GetComponentInParent<Canvas>();
            return canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;
        }
    }
}
