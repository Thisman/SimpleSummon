using SimpleSummon.Domain;
using SimpleSummon.Network;
using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class QuestHudController : MonoBehaviour
    {
        [SerializeField] private NetworkQuestState questState;
        [SerializeField] private QuestHudView view;

        private PlayerController localController;

        private void OnEnable()
        {
            questState.Changed += Refresh;
            NetworkPlayer.LocalPlayerChanged += BindLocalPlayer;
            PlayerRegistry.Changed += BindLocalPlayer;
            GameLocalization.LocaleChanged += Refresh;
            BindLocalPlayer();
            Refresh();
        }

        private void OnDisable()
        {
            questState.Changed -= Refresh;
            NetworkPlayer.LocalPlayerChanged -= BindLocalPlayer;
            PlayerRegistry.Changed -= BindLocalPlayer;
            GameLocalization.LocaleChanged -= Refresh;
            UnbindLocalPlayer();
        }

        private void BindLocalPlayer()
        {
            UnbindLocalPlayer();
            NetworkPlayer localPlayer = NetworkPlayer.LocalPlayer;
            if (localPlayer == null)
            {
                localPlayer = PlayerRegistry.GetLocalPlayer()?.GetComponent<NetworkPlayer>();
            }
            if (localPlayer == null)
            {
                return;
            }

            localController = localPlayer.GetComponent<PlayerController>();
            localController.VitalStateChanged += HandleVitalStateChanged;
            RefreshHealth(localController.CurrentHealth);
        }

        private void UnbindLocalPlayer()
        {
            if (localController != null)
            {
                localController.VitalStateChanged -= HandleVitalStateChanged;
            }
            localController = null;
        }

        private void HandleVitalStateChanged(float health, float _) => RefreshHealth(health);

        private void Refresh()
        {
            RefreshHealth(localController != null ? localController.CurrentHealth : 0f);
            view.SetQuestState(
                questState.BossHeartCollected,
                questState.ArtifactResourceCount,
                QuestProgress.ArtifactResourceRequirement,
                questState.ArtifactCrafted,
                questState.CollectedSignFragmentCount,
                QuestProgress.SignFragmentCount);
        }

        private void RefreshHealth(float health)
        {
            view.SetHealth(health, localController != null ? localController.MaximumHealth : 0f);
        }
    }
}
