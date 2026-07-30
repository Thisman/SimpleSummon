using SimpleSummon.Network;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SimpleSummon.Runtime
{
    public sealed class InstructionModeController : MonoBehaviour
    {
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerInteractionController interactionController;
        [SerializeField] private OrbitCameraController lookController;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private InputActionReference exitAction;
        [SerializeField] private GameObject exitHint;
        [SerializeField] private Renderer[] playerRenderers;

        private InstructionInteraction activeInstruction;
        private bool[] initialForceRenderingOff;
        private Vector3 gameplayCameraLocalPosition;
        private Quaternion gameplayCameraLocalRotation;
        private Vector3 gameplayCameraLocalScale;
        private NetworkPlayer networkPlayer;
        private InputAction exitInput;
        private bool isLocalPlayer;

        private void Awake()
        {
            networkPlayer = GetComponent<NetworkPlayer>();
            exitInput = exitAction.action.Clone();
            initialForceRenderingOff = new bool[playerRenderers.Length];
            for (int i = 0; i < playerRenderers.Length; i++)
            {
                initialForceRenderingOff[i] = playerRenderers[i].forceRenderingOff;
            }

            gameplayCameraLocalPosition = cameraTransform.localPosition;
            gameplayCameraLocalRotation = cameraTransform.localRotation;
            gameplayCameraLocalScale = cameraTransform.localScale;

            exitInput.Disable();
            SetExitHintActive(false);
        }

        private void OnEnable()
        {
            if (networkPlayer != null)
            {
                networkPlayer.RoleChanged += RefreshLocalRole;
            }

            RefreshLocalRole();
        }

        private void OnDisable()
        {
            if (networkPlayer != null)
            {
                networkPlayer.RoleChanged -= RefreshLocalRole;
            }

            if (activeInstruction != null)
            {
                Exit();
            }
        }

        private void OnDestroy()
        {
            exitInput?.Dispose();
        }

        private void Update()
        {
            if (isLocalPlayer &&
                activeInstruction != null &&
                exitInput.WasPressedThisFrame())
            {
                Exit();
            }
        }

        public void Enter(InstructionInteraction instruction)
        {
            if (!isLocalPlayer || activeInstruction != null)
            {
                return;
            }

            activeInstruction = instruction;
            playerController.SetLocalInputEnabled(false);
            playerController.StopHorizontalMovement();
            interactionController.enabled = false;
            lookController.enabled = false;

            cameraTransform.SetPositionAndRotation(
                instruction.CameraPosition,
                instruction.CameraRotation);
            cameraTransform.localScale = instruction.CameraScale;

            instruction.InstructionText.SetActive(true);
            SetExitHintActive(true);
            SetPlayerRenderingOff(true);
            InteractiveMarker.SetMarkersVisible(false);
            exitInput.Enable();
        }

        private void Exit()
        {
            exitInput.Disable();
            activeInstruction.InstructionText.SetActive(false);
            SetExitHintActive(false);
            SetPlayerRenderingOff(false);
            InteractiveMarker.SetMarkersVisible(true);
            activeInstruction = null;

            cameraTransform.localPosition = gameplayCameraLocalPosition;
            cameraTransform.localRotation = gameplayCameraLocalRotation;
            cameraTransform.localScale = gameplayCameraLocalScale;

            lookController.enabled = true;
            interactionController.enabled = true;
            playerController.SetLocalInputEnabled(true);
        }

        private void RefreshLocalRole()
        {
            isLocalPlayer = networkPlayer == null || networkPlayer.CanReadLocalInput;
        }

        private void SetPlayerRenderingOff(bool renderingOff)
        {
            for (int i = 0; i < playerRenderers.Length; i++)
            {
                playerRenderers[i].forceRenderingOff = renderingOff || initialForceRenderingOff[i];
            }
        }

        private void SetExitHintActive(bool active)
        {
            if (exitHint == null && LocalPlayerHud.Instance != null)
            {
                exitHint = LocalPlayerHud.Instance.InstructionExitHint;
            }

            if (exitHint != null)
            {
                exitHint.SetActive(active);
            }
        }
    }
}
