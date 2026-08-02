using System;
using SimpleSummon.Domain;

namespace SimpleSummon.Application
{
    public sealed class SignPuzzleService
    {
        private readonly IRandomSource random;

        public SignPuzzleService(IRandomSource random)
        {
            this.random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public bool Move(
            byte[] board,
            int sourceSlot,
            SignPuzzleMoveDirection direction) =>
            SignPuzzleState.TryMove(board, sourceSlot, direction);

        public bool AddFragments(byte[] board, byte fragmentMask)
        {
            bool changed = false;
            for (byte id = 0; id < SignPuzzleState.FragmentCount; id++)
            {
                if ((fragmentMask & 1 << id) != 0)
                {
                    changed |= SignPuzzleState.TryAddFragment(
                        board,
                        id,
                        random.Next());
                }
            }

            return changed;
        }

        public bool IsCompleted(byte[] board) => SignPuzzleState.IsCompleted(board);
    }
}
