using System;
using SimpleSummon.Domain;
using SimpleSummon.Network;

namespace SimpleSummon.Runtime
{
    internal sealed class EnemyBossProgression
    {
        private readonly EnemySettings settings;
        private readonly NetworkQuestState questState;
        private Action artifactChanged;

        public EnemyBossProgression(
            EnemySettings settings,
            NetworkQuestState questState)
        {
            this.settings = settings;
            this.questState = questState;
        }

        public float InitialStatMultiplier => EnemyStatRules.GetStatMultiplier(
            settings.IsBoss,
            questState != null && questState.ArtifactCrafted,
            settings.BossStatMultiplier);

        public bool IsInitiallyWeakened =>
            settings.IsBoss && InitialStatMultiplier <= 1f;

        public bool CanApplyWeakening(UnitModel model, bool alreadyWeakened) =>
            settings.IsBoss &&
            !alreadyWeakened &&
            questState != null &&
            questState.ArtifactCrafted &&
            !model.IsDead;

        public void Enable(Action onArtifactChanged)
        {
            if (!settings.IsBoss || questState == null)
            {
                return;
            }

            artifactChanged = onArtifactChanged;
            questState.Changed += artifactChanged;
            artifactChanged();
        }

        public void Disable()
        {
            if (artifactChanged != null && questState != null)
            {
                questState.Changed -= artifactChanged;
                artifactChanged = null;
            }
        }

        public void CollectHeart()
        {
            if (settings.IsBoss)
            {
                questState?.Collect(QuestCollectableType.BossHeart, 0);
            }
        }
    }
}
