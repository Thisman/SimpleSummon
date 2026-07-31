using System;
using SimpleSummon.Domain;
using Unity.Netcode;

namespace SimpleSummon.Network
{
    public sealed class NetworkQuestState : NetworkBehaviour
    {
        private readonly NetworkVariable<byte> signFragments = new();
        private readonly NetworkVariable<bool> bossHeartCollected = new();
        private readonly NetworkVariable<bool> signDrawn = new();
        private readonly QuestProgress progress = new();

        public event Action Changed;

        public byte SignFragmentMask => IsSpawned
            ? signFragments.Value
            : progress.SignFragmentMask;
        public int CollectedSignFragmentCount => progress.CollectedSignFragmentCount;
        public bool BossHeartCollected => IsSpawned
            ? bossHeartCollected.Value
            : progress.BossHeartCollected;
        public bool SignDrawn => IsSpawned ? signDrawn.Value : progress.SignDrawn;

        public override void OnNetworkSpawn()
        {
            signFragments.OnValueChanged += HandleFragmentsChanged;
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
            signFragments.OnValueChanged -= HandleFragmentsChanged;
            bossHeartCollected.OnValueChanged -= HandleBossHeartChanged;
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

            bool changed = type == QuestCollectableType.SignFragment
                ? progress.CollectSignFragment(id)
                : progress.CollectBossHeart();
            if (changed)
            {
                Publish();
            }
        }

        public void RecordSignDrawn()
        {
            if ((!IsSpawned || IsServer) && progress.DrawSign())
            {
                Publish();
            }
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
                signDrawn.Value);
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
    }
}
