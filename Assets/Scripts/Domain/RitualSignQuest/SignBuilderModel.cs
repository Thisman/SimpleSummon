using System;

namespace SimpleSummon.Domain
{
    public sealed class SignBuilderModel
    {
        private readonly byte[] slots = new byte[SignBuilderState.SlotCount];

        public SignBuilderModel()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = SignBuilderState.Empty;
            }
        }

        public bool Completed { get; private set; }
        public ReadOnlySpan<byte> Slots => slots;

        public void CopyTo(byte[] destination)
        {
            if (destination == null || destination.Length != slots.Length)
            {
                throw new ArgumentException(
                    "Destination must contain nine slots.",
                    nameof(destination));
            }
            Array.Copy(slots, destination, slots.Length);
        }

        public void Apply(byte[] board)
        {
            if (board == null || board.Length != slots.Length)
            {
                throw new ArgumentException(
                    "Board must contain nine slots.",
                    nameof(board));
            }
            Array.Copy(board, slots, slots.Length);
            Completed = SignBuilderState.IsCompleted(slots);
        }
    }
}
