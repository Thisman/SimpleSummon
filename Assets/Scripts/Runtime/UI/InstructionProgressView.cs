using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class InstructionProgressView : MonoBehaviour
    {
        [SerializeField] private StrikethroughText signQuestText;
        [SerializeField] private StrikethroughText bossQuestText;

        public void SetProgress(bool signDrawn, bool bossHeartCollected)
        {
            string sign = GameLocalizationController.Get("instruction.sign");
            string boss = GameLocalizationController.Get("instruction.boss");
            signQuestText.text = sign;
            signQuestText.SetCompleted(signDrawn);
            bossQuestText.text = boss;
            bossQuestText.SetCompleted(bossHeartCollected);
        }
    }
}
