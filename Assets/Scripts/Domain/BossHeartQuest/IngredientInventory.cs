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
                    if (GreenBottleCount == int.MaxValue)
                    {
                        return false;
                    }
                    GreenBottleCount++;
                    return true;
                case IngredientType.BottleBrown:
                    if (BrownBottleCount == int.MaxValue)
                    {
                        return false;
                    }
                    BrownBottleCount++;
                    return true;
                default:
                    return false;
            }
        }

        public void Apply(int greenBottleCount, int brownBottleCount)
        {
            if (greenBottleCount < 0)
            {
                throw new System.ArgumentOutOfRangeException(nameof(greenBottleCount));
            }
            if (brownBottleCount < 0)
            {
                throw new System.ArgumentOutOfRangeException(nameof(brownBottleCount));
            }

            GreenBottleCount = greenBottleCount;
            BrownBottleCount = brownBottleCount;
        }
    }
}
