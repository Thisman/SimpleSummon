using System;
using SimpleSummon.Domain;
using Unity.Netcode;

namespace SimpleSummon.Network
{
    internal sealed class NetworkSignPuzzleBoard
    {
        private readonly NetworkList<byte> slots;
        private readonly NetworkVariable<bool> completed;
        private readonly SignPuzzleModel offlineModel;

        public NetworkSignPuzzleBoard(
            NetworkList<byte> slots,
            NetworkVariable<bool> completed,
            SignPuzzleModel offlineModel)
        {
            this.slots = slots;
            this.completed = completed;
            this.offlineModel = offlineModel;
        }

        public bool GetCompleted(bool isSpawned) =>
            isSpawned ? completed.Value : offlineModel.Completed;

        public byte GetSlot(bool isSpawned, int index)
        {
            if (index < 0 || index >= SignPuzzleState.SlotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return isSpawned
                ? index < slots.Count ? slots[index] : SignPuzzleState.Empty
                : offlineModel.Slots[index];
        }

        public void Copy(bool isSpawned, byte[] destination)
        {
            if (destination == null || destination.Length != SignPuzzleState.SlotCount)
            {
                throw new ArgumentException(
                    "Destination must contain nine slots.",
                    nameof(destination));
            }
            for (int i = 0; i < destination.Length; i++)
            {
                destination[i] = GetSlot(isSpawned, i);
            }
        }

        public byte[] Read(bool isSpawned)
        {
            byte[] result = new byte[SignPuzzleState.SlotCount];
            Copy(isSpawned, result);
            return result;
        }

        public void Publish(bool isSpawned, byte[] board)
        {
            if (isSpawned)
            {
                for (int i = 0; i < board.Length; i++)
                {
                    slots[i] = board[i];
                }
                completed.Value = SignPuzzleState.IsCompleted(board);
            }
            else
            {
                offlineModel.Apply(board);
            }
        }

        public int CountFragments(bool isSpawned)
        {
            int count = 0;
            for (int i = 0; i < SignPuzzleState.SlotCount; i++)
            {
                if (GetSlot(isSpawned, i) != SignPuzzleState.Empty)
                {
                    count++;
                }
            }
            return count;
        }
    }
}
