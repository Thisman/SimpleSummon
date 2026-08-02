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

            NetworkSummonRitual ritual =
                Object.FindAnyObjectByType<NetworkSummonRitual>();
            Assert.That(ritual, Is.Not.Null);

            ritual.RequestClaim();
            Assert.That(ritual.State, Is.EqualTo(SummonRitualState.Claimed));
            ritual.SubmitPoints(new[]
            {
                new NetworkSummonPoint(new Vector2(0.1f, 0.1f), true),
                new NetworkSummonPoint(new Vector2(0.8f, 0.8f), false)
            });
            ritual.Finish();

            Assert.That(ritual.State, Is.EqualTo(SummonRitualState.Finished));
            Assert.That(ritual.PointCount, Is.EqualTo(2));
        }
    }
}
