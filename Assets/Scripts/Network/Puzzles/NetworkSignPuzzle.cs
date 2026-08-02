using System;
using SimpleSummon.Domain;
using Unity.Netcode;
using UnityEngine;

namespace SimpleSummon.Network
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class NetworkSignPuzzle : NetworkBehaviour
    {
        public const ulong NoOwner = ulong.MaxValue;

        [SerializeField] private NetworkQuestState questState;
        [SerializeField, Min(1)] private int signVariantCount = 3;

        private readonly NetworkList<byte> slots = new();
        private readonly NetworkVariable<ulong> controllingClientId = new(NoOwner);
        private readonly NetworkVariable<bool> completed = new();
        private readonly NetworkVariable<byte> signVariant = new();
        private readonly byte[] offlineSlots = new byte[SignPuzzleState.SlotCount];
        private ulong offlineControllingClientId = NoOwner;
        private bool offlineCompleted;
        private byte offlineSignVariant;
        private byte pendingFragmentMask;

        public event Action StateChanged;
        public event Action BoardChanged;
        public ulong ControllingClientId => IsSpawned ? controllingClientId.Value : offlineControllingClientId;
        public bool Completed => IsSpawned ? completed.Value : offlineCompleted;
        public int SignVariant => IsSpawned ? signVariant.Value : offlineSignVariant;
        public bool HasFragments => GetFragmentCount() > 0;

        private void Awake()
        {
            FillWithEmpty(offlineSlots);
            offlineSignVariant = (byte)UnityEngine.Random.Range(0, signVariantCount);
        }

        private void OnEnable()
        {
            if (questState != null)
            {
                questState.Changed += QueueCollectedFragments;
                QueueCollectedFragments();
            }
        }

        private void OnDisable()
        {
            if (questState != null) questState.Changed -= QueueCollectedFragments;
        }

        private void LateUpdate()
        {
            if (!IsSpawned || IsServer) ApplyPendingFragments();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                if (slots.Count == 0)
                {
                    for (int i = 0; i < SignPuzzleState.SlotCount; i++) slots.Add(SignPuzzleState.Empty);
                }
                signVariant.Value = (byte)UnityEngine.Random.Range(0, signVariantCount);
                NetworkManager.OnClientDisconnectCallback += HandleClientDisconnected;
                QueueCollectedFragments();
            }

            controllingClientId.OnValueChanged += HandleOwnerChanged;
            completed.OnValueChanged += HandleCompletedChanged;
            signVariant.OnValueChanged += HandleVariantChanged;
            slots.OnListChanged += HandleSlotsChanged;
            StateChanged?.Invoke();
            BoardChanged?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            controllingClientId.OnValueChanged -= HandleOwnerChanged;
            completed.OnValueChanged -= HandleCompletedChanged;
            signVariant.OnValueChanged -= HandleVariantChanged;
            slots.OnListChanged -= HandleSlotsChanged;
            if (NetworkManager != null && IsServer) NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
        }

        public byte GetSlot(int index)
        {
            if (index < 0 || index >= SignPuzzleState.SlotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return IsSpawned
                ? index < slots.Count ? slots[index] : SignPuzzleState.Empty
                : offlineSlots[index];
        }

        public void CopySlots(byte[] destination)
        {
            if (destination == null || destination.Length != SignPuzzleState.SlotCount)
                throw new ArgumentException("Destination must contain nine slots.", nameof(destination));
            for (int i = 0; i < destination.Length; i++) destination[i] = GetSlot(i);
        }

        public void TryClaim(ulong clientId)
        {
            if (!IsSpawned)
            {
                if (offlineControllingClientId == NoOwner)
                {
                    offlineControllingClientId = clientId;
                    StateChanged?.Invoke();
                }
                return;
            }
            if (IsServer && controllingClientId.Value == NoOwner) controllingClientId.Value = clientId;
        }

        public void RequestMove(int sourceSlot, SignPuzzleMoveDirection direction)
        {
            if (!IsSpawned) { MoveOffline(sourceSlot, direction); return; }
            if (IsServer) MoveOnServer(NetworkManager.LocalClientId, sourceSlot, direction);
            else MoveRpc((byte)sourceSlot, direction);
        }

        public void RequestRelease()
        {
            if (!IsSpawned)
            {
                if (offlineControllingClientId != NoOwner)
                {
                    offlineControllingClientId = NoOwner;
                    StateChanged?.Invoke();
                }
                return;
            }
            if (IsServer) ReleaseOnServer(NetworkManager.LocalClientId);
            else ReleaseRpc();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void MoveRpc(byte sourceSlot, SignPuzzleMoveDirection direction, RpcParams rpcParams = default) =>
            MoveOnServer(rpcParams.Receive.SenderClientId, sourceSlot, direction);

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void ReleaseRpc(RpcParams rpcParams = default) => ReleaseOnServer(rpcParams.Receive.SenderClientId);

        private void MoveOnServer(ulong clientId, int sourceSlot, SignPuzzleMoveDirection direction)
        {
            if (controllingClientId.Value != clientId) return;
            byte[] board = ReadBoard();
            if (SignPuzzleState.TryMove(board, sourceSlot, direction)) PublishBoard(board);
        }

        private void MoveOffline(int sourceSlot, SignPuzzleMoveDirection direction)
        {
            if (offlineControllingClientId == NoOwner || !SignPuzzleState.TryMove(offlineSlots, sourceSlot, direction)) return;
            offlineCompleted = SignPuzzleState.IsCompleted(offlineSlots);
            BoardChanged?.Invoke();
            StateChanged?.Invoke();
        }

        private void ReleaseOnServer(ulong clientId)
        {
            if (controllingClientId.Value == clientId) controllingClientId.Value = NoOwner;
        }

        private void HandleClientDisconnected(ulong clientId) => ReleaseOnServer(clientId);

        private void QueueCollectedFragments()
        {
            if (questState != null && (!IsSpawned || IsServer)) pendingFragmentMask |= questState.SignFragmentMask;
        }

        private void ApplyPendingFragments()
        {
            byte requestedMask = pendingFragmentMask;
            pendingFragmentMask = 0;
            if (requestedMask == 0) return;
            byte[] board = ReadBoard();
            bool changed = false;
            for (byte id = 0; id < SignPuzzleState.FragmentCount; id++)
            {
                if ((requestedMask & 1 << id) != 0)
                    changed |= SignPuzzleState.TryAddFragment(board, id, UnityEngine.Random.Range(0, int.MaxValue));
            }
            if (changed) PublishBoard(board);
        }

        private byte[] ReadBoard()
        {
            byte[] board = new byte[SignPuzzleState.SlotCount];
            CopySlots(board);
            return board;
        }

        private void PublishBoard(byte[] board)
        {
            bool solved = SignPuzzleState.IsCompleted(board);
            if (IsSpawned)
            {
                for (int i = 0; i < board.Length; i++) slots[i] = board[i];
                completed.Value = solved;
            }
            else
            {
                Array.Copy(board, offlineSlots, board.Length);
                offlineCompleted = solved;
                BoardChanged?.Invoke();
                StateChanged?.Invoke();
            }
        }

        private int GetFragmentCount()
        {
            int count = 0;
            for (int i = 0; i < SignPuzzleState.SlotCount; i++) if (GetSlot(i) != SignPuzzleState.Empty) count++;
            return count;
        }

        private static void FillWithEmpty(byte[] board)
        {
            for (int i = 0; i < board.Length; i++) board[i] = SignPuzzleState.Empty;
        }

        private void HandleOwnerChanged(ulong _, ulong __) => StateChanged?.Invoke();
        private void HandleCompletedChanged(bool _, bool __) => StateChanged?.Invoke();
        private void HandleVariantChanged(byte _, byte __) => BoardChanged?.Invoke();
        private void HandleSlotsChanged(NetworkListEvent<byte> _) => BoardChanged?.Invoke();
    }
}
