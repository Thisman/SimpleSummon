using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleSummon.Runtime
{
    public sealed class CraftingView : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private GameObject container;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Text exitHintText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button craftButton;
        [SerializeField] private List<CraftingSlotView> slots = new();

        public event Action CraftRequested;
        public event Action<int, int> MergeRequested;

        public int SlotCount => slots.Count;

        private void Awake()
        {
            container.SetActive(false);
            foreach (CraftingSlotView slot in slots)
            {
                slot.Configure(canvas);
                slot.MergeRequested += HandleMergeRequested;
            }
            craftButton.onClick.AddListener(HandleCraftRequested);
        }

        private void OnEnable()
        {
            GameLocalization.LocaleChanged += RefreshText;
        }

        private void OnDestroy()
        {
            GameLocalization.LocaleChanged -= RefreshText;
            foreach (CraftingSlotView slot in slots)
            {
                slot.MergeRequested -= HandleMergeRequested;
            }
            craftButton.onClick.RemoveListener(HandleCraftRequested);
        }

        public void Show(int[] slotCounts)
        {
            container.SetActive(true);
            SetSlotCounts(slotCounts);
        }

        public void Hide()
        {
            container.SetActive(false);
        }

        public void SetSlotCounts(IReadOnlyList<int> slotCounts)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                slots[i].SetCount(i < slotCounts.Count ? slotCounts[i] : 0);
            }
        }

        public void SetState(bool crafted, int resources, int requiredResources, bool canCraft)
        {
            statusText.text = crafted
                ? GameLocalization.Get("craft.complete")
                : GameLocalization.FormatQuestCount("craft.resources", resources, requiredResources);
            craftButton.interactable = !crafted && canCraft;
        }

        private void RefreshText()
        {
            titleText.text = GameLocalization.Get("craft.title");
            exitHintText.text = GameLocalization.Get("craft.exit_hint");
            craftButton.GetComponentInChildren<TMP_Text>().text = GameLocalization.Get("craft.button");
        }

        private void HandleCraftRequested() => CraftRequested?.Invoke();

        private void HandleMergeRequested(CraftingSlotView source, CraftingSlotView target) =>
            MergeRequested?.Invoke(slots.IndexOf(source), slots.IndexOf(target));
    }
}
