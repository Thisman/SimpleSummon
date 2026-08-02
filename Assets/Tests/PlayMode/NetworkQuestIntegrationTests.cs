using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SimpleSummon.Domain;
using SimpleSummon.Network;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SimpleSummon.Tests.PlayMode
{
    public sealed class NetworkQuestIntegrationTests
    {
        private const float Timeout = 5f;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            NetworkManager[] managers = Object.FindObjectsByType<NetworkManager>(
                FindObjectsInactive.Include);
            for (int i = 0; i < managers.Length; i++)
            {
                Shutdown(managers[i]);
            }

            yield return new WaitForSecondsRealtime(1f / 30f);
            for (int i = 0; i < managers.Length; i++)
            {
                if (managers[i] != null)
                {
                    Object.DestroyImmediate(managers[i].gameObject);
                }
            }
        }

        [UnityTest]
        public IEnumerator SharedStates_SynchronizeChangesLateJoinAndClientDisconnect()
        {
            yield return PrepareEmptyScene();
            ushort port = (ushort)Random.Range(12000, 20000);
            GameObject prefab = CreateQuestPrefab();
            NetworkManager host = CreateManager("Host", port, true, prefab);
            NetworkManager client = CreateManager("Client", port, false, prefab);
            NetworkManager lateClient = null;

            try
            {
                Assert.That(host.StartHost(), Is.True);
                Assert.That(client.StartClient(), Is.True);
                yield return WaitUntil(() => client.IsConnectedClient, "client connection");

                GameObject serverObject = Object.Instantiate(prefab);
                NetworkObject serverNetworkObject = serverObject.GetComponent<NetworkObject>();
                serverNetworkObject.Spawn();
                yield return WaitUntil(
                    () => FindQuest(client) != null,
                    "initial quest replication");

                NetworkQuestState serverState = serverObject.GetComponent<NetworkQuestState>();
                NetworkQuestState clientState = FindQuest(client);
                NetworkEnemyState serverEnemy = serverObject.GetComponent<NetworkEnemyState>();
                NetworkPlayerVitals serverVitals = serverObject.GetComponent<NetworkPlayerVitals>();
                NetworkPlayerTorch serverTorch = serverObject.GetComponent<NetworkPlayerTorch>();
                NetworkRitualSignPlates serverPlates =
                    serverObject.GetComponent<NetworkRitualSignPlates>();
                serverState.CollectBossHeart();
                serverState.RecordSignDrawn();
                serverState.CollectIngredient(IngredientType.BottleGreen);
                serverState.CollectIngredient(IngredientType.BottleBrown);
                yield return WaitUntil(
                    () => clientState.BossHeartCollected &&
                          clientState.SignDrawn &&
                          clientState.GreenBottleCount == 1 &&
                          clientState.BrownBottleCount == 1,
                    "quest state update");

                Assert.That(clientState.CollectIngredient(IngredientType.BottleGreen), Is.False);
                Assert.That(clientState.GreenBottleCount, Is.EqualTo(1));

                serverEnemy.Publish(true);
                serverEnemy.PublishDeathCompleted();
                serverEnemy.PublishLootCollected();
                serverVitals.Publish(35f, false);
                serverVitals.PublishDamage();
                InvokeTorchPublish(serverTorch, true, 65f);
                serverPlates.Synchronize(new[]
                {
                    new RitualSignPlateAssignment(10, 1),
                    new RitualSignPlateAssignment(20, 7)
                });
                yield return WaitUntil(
                    () => FindComponent<NetworkEnemyState>(client).IsDead &&
                          FindComponent<NetworkEnemyState>(client).Disappeared &&
                          FindComponent<NetworkEnemyState>(client).LootCollected &&
                          FindComponent<NetworkPlayerVitals>(client).CurrentHealth == 35f &&
                          FindComponent<NetworkPlayerTorch>(client).IsHeld &&
                          FindComponent<NetworkPlayerTorch>(client).Strength == 65f &&
                          FindComponent<NetworkRitualSignPlates>(client).OccupiedMask ==
                          ((1 << 1) | (1 << 7)),
                    "enemy, vital and plate replication");

                lateClient = CreateManager("Late Client", port, false, prefab);
                Assert.That(lateClient.StartClient(), Is.True);
                yield return WaitUntil(
                    () => lateClient.IsConnectedClient && FindQuest(lateClient) != null,
                    "late client state");
                NetworkQuestState lateState = FindQuest(lateClient);
                Assert.That(lateState.BossHeartCollected, Is.True);
                Assert.That(lateState.SignDrawn, Is.True);
                Assert.That(lateState.GreenBottleCount, Is.EqualTo(1));
                Assert.That(lateState.BrownBottleCount, Is.EqualTo(1));
                Assert.That(FindComponent<NetworkEnemyState>(lateClient).IsDead, Is.True);
                Assert.That(FindComponent<NetworkEnemyState>(lateClient).Disappeared, Is.True);
                Assert.That(FindComponent<NetworkEnemyState>(lateClient).LootCollected, Is.True);
                Assert.That(FindComponent<NetworkPlayerVitals>(lateClient).CurrentHealth, Is.EqualTo(35f));
                Assert.That(FindComponent<NetworkPlayerTorch>(lateClient).IsHeld, Is.True);
                Assert.That(FindComponent<NetworkPlayerTorch>(lateClient).Strength, Is.EqualTo(65f));
                Assert.That(
                    FindComponent<NetworkRitualSignPlates>(lateClient).OccupiedMask,
                    Is.EqualTo((1 << 1) | (1 << 7)));

                client.Shutdown();
                yield return WaitUntil(
                    () => host.ConnectedClientsIds.Count == 2,
                    "client disconnect propagation");
                Assert.That(serverState.BossHeartCollected, Is.True);
                Assert.That(serverState.GreenBottleCount, Is.EqualTo(1));
            }
            finally
            {
                Shutdown(lateClient);
                Shutdown(client);
                Shutdown(host);
                Object.DestroyImmediate(prefab);
            }
        }

        [UnityTest]
        public IEnumerator Drawing_SynchronizesOwnershipPointsLateJoinAndOwnerDisconnect()
        {
            yield return PrepareEmptyScene();
            ushort port = (ushort)Random.Range(20001, 28000);
            GameObject prefab = CreateSignPrefab();
            NetworkManager host = CreateManager("Sign Host", port, true, prefab);
            NetworkManager client = CreateManager("Sign Client", port, false, prefab);
            NetworkManager lateClient = null;

            try
            {
                Assert.That(host.StartHost(), Is.True);
                Assert.That(client.StartClient(), Is.True);
                yield return WaitUntil(() => client.IsConnectedClient, "client connection");

                GameObject serverObject = Object.Instantiate(prefab);
                serverObject.GetComponent<NetworkObject>().Spawn();
                yield return WaitUntil(
                    () => FindComponent<NetworkSignDrawing>(client) != null,
                    "drawing spawn");

                NetworkSignDrawing serverDrawing =
                    serverObject.GetComponent<NetworkSignDrawing>();
                NetworkSignDrawing clientDrawing =
                    FindComponent<NetworkSignDrawing>(client);
                clientDrawing.RequestClaim();
                yield return WaitUntil(
                    () => serverDrawing.State == SignDrawingState.Claimed &&
                          clientDrawing.State == SignDrawingState.Claimed,
                    "claim replication");
                Assert.That(serverDrawing.DrawingClientId, Is.EqualTo(client.LocalClientId));

                clientDrawing.SubmitPoints(new[]
                {
                    new NetworkSignPoint(new Vector2(0.1f, 0.1f), true),
                    new NetworkSignPoint(new Vector2(0.8f, 0.8f), false)
                });
                yield return WaitUntil(
                    () => serverDrawing.PointCount == 2 && clientDrawing.PointCount == 2,
                    "point replication");

                lateClient = CreateManager("Sign Late Client", port, false, prefab);
                Assert.That(lateClient.StartClient(), Is.True);
                yield return WaitUntil(
                    () => lateClient.IsConnectedClient &&
                          FindComponent<NetworkSignDrawing>(lateClient)?.PointCount == 2,
                    "late join drawing snapshot");

                client.Shutdown();
                yield return WaitUntil(
                    () => serverDrawing.State == SignDrawingState.Available,
                    "owner release after disconnect");
                Assert.That(serverDrawing.PointCount, Is.EqualTo(2));

                serverDrawing.RequestClaim();
                serverDrawing.Finish();
                yield return WaitUntil(
                    () => FindComponent<NetworkSignDrawing>(lateClient).State ==
                          SignDrawingState.Finished &&
                          FindQuest(lateClient).SignDrawn,
                    "finished drawing and shared quest replication");
            }
            finally
            {
                Shutdown(lateClient);
                Shutdown(client);
                Shutdown(host);
                Object.DestroyImmediate(prefab);
            }
        }

        private static GameObject CreateQuestPrefab()
        {
            GameObject prefab = new("Quest Prefab");
            NetworkObject networkObject = prefab.AddComponent<NetworkObject>();
            FieldInfo hash = typeof(NetworkObject).GetField(
                "GlobalObjectIdHash",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(hash, Is.Not.Null);
            hash.SetValue(networkObject, 0x51A7E001u);
            prefab.AddComponent<NetworkQuestState>();
            prefab.AddComponent<NetworkEnemyState>();
            prefab.AddComponent<NetworkPlayerVitals>();
            prefab.AddComponent<NetworkPlayerTorch>();
            prefab.AddComponent<NetworkRitualSignPlates>();
            return prefab;
        }

        private static GameObject CreateSignPrefab()
        {
            GameObject prefab = new("Sign Prefab");
            NetworkObject networkObject = prefab.AddComponent<NetworkObject>();
            SetNetworkHash(networkObject, 0x51A7E002u);
            NetworkQuestState quest = prefab.AddComponent<NetworkQuestState>();
            NetworkSignDrawing drawing = prefab.AddComponent<NetworkSignDrawing>();
            FieldInfo questField = typeof(NetworkSignDrawing).GetField(
                "questState",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(questField, Is.Not.Null);
            questField.SetValue(drawing, quest);
            return prefab;
        }

        private static void SetNetworkHash(NetworkObject target, uint value)
        {
            FieldInfo field = typeof(NetworkObject).GetField(
                "GlobalObjectIdHash",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }

        private static NetworkManager CreateManager(
            string name,
            ushort port,
            bool server,
            GameObject prefab)
        {
            GameObject gameObject = new(name);
            UnityTransport transport = gameObject.AddComponent<UnityTransport>();
            if (server)
            {
                transport.SetConnectionData("127.0.0.1", port, "127.0.0.1");
            }
            else
            {
                transport.SetConnectionData("127.0.0.1", port);
            }

            NetworkManager manager = gameObject.AddComponent<NetworkManager>();
            manager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,
                EnableSceneManagement = false
            };
            manager.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = prefab });
            return manager;
        }

        private static NetworkQuestState FindQuest(NetworkManager manager) =>
            FindComponent<NetworkQuestState>(manager);

        private static T FindComponent<T>(NetworkManager manager) where T : Component
        {
            if (manager == null || manager.SpawnManager == null)
            {
                return null;
            }

            return manager.SpawnManager.SpawnedObjectsList
                .Select(item => item.GetComponent<T>())
                .FirstOrDefault(item => item != null);
        }

        private static IEnumerator WaitUntil(System.Func<bool> condition, string operation)
        {
            float deadline = Time.realtimeSinceStartup + Timeout;
            while (!condition() && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(condition(), Is.True, $"Timed out waiting for {operation}.");
        }

        private static void InvokeTorchPublish(
            NetworkPlayerTorch target,
            bool held,
            float strength)
        {
            MethodInfo method = typeof(NetworkPlayerTorch).GetMethod(
                "Publish",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(target, new object[] { held, strength });
        }

        private static void Shutdown(NetworkManager manager)
        {
            if (manager == null)
            {
                return;
            }
            if (manager.IsListening)
            {
                manager.Shutdown(true);
            }
        }

        private static IEnumerator PrepareEmptyScene()
        {
            NetworkManager[] managers = Object.FindObjectsByType<NetworkManager>(
                FindObjectsInactive.Include);
            for (int i = 0; i < managers.Length; i++)
            {
                Shutdown(managers[i]);
            }

            yield return new WaitForSecondsRealtime(1f / 30f);
            for (int i = 0; i < managers.Length; i++)
            {
                if (managers[i] != null)
                {
                    Object.DestroyImmediate(managers[i].gameObject);
                }
            }

            Scene emptyScene = SceneManager.CreateScene(
                $"Network Test {Time.frameCount}");
            SceneManager.SetActiveScene(emptyScene);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene != emptyScene)
                {
                    AsyncOperation unload = SceneManager.UnloadSceneAsync(scene);
                    if (unload != null)
                    {
                        yield return unload;
                    }
                }
            }
        }
    }
}
