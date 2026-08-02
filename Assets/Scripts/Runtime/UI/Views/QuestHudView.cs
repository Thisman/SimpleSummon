using TMPro;
using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class QuestHudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private TMP_Text bossHeartText;
        [SerializeField] private TMP_Text resourcesText;
        [SerializeField] private TMP_Text artifactText;
        [SerializeField] private TMP_Text fragmentsText;

        public void SetHealth(float current, float maximum)
        {
            healthText.text = GameLocalization.FormatHealth(current, maximum);
        }

        public void SetQuestState(
            bool bossHeartCollected,
            int artifactResourceCount,
            int requiredArtifactResources,
            bool artifactCrafted,
            int collectedFragmentCount,
            int requiredFragmentCount)
        {
            bossHeartText.text = GameLocalization.FormatQuestFlag("hud.boss_heart", bossHeartCollected);
            resourcesText.text = GameLocalization.FormatQuestCount(
                "hud.artifact_resources", artifactResourceCount, requiredArtifactResources);
            artifactText.text = GameLocalization.FormatQuestFlag("hud.artifact", artifactCrafted);
            fragmentsText.text = GameLocalization.FormatQuestCount(
                "hud.fragments", collectedFragmentCount, requiredFragmentCount);
        }
    }
}
