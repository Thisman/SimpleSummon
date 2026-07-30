using UnityEngine;
using UnityEngine.UI;

namespace SimpleSummon.Runtime
{
    public sealed class LocalPlayerHud : MonoBehaviour
    {
        [SerializeField] private InteractionPromptView interactionPrompt;
        [SerializeField] private GameObject instructionExitHint;
        [SerializeField] private GameObject summonContainer;
        [SerializeField] private RectTransform signContainer;
        [SerializeField] private SummonSignGraphic summonSignGraphic;
        [SerializeField] private Button summonButton;

        public static LocalPlayerHud Instance { get; private set; }

        public InteractionPromptView InteractionPrompt => interactionPrompt;
        public GameObject InstructionExitHint => instructionExitHint;
        public GameObject SummonContainer => summonContainer;
        public RectTransform SignContainer => signContainer;
        public SummonSignGraphic SummonSignGraphic => summonSignGraphic;
        public Button SummonButton => summonButton;

        private void Awake()
        {
            Instance = this;
            summonContainer.SetActive(false);
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
