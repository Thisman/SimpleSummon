using System;
using SimpleSummon.Domain;
using SimpleSummon.Application;
using Unity.Netcode;
using UnityEngine;

namespace SimpleSummon.Network
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class NetworkSignBuilder : NetworkBehaviour
    {
        public const ulong NoOwner = ulong.MaxValue;

        [SerializeField] private NetworkQuestState questState;
        [SerializeField, Min(1)] private int signVariantCount = 3;

        private readonly NetworkList<byte> slots = new();
        private readonly NetworkVariable<ulong> controllingClientId = new(NoOwner);
        private readonly NetworkVariable<bool> completed = new();
        private readonly NetworkVariable<byte> signVariant = new();
        private readonly SignBuilderModel offlineModel = new();
        private ulong offlineControllingClientId = NoOwner;
        private byte offlineSignVariant;
        private byte pendingFragmentMask;
        private SignBuilderService service;
        private NetworkSignBuilderBoard boardState;

        public event Action StateChanged;
        public event Action BoardChanged;
        public ulong ControllingClientId => IsSpawned ? controllingClientId.Value : offlineControllingClientId;
        public bool Completed => boardState.GetCompleted(IsSpawned);
        public int SignVariant => IsSpawned ? signVariant.Value : offlineSignVariant;
        public bool HasFragments => GetFragmentCount() > 0;

        private void Awake()
        {
            service = new SignBuilderService(new UnityRandomSource());
            boardState = new NetworkSignBuilderBoard(slots, completed, offlineModel);
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
                    for (int i = 0; i < SignBuilderState.SlotCount; i++) slots.Add(SignBuilderState.Empty);
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

        public byte GetSlot(int index) => boardState.GetSlot(IsSpawned, index);

        public void CopySlots(byte[] destination)
        {
            boardState.Copy(IsSpawned, destination);
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

        public void RequestMove(int sourceSlot, SignBuilderMoveDirection direction)
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
        private void MoveRpc(byte sourceSlot, SignBuilderMoveDirection direction, RpcParams rpcParams = default) =>
            MoveOnServer(rpcParams.Receive.SenderClientId, sourceSlot, direction);

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void ReleaseRpc(RpcParams rpcParams = default) => ReleaseOnServer(rpcParams.Receive.SenderClientId);

        private void MoveOnServer(ulong clientId, int sourceSlot, SignBuilderMoveDirection direction)
        {
            if (controllingClientId.Value != clientId) return;
            byte[] board = boardState.Read(IsSpawned);
            if (service.Move(board, sourceSlot, direction)) PublishBoard(board);
        }

        private void MoveOffline(int sourceSlot, SignBuilderMoveDirection direction)
        {
            if (offlineControllingClientId == NoOwner) return;
            byte[] board = boardState.Read(IsSpawned);
            if (!service.Move(board, sourceSlot, direction)) return;
            PublishBoard(board);
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
            byte[] board = boardState.Read(IsSpawned);
            bool changed = service.AddFragments(board, requestedMask);
            if (changed) PublishBoard(board);
        }

        private void PublishBoard(byte[] board)
        {
            boardState.Publish(IsSpawned, board);
            if (!IsSpawned)
            {
                BoardChanged?.Invoke();
                StateChanged?.Invoke();
            }
        }

        private int GetFragmentCount()
        {
            return boardState.CountFragments(IsSpawned);
        }

        private void HandleOwnerChanged(ulong _, ulong __) => StateChanged?.Invoke();
        private void HandleCompletedChanged(bool _, bool __) => StateChanged?.Invoke();
        private void HandleVariantChanged(byte _, byte __) => BoardChanged?.Invoke();
        private void HandleSlotsChanged(NetworkListEvent<byte> _) => BoardChanged?.Invoke();
    }
}
