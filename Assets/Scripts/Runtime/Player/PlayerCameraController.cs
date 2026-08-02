using UnityEngine;
using UnityEngine.InputSystem;

namespace SimpleSummon.Runtime
{
    public sealed class PlayerCameraController : MonoBehaviour
    {
        private const int ObstructionBufferSize = 16;

        [SerializeField] private Transform playerRoot;
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private InputActionReference lookAction;
        [SerializeField, Min(0f)] private float sensitivity = 0.12f;
        [SerializeField] private float minimumPitch = -35f;
        [SerializeField] private float maximumPitch = 65f;
        [SerializeField, Min(0f)] private float distance = 5f;
        [SerializeField] private Vector3 shoulderOffset = new Vector3(0.75f, 0.25f, 0f);
        [SerializeField] private LayerMask obstructionLayers = ~0;
        [SerializeField, Min(0f)] private float collisionRadius = 0.3f;
        [SerializeField, Min(0f)] private float collisionClearance = 0.05f;
        [SerializeField, Min(0f)] private float damageShakeDuration = 0.2f;
        [SerializeField, Min(0f)] private float damageShakeStrength = 0.08f;

        private float yaw;
        private float pitch;
        private float focusHeight;
        private float damageShakeTime;
        private InputAction lookInput;
        private readonly RaycastHit[] obstructionHits =
            new RaycastHit[ObstructionBufferSize];

        private void Awake()
        {
            lookInput = lookAction.action.Clone();
            yaw = playerRoot.eulerAngles.y;
            focusHeight = cameraPivot.position.y - playerRoot.position.y;
            pitch = Mathf.Clamp(
                NormalizeAngle(cameraPivot.eulerAngles.x),
                minimumPitch,
                maximumPitch);
        }

        private void OnEnable()
        {
            lookInput.Enable();
            LockCursor();
        }

        private void OnDisable()
        {
            lookInput.Disable();
        }

        private void OnDestroy()
        {
            lookInput?.Dispose();
        }

        private void Update()
        {
            Vector2 look = lookInput.ReadValue<Vector2>();
            yaw += look.x * sensitivity;
            pitch = Mathf.Clamp(pitch - look.y * sensitivity, minimumPitch, maximumPitch);
        }

        private void LateUpdate()
        {
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 focusPoint = playerRoot.position + Vector3.up * focusHeight;
            Vector3 idealPosition = focusPoint + rotation * (
                shoulderOffset + Vector3.back * distance);
            Vector3 cameraDirection = idealPosition - focusPoint;
            float idealDistance = cameraDirection.magnitude;

            if (idealDistance > 0f)
            {
                int hitCount = Physics.SphereCastNonAlloc(
                    focusPoint,
                    collisionRadius,
                    cameraDirection / idealDistance,
                    obstructionHits,
                    idealDistance,
                    obstructionLayers,
                    QueryTriggerInteraction.Ignore);
                float closestDistance = idealDistance;
                bool hasObstruction = false;

                for (int i = 0; i < hitCount; i++)
                {
                    RaycastHit hit = obstructionHits[i];
                    if (!hit.transform.IsChildOf(playerRoot))
                    {
                        closestDistance = Mathf.Min(closestDistance, hit.distance);
                        hasObstruction = true;
                    }
                }

                if (hasObstruction)
                {
                    float safeDistance = Mathf.Max(0f, closestDistance - collisionClearance);
                    idealPosition = focusPoint + cameraDirection.normalized * safeDistance;
                }
            }

            if (damageShakeTime > 0f)
            {
                damageShakeTime = Mathf.Max(0f, damageShakeTime - Time.unscaledDeltaTime);
                float strength = damageShakeStrength * damageShakeTime / damageShakeDuration;
                idealPosition += Random.insideUnitSphere * strength;
            }

            transform.SetPositionAndRotation(idealPosition, rotation);
        }

        public void PlayDamageShake()
        {
            damageShakeTime = damageShakeDuration;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                LockCursor();
            }
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private static float NormalizeAngle(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }
    }
}
