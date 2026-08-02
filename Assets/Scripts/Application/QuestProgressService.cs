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

        public bool CollectBossHeart() => progress.CollectBossHeart();
        public bool RecordSignDrawn() => progress.DrawSign();
        public bool CollectIngredient(IngredientType ingredient) =>
            progress.Ingredients.Add(ingredient);
    }
}
