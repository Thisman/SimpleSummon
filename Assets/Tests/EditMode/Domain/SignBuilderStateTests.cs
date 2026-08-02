using SimpleSummon.Domain;
using NUnit.Framework;

namespace SimpleSummon.Domain.Tests
{
    public sealed class SignBuilderStateTests
    {
        [Test]
        public void TryMove_FragmentNextToEmpty_MovesFragment()
        {
            byte[] slots =
            {
                0, SignBuilderState.Empty, SignBuilderState.Empty,
                SignBuilderState.Empty, SignBuilderState.Empty, SignBuilderState.Empty,
                SignBuilderState.Empty, SignBuilderState.Empty, SignBuilderState.Empty
            };

            bool moved = SignBuilderState.TryMove(
                slots,
                0,
                SignBuilderMoveDirection.Right);

            Assert.That(moved, Is.True);
            Assert.That(slots[0], Is.EqualTo(SignBuilderState.Empty));
            Assert.That(slots[1], Is.EqualTo(0));
        }

        [Test]
        public void TryMove_TargetOccupied_DoesNotChangeBoard()
        {
            byte[] slots =
            {
                0, 1, SignBuilderState.Empty,
                SignBuilderState.Empty, SignBuilderState.Empty, SignBuilderState.Empty,
                SignBuilderState.Empty, SignBuilderState.Empty, SignBuilderState.Empty
            };

            bool moved = SignBuilderState.TryMove(
                slots,
                0,
                SignBuilderMoveDirection.Right);

            Assert.That(moved, Is.False);
            Assert.That(slots[0], Is.EqualTo(0));
            Assert.That(slots[1], Is.EqualTo(1));
        }

        [Test]
        public void TryAddFragment_AddsOnlyOnce()
        {
            byte[] slots = CreateEmptyBoard();

            Assert.That(SignBuilderState.TryAddFragment(slots, 3, 5), Is.True);
            Assert.That(SignBuilderState.TryAddFragment(slots, 3, 2), Is.False);
        }

        [Test]
        public void TryAddFragment_LastFragment_KeepsPuzzleSolvable()
        {
            byte[] slots =
            {
                0, 1, 2,
                3, 4, 5,
                SignBuilderState.Empty, 7, SignBuilderState.Empty
            };

            Assert.That(SignBuilderState.TryAddFragment(slots, 6, 0), Is.True);
            Assert.That(slots[6], Is.EqualTo(6));
            Assert.That(slots[8], Is.EqualTo(SignBuilderState.Empty));
        }

        [Test]
        public void IsCompleted_OnlyMatchesOrderedBoardWithFinalEmptySlot()
        {
            byte[] completed = { 0, 1, 2, 3, 4, 5, 6, 7, SignBuilderState.Empty };
            byte[] broken = { 1, 0, 2, 3, 4, 5, 6, 7, SignBuilderState.Empty };

            Assert.That(SignBuilderState.IsCompleted(completed), Is.True);
            Assert.That(SignBuilderState.IsCompleted(broken), Is.False);
        }

        private static byte[] CreateEmptyBoard()
        {
            byte[] slots = new byte[SignBuilderState.SlotCount];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = SignBuilderState.Empty;
            }
            return slots;
        }
    }
}
