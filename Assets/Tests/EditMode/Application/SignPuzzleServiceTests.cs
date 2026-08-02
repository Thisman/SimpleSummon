using NUnit.Framework;
using SimpleSummon.Application;
using SimpleSummon.Domain;

namespace SimpleSummon.Tests.Application
{
    public sealed class SignPuzzleServiceTests
    {
        [Test]
        public void MoveThenAddFragments_AppliesBothOperationsInOrder()
        {
            byte[] board =
            {
                0, SignPuzzleState.Empty, SignPuzzleState.Empty,
                SignPuzzleState.Empty, SignPuzzleState.Empty, SignPuzzleState.Empty,
                SignPuzzleState.Empty, SignPuzzleState.Empty, SignPuzzleState.Empty
            };
            SignPuzzleService service = new(new FixedRandomSource());

            Assert.That(service.Move(board, 0, SignPuzzleMoveDirection.Right), Is.True);
            Assert.That(service.AddFragments(board, 1 << 1), Is.True);
            Assert.That(board[1], Is.EqualTo(0));
            Assert.That(board, Does.Contain((byte)1));
        }

        private sealed class FixedRandomSource : IRandomSource
        {
            public int Next() => 0;
        }
    }
}
