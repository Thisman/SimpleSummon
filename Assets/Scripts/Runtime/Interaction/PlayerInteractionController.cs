using SimpleSummon.Network;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SimpleSummon.Runtime
{
    public sealed class PlayerInteractionController : MonoBehaviour
    {
        [SerializeField] private Camera interactionCamera;
        [SerializeField] private InteractionPromptView promptView;
        [SerializeField] private InputActionReference interactAction;
        [SerializeField, Min(0f)] private float interactionDistance = 3f;
        [SerializeField, Min(0.01f)] private float holdDuration = 1f;
        [SerializeField] private LayerMask interactionLayers = Physics.DefaultRaycastLayers;

        private InteractiveActor currentActor;
        private float holdTime;
        private bool interactionTriggered;
        private InteractionTargetScanner targetScanner;
        private NetworkPlayer networkPlayer;
        private bool inputEnabled;
        private InputAction interactInput;

        private void Awake()
        {
            networkPlayer = GetComponent<NetworkPlayer>();
            interactInput = interactAction.action.Clone();
            targetScanner = new InteractionTargetScanner(
                interactionCamera,
                transform,
                interactionDistance,
                interactionLayers);
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

            SetInputEnabled(false);
            ClearTarget();
        }

        private void OnDestroy()
        {
            interactInput?.Dispose();
        }

        private void Update()
        {
            if (networkPlayer != null && !networkPlayer.CanReadLocalInput)
            {
                return;
            }

            InteractiveActor actor = targetScanner.Find();

            if (actor != currentActor)
            {
                SetTarget(actor);
            }

            if (currentActor == null || interactionTriggered)
            {
                return;
            }

            if (!interactInput.IsPressed())
            {
                ResetProgress();
                return;
            }

            holdTime += Time.deltaTime;
            ResolvePromptView()?.SetProgress(holdTime / holdDuration);

            if (holdTime < holdDuration)
            {
                return;
            }

            interactionTriggered = true;
            if (currentActor.IsLocalPresentation ||
                networkPlayer == null ||
                !networkPlayer.IsSpawned)
            {
                currentActor.Interact(gameObject);
            }
            else
            {
                NetworkObject target = currentActor.GetComponentInParent<NetworkObject>();
                if (target != null)
                {
                    networkPlayer.RequestInteraction(target, interactionDistance);
                }
            }
            ClearTarget();
        }

        private void SetTarget(InteractiveActor actor)
        {
            currentActor = actor;
            ResetProgress();

            if (currentActor == null)
            {
                ResolvePromptView()?.Hide();
                return;
            }

            ResolvePromptView()?.Show(GameLocalization.TranslateRaw(currentActor.InteractionText));
        }

        private void ResetProgress()
        {
            holdTime = 0f;
            interactionTriggered = false;
            ResolvePromptView()?.SetProgress(0f);
        }

        private void ClearTarget()
        {
            currentActor = null;
            holdTime = 0f;
            interactionTriggered = false;

            if (promptView != null)
            {
                promptView.Hide();
            }
        }

        private void RefreshLocalRole()
        {
            SetInputEnabled(networkPlayer == null || networkPlayer.CanReadLocalInput);
        }

        private void SetInputEnabled(bool value)
        {
            if (inputEnabled == value)
            {
                return;
            }

            inputEnabled = value;
            if (value)
            {
                interactInput.Enable();
            }
            else
            {
                interactInput.Disable();
            }
        }

        public void SetLocalInputEnabled(bool enabled)
        {
            if (networkPlayer == null || networkPlayer.CanReadLocalInput)
            {
                SetInputEnabled(enabled);
                if (!enabled)
                {
                    ClearTarget();
                }
            }
        }

        private InteractionPromptView ResolvePromptView()
        {
            if (promptView == null && LocalPlayerHud.Instance != null)
            {
                promptView = LocalPlayerHud.Instance.InteractionPrompt;
            }

            return promptView;
        }
    }
}
