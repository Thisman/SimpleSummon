using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SimpleSummon.Runtime
{
    public sealed class CraftingSlotView : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text countText;

        private Canvas canvas;
        private int count;
        private Vector2 startPosition;

        public int Count => count;

        public event Action<CraftingSlotView, CraftingSlotView> MergeRequested;

        public void Configure(Canvas rootCanvas)
        {
            canvas = rootCanvas;
        }

        public void SetCount(int value)
        {
            count = value;
            icon.enabled = count > 0;
            countText.text = count > 1 ? count.ToString() : string.Empty;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (count <= 0)
            {
                return;
            }
            startPosition = icon.rectTransform.anchoredPosition;
            icon.raycastTarget = false;
            icon.transform.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (count > 0)
            {
                icon.rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            icon.raycastTarget = true;
            icon.rectTransform.anchoredPosition = startPosition;
        }

        public void OnDrop(PointerEventData eventData)
        {
            CraftingSlotView source = eventData.pointerDrag != null
                ? eventData.pointerDrag.GetComponent<CraftingSlotView>()
                : null;
            if (source != null && source != this)
            {
                MergeRequested?.Invoke(source, this);
            }
        }
    }
}
