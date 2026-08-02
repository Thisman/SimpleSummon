using UnityEngine;
using UnityEngine.InputSystem;

namespace SimpleSummon.Runtime
{
    internal sealed class SignDrawingInput
    {
        private readonly InputActionMap map;
        private readonly InputAction point;
        private readonly InputAction draw;
        private readonly InputAction erase;
        private readonly InputAction exit;

        public SignDrawingInput(InputActionAsset inputActions)
        {
            map = inputActions != null
                ? inputActions.FindActionMap("Drawing", true).Clone()
                : CreateFallbackMap();
            point = map.FindAction("Point", true);
            draw = map.FindAction("Draw", true);
            erase = map.FindAction("Erase", true);
            exit = map.FindAction("Exit", true);
            map.Disable();
        }

        public Vector2 PointerPosition => point.ReadValue<Vector2>();
        public bool DrawPressed => draw.IsPressed();
        public bool DrawStarted => draw.WasPressedThisFrame();
        public bool ErasePressed => erase.IsPressed();
        public bool ExitStarted => exit.WasPressedThisFrame();

        public void Enable() => map.Enable();
        public void Disable() => map.Disable();
        public void Dispose() => map.Dispose();

        private static InputActionMap CreateFallbackMap()
        {
            InputActionMap result = new("Drawing");
            InputAction pointAction = result.AddAction(
                "Point",
                InputActionType.PassThrough,
                "<Mouse>/position");
            pointAction.expectedControlType = "Vector2";
            result.AddAction("Draw", InputActionType.Button, "<Mouse>/leftButton")
                .expectedControlType = "Button";
            result.AddAction("Exit", InputActionType.Button, "<Keyboard>/escape")
                .expectedControlType = "Button";
            result.AddAction("Erase", InputActionType.Button, "<Keyboard>/shift")
                .expectedControlType = "Button";
            return result;
        }
    }
}
