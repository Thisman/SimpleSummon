using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class LocalPlayerHud : MonoBehaviour
    {
        [SerializeField] private InteractionPromptView interactionPrompt;
        [SerializeField] private GameObject instructionExitHint;

        public static LocalPlayerHud Instance { get; private set; }

        public InteractionPromptView InteractionPrompt => interactionPrompt;
        public GameObject InstructionExitHint => instructionExitHint;

        private void Awake()
        {
            Instance = this;
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
