namespace SimpleSummon.Domain
{
    public sealed class QuestProgress
    {
        private readonly IngredientInventory ingredients = new();

        public bool BossHeartCollected { get; private set; }
        public bool SignDrawn { get; private set; }
        public IngredientInventory Ingredients => ingredients;

        public bool CollectBossHeart()
        {
            if (BossHeartCollected)
            {
                return false;
            }

            BossHeartCollected = true;
            return true;
        }

        public bool DrawSign()
        {
            if (SignDrawn)
            {
                return false;
            }

            SignDrawn = true;
            return true;
        }

        public void Apply(bool bossHeart, bool signDrawn, int greenBottles, int brownBottles)
        {
            BossHeartCollected = bossHeart;
            SignDrawn = signDrawn;
            ingredients.Apply(greenBottles, brownBottles);
        }
    }
}
