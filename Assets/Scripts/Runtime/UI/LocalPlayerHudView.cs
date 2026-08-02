using UnityEngine;
using UnityEngine.UI;

namespace SimpleSummon.Runtime
{
    public sealed class LocalPlayerHudView : MonoBehaviour
    {
        [SerializeField] private InteractionPromptView interactionPrompt;
        [SerializeField] private GameObject instructionContainer;
        [SerializeField] private GameObject summonContainer;
        [SerializeField] private RectTransform signContainer;
        [SerializeField] private SignDrawingGraphic summonSignGraphic;
        [SerializeField] private Button summonButton;
        [SerializeField] private GameObject signPuzzleContainer;
        [SerializeField] private SignBuilderView signPuzzleView;
        [SerializeField] private GameObject questProgressContainer;

        public InteractionPromptView InteractionPrompt => interactionPrompt;
        public GameObject InstructionContainer => instructionContainer;
        public GameObject SummonContainer => summonContainer;
        public RectTransform SignContainer => signContainer;
        public SignDrawingGraphic SignDrawingGraphic => summonSignGraphic;
        public Button SummonButton => summonButton;
        public GameObject SignBuilderContainer => signPuzzleContainer;
        public SignBuilderView SignBuilderView => signPuzzleView;

        public void Initialize()
        {
            summonContainer.SetActive(false);
            instructionContainer.SetActive(false);
            signPuzzleContainer.SetActive(false);
            SetQuestProgressVisible(true);
        }

        public void SetQuestProgressVisible(bool visible)
        {
            if (questProgressContainer != null)
            {
                questProgressContainer.SetActive(visible);
            }
        }
    }
}
