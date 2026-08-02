using UnityEngine;
using UnityEngine.UI;

namespace SimpleSummon.Runtime
{
    public sealed class LocalPlayerHudView : MonoBehaviour
    {
        [SerializeField] private InteractionPromptView interactionPrompt;
        [SerializeField] private GameObject summonContainer;
        [SerializeField] private RectTransform signContainer;
        [SerializeField] private SignDrawingGraphic summonSignGraphic;
        [SerializeField] private Button submitRitualSignButton;
        [SerializeField] private GameObject playerStatsContainer;
        [SerializeField] private GameObject ingredientsContainer;

        public InteractionPromptView InteractionPrompt => interactionPrompt;
        public GameObject SummonContainer => summonContainer;
        public RectTransform SignContainer => signContainer;
        public SignDrawingGraphic SignDrawingGraphic => summonSignGraphic;
        public Button SubmitRitualSignButton => submitRitualSignButton;

        public void SetGameplayHudVisible(bool visible)
        {
            playerStatsContainer.SetActive(visible);
            ingredientsContainer.SetActive(visible);
        }

        public void Initialize()
        {
            summonContainer.SetActive(false);
        }
    }
}
