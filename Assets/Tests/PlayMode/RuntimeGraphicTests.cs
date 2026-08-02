using System.Reflection;
using NUnit.Framework;
using SimpleSummon.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleSummon.Tests.PlayMode
{
    public sealed class RuntimeGraphicTests
    {
        [TestCase(-1f)]
        [TestCase(0f)]
        [TestCase(0.5f)]
        [TestCase(1f)]
        [TestCase(2f)]
        public void InteractionProgressRing_AlwaysBuildsCompleteRing(float progress)
        {
            GameObject gameObject = new(
                "Ring",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(InteractionProgressRing));
            try
            {
                gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(100f, 100f);
                InteractionProgressRing ring = gameObject.GetComponent<InteractionProgressRing>();
                ring.SetProgress(progress);
                using VertexHelper vertices = Populate(ring);

                Assert.That(vertices.currentVertCount, Is.EqualTo(64 * 4));
                Assert.That(vertices.currentIndexCount, Is.EqualTo(64 * 6));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void HandDrawnStrikeGraphic_SameSeedBuildsSameGeometry()
        {
            HandDrawnStrikeGraphic first = CreateStrike("First");
            HandDrawnStrikeGraphic second = CreateStrike("Second");
            try
            {
                first.Configure(42);
                second.Configure(42);
                using VertexHelper firstVertices = Populate(first);
                using VertexHelper secondVertices = Populate(second);

                Assert.That(firstVertices.currentVertCount, Is.EqualTo(12));
                Assert.That(firstVertices.currentIndexCount, Is.EqualTo(30));
                for (int i = 0; i < firstVertices.currentVertCount; i++)
                {
                    UIVertex firstVertex = default;
                    UIVertex secondVertex = default;
                    firstVertices.PopulateUIVertex(ref firstVertex, i);
                    secondVertices.PopulateUIVertex(ref secondVertex, i);
                    Assert.That(firstVertex.position, Is.EqualTo(secondVertex.position));
                }
            }
            finally
            {
                Object.DestroyImmediate(first.gameObject);
                Object.DestroyImmediate(second.gameObject);
            }
        }

        private static HandDrawnStrikeGraphic CreateStrike(string name)
        {
            GameObject gameObject = new(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(HandDrawnStrikeGraphic));
            gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, 20f);
            return gameObject.GetComponent<HandDrawnStrikeGraphic>();
        }

        private static VertexHelper Populate(MaskableGraphic graphic)
        {
            VertexHelper helper = new();
            MethodInfo method = graphic.GetType().GetMethod(
                "OnPopulateMesh",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(VertexHelper) },
                null);
            Assert.That(method, Is.Not.Null);
            method.Invoke(graphic, new object[] { helper });
            return helper;
        }
    }
}
