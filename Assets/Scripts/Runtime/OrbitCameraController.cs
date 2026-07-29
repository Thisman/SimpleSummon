using UnityEngine;
using UnityEngine.InputSystem;

namespace SimpleSummon.Runtime
{
    public sealed class OrbitCameraController : MonoBehaviour
    {
        private const int ObstructionBufferSize = 16;

        [SerializeField] private Transform target;
        [SerializeField] private InputActionReference lookAction;
        [SerializeField, Min(0f)] private float distance = 5f;
        [SerializeField] private float targetHeight = 1.2f;
        [SerializeField, Min(0f)] private float sensitivity = 0.12f;
        [SerializeField] private float minimumPitch = -20f;
        [SerializeField] private float maximumPitch = 65f;
        [SerializeField] private LayerMask obstructionLayers = Physics.DefaultRaycastLayers;
        [SerializeField, Min(0f)] private float collisionRadius = 0.3f;
        [SerializeField, Min(0f)] private float collisionClearance = 0.05f;

        private float yaw;
        private float pitch = 20f;
        private bool skipEscapeFrame;
        private readonly RaycastHit[] obstructionHits = new RaycastHit[ObstructionBufferSize];

        private void Awake()
        {
            Vector3 angles = transform.eulerAngles;
            yaw = angles.y;
            pitch = NormalizeAngle(angles.x);
        }

        private void OnEnable()
        {
            lookAction.action.Enable();
            skipEscapeFrame = true;
            LockCursor();
        }

        private void OnDisable()
        {
            lookAction.action.Disable();
        }

        private void Update()
        {
            if (skipEscapeFrame)
            {
                skipEscapeFrame = false;
            }
            else if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (Mouse.current != null &&
                     Mouse.current.leftButton.wasPressedThisFrame &&
                     Cursor.lockState != CursorLockMode.Locked)
            {
                LockCursor();
            }

            if (Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            Vector2 look = lookAction.action.ReadValue<Vector2>();
            yaw += look.x * sensitivity;
            pitch = Mathf.Clamp(pitch - look.y * sensitivity, minimumPitch, maximumPitch);
        }

        private void LateUpdate()
        {
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 focusPoint = target.position + Vector3.up * targetHeight;
            Vector3 cameraDirection = -(rotation * Vector3.forward);
            float cameraDistance = GetCameraDistance(focusPoint, cameraDirection);

            transform.SetPositionAndRotation(
                focusPoint + cameraDirection * cameraDistance,
                rotation);
        }

        private float GetCameraDistance(Vector3 focusPoint, Vector3 cameraDirection)
        {
            int hitCount = Physics.SphereCastNonAlloc(
                focusPoint,
                collisionRadius,
                cameraDirection,
                obstructionHits,
                distance,
                obstructionLayers,
                QueryTriggerInteraction.Ignore);

            float nearestDistance = distance;
            bool obstructionFound = false;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = obstructionHits[i];
                if (hit.collider == null || hit.collider.transform.IsChildOf(target))
                {
                    continue;
                }

                nearestDistance = Mathf.Min(nearestDistance, hit.distance);
                obstructionFound = true;
            }

            return obstructionFound
                ? Mathf.Max(0f, nearestDistance - collisionClearance)
                : distance;
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
