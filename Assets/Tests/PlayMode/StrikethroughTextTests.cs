using NUnit.Framework;
using SimpleSummon.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleSummon.Tests.PlayMode
{
    public sealed class StrikethroughTextTests
    {
        [Test]
        public void SetCompleted_ShowsAndHidesRenderedLine()
        {
            GameObject gameObject = new(
                "Quest Text",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(StrikethroughText));

            try
            {
                StrikethroughText text = gameObject.GetComponent<StrikethroughText>();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.text = "Quest";

                text.SetCompleted(true);

                Transform line = gameObject.transform.Find("Strikethrough");
                Assert.That(line, Is.Not.Null);
                Assert.That(line.gameObject.activeSelf, Is.True);
                Assert.That(line.GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(12f));
                float signedAngle = Mathf.DeltaAngle(0f, line.localEulerAngles.z);
                Assert.That(signedAngle, Is.InRange(-7f, 7f));
                Assert.That(text.StrikeGraphic, Is.Not.Null);
                Assert.That(text.StrikeGraphic.color, Is.EqualTo(Color.white));

                text.SetCompleted(false);

                Assert.That(line.gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void SetCompleted_ReplacesLineFromPreviousImplementation()
        {
            GameObject gameObject = new(
                "Quest Text",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(StrikethroughText));
            GameObject oldLine = new(
                "Strikethrough",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            oldLine.transform.SetParent(gameObject.transform, false);

            try
            {
                StrikethroughText text = gameObject.GetComponent<StrikethroughText>();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.text = "Quest";

                Assert.DoesNotThrow(() => text.SetCompleted(true));
                Assert.That(text.StrikeGraphic, Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
