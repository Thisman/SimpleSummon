using NUnit.Framework;
using SimpleSummon.Domain;
using SimpleSummon.Network;
using Unity.Netcode;
using UnityEngine;

namespace SimpleSummon.Tests.PlayMode
{
    public sealed class NetworkStateOfflineTests
    {
        [SetUp]
        public void SetUp()
        {
            NetworkManager[] managers = Object.FindObjectsByType<NetworkManager>(
                FindObjectsInactive.Include);
            for (int i = 0; i < managers.Length; i++)
            {
                if (managers[i].IsListening)
                {
                    managers[i].Shutdown();
                }
                Object.DestroyImmediate(managers[i].gameObject);
            }
        }

        [Test]
        public void QuestState_OfflineMutationsAreSharedAndRaiseOnlyForChanges()
        {
            GameObject gameObject = new("Quest State");
            NetworkQuestState state = gameObject.AddComponent<NetworkQuestState>();
            int changes = 0;
            state.Changed += () => changes++;
            try
            {
                state.CollectBossHeart();
                state.CollectBossHeart();
                state.RecordSignDrawn();
                state.RecordSignDrawn();
                Assert.That(state.CollectIngredient(IngredientType.BottleGreen), Is.True);
                Assert.That(state.CollectIngredient(IngredientType.None), Is.False);

                Assert.That(state.BossHeartCollected, Is.True);
                Assert.That(state.SignDrawn, Is.True);
                Assert.That(state.GreenBottleCount, Is.EqualTo(1));
                Assert.That(state.BrownBottleCount, Is.Zero);
                Assert.That(changes, Is.EqualTo(3));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void EnemyState_OfflinePublishesEveryIndependentFlag()
        {
            GameObject gameObject = new("Enemy State");
            NetworkEnemyState state = gameObject.AddComponent<NetworkEnemyState>();
            int deaths = 0;
            int disappearances = 0;
            int lootChanges = 0;
            state.StateChanged += _ => deaths++;
            state.DisappearedChanged += () => disappearances++;
            state.LootChanged += () => lootChanges++;
            try
            {
                state.Publish(true);
                state.PublishDeathCompleted();
                state.PublishLootCollected();

                Assert.That(state.IsDead, Is.True);
                Assert.That(state.Disappeared, Is.True);
                Assert.That(state.LootCollected, Is.True);
                Assert.That(deaths, Is.EqualTo(1));
                Assert.That(disappearances, Is.EqualTo(1));
                Assert.That(lootChanges, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void RitualPlates_OfflineSynchronizeAndSuppressIdenticalSnapshot()
        {
            GameObject gameObject = new("Plates", typeof(NetworkObject));
            NetworkRitualSignPlates state = gameObject.AddComponent<NetworkRitualSignPlates>();
            int changes = 0;
            state.Changed += () => changes++;
            RitualSignPlateAssignment[] assignments =
            {
                new(10, 1),
                new(20, 7)
            };
            try
            {
                state.Synchronize(assignments);
                state.Synchronize(assignments);

                Assert.That(state.OccupiedMask, Is.EqualTo((1 << 1) | (1 << 7)));
                Assert.That(state.TryGetPlate(10, out int plate), Is.True);
                Assert.That(plate, Is.EqualTo(1));
                Assert.That(changes, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
