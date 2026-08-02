using System.Collections;
using NUnit.Framework;
using SimpleSummon.Domain;
using SimpleSummon.Network;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SimpleSummon.Tests.PlayMode
{
    public sealed class ArchitecturePlayModeTests
    {
        [UnityTest]
        public IEnumerator GameScene_OfflineRitualUsesApplicationRules()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            while (!load.isDone)
            {
                yield return null;
            }

            NetworkSignDrawing ritual =
                Object.FindAnyObjectByType<NetworkSignDrawing>();
            Assert.That(ritual, Is.Not.Null);

            ritual.RequestClaim();
            Assert.That(ritual.State, Is.EqualTo(SignDrawingState.Claimed));
            ritual.SubmitPoints(new[]
            {
                new NetworkSignPoint(new Vector2(0.1f, 0.1f), true),
                new NetworkSignPoint(new Vector2(0.8f, 0.8f), false)
            });
            NetworkQuestState questState =
                Object.FindAnyObjectByType<NetworkQuestState>();
            Assert.That(questState, Is.Not.Null);
            Assert.That(questState.SignDrawn, Is.True);

            ritual.Finish();

            Assert.That(ritual.State, Is.EqualTo(SignDrawingState.Finished));
            Assert.That(ritual.PointCount, Is.EqualTo(2));
        }
    }
}
