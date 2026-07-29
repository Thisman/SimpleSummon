using UnityEngine;
using UnityEngine.InputSystem;

namespace SimpleSummon.Runtime
{
    public sealed class OrbitCameraController : MonoBehaviour
    {
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

        private void Awake()
        {
            Vector3 angles = transform.eulerAngles;
            yaw = angles.y;
            pitch = NormalizeAngle(angles.x);
        }

        private void OnEnable()
        {
            lookAction.action.Enable();
            LockCursor();
        }

        private void OnDisable()
        {
            lookAction.action.Disable();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
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
            if (!Physics.SphereCast(
                    focusPoint,
                    collisionRadius,
                    cameraDirection,
                    out RaycastHit hit,
                    distance,
                    obstructionLayers,
                    QueryTriggerInteraction.Ignore))
            {
                return distance;
            }

            return Mathf.Max(0f, hit.distance - collisionClearance);
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
