using UnityEngine;
using UnityEngine.InputSystem;

namespace SimpleSummon.Runtime
{
    public sealed class OrbitCameraController : MonoBehaviour
    {
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

        private float yaw;
        private float pitch;
        private float focusHeight;

        private void Awake()
        {
            yaw = playerRoot.eulerAngles.y;
            focusHeight = cameraPivot.position.y - playerRoot.position.y;
            pitch = Mathf.Clamp(
                NormalizeAngle(cameraPivot.eulerAngles.x),
                minimumPitch,
                maximumPitch);
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
            Vector2 look = lookAction.action.ReadValue<Vector2>();
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
                RaycastHit[] hits = Physics.SphereCastAll(
                    focusPoint,
                    collisionRadius,
                    cameraDirection / idealDistance,
                    idealDistance,
                    obstructionLayers,
                    QueryTriggerInteraction.Ignore);
                float closestDistance = idealDistance;
                bool hasObstruction = false;

                foreach (RaycastHit hit in hits)
                {
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

            transform.SetPositionAndRotation(idealPosition, rotation);
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
