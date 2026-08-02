using SimpleSummon.Network;
using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class InstructionProgressController : MonoBehaviour
    {
        [SerializeField] private NetworkQuestState questState;
        [SerializeField] private InstructionProgressView view;

        private void OnEnable()
        {
            questState.Changed += Refresh;
            GameLocalizationController.AddLocaleChangedListener(Refresh);
            Refresh();
        }

        private void OnDisable()
        {
            questState.Changed -= Refresh;
            GameLocalizationController.RemoveLocaleChangedListener(Refresh);
        }

        private void Refresh() => view.SetProgress(
            questState.SignDrawn,
            questState.BossHeartCollected);
    }
}
