using System.Collections.Generic;

namespace SimpleSummon.Application
{
    public static class ArtifactCraftingService
    {
        public static bool TryMerge(
            IList<int> slotCounts,
            int sourceIndex,
            int targetIndex)
        {
            if (slotCounts == null ||
                sourceIndex < 0 || sourceIndex >= slotCounts.Count ||
                targetIndex < 0 || targetIndex >= slotCounts.Count ||
                sourceIndex == targetIndex ||
                slotCounts[sourceIndex] <= 0 ||
                slotCounts[targetIndex] <= 0)
            {
                return false;
            }

            slotCounts[targetIndex] += slotCounts[sourceIndex];
            slotCounts[sourceIndex] = 0;
            return true;
        }

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
