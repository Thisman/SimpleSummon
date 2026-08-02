using System;
using System.Collections.Generic;
using SimpleSummon.Application;
using SimpleSummon.Domain;
using Unity.Netcode;
using UnityEngine;

namespace SimpleSummon.Network
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class NetworkSummonRitual : NetworkBehaviour
    {
        private const ulong NoOwner = ulong.MaxValue;
        private const ulong OfflineActorId = 0;

        [SerializeField] private NetworkQuestState questState;

        private readonly NetworkVariable<SummonRitualState> state =
            new(SummonRitualState.Available);
        private readonly NetworkVariable<ulong> drawingClientId = new(NoOwner);
        private readonly NetworkList<NetworkSummonPoint> points = new();
        private readonly NetworkRateLimiter submitLimiter = new(30d);
        private readonly NetworkRateLimiter eraseLimiter = new(30d);
        private readonly SummonRitualModel model = new();
        private SummonRitualService service;
        private NetworkSummonDrawingState drawingState;

        public event Action StateChanged;
        public event Action DrawingChanged;

        public SummonRitualState State => IsSpawned ? state.Value : model.State;
        public ulong DrawingClientId => IsSpawned
            ? drawingClientId.Value
            : model.OwnerId.HasValue ? (ulong)model.OwnerId.Value : NoOwner;
        public int PointCount => drawingState.Count(IsSpawned);

        private void Awake()
        {
            service = new SummonRitualService(model);
            drawingState = new NetworkSummonDrawingState(model, points);
        }

        public override void OnNetworkSpawn()
        {
            state.OnValueChanged += HandleStateChanged;
            drawingClientId.OnValueChanged += HandleOwnerChanged;
            points.OnListChanged += HandlePointsChanged;

            if (IsServer)
            {
                NetworkManager.OnClientDisconnectCallback += HandleClientDisconnected;
                PublishState();
            }

            StateChanged?.Invoke();
            DrawingChanged?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            state.OnValueChanged -= HandleStateChanged;
            drawingClientId.OnValueChanged -= HandleOwnerChanged;
            points.OnListChanged -= HandlePointsChanged;

            if (NetworkManager != null && IsServer)
            {
                NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
            }
        }

        public NetworkSummonPoint GetPoint(int index) => drawingState.Get(IsSpawned, index);

        public void RequestClaim()
        {
            if (!IsSpawned)
            {
                if (service.Claim(OfflineActorId))
                {
                    StateChanged?.Invoke();
                }
                return;
            }

            if (IsServer)
            {
                ClaimOnServer(NetworkManager.LocalClientId);
            }
            else
            {
                ClaimRpc();
            }
        }

        public void SubmitPoints(NetworkSummonPoint[] submittedPoints)
        {
            if (submittedPoints == null || submittedPoints.Length == 0)
            {
                return;
            }

            if (!IsSpawned)
            {
                if (service.Submit(NetworkSummonPointMapper.ToCommand(
                        OfflineActorId,
                        submittedPoints)))
                {
                    DrawingChanged?.Invoke();
                }
                return;
            }

            if (IsServer)
            {
                SubmitOnServer(NetworkManager.LocalClientId, submittedPoints);
            }
            else
            {
                SubmitPointsRpc(submittedPoints);
            }
        }

        public void Release()
        {
            if (!IsSpawned)
            {
                if (service.Release(OfflineActorId))
                {
                    StateChanged?.Invoke();
                }
                return;
            }

            if (IsServer)
            {
                ReleaseOnServer(NetworkManager.LocalClientId);
            }
            else
            {
                ReleaseRpc();
            }
        }

        public void Erase(Vector2 position, float radius)
        {
            if (!IsSpawned)
            {
                if (service.Erase(NetworkSummonPointMapper.ToEraseCommand(
                        OfflineActorId,
                        position,
                        radius)))
                {
                    DrawingChanged?.Invoke();
                }
                return;
            }

            if (IsServer)
            {
                EraseOnServer(NetworkManager.LocalClientId, position, radius);
            }
            else
            {
                EraseRpc(position, radius);
            }
        }

        public void Finish()
        {
            if (!IsSpawned)
            {
                if (service.Finish(OfflineActorId))
                {
                    questState.RecordSignDrawn();
                    StateChanged?.Invoke();
                }
                return;
            }

            if (IsServer)
            {
                FinishOnServer(NetworkManager.LocalClientId);
            }
            else
            {
                FinishRpc();
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void ClaimRpc(RpcParams rpcParams = default) =>
            ClaimOnServer(rpcParams.Receive.SenderClientId);

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SubmitPointsRpc(
            NetworkSummonPoint[] submittedPoints,
            RpcParams rpcParams = default) =>
            SubmitOnServer(rpcParams.Receive.SenderClientId, submittedPoints);

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void ReleaseRpc(RpcParams rpcParams = default) =>
            ReleaseOnServer(rpcParams.Receive.SenderClientId);

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void EraseRpc(
            Vector2 position,
            float radius,
            RpcParams rpcParams = default) =>
            EraseOnServer(rpcParams.Receive.SenderClientId, position, radius);

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void FinishRpc(RpcParams rpcParams = default) =>
            FinishOnServer(rpcParams.Receive.SenderClientId);

        private void ClaimOnServer(ulong clientId)
        {
            if (service.Claim(clientId))
            {
                PublishState();
            }
        }

        private void SubmitOnServer(
            ulong clientId,
            NetworkSummonPoint[] submittedPoints)
        {
            NetworkRequestContext request = new(clientId, Time.unscaledTimeAsDouble);
            if (!submitLimiter.TryAcquire(request))
            {
                return;
            }

            int previousCount = model.Points.Count;
            if (!service.Submit(NetworkSummonPointMapper.ToCommand(
                    clientId,
                    submittedPoints)))
            {
                return;
            }

            drawingState.PublishAppended(previousCount);
        }

        private void EraseOnServer(ulong clientId, Vector2 position, float radius)
        {
            NetworkRequestContext request = new(clientId, Time.unscaledTimeAsDouble);
            if (!eraseLimiter.TryAcquire(request) ||
                !service.Erase(NetworkSummonPointMapper.ToEraseCommand(
                    clientId,
                    position,
                    radius)))
            {
                return;
            }

            drawingState.PublishAll();
        }

        private void ReleaseOnServer(ulong clientId)
        {
            if (service.Release(clientId))
            {
                PublishState();
            }
        }

        private void FinishOnServer(ulong clientId)
        {
            if (!service.Finish(clientId))
            {
                return;
            }

            PublishState();
            questState.RecordSignDrawn();
        }

        private void PublishState()
        {
            state.Value = model.State;
            drawingClientId.Value = model.OwnerId.HasValue
                ? (ulong)model.OwnerId.Value
                : NoOwner;
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            submitLimiter.Forget(clientId);
            eraseLimiter.Forget(clientId);
            ReleaseOnServer(clientId);
        }

        private void HandleStateChanged(SummonRitualState _, SummonRitualState __) =>
            StateChanged?.Invoke();

        private void HandleOwnerChanged(ulong _, ulong __) => StateChanged?.Invoke();
        private void HandlePointsChanged(NetworkListEvent<NetworkSummonPoint> _) =>
            DrawingChanged?.Invoke();
    }
}
