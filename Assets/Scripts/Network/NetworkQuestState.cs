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
        private readonly NetworkVariable<int> greenBottleCount = new();
        private readonly NetworkVariable<int> brownBottleCount = new();
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
        public int GreenBottleCount => IsSpawned
            ? greenBottleCount.Value
            : progress.Ingredients.GreenBottleCount;
        public int BrownBottleCount => IsSpawned
            ? brownBottleCount.Value
            : progress.Ingredients.BrownBottleCount;

        public override void OnNetworkSpawn()
        {
            bossHeartCollected.OnValueChanged += HandleBossHeartChanged;
            signDrawn.OnValueChanged += HandleSignDrawnChanged;
            greenBottleCount.OnValueChanged += HandleIngredientChanged;
            brownBottleCount.OnValueChanged += HandleIngredientChanged;
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
            greenBottleCount.OnValueChanged -= HandleIngredientChanged;
            brownBottleCount.OnValueChanged -= HandleIngredientChanged;
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

        public bool CollectIngredient(IngredientType ingredient)
        {
            if (IsSpawned && !IsServer)
            {
                return false;
            }

            bool changed = service.CollectIngredient(ingredient);
            if (changed)
            {
                Publish();
            }
            return changed;
        }

        private void Publish()
        {
            if (IsSpawned)
            {
                bossHeartCollected.Value = progress.BossHeartCollected;
                signDrawn.Value = progress.SignDrawn;
                greenBottleCount.Value = progress.Ingredients.GreenBottleCount;
                brownBottleCount.Value = progress.Ingredients.BrownBottleCount;
            }
            else
            {
                Changed?.Invoke();
            }
        }

        private void ApplyReplicatedState()
        {
            progress.Apply(
                bossHeartCollected.Value,
                signDrawn.Value,
                greenBottleCount.Value,
                brownBottleCount.Value);
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

        private void HandleIngredientChanged(int _, int __)
        {
            ApplyReplicatedState();
            Changed?.Invoke();
        }

    }
}
