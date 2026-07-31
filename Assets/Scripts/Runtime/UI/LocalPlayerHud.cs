using UnityEngine;
using UnityEngine.UI;

namespace SimpleSummon.Runtime
{
    public sealed class LocalPlayerHud : MonoBehaviour
    {
        [SerializeField] private InteractionPromptView interactionPrompt;
        [SerializeField] private GameObject instructionContainer;
        [SerializeField] private GameObject summonContainer;
        [SerializeField] private RectTransform signContainer;
        [SerializeField] private SummonSignGraphic summonSignGraphic;
        [SerializeField] private Button summonButton;
        [SerializeField] private GameObject signPuzzleContainer;
        [SerializeField] private SignPuzzleView signPuzzleView;

        public static LocalPlayerHud Instance { get; private set; }

        public InteractionPromptView InteractionPrompt => interactionPrompt;
        public GameObject InstructionContainer => instructionContainer;
        public GameObject SummonContainer => summonContainer;
        public RectTransform SignContainer => signContainer;
        public SummonSignGraphic SummonSignGraphic => summonSignGraphic;
        public Button SummonButton => summonButton;
        public GameObject SignPuzzleContainer => signPuzzleContainer;
        public SignPuzzleView SignPuzzleView => signPuzzleView;

        private void Awake()
        {
            Instance = this;
            summonContainer.SetActive(false);
            instructionContainer.SetActive(false);
            signPuzzleContainer.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
