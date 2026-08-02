using System.Collections.Generic;
using SimpleSummon.Domain;
using SimpleSummon.Network;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SimpleSummon.Runtime
{
    public sealed class CraftingView : MonoBehaviour
    {
        [SerializeField] private NetworkQuestState questState;
        [SerializeField] private Canvas canvas;
        [SerializeField] private GameObject container;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Text exitHintText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button craftButton;
        [SerializeField] private List<CraftingSlotView> slots = new();

        private bool open;
        private PlayerController playerController;
        private PlayerInteractionController interactionController;
        private OrbitCameraController lookController;

        private void Awake()
        {
            container.SetActive(false);
            foreach (CraftingSlotView slot in slots)
            {
                slot.Configure(this, canvas);
            }
            craftButton.onClick.AddListener(Craft);
        }

        private void OnEnable()
        {
            questState.Changed += HandleQuestChanged;
            GameLocalization.LocaleChanged += RefreshText;
        }

        private void OnDisable()
        {
            questState.Changed -= HandleQuestChanged;
            GameLocalization.LocaleChanged -= RefreshText;
        }

        private void Update()
        {
            if (open && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        public void Open(GameObject interactor)
        {
            playerController = interactor.GetComponent<PlayerController>();
            interactionController = interactor.GetComponent<PlayerInteractionController>();
            lookController = interactor.GetComponentInChildren<OrbitCameraController>(true);
            playerController?.SetLocalInputEnabled(false);
            interactionController?.SetLocalInputEnabled(false);
            if (lookController != null)
            {
                lookController.enabled = false;
            }
            open = true;
            container.SetActive(true);
            LocalPlayerHud.Instance?.EnterModalMode();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            ResetSlots();
            RefreshText();
        }

        public void Close()
        {
            open = false;
            container.SetActive(false);
            LocalPlayerHud.Instance?.ExitModalMode();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            playerController?.SetLocalInputEnabled(true);
            interactionController?.SetLocalInputEnabled(true);
            if (lookController != null)
            {
                lookController.enabled = true;
            }
            playerController = null;
            interactionController = null;
            lookController = null;
        }

        public void TryMerge(CraftingSlotView source, CraftingSlotView target)
        {
            if (!open || source.Count <= 0 || target.Count <= 0)
            {
                return;
            }

            target.SetCount(target.Count + source.Count);
            source.SetCount(0);
            RefreshCraftButton();
        }

        private void Craft()
        {
            if (HasCompleteStack() &&
                questState.ArtifactResourceCount == QuestProgress.ArtifactResourceRequirement)
            {
                questState.RequestCraftArtifact();
            }
        }

        private void HandleQuestChanged()
        {
            if (!open)
            {
                return;
            }
            if (questState.ArtifactCrafted)
            {
                ResetSlots();
            }
            RefreshText();
        }

        private void ResetSlots()
        {
            int resources = questState.ArtifactResourceCount;
            for (int i = 0; i < slots.Count; i++)
            {
                slots[i].SetCount(i < resources ? 1 : 0);
            }
            RefreshCraftButton();
        }

        private void RefreshCraftButton()
        {
            craftButton.interactable = !questState.ArtifactCrafted && HasCompleteStack();
        }

        private bool HasCompleteStack()
        {
            int nonEmptySlotCount = 0;
            int mergedResourceCount = 0;
            foreach (CraftingSlotView slot in slots)
            {
                if (slot.Count <= 0)
                {
                    continue;
                }

                nonEmptySlotCount++;
                mergedResourceCount = slot.Count;
            }

            return nonEmptySlotCount == 1 &&
                   mergedResourceCount == QuestProgress.ArtifactResourceRequirement;
        }

        private void RefreshText()
        {
            titleText.text = GameLocalization.Get("craft.title");
            exitHintText.text = GameLocalization.Get("craft.exit_hint");
            statusText.text = questState.ArtifactCrafted
                ? GameLocalization.Get("craft.complete")
                : GameLocalization.FormatQuestCount(
                    "craft.resources", questState.ArtifactResourceCount,
                    QuestProgress.ArtifactResourceRequirement);
            craftButton.GetComponentInChildren<TMP_Text>().text = GameLocalization.Get("craft.button");
        }
    }
}
