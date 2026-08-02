using NUnit.Framework;
using SimpleSummon.Application;

namespace SimpleSummon.Tests.Application
{
    public sealed class ArtifactCraftingServiceTests
    {
        [TestCase(new[] { 5, 0, 0 }, true)]
        [TestCase(new[] { 4, 1, 0 }, false)]
        [TestCase(new[] { 4, 0, 0 }, false)]
        public void HasCompleteStack_ReturnsExpected(int[] slots, bool expected)
        {
            Assert.That(
                ArtifactCraftingService.HasCompleteStack(slots, 5),
                Is.EqualTo(expected));
        }

        [Test]
        public void TryMerge_MovesSourceStackIntoTarget()
        {
            int[] slots = { 2, 3, 0 };

            bool merged = ArtifactCraftingService.TryMerge(slots, 0, 1);

            Assert.That(merged, Is.True);
            Assert.That(slots, Is.EqualTo(new[] { 0, 5, 0 }));
        }

        [TestCase(-1, 1)]
        [TestCase(0, 3)]
        [TestCase(0, 0)]
        [TestCase(2, 1)]
        public void TryMerge_InvalidMove_DoesNotChangeSlots(int source, int target)
        {
            int[] slots = { 2, 3, 0 };

            bool merged = ArtifactCraftingService.TryMerge(slots, source, target);

            Assert.That(merged, Is.False);
            Assert.That(slots, Is.EqualTo(new[] { 2, 3, 0 }));
        }
    }
}
