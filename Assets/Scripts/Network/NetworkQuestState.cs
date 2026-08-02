using System;
using SimpleSummon.Domain;
using SimpleSummon.Application;
using Unity.Netcode;

namespace SimpleSummon.Network
{
    public sealed class NetworkQuestState : NetworkBehaviour
    {
        private readonly NetworkVariable<bool> bossHeartCollected = new();
        private readonly NetworkVariable<bool> signDrawn = new();
        private readonly QuestProgress progress = new();
        private QuestProgressService service;

        public event Action Changed;

        private void Awake()
        {
            service = new QuestProgressService(progress);
        }

        public bool BossHeartCollected => IsSpawned
            ? bossHeartCollected.Value
            : progress.BossHeartCollected;
        public bool SignDrawn => IsSpawned ? signDrawn.Value : progress.SignDrawn;

        public override void OnNetworkSpawn()
        {
            bossHeartCollected.OnValueChanged += HandleBossHeartChanged;
            signDrawn.OnValueChanged += HandleSignDrawnChanged;
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
            bossHeartCollected.OnValueChanged -= HandleBossHeartChanged;
            signDrawn.OnValueChanged -= HandleSignDrawnChanged;
        }

        public void CollectBossHeart()
        {
            if (IsSpawned && !IsServer)
            {
                return;
            }

            bool changed = service.CollectBossHeart();
            if (changed)
            {
                Publish();
            }
        }

        public void RecordSignDrawn()
        {
            if ((!IsSpawned || IsServer) && service.RecordSignDrawn())
            {
                Publish();
            }
        }

        private void Publish()
        {
            if (IsSpawned)
            {
                bossHeartCollected.Value = progress.BossHeartCollected;
                signDrawn.Value = progress.SignDrawn;
            }
            else
            {
                Changed?.Invoke();
            }
        }

        private void ApplyReplicatedState()
        {
            progress.Apply(bossHeartCollected.Value, signDrawn.Value);
        }

        private void HandleBossHeartChanged(bool _, bool __)
        {
            ApplyReplicatedState();
            Changed?.Invoke();
        }

        private void HandleSignDrawnChanged(bool _, bool __)
        {
            ApplyReplicatedState();
            Changed?.Invoke();
        }

    }
}
