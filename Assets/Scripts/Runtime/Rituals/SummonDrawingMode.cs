using System.Collections.Generic;
using SimpleSummon.Network;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SimpleSummon.Runtime
{
    public sealed class SummonDrawingMode : MonoBehaviour
    {
        private const int MaximumBatchSize = 32;

        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerInteractionController interactionController;
        [SerializeField] private OrbitCameraController lookController;
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField, Min(0.001f)] private float minimumPointDistance = 0.005f;
        [SerializeField, Min(0.01f)] private float sendInterval = 0.05f;
        [SerializeField, Range(0.005f, 0.2f)] private float eraserRadius = 0.04f;

        private readonly List<NetworkSummonPoint> pendingPoints = new();
        private NetworkSummonRitual ritual;
        private InputActionMap drawingMap;
        private InputAction pointAction;
        private InputAction drawAction;
        private InputAction eraseAction;
        private InputAction exitAction;
        private GameObject summonContainer;
        private RectTransform signContainer;
        private SummonSignGraphic signGraphic;
        private Button summonButton;
        private Vector2 previousPoint;
        private float sendTime;
        private bool drawing;

        private void Awake()
        {
            drawingMap = inputActions != null
                ? inputActions.FindActionMap("Drawing", true).Clone()
                : CreateDrawingMap();
            pointAction = drawingMap.FindAction("Point", true);
            drawAction = drawingMap.FindAction("Draw", true);
            eraseAction = drawingMap.FindAction("Erase", true);
            exitAction = drawingMap.FindAction("Exit", true);
            drawingMap.Disable();
        }

        private void OnDisable()
        {
            if (ritual != null)
            {
                Exit(true);
            }
        }

        private void OnDestroy()
        {
            drawingMap?.Dispose();
        }

        private void Update()
        {
            if (ritual == null)
            {
                return;
            }

            if (exitAction.WasPressedThisFrame())
            {
                Exit(true);
                return;
            }

            if (summonButton.interactable &&
                drawAction.WasPressedThisFrame() &&
                IsPointerOverSummonButton())
            {
                Finish();
                return;
            }

            bool pressed = drawAction.IsPressed();
            if (pressed && TryReadPoint(out Vector2 point))
            {
                if (eraseAction.IsPressed())
                {
                    drawing = false;
                    FlushPoints();
                    sendTime += Time.unscaledDeltaTime;
                    if (sendTime >= sendInterval)
                    {
                        ritual.Erase(point, eraserRadius);
                        sendTime = 0f;
                    }
                    return;
                }

                if (!drawing)
                {
                    drawing = true;
                    AddPoint(point, true);
                }
                else if (Vector2.Distance(previousPoint, point) >= minimumPointDistance)
                {
                    AddPoint(point, false);
                }
            }
            else
            {
                drawing = false;
            }

            sendTime += Time.unscaledDeltaTime;
            if (sendTime >= sendInterval || pendingPoints.Count >= MaximumBatchSize)
            {
                FlushPoints();
            }

            summonButton.interactable =
                ritual.PointCount + pendingPoints.Count > 1;
        }

        public void Enter(NetworkSummonRitual targetRitual)
        {
            if (ritual != null || targetRitual == null)
            {
                return;
            }

            LocalPlayerHud hud = LocalPlayerHud.Instance;
            if (hud == null)
            {
                targetRitual.Release();
                return;
            }

            ritual = targetRitual;
            summonContainer = hud.SummonContainer;
            signContainer = hud.SignContainer;
            signGraphic = hud.SummonSignGraphic;
            summonButton = hud.SummonButton;

            signGraphic.SetRitual(ritual);
            summonButton.onClick.AddListener(Finish);
            summonButton.interactable = ritual.PointCount > 1;
            summonContainer.SetActive(true);
            hud.EnterModalMode();

            playerController.SetLocalInputEnabled(false);
            playerController.StopHorizontalMovement();
            interactionController.SetLocalInputEnabled(false);
            lookController.enabled = false;
            drawingMap.Enable();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Finish()
        {
            FlushPoints();
            ritual.Finish();
            Exit(false);
        }

        private void Exit(bool release)
        {
            FlushPoints();
            NetworkSummonRitual previousRitual = ritual;
            ritual = null;
            drawing = false;

            drawingMap.Disable();
            summonButton.onClick.RemoveListener(Finish);
            summonContainer.SetActive(false);
            LocalPlayerHud.Instance?.ExitModalMode();
            signGraphic.SetRitual(null);

            lookController.enabled = true;
            interactionController.SetLocalInputEnabled(true);
            playerController.SetLocalInputEnabled(true);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (release)
            {
                previousRitual.Release();
            }
        }

        private bool TryReadPoint(out Vector2 normalized)
        {
            Vector2 screenPoint = pointAction.ReadValue<Vector2>();
            Camera eventCamera = GetEventCamera();

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    signContainer,
                    screenPoint,
                    eventCamera,
                    out Vector2 localPoint))
            {
                normalized = default;
                return false;
            }

            Rect square = SummonSignGraphic.GetSquareRect(signContainer.rect);
            if (!square.Contains(localPoint))
            {
                normalized = default;
                return false;
            }

            normalized = new Vector2(
                Mathf.InverseLerp(square.xMin, square.xMax, localPoint.x),
                Mathf.InverseLerp(square.yMin, square.yMax, localPoint.y));
            return true;
        }

        private bool IsPointerOverSummonButton()
        {
            RectTransform buttonTransform = (RectTransform)summonButton.transform;
            return RectTransformUtility.RectangleContainsScreenPoint(
                buttonTransform,
                pointAction.ReadValue<Vector2>(),
                GetEventCamera());
        }

        private Camera GetEventCamera()
        {
            Canvas canvas = signContainer.GetComponentInParent<Canvas>();
            return canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;
        }

        private void AddPoint(Vector2 point, bool startsStroke)
        {
            previousPoint = point;
            pendingPoints.Add(new NetworkSummonPoint(point, startsStroke));
            if (pendingPoints.Count >= MaximumBatchSize)
            {
                FlushPoints();
            }
        }

        private void FlushPoints()
        {
            if (ritual == null || pendingPoints.Count == 0)
            {
                return;
            }

            ritual.SubmitPoints(pendingPoints.ToArray());
            pendingPoints.Clear();
            sendTime = 0f;
        }

        private static InputActionMap CreateDrawingMap()
        {
            InputActionMap map = new InputActionMap("Drawing");
            InputAction point = map.AddAction(
                "Point",
                InputActionType.PassThrough,
                "<Mouse>/position");
            point.expectedControlType = "Vector2";
            InputAction draw = map.AddAction(
                "Draw",
                InputActionType.Button,
                "<Mouse>/leftButton");
            draw.expectedControlType = "Button";
            InputAction exit = map.AddAction(
                "Exit",
                InputActionType.Button,
                "<Keyboard>/escape");
            exit.expectedControlType = "Button";
            InputAction erase = map.AddAction(
                "Erase",
                InputActionType.Button,
                "<Keyboard>/shift");
            erase.expectedControlType = "Button";
            return map;
        }
    }
}
