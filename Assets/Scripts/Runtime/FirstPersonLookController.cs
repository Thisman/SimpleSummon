using UnityEngine;
using UnityEngine.InputSystem;

namespace SimpleSummon.Runtime
{
    public sealed class FirstPersonLookController : MonoBehaviour
    {
        [SerializeField] private Transform playerRoot;
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private InputActionReference lookAction;
        [SerializeField, Min(0f)] private float sensitivity = 0.12f;
        [SerializeField] private float minimumPitch = -80f;
        [SerializeField] private float maximumPitch = 80f;

        private float pitch;

        private void Awake()
        {
            pitch = NormalizeAngle(cameraPivot.localEulerAngles.x);
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
            playerRoot.Rotate(0f, look.x * sensitivity, 0f);
            pitch = Mathf.Clamp(pitch - look.y * sensitivity, minimumPitch, maximumPitch);
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
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
