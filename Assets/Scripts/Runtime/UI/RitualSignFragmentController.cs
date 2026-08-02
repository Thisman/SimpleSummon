using SimpleSummon.Domain;
using SimpleSummon.Network;
using Unity.Netcode;
using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class RitualSignFragmentController : MonoBehaviour
    {
        [SerializeField] private NetworkRitualSignPlates networkState;
        [SerializeField] private RitualSignFragmentView view;
        [SerializeField, Min(0.05f)] private float shuffleInterval = 1f;

        private readonly int[] shuffledFragments =
            new int[RitualSignPlateState.PlateCount];
        private bool showingOpenedFragments;
        private float nextShuffleTime;

        private void OnEnable()
        {
            networkState.Changed += RefreshMode;
            NetworkPlayer.LocalPlayerChanged += RefreshMode;
            PlayerRegistry.Changed += RefreshMode;
            RefreshMode();
        }

        private void OnDisable()
        {
            networkState.Changed -= RefreshMode;
            NetworkPlayer.LocalPlayerChanged -= RefreshMode;
            PlayerRegistry.Changed -= RefreshMode;
        }

        private void Update()
        {
            if (!showingOpenedFragments && Time.unscaledTime >= nextShuffleTime)
            {
                Shuffle();
            }
        }

        private void RefreshMode()
        {
            showingOpenedFragments = TryGetLocalActorId(out ulong actorId) &&
                                     networkState.TryGetPlate(actorId, out _);
            if (showingOpenedFragments)
            {
                view.ShowOpened(networkState.OccupiedMask);
            }
            else
            {
                Shuffle();
            }
        }

        private void Shuffle()
        {
            for (int i = 0; i < shuffledFragments.Length; i++)
            {
                shuffledFragments[i] = Random.Range(
                    0,
                    RitualSignPlateState.PlateCount);
            }
            view.ShowScrambled(shuffledFragments);
            nextShuffleTime = Time.unscaledTime + shuffleInterval;
        }

        private static bool TryGetLocalActorId(out ulong actorId)
        {
            NetworkManager manager = NetworkManager.Singleton;
            if (manager != null && manager.IsListening)
            {
                NetworkPlayer localPlayer = NetworkPlayer.LocalPlayer;
                if (localPlayer != null && localPlayer.IsSpawned)
                {
                    actorId = localPlayer.OwnerClientId;
                    return true;
                }

                actorId = 0;
                return false;
            }

            actorId = 0;
            return PlayerRegistry.GetLocalPlayer() != null;
        }
    }
}
