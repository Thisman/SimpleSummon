using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class LocalPlayerHudController : MonoBehaviour
    {
        [SerializeField] private LocalPlayerHudView view;

        private int modalModeCount;

        public static LocalPlayerHudController Instance { get; private set; }

        public InteractionPromptView InteractionPrompt => view.InteractionPrompt;
        public GameObject InstructionContainer => view.InstructionContainer;
        public GameObject SummonContainer => view.SummonContainer;
        public RectTransform SignContainer => view.SignContainer;
        public SignDrawingGraphic SignDrawingGraphic => view.SignDrawingGraphic;
        public UnityEngine.UI.Button SummonButton => view.SummonButton;
        public GameObject SignBuilderContainer => view.SignBuilderContainer;
        public SignBuilderView SignBuilderView => view.SignBuilderView;

        private void Awake()
        {
            Instance = this;
            view.Initialize();
        }

        public void EnterModalMode()
        {
            modalModeCount++;
            view.SetQuestProgressVisible(false);
        }

        public void ExitModalMode()
        {
            modalModeCount = Mathf.Max(0, modalModeCount - 1);
            view.SetQuestProgressVisible(modalModeCount == 0);
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
