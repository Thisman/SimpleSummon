using UnityEngine.InputSystem;

namespace SimpleSummon.Runtime
{
    internal sealed class PlayerInputReader
    {
        private readonly InputAction move;
        private readonly InputAction jump;
        private readonly InputAction attack;
        private bool enabled;

        public PlayerInputReader(
            InputActionReference move,
            InputActionReference jump,
            InputActionReference attack)
        {
            this.move = move.action.Clone();
            this.jump = jump.action.Clone();
            this.attack = attack.action.Clone();
        }

        public PlayerInputFrame Read() => new(
            move.ReadValue<UnityEngine.Vector2>(),
            jump.WasPressedThisFrame(),
            attack.WasPressedThisFrame());

        public void SetEnabled(bool value)
        {
            if (enabled == value)
            {
                return;
            }

            enabled = value;
            if (enabled)
            {
                move.Enable();
                jump.Enable();
                attack.Enable();
            }
            else
            {
                move.Disable();
                jump.Disable();
                attack.Disable();
            }
        }

        public void Dispose()
        {
            move.Dispose();
            jump.Dispose();
            attack.Dispose();
        }
    }
}
