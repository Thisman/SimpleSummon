using SimpleSummon.Network;
using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class IngredientsController : MonoBehaviour
    {
        [SerializeField] private NetworkQuestState questState;
        [SerializeField] private IngredientsView view;

        private void OnEnable()
        {
            questState.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            questState.Changed -= Refresh;
        }

        private void Refresh() => view.SetCounts(
            questState.GreenBottleCount,
            questState.BrownBottleCount);
    }
}
