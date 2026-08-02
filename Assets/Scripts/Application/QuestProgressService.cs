using System;
using SimpleSummon.Domain;

namespace SimpleSummon.Application
{
    public sealed class QuestProgressService
    {
        private readonly QuestProgress progress;

        public QuestProgressService(QuestProgress progress)
        {
            this.progress = progress ?? throw new ArgumentNullException(nameof(progress));
        }

        public bool Collect(QuestCollectableType type, int id) =>
            type == QuestCollectableType.SignFragment
                ? progress.CollectSignFragment(id)
                : progress.CollectBossHeart();

        public bool RecordSignDrawn() => progress.DrawSign();
        public bool CollectArtifactResource() => progress.CollectArtifactResource();
        public bool CraftArtifact() => progress.CraftArtifact();
    }
}
