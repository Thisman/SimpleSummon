using System;
using System.Collections.Generic;

namespace SimpleSummon.Domain
{
    public static class SignBuilderState
    {
        public const int GridSize = 3;
        public const int SlotCount = GridSize * GridSize;
        public const int FragmentCount = SlotCount - 1;
        public const byte Empty = byte.MaxValue;

        public static bool TryMove(
            byte[] slots,
            int sourceSlot,
            SignBuilderMoveDirection direction)
        {
            ValidateSlots(slots);
            if (sourceSlot < 0 || sourceSlot >= SlotCount ||
                slots[sourceSlot] == Empty)
            {
                return false;
            }

            if (direction == SignBuilderMoveDirection.Automatic)
            {
                return TryMove(slots, sourceSlot, SignBuilderMoveDirection.Left) ||
                       TryMove(slots, sourceSlot, SignBuilderMoveDirection.Right) ||
                       TryMove(slots, sourceSlot, SignBuilderMoveDirection.Up) ||
                       TryMove(slots, sourceSlot, SignBuilderMoveDirection.Down);
            }

            int targetSlot = GetTargetSlot(sourceSlot, direction);
            if (targetSlot < 0 || slots[targetSlot] != Empty)
            {
                return false;
            }

            slots[targetSlot] = slots[sourceSlot];
            slots[sourceSlot] = Empty;
            return true;
        }

        public static bool TryAddFragment(
            byte[] slots,
            byte fragmentId,
            int randomValue)
        {
            ValidateSlots(slots);
            if (fragmentId >= FragmentCount || Contains(slots, fragmentId))
            {
                return false;
            }

            List<int> candidates = new();
            bool isLastFragment = CountFragments(slots) == FragmentCount - 1;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != Empty)
                {
                    continue;
                }

                slots[i] = fragmentId;
                if (!isLastFragment || IsSolvable(slots))
                {
                    candidates.Add(i);
                }
                slots[i] = Empty;
            }

            if (candidates.Count == 0)
            {
                return false;
            }

            int candidateIndex = (int)((uint)randomValue % (uint)candidates.Count);
            slots[candidates[candidateIndex]] = fragmentId;
            return true;
        }

        public static bool IsCompleted(byte[] slots)
        {
            ValidateSlots(slots);
            for (int i = 0; i < FragmentCount; i++)
            {
                if (slots[i] != i)
                {
                    return false;
                }
            }

            return slots[FragmentCount] == Empty;
        }

        public static bool CanMove(
            byte[] slots,
            int sourceSlot,
            SignBuilderMoveDirection direction)
        {
            ValidateSlots(slots);
            if (sourceSlot < 0 || sourceSlot >= SlotCount ||
                slots[sourceSlot] == Empty)
            {
                return false;
            }

            if (direction == SignBuilderMoveDirection.Automatic)
            {
                return CanMove(slots, sourceSlot, SignBuilderMoveDirection.Left) ||
                       CanMove(slots, sourceSlot, SignBuilderMoveDirection.Right) ||
                       CanMove(slots, sourceSlot, SignBuilderMoveDirection.Up) ||
                       CanMove(slots, sourceSlot, SignBuilderMoveDirection.Down);
            }

            int target = GetTargetSlot(sourceSlot, direction);
            return target >= 0 && slots[target] == Empty;
        }

        private static int GetTargetSlot(
            int sourceSlot,
            SignBuilderMoveDirection direction)
        {
            int row = sourceSlot / GridSize;
            int column = sourceSlot % GridSize;
            return direction switch
            {
                SignBuilderMoveDirection.Left when column > 0 => sourceSlot - 1,
                SignBuilderMoveDirection.Right when column < GridSize - 1 => sourceSlot + 1,
                SignBuilderMoveDirection.Up when row > 0 => sourceSlot - GridSize,
                SignBuilderMoveDirection.Down when row < GridSize - 1 => sourceSlot + GridSize,
                _ => -1
            };
        }

        private static bool IsSolvable(byte[] slots)
        {
            int inversions = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == Empty)
                {
                    continue;
                }

                for (int j = i + 1; j < slots.Length; j++)
                {
                    if (slots[j] != Empty && slots[i] > slots[j])
                    {
                        inversions++;
                    }
                }
            }

            return inversions % 2 == 0;
        }

        private static int CountFragments(byte[] slots)
        {
            int count = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != Empty)
                {
                    count++;
                }
            }
            return count;
        }

        private static bool Contains(byte[] slots, byte fragmentId)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == fragmentId)
                {
                    return true;
                }
            }
            return false;
        }

        private static void ValidateSlots(byte[] slots)
        {
            if (slots == null || slots.Length != SlotCount)
            {
                throw new ArgumentException(
                    $"The puzzle must contain exactly {SlotCount} slots.",
                    nameof(slots));
            }
        }
    }
}
