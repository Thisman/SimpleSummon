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
    }
}
