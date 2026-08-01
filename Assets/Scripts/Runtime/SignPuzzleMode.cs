using SimpleSummon.Domain;
using SimpleSummon.Network;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SimpleSummon.Runtime
{
    public sealed class SignPuzzleMode : MonoBehaviour
    {
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerInteractionController interactionController;
        [SerializeField] private OrbitCameraController lookController;
        [SerializeField] private InputActionAsset inputActions;
        private readonly byte[] board = new byte[SignPuzzleState.SlotCount];
        private NetworkSignPuzzle puzzle;
        private InputActionMap map;
        private InputAction point;
        private InputAction click;
        private InputAction exit;
        private InputAction left;
        private InputAction right;
        private InputAction up;
        private InputAction down;
        private GameObject container;
        private SignPuzzleView view;
        private byte selectedFragment = SignPuzzleState.Empty;

        private void Awake()
        {
            map = inputActions != null && inputActions.FindActionMap("SignPuzzle") != null
                ? inputActions.FindActionMap("SignPuzzle", true).Clone() : CreateMap();
            point = map.FindAction("Point", true); click = map.FindAction("Click", true);
            exit = map.FindAction("Exit", true); left = map.FindAction("Left", true);
            right = map.FindAction("Right", true); up = map.FindAction("Up", true); down = map.FindAction("Down", true);
            map.Disable();
        }

        private void OnDisable() { if (puzzle != null) Exit(true); }
        private void OnDestroy() => map?.Dispose();

        private void Update()
        {
            if (puzzle == null) return;
            if (exit.WasPressedThisFrame()) { Exit(true); return; }
            if (click.WasPressedThisFrame() && view.TryGetSlot(point.ReadValue<Vector2>(), out int slot))
            {
                byte fragment = puzzle.GetSlot(slot);
                if (fragment < SignPuzzleState.FragmentCount)
                {
                    selectedFragment = fragment;
                    view.SetSelectedFragment(selectedFragment);
                }
            }
            TryKeyboardMove(left, SignPuzzleMoveDirection.Left);
            TryKeyboardMove(right, SignPuzzleMoveDirection.Right);
            TryKeyboardMove(up, SignPuzzleMoveDirection.Up);
            TryKeyboardMove(down, SignPuzzleMoveDirection.Down);
        }

        public void Enter(NetworkSignPuzzle target)
        {
            if (puzzle != null || target == null) return;
            LocalPlayerHud hud = LocalPlayerHud.Instance;
            if (hud == null) { target.RequestRelease(); return; }
            puzzle = target; container = hud.SignPuzzleContainer; view = hud.SignPuzzleView;
            selectedFragment = FindFirstFragment();
            container.SetActive(true); view.SetPuzzle(puzzle); view.SetSelectedFragment(selectedFragment);
            playerController.SetLocalInputEnabled(false); playerController.StopHorizontalMovement();
            interactionController.SetLocalInputEnabled(false); lookController.enabled = false;
            map.Enable(); Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }

        private void Exit(bool release)
        {
            NetworkSignPuzzle previous = puzzle; puzzle = null; selectedFragment = SignPuzzleState.Empty; map.Disable();
            view.SetPuzzle(null); container.SetActive(false);
            lookController.enabled = true; interactionController.SetLocalInputEnabled(true); playerController.SetLocalInputEnabled(true);
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
            if (release) previous.RequestRelease();
        }

        private void TryKeyboardMove(InputAction action, SignPuzzleMoveDirection direction)
        {
            if (!action.WasPressedThisFrame()) return;
            puzzle.CopySlots(board);
            int source = FindSelectedSlot();
            if (SignPuzzleState.CanMove(board, source, direction)) puzzle.RequestMove(source, direction);
        }

        private byte FindFirstFragment()
        {
            puzzle.CopySlots(board);
            for (int i = 0; i < board.Length; i++)
                if (board[i] < SignPuzzleState.FragmentCount) return board[i];
            return SignPuzzleState.Empty;
        }

        private int FindSelectedSlot()
        {
            for (int i = 0; i < board.Length; i++) if (board[i] == selectedFragment) return i;
            return -1;
        }

        private static InputActionMap CreateMap()
        {
            InputActionMap result = new("SignPuzzle");
            result.AddAction("Point", InputActionType.PassThrough, "<Mouse>/position");
            result.AddAction("Click", InputActionType.Button, "<Mouse>/leftButton");
            result.AddAction("Exit", InputActionType.Button, "<Keyboard>/escape");
            result.AddAction("Left", InputActionType.Button, "<Keyboard>/leftArrow");
            result.AddAction("Right", InputActionType.Button, "<Keyboard>/rightArrow");
            result.AddAction("Up", InputActionType.Button, "<Keyboard>/upArrow");
            result.AddAction("Down", InputActionType.Button, "<Keyboard>/downArrow");
            return result;
        }
    }
}
