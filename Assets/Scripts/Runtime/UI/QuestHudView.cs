using SimpleSummon.Domain;
using SimpleSummon.Network;
using TMPro;
using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class QuestHudView : MonoBehaviour
    {
        [SerializeField] private NetworkQuestState questState;
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private TMP_Text bossHeartText;
        [SerializeField] private TMP_Text resourcesText;
        [SerializeField] private TMP_Text artifactText;
        [SerializeField] private TMP_Text fragmentsText;

        private NetworkPlayer localPlayer;
        private PlayerController localController;

        private void OnEnable()
        {
            questState.Changed += RefreshQuest;
            NetworkPlayer.LocalPlayerChanged += BindLocalPlayer;
            PlayerRegistry.Changed += BindLocalPlayer;
            GameLocalization.LocaleChanged += RefreshAll;
            BindLocalPlayer();
            RefreshAll();
        }

        private void OnDisable()
        {
            questState.Changed -= RefreshQuest;
            NetworkPlayer.LocalPlayerChanged -= BindLocalPlayer;
            PlayerRegistry.Changed -= BindLocalPlayer;
            GameLocalization.LocaleChanged -= RefreshAll;
            UnbindLocalPlayer();
        }

        private void BindLocalPlayer()
        {
            UnbindLocalPlayer();
            localPlayer = NetworkPlayer.LocalPlayer;
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
            if (localPlayer != null)
            {
                localController.VitalStateChanged -= HandleVitalStateChanged;
            }
            localPlayer = null;
            localController = null;
        }

        private void HandleVitalStateChanged(float health, float _) => RefreshHealth(health);
        private void RefreshQuest() => RefreshAll();

        private void RefreshAll()
        {
            RefreshHealth(localController != null ? localController.CurrentHealth : 0f);
            bossHeartText.text = GameLocalization.FormatQuestFlag("hud.boss_heart", questState.BossHeartCollected);
            resourcesText.text = GameLocalization.FormatQuestCount(
                "hud.artifact_resources", questState.ArtifactResourceCount,
                QuestProgress.ArtifactResourceRequirement);
            artifactText.text = GameLocalization.FormatQuestFlag("hud.artifact", questState.ArtifactCrafted);
            fragmentsText.text = GameLocalization.FormatQuestCount(
                "hud.fragments", questState.CollectedSignFragmentCount,
                QuestProgress.SignFragmentCount);
        }

        private void RefreshHealth(float health)
        {
            float maximum = localController != null ? localController.MaximumHealth : 0f;
            healthText.text = GameLocalization.FormatHealth(health, maximum);
        }
    }
}
