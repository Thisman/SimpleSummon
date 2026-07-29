using Unity.Netcode;
using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class OfflineGameBootstrap : MonoBehaviour
    {
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Transform spawnPoint;

        private void Awake()
        {
            if (NetworkManager.Singleton == null ||
                !NetworkManager.Singleton.IsListening)
            {
                Instantiate(
                    playerPrefab,
                    spawnPoint.position,
                    spawnPoint.rotation);
            }
        }
    }
}
