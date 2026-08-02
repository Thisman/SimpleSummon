namespace SimpleSummon.Domain
{
    public sealed class IngredientInventory
    {
        public int GreenBottleCount { get; private set; }
        public int BrownBottleCount { get; private set; }

        public bool Add(IngredientType ingredient)
        {
            switch (ingredient)
            {
                case IngredientType.BottleGreen:
                    GreenBottleCount++;
                    return true;
                case IngredientType.BottleBrown:
                    BrownBottleCount++;
                    return true;
                default:
                    return false;
            }
        }

        public void Apply(int greenBottleCount, int brownBottleCount)
        {
            GreenBottleCount = greenBottleCount;
            BrownBottleCount = brownBottleCount;
        }
    }
}
