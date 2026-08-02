using System.Collections.Generic;
using SimpleSummon.Domain;
using SimpleSummon.Network;
using Unity.Netcode;
using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class RitualSignPlateCoordinator : MonoBehaviour
    {
        [SerializeField] private NetworkRitualSignPlates networkState;
        [SerializeField] private RitualSignPlateController[] plates;

        private readonly List<RitualSignPlateAssignment> assignments = new();

        private void OnEnable()
        {
            networkState.Changed += RefreshPlatePresentation;
            PlayerRegistry.Changed += Synchronize;
            Synchronize();
            RefreshPlatePresentation();
        }

        private void OnDisable()
        {
            networkState.Changed -= RefreshPlatePresentation;
            PlayerRegistry.Changed -= Synchronize;
        }

        private void FixedUpdate()
        {
            Synchronize();
        }

        private void Synchronize()
        {
            if (!networkState.CanWrite)
            {
                return;
            }

            assignments.Clear();
            IReadOnlyList<PlayerController> players = PlayerRegistry.Players;
            for (int i = 0; i < players.Count; i++)
            {
                PlayerController player = players[i];
                if (player == null || player.IsDead || !player.IsGrounded ||
                    !TryGetPlate(player.transform.position, out int plateIndex))
                {
                    continue;
                }

                assignments.Add(new RitualSignPlateAssignment(
                    GetActorId(player),
                    plateIndex));
            }
            networkState.Synchronize(assignments);
        }

        private bool TryGetPlate(Vector3 position, out int plateIndex)
        {
            plateIndex = -1;
            float closestDistance = float.PositiveInfinity;
            for (int i = 0; i < plates.Length; i++)
            {
                RitualSignPlateController plate = plates[i];
                if (plate == null || !plate.ContainsStandingPoint(position))
                {
                    continue;
                }

                float distance = plate.GetSqrDistance(position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    plateIndex = plate.PlateIndex;
                }
            }
            return plateIndex >= 0;
        }

        private void RefreshPlatePresentation()
        {
            ushort occupiedMask = networkState.OccupiedMask;
            for (int i = 0; i < plates.Length; i++)
            {
                RitualSignPlateController plate = plates[i];
                if (plate != null)
                {
                    plate.SetOccupied((occupiedMask & (1 << plate.PlateIndex)) != 0);
                }
            }
        }

        private static ulong GetActorId(PlayerController player)
        {
            if (player.TryGetComponent(out NetworkPlayer networkPlayer) &&
                networkPlayer.IsSpawned)
            {
                return networkPlayer.OwnerClientId;
            }
            return 0;
        }
    }
}
