using System;
using UnityEngine;

namespace SimpleSummon.Runtime
{
    [DisallowMultipleComponent]
    public sealed class InteractiveMarker : MonoBehaviour
    {
        private static event Action VisibilityChanged;
        private static bool markersVisible = true;

        [Header("References")]
        [SerializeField] private Transform markerVisual;
        [SerializeField] private Canvas markerCanvas;
        [SerializeField] private Camera targetCamera;

        [Header("Floating")]
        [SerializeField, Min(0f)] private float movementDistance = 0.15f;
        [SerializeField, Min(0f)] private float movementSpeed = 2f;

        private Vector3 startLocalPosition;
        private bool canvasInitiallyEnabled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            markersVisible = true;
            VisibilityChanged = null;
        }

        private void Awake()
        {
            startLocalPosition = markerVisual.localPosition;
            canvasInitiallyEnabled = markerCanvas.enabled;
        }

        private void OnEnable()
        {
            VisibilityChanged += ApplyVisibility;
            ApplyVisibility();
        }

        private void OnDisable()
        {
            VisibilityChanged -= ApplyVisibility;
        }

        private void LateUpdate()
        {
            Camera cameraToUse = targetCamera != null ? targetCamera : Camera.main;
            if (cameraToUse != null)
            {
                markerVisual.rotation = cameraToUse.transform.rotation;
            }

            float offset = Mathf.Sin(Time.time * movementSpeed) * movementDistance;
            markerVisual.localPosition = startLocalPosition + Vector3.up * offset;
        }

        public static void SetMarkersVisible(bool visible)
        {
            markersVisible = visible;
            VisibilityChanged?.Invoke();
        }

        private void ApplyVisibility()
        {
            markerCanvas.enabled = canvasInitiallyEnabled && markersVisible;
        }
    }
}
