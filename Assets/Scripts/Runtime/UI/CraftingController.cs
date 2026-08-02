using SimpleSummon.Application;
using SimpleSummon.Domain;
using SimpleSummon.Network;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SimpleSummon.Runtime
{
    public sealed class CraftingController : MonoBehaviour
    {
        [SerializeField] private NetworkQuestState questState;
        [SerializeField] private CraftingView view;

        private int[] slotCounts;
        private bool open;
        private PlayerController playerController;
        private PlayerInteractionController interactionController;
        private PlayerCameraController lookController;

        private void Awake()
        {
            slotCounts = new int[view.SlotCount];
            view.CraftRequested += Craft;
            view.MergeRequested += Merge;
        }

        private void OnEnable()
        {
            questState.Changed += HandleQuestChanged;
            GameLocalizationController.AddLocaleChangedListener(Refresh);
        }

        private void OnDisable()
        {
            questState.Changed -= HandleQuestChanged;
            GameLocalizationController.RemoveLocaleChangedListener(Refresh);
        }

        private void OnDestroy()
        {
            view.CraftRequested -= Craft;
            view.MergeRequested -= Merge;
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
            lookController = interactor.GetComponentInChildren<PlayerCameraController>(true);
            playerController?.SetLocalInputEnabled(false);
            interactionController?.SetLocalInputEnabled(false);
            if (lookController != null)
            {
                lookController.enabled = false;
            }

            ResetSlots();
            open = true;
            view.Show(slotCounts);
            LocalPlayerHudController.Instance?.EnterModalMode();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Refresh();
        }

        public void Close()
        {
            open = false;
            view.Hide();
            LocalPlayerHudController.Instance?.ExitModalMode();
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

        private void Merge(int sourceIndex, int targetIndex)
        {
            if (!open || !ArtifactCraftingService.TryMerge(slotCounts, sourceIndex, targetIndex))
            {
                return;
            }

            view.SetSlotCounts(slotCounts);
            Refresh();
        }

        private void Craft()
        {
            if (CanCraft())
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
                view.SetSlotCounts(slotCounts);
            }

            Refresh();
        }

        private void ResetSlots()
        {
            int resources = questState.ArtifactResourceCount;
            for (int i = 0; i < slotCounts.Length; i++)
            {
                slotCounts[i] = i < resources ? 1 : 0;
            }
        }

        private bool CanCraft() =>
            questState.ArtifactResourceCount == QuestProgress.ArtifactResourceRequirement &&
            ArtifactCraftingService.HasCompleteStack(
                slotCounts,
                QuestProgress.ArtifactResourceRequirement);

        private void Refresh()
        {
            if (!open)
            {
                return;
            }

            view.SetState(
                questState.ArtifactCrafted,
                questState.ArtifactResourceCount,
                QuestProgress.ArtifactResourceRequirement,
                CanCraft());
        }
    }
}
