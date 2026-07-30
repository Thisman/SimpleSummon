using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SimpleSummon.Network
{
    public enum SummonRitualState : byte
    {
        Available,
        Claimed,
        Finished
    }

    [RequireComponent(typeof(NetworkObject))]
    public sealed class NetworkSummonRitual : NetworkBehaviour
    {
        private const ulong NoOwner = ulong.MaxValue;
        private const int MaximumBatchSize = 32;

        private readonly NetworkVariable<SummonRitualState> state =
            new(SummonRitualState.Available);
        private readonly NetworkVariable<ulong> drawingClientId = new(NoOwner);
        private readonly NetworkList<NetworkSummonPoint> points = new();
        private readonly List<NetworkSummonPoint> offlinePoints = new();
        private SummonRitualState offlineState;
        private ulong offlineDrawingClientId = NoOwner;

        public event Action StateChanged;
        public event Action DrawingChanged;

        public SummonRitualState State => IsSpawned ? state.Value : offlineState;
        public ulong DrawingClientId =>
            IsSpawned ? drawingClientId.Value : offlineDrawingClientId;
        public int PointCount => IsSpawned ? points.Count : offlinePoints.Count;

        public override void OnNetworkSpawn()
        {
            state.OnValueChanged += HandleStateChanged;
            drawingClientId.OnValueChanged += HandleOwnerChanged;
            points.OnListChanged += HandlePointsChanged;

            if (IsServer)
            {
                NetworkManager.OnClientDisconnectCallback += HandleClientDisconnected;
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

        public NetworkSummonPoint GetPoint(int index)
        {
            return IsSpawned ? points[index] : offlinePoints[index];
        }

        public void RequestClaim()
        {
            if (!IsSpawned)
            {
                if (offlineState != SummonRitualState.Available)
                {
                    return;
                }

                offlineDrawingClientId = 0;
                offlineState = SummonRitualState.Claimed;
                StateChanged?.Invoke();
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
                AppendValidatedPoints(submittedPoints);
                return;
            }

            SubmitPointsRpc(submittedPoints);
        }

        public void Release()
        {
            if (!IsSpawned)
            {
                if (offlineState == SummonRitualState.Claimed)
                {
                    offlineDrawingClientId = NoOwner;
                    offlineState = SummonRitualState.Available;
                    StateChanged?.Invoke();
                }
                return;
            }

            ReleaseRpc();
        }

        public void Finish()
        {
            if (!IsSpawned)
            {
                if (offlineState == SummonRitualState.Claimed &&
                    offlinePoints.Count > 0)
                {
                    offlineDrawingClientId = NoOwner;
                    offlineState = SummonRitualState.Finished;
                    StateChanged?.Invoke();
                }
                return;
            }

            FinishRpc();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void ClaimRpc(RpcParams rpcParams = default)
        {
            ClaimOnServer(rpcParams.Receive.SenderClientId);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SubmitPointsRpc(
            NetworkSummonPoint[] submittedPoints,
            RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != drawingClientId.Value ||
                state.Value != SummonRitualState.Claimed)
            {
                return;
            }

            AppendValidatedPoints(submittedPoints);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void ReleaseRpc(RpcParams rpcParams = default)
        {
            ReleaseOnServer(rpcParams.Receive.SenderClientId);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void FinishRpc(RpcParams rpcParams = default)
        {
            FinishOnServer(rpcParams.Receive.SenderClientId);
        }

        private void ClaimOnServer(ulong clientId)
        {
            if (state.Value != SummonRitualState.Available)
            {
                return;
            }

            drawingClientId.Value = clientId;
            state.Value = SummonRitualState.Claimed;
        }

        private void AppendValidatedPoints(NetworkSummonPoint[] submittedPoints)
        {
            int count = Mathf.Min(submittedPoints.Length, MaximumBatchSize);
            for (int i = 0; i < count; i++)
            {
                NetworkSummonPoint point = submittedPoints[i];
                if (!float.IsFinite(point.Position.x) ||
                    !float.IsFinite(point.Position.y))
                {
                    continue;
                }

                point.Position = new Vector2(
                    Mathf.Clamp01(point.Position.x),
                    Mathf.Clamp01(point.Position.y));
                if (IsSpawned)
                {
                    points.Add(point);
                }
                else
                {
                    offlinePoints.Add(point);
                }
            }

            if (!IsSpawned)
            {
                DrawingChanged?.Invoke();
            }
        }

        private void ReleaseOnServer(ulong clientId)
        {
            if (state.Value != SummonRitualState.Claimed ||
                drawingClientId.Value != clientId)
            {
                return;
            }

            drawingClientId.Value = NoOwner;
            state.Value = SummonRitualState.Available;
        }

        private void FinishOnServer(ulong clientId)
        {
            if (state.Value != SummonRitualState.Claimed ||
                drawingClientId.Value != clientId ||
                points.Count == 0)
            {
                return;
            }

            drawingClientId.Value = NoOwner;
            state.Value = SummonRitualState.Finished;
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            ReleaseOnServer(clientId);
        }

        private void HandleStateChanged(SummonRitualState _, SummonRitualState __)
        {
            StateChanged?.Invoke();
        }

        private void HandleOwnerChanged(ulong _, ulong __)
        {
            StateChanged?.Invoke();
        }

        private void HandlePointsChanged(NetworkListEvent<NetworkSummonPoint> _)
        {
            DrawingChanged?.Invoke();
        }
    }
}
