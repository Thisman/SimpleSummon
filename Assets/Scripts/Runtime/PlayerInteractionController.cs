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

        private void OnEnable()
        {
            interactAction.action.Enable();
        }

        private void OnDisable()
        {
            interactAction.action.Disable();
            ClearTarget();
        }

        private void Update()
        {
            InteractiveActor actor = FindActor();

            if (actor != currentActor)
            {
                SetTarget(actor);
            }

            if (currentActor == null || interactionTriggered)
            {
                return;
            }

            if (!interactAction.action.IsPressed())
            {
                ResetProgress();
                return;
            }

            holdTime += Time.deltaTime;
            promptView.SetProgress(holdTime / holdDuration);

            if (holdTime < holdDuration)
            {
                return;
            }

            interactionTriggered = true;
            currentActor.Interact(gameObject);
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
                promptView.Hide();
                return;
            }

            promptView.Show(currentActor.InteractionText);
        }

        private void ResetProgress()
        {
            holdTime = 0f;
            interactionTriggered = false;
            promptView.SetProgress(0f);
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
    }
}
