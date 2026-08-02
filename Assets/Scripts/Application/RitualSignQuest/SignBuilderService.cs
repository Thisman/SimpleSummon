using System;
using SimpleSummon.Domain;

namespace SimpleSummon.Application
{
    public sealed class SignBuilderService
    {
        private readonly IRandomSource random;

        public SignBuilderService(IRandomSource random)
        {
            this.random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public bool Move(
            byte[] board,
            int sourceSlot,
            SignBuilderMoveDirection direction) =>
            SignBuilderState.TryMove(board, sourceSlot, direction);

        public bool AddFragments(byte[] board, byte fragmentMask)
        {
            bool changed = false;
            for (byte id = 0; id < SignBuilderState.FragmentCount; id++)
            {
                if ((fragmentMask & 1 << id) != 0)
                {
                    changed |= SignBuilderState.TryAddFragment(
                        board,
                        id,
                        random.Next());
                }
            }

            return changed;
        }

        public bool IsCompleted(byte[] board) => SignBuilderState.IsCompleted(board);
    }
}
