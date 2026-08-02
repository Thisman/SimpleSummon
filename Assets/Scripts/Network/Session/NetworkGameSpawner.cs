using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SimpleSummon.Network
{
    internal sealed class NetworkGameSpawner
    {
        private readonly NetworkManager networkManager;
        private readonly string gameSceneName;
        private readonly int maximumPlayers;
        private bool subscribed;

        public NetworkGameSpawner(
            NetworkManager networkManager,
            string gameSceneName,
            int maximumPlayers)
        {
            this.networkManager = networkManager;
            this.gameSceneName = gameSceneName;
            this.maximumPlayers = maximumPlayers;
        }

        public void ConfigureApproval()
        {
            networkManager.NetworkConfig.ConnectionApproval = true;
            networkManager.ConnectionApprovalCallback = Approve;
        }

        public void Subscribe()
        {
            if (subscribed)
            {
                return;
            }
            subscribed = true;
            networkManager.SceneManager.OnLoadEventCompleted += HandleLoadCompleted;
        }

        public void Unsubscribe()
        {
            subscribed = false;
            networkManager.ConnectionApprovalCallback = null;
            if (networkManager.SceneManager != null)
            {
                networkManager.SceneManager.OnLoadEventCompleted -= HandleLoadCompleted;
            }
        }

        private static void Approve(
            NetworkManager.ConnectionApprovalRequest _,
            NetworkManager.ConnectionApprovalResponse response)
        {
            response.Approved = true;
            response.CreatePlayerObject = false;
            response.Pending = false;
        }

        private void HandleLoadCompleted(
            string sceneName,
            LoadSceneMode _,
            List<ulong> __,
            List<ulong> ___)
        {
            if (!networkManager.IsServer || sceneName != gameSceneName)
            {
                return;
            }

            foreach (ulong clientId in networkManager.ConnectedClientsIds)
            {
                NetworkClient client = networkManager.ConnectedClients[clientId];
                if (client.PlayerObject != null ||
                    !NetworkSpawnPoint.TryGet(
                        (int)(clientId % (ulong)maximumPlayers),
                        out NetworkSpawnPoint spawnPoint))
                {
                    continue;
                }

                GameObject player = Object.Instantiate(
                    networkManager.NetworkConfig.PlayerPrefab,
                    spawnPoint.transform.position,
                    spawnPoint.transform.rotation);
                player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
            }
        }
    }
}
