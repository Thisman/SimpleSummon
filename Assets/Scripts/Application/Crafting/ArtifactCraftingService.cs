using System.Collections.Generic;

namespace SimpleSummon.Application
{
    public static class ArtifactCraftingService
    {
        public static bool HasCompleteStack(
            IReadOnlyList<int> slotCounts,
            int requiredResources)
        {
            int nonEmptySlots = 0;
            int resourcesInStack = 0;
            for (int i = 0; i < slotCounts.Count; i++)
            {
                if (slotCounts[i] <= 0)
                {
                    continue;
                }

                nonEmptySlots++;
                resourcesInStack = slotCounts[i];
            }

            return nonEmptySlots == 1 && resourcesInStack == requiredResources;
        }
    }
}
