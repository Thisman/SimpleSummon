using System;
using System.Collections.Generic;
using SimpleSummon.Application;
using SimpleSummon.Domain;
using Unity.Netcode;
using UnityEngine;

namespace SimpleSummon.Network
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class NetworkRitualSignPlates : NetworkBehaviour
    {
        private readonly NetworkList<NetworkRitualSignPlateAssignment> assignments = new();
        private readonly RitualSignPlateState state = new();
        private RitualSignPlateService service;
        private bool publishing;

        public event Action Changed;

        public ushort OccupiedMask => state.GetOccupiedMask();
        public bool CanWrite => !IsSpawned || IsServer;

        private void Awake()
        {
            service = new RitualSignPlateService(state);
        }

        public override void OnNetworkSpawn()
        {
            assignments.OnListChanged += HandleAssignmentsChanged;
            if (IsServer)
            {
                Publish();
            }
            else
            {
                ApplyReplicatedState();
            }
            Changed?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            assignments.OnListChanged -= HandleAssignmentsChanged;
        }

        public void Synchronize(IReadOnlyList<RitualSignPlateAssignment> nextAssignments)
        {
            if (!CanWrite || !service.ReplaceOccupancy(nextAssignments))
            {
                return;
            }

            if (IsSpawned)
            {
                Publish();
            }
            else
            {
                Changed?.Invoke();
            }
        }

        public bool TryGetPlate(ulong actorId, out int plateIndex) =>
            state.TryGetPlate(actorId, out plateIndex);

        private void Publish()
        {
            NetworkRitualSignPlateAssignment[] snapshot =
                new NetworkRitualSignPlateAssignment[state.Assignments.Count];
            int index = 0;
            foreach (KeyValuePair<ulong, int> assignment in state.Assignments)
            {
                snapshot[index++] = new NetworkRitualSignPlateAssignment(
                    assignment.Key,
                    assignment.Value);
            }

            publishing = true;
            assignments.Clear();
            for (int i = 0; i < snapshot.Length; i++)
            {
                assignments.Add(snapshot[i]);
            }
            publishing = false;
            Changed?.Invoke();
        }

        private void ApplyReplicatedState()
        {
            RitualSignPlateAssignment[] replicated =
                new RitualSignPlateAssignment[assignments.Count];
            for (int i = 0; i < assignments.Count; i++)
            {
                NetworkRitualSignPlateAssignment assignment = assignments[i];
                replicated[i] = new RitualSignPlateAssignment(
                    assignment.ActorId,
                    assignment.PlateIndex);
            }
            service.ReplaceOccupancy(replicated);
        }

        private void HandleAssignmentsChanged(
            NetworkListEvent<NetworkRitualSignPlateAssignment> _)
        {
            if (publishing)
            {
                return;
            }

            ApplyReplicatedState();
            Changed?.Invoke();
        }
    }
}
