using SimpleSummon.Network;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SimpleSummon.Runtime
{
    public sealed class SummonDrawingMode : MonoBehaviour
    {
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerInteractionController interactionController;
        [SerializeField] private OrbitCameraController lookController;
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField, Min(0.001f)] private float minimumPointDistance = 0.005f;
        [SerializeField, Min(0.01f)] private float sendInterval = 0.05f;
        [SerializeField, Range(0.005f, 0.2f)] private float eraserRadius = 0.04f;

        private NetworkSummonRitual ritual;
        private SummonDrawingInput drawingInput;
        private SummonStrokeBuffer strokeBuffer;
        private SummonCanvasCoordinates canvasCoordinates;
        private GameObject summonContainer;
        private RectTransform signContainer;
        private SummonSignGraphic signGraphic;
        private Button summonButton;
        private float sendTime;
        private bool drawing;

        private void Awake()
        {
            drawingInput = new SummonDrawingInput(inputActions);
            strokeBuffer = new SummonStrokeBuffer(minimumPointDistance);
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
            drawingInput.Dispose();
        }

        private void Update()
        {
            if (ritual == null)
            {
                return;
            }

            if (drawingInput.ExitStarted)
            {
                Exit(true);
                return;
            }

            if (summonButton.interactable &&
                drawingInput.DrawStarted &&
                IsPointerOverSummonButton())
            {
                Finish();
                return;
            }

            bool pressed = drawingInput.DrawPressed;
            if (pressed && TryReadPoint(out Vector2 point))
            {
                if (drawingInput.ErasePressed)
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
                else
                {
                    AddPoint(point, false);
                }
            }
            else
            {
                drawing = false;
            }

            sendTime += Time.unscaledDeltaTime;
            if (sendTime >= sendInterval ||
                strokeBuffer.Count >= SummonStrokeBuffer.MaximumBatchSize)
            {
                FlushPoints();
            }

            summonButton.interactable =
                ritual.PointCount + strokeBuffer.Count > 1;
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
            canvasCoordinates = new SummonCanvasCoordinates(signContainer);

            signGraphic.SetRitual(ritual);
            summonButton.onClick.AddListener(Finish);
            summonButton.interactable = ritual.PointCount > 1;
            summonContainer.SetActive(true);
            hud.EnterModalMode();

            playerController.SetLocalInputEnabled(false);
            playerController.StopHorizontalMovement();
            interactionController.SetLocalInputEnabled(false);
            lookController.enabled = false;
            drawingInput.Enable();
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

            drawingInput.Disable();
            if (summonButton != null)
            {
                summonButton.onClick.RemoveListener(Finish);
            }
            if (summonContainer != null)
            {
                summonContainer.SetActive(false);
            }
            LocalPlayerHud.Instance?.ExitModalMode();
            if (signGraphic != null)
            {
                signGraphic.SetRitual(null);
            }

            if (lookController != null)
            {
                lookController.enabled = true;
            }
            interactionController?.SetLocalInputEnabled(true);
            playerController?.SetLocalInputEnabled(true);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (release && previousRitual != null)
            {
                previousRitual.Release();
            }
        }

        private bool TryReadPoint(out Vector2 normalized)
        {
            return canvasCoordinates.TryNormalize(
                drawingInput.PointerPosition,
                out normalized);
        }

        private bool IsPointerOverSummonButton()
        {
            RectTransform buttonTransform = (RectTransform)summonButton.transform;
            return canvasCoordinates.Contains(
                buttonTransform,
                drawingInput.PointerPosition);
        }

        private void AddPoint(Vector2 point, bool startsStroke)
        {
            if (strokeBuffer.TryAdd(point, startsStroke) &&
                strokeBuffer.Count >= SummonStrokeBuffer.MaximumBatchSize)
            {
                FlushPoints();
            }
        }

        private void FlushPoints()
        {
            if (ritual == null || strokeBuffer.Count == 0)
            {
                return;
            }

            ritual.SubmitPoints(strokeBuffer.Take());
            sendTime = 0f;
        }
    }
}
