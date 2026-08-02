using System;
using SimpleSummon.Domain;
using SimpleSummon.Application;
using Unity.Netcode;

namespace SimpleSummon.Network
{
    public sealed class NetworkQuestState : NetworkBehaviour
    {
        private readonly NetworkVariable<byte> signFragments = new();
        private readonly NetworkVariable<bool> bossHeartCollected = new();
        private readonly NetworkVariable<int> artifactResourceCount = new();
        private readonly NetworkVariable<bool> artifactCrafted = new();
        private readonly NetworkVariable<bool> signDrawn = new();
        private readonly QuestProgress progress = new();
        private QuestProgressService service;

        public event Action Changed;

        private void Awake()
        {
            service = new QuestProgressService(progress);
        }

        public byte SignFragmentMask => IsSpawned
            ? signFragments.Value
            : progress.SignFragmentMask;
        public int CollectedSignFragmentCount => progress.CollectedSignFragmentCount;
        public bool BossHeartCollected => IsSpawned
            ? bossHeartCollected.Value
            : progress.BossHeartCollected;
        public int ArtifactResourceCount => IsSpawned
            ? artifactResourceCount.Value
            : progress.ArtifactResourceCount;
        public bool ArtifactCrafted => IsSpawned
            ? artifactCrafted.Value
            : progress.ArtifactCrafted;
        public bool SignDrawn => IsSpawned ? signDrawn.Value : progress.SignDrawn;

        public override void OnNetworkSpawn()
        {
            signFragments.OnValueChanged += HandleFragmentsChanged;
            bossHeartCollected.OnValueChanged += HandleBossHeartChanged;
            artifactResourceCount.OnValueChanged += HandleArtifactResourcesChanged;
            artifactCrafted.OnValueChanged += HandleArtifactCraftedChanged;
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
            signFragments.OnValueChanged -= HandleFragmentsChanged;
            bossHeartCollected.OnValueChanged -= HandleBossHeartChanged;
            artifactResourceCount.OnValueChanged -= HandleArtifactResourcesChanged;
            artifactCrafted.OnValueChanged -= HandleArtifactCraftedChanged;
            signDrawn.OnValueChanged -= HandleSignDrawnChanged;
        }

        public bool IsCollected(QuestCollectableType type, int id)
        {
            return type == QuestCollectableType.SignFragment
                ? IsSignFragmentCollected(id)
                : BossHeartCollected;
        }

        public void Collect(QuestCollectableType type, int id)
        {
            if (IsSpawned && !IsServer)
            {
                return;
            }

            bool changed = service.Collect(type, id);
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

        public bool CollectArtifactResource()
        {
            if (IsSpawned && !IsServer)
            {
                return false;
            }

            bool changed = service.CollectArtifactResource();
            if (changed)
            {
                Publish();
            }

            return changed;
        }

        public bool CraftArtifact()
        {
            if (IsSpawned && !IsServer)
            {
                return false;
            }

            bool changed = service.CraftArtifact();
            if (changed)
            {
                Publish();
            }

            return changed;
        }

        public void RequestCraftArtifact()
        {
            if (!IsSpawned || IsServer)
            {
                CraftArtifact();
                return;
            }

            CraftArtifactRpc();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void CraftArtifactRpc()
        {
            CraftArtifact();
        }

        private bool IsSignFragmentCollected(int id)
        {
            if (id < 0 || id >= QuestProgress.SignFragmentCount)
            {
                return false;
            }

            return (SignFragmentMask & 1 << id) != 0;
        }

        private void Publish()
        {
            if (IsSpawned)
            {
                signFragments.Value = progress.SignFragmentMask;
                bossHeartCollected.Value = progress.BossHeartCollected;
                artifactResourceCount.Value = progress.ArtifactResourceCount;
                artifactCrafted.Value = progress.ArtifactCrafted;
                signDrawn.Value = progress.SignDrawn;
            }
            else
            {
                Changed?.Invoke();
            }
        }

        private void ApplyReplicatedState()
        {
            progress.Apply(
                signFragments.Value,
                bossHeartCollected.Value,
                signDrawn.Value,
                artifactResourceCount.Value,
                artifactCrafted.Value);
        }

        private void HandleFragmentsChanged(byte _, byte __)
        {
            ApplyReplicatedState();
            Changed?.Invoke();
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

        private void HandleArtifactResourcesChanged(int _, int __)
        {
            ApplyReplicatedState();
            Changed?.Invoke();
        }

        private void HandleArtifactCraftedChanged(bool _, bool __)
        {
            ApplyReplicatedState();
            Changed?.Invoke();
        }
    }
}
