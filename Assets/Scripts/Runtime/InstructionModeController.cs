using UnityEngine;
using UnityEngine.InputSystem;

namespace SimpleSummon.Runtime
{
    public sealed class InstructionModeController : MonoBehaviour
    {
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerInteractionController interactionController;
        [SerializeField] private FirstPersonLookController lookController;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private InputActionReference playerAction;
        [SerializeField] private InputActionReference exitAction;
        [SerializeField] private GameObject exitHint;
        [SerializeField] private Renderer[] playerRenderers;

        private InstructionInteraction activeInstruction;
        private bool[] initialForceRenderingOff;
        private Vector3 gameplayCameraLocalPosition;
        private Quaternion gameplayCameraLocalRotation;
        private Vector3 gameplayCameraLocalScale;

        private void Awake()
        {
            initialForceRenderingOff = new bool[playerRenderers.Length];
            for (int i = 0; i < playerRenderers.Length; i++)
            {
                initialForceRenderingOff[i] = playerRenderers[i].forceRenderingOff;
            }

            gameplayCameraLocalPosition = cameraTransform.localPosition;
            gameplayCameraLocalRotation = cameraTransform.localRotation;
            gameplayCameraLocalScale = cameraTransform.localScale;

            exitAction.action.actionMap.Disable();
            exitHint.SetActive(false);
        }

        private void OnDisable()
        {
            if (activeInstruction != null)
            {
                Exit();
            }
        }

        private void Update()
        {
            if (activeInstruction != null && exitAction.action.WasPressedThisFrame())
            {
                Exit();
            }
        }

        public void Enter(InstructionInteraction instruction)
        {
            if (activeInstruction != null)
            {
                return;
            }

            activeInstruction = instruction;
            playerAction.action.actionMap.Disable();
            playerController.StopHorizontalMovement();
            interactionController.enabled = false;
            lookController.enabled = false;

            cameraTransform.SetPositionAndRotation(
                instruction.CameraPosition,
                instruction.CameraRotation);
            cameraTransform.localScale = instruction.CameraScale;

            instruction.InstructionText.SetActive(true);
            exitHint.SetActive(true);
            SetPlayerRenderingOff(true);
            exitAction.action.actionMap.Enable();
        }

        private void Exit()
        {
            exitAction.action.actionMap.Disable();
            activeInstruction.InstructionText.SetActive(false);
            exitHint.SetActive(false);
            SetPlayerRenderingOff(false);
            activeInstruction = null;

            cameraTransform.localPosition = gameplayCameraLocalPosition;
            cameraTransform.localRotation = gameplayCameraLocalRotation;
            cameraTransform.localScale = gameplayCameraLocalScale;

            lookController.enabled = true;
            interactionController.enabled = true;
            playerAction.action.actionMap.Enable();
        }

        private void SetPlayerRenderingOff(bool renderingOff)
        {
            for (int i = 0; i < playerRenderers.Length; i++)
            {
                playerRenderers[i].forceRenderingOff = renderingOff || initialForceRenderingOff[i];
            }
        }
    }
}
