using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class LocalPlayerHudController : MonoBehaviour
    {
        [SerializeField] private LocalPlayerHudView view;

        public static LocalPlayerHudController Instance { get; private set; }

        public InteractionPromptView InteractionPrompt => view.InteractionPrompt;
        public GameObject SummonContainer => view.SummonContainer;
        public RectTransform SignContainer => view.SignContainer;
        public SignDrawingGraphic SignDrawingGraphic => view.SignDrawingGraphic;
        public UnityEngine.UI.Button SubmitRitualSignButton =>
            view.SubmitRitualSignButton;

        private void Awake()
        {
            Instance = this;
            view.Initialize();
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
