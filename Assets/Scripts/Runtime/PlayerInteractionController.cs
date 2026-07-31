using SimpleSummon.Network;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SimpleSummon.Runtime
{
    public sealed class PlayerInteractionController : MonoBehaviour
    {
        private const int RaycastBufferSize = 16;

        [SerializeField] private Camera interactionCamera;
        [SerializeField] private InteractionPromptView promptView;
        [SerializeField] private InputActionReference interactAction;
        [SerializeField, Min(0f)] private float interactionDistance = 3f;
        [SerializeField, Min(0.01f)] private float holdDuration = 1f;
        [SerializeField] private LayerMask interactionLayers = Physics.DefaultRaycastLayers;

        private InteractiveActor currentActor;
        private float holdTime;
        private bool interactionTriggered;
        private readonly RaycastHit[] raycastHits = new RaycastHit[RaycastBufferSize];
        private NetworkPlayer networkPlayer;
        private bool inputEnabled;
        private InputAction interactInput;

        private void Awake()
        {
            networkPlayer = GetComponent<NetworkPlayer>();
            interactInput = interactAction.action.Clone();
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

            InteractiveActor actor = FindActor();

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

        private InteractiveActor FindActor()
        {
            Ray ray = interactionCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            float cameraOffset = Vector3.Distance(interactionCamera.transform.position, transform.position);
            float rayDistance = cameraOffset + interactionDistance;
            int hitCount = Physics.RaycastNonAlloc(
                ray,
                raycastHits,
                rayDistance,
                interactionLayers,
                QueryTriggerInteraction.Ignore);

            InteractiveActor nearestActor = null;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = raycastHits[i];
                InteractiveActor actor = hit.collider.GetComponentInParent<InteractiveActor>();
                bool isInRange = Vector3.Distance(transform.position, hit.point) <= interactionDistance;

                if (actor == null ||
                    !actor.CompareTag("Interactive") ||
                    !isInRange ||
                    hit.distance >= nearestDistance)
                {
                    continue;
                }

                nearestActor = actor;
                nearestDistance = hit.distance;
            }

            return nearestActor;
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
