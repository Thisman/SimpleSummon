using System;
using System.Collections.Generic;
using SimpleSummon.Application;
using Unity.Netcode;
using UnityEngine;
using NumericsVector2 = System.Numerics.Vector2;

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
        private const float MinimumPointDistance = 0.001f;
        private const float MinimumBatchInterval = 1f / 30f;

        [SerializeField] private NetworkQuestState questState;

        private readonly NetworkVariable<SummonRitualState> state =
            new(SummonRitualState.Available);
        private readonly NetworkVariable<ulong> drawingClientId = new(NoOwner);
        private readonly NetworkList<NetworkSummonPoint> points = new();
        private readonly List<NetworkSummonPoint> offlinePoints = new();
        private SummonRitualState offlineState;
        private ulong offlineDrawingClientId = NoOwner;
        private float nextBatchTime;

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

        public void Erase(Vector2 position, float radius)
        {
            radius = Mathf.Clamp(radius, 0.005f, 0.2f);
            if (!IsSpawned)
            {
                ErasePoints(offlinePoints, position, radius);
                DrawingChanged?.Invoke();
                return;
            }

            EraseRpc(position, radius);
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
                    questState.RecordSignDrawn();
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
                state.Value != SummonRitualState.Claimed ||
                Time.unscaledTime < nextBatchTime)
            {
                return;
            }

            nextBatchTime = Time.unscaledTime + MinimumBatchInterval;
            AppendValidatedPoints(submittedPoints);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void ReleaseRpc(RpcParams rpcParams = default)
        {
            ReleaseOnServer(rpcParams.Receive.SenderClientId);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void EraseRpc(
            Vector2 position,
            float radius,
            RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != drawingClientId.Value ||
                state.Value != SummonRitualState.Claimed ||
                Time.unscaledTime < nextBatchTime)
            {
                return;
            }

            nextBatchTime = Time.unscaledTime + MinimumBatchInterval;
            EraseNetworkPoints(position, Mathf.Clamp(radius, 0.005f, 0.2f));
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
                NumericsVector2? previousPosition =
                    TryGetLastPoint(out NetworkSummonPoint previousPoint)
                        ? new NumericsVector2(
                            previousPoint.Position.x,
                            previousPoint.Position.y)
                        : null;
                if (!SummonPointValidationService.TryValidate(
                        previousPosition,
                        new NumericsVector2(point.Position.x, point.Position.y),
                        point.StartsStroke,
                        MinimumPointDistance,
                        out NumericsVector2 validatedPosition))
                {
                    continue;
                }

                point.Position = new Vector2(
                    validatedPosition.X,
                    validatedPosition.Y);
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

        private bool TryGetLastPoint(out NetworkSummonPoint point)
        {
            int count = IsSpawned ? points.Count : offlinePoints.Count;
            if (count == 0)
            {
                point = default;
                return false;
            }

            point = IsSpawned ? points[count - 1] : offlinePoints[count - 1];
            return true;
        }

        private static void ErasePoints(
            IList<NetworkSummonPoint> targetPoints,
            Vector2 position,
            float radius)
        {
            float squareRadius = radius * radius;
            bool removedPrevious = false;
            for (int i = targetPoints.Count - 1; i >= 0; i--)
            {
                if ((targetPoints[i].Position - position).sqrMagnitude <= squareRadius)
                {
                    targetPoints.RemoveAt(i);
                    removedPrevious = true;
                }
                else if (removedPrevious)
                {
                    if (i + 1 < targetPoints.Count)
                    {
                        NetworkSummonPoint next = targetPoints[i + 1];
                        next.StartsStroke = true;
                        targetPoints[i + 1] = next;
                    }
                    removedPrevious = false;
                }
            }

            if (targetPoints.Count > 0 && removedPrevious)
            {
                NetworkSummonPoint first = targetPoints[0];
                first.StartsStroke = true;
                targetPoints[0] = first;
            }
        }

        private void EraseNetworkPoints(Vector2 position, float radius)
        {
            List<NetworkSummonPoint> remainingPoints = new(points.Count);
            for (int i = 0; i < points.Count; i++)
            {
                remainingPoints.Add(points[i]);
            }

            ErasePoints(remainingPoints, position, radius);
            if (remainingPoints.Count == points.Count)
            {
                return;
            }

            points.Clear();
            foreach (NetworkSummonPoint point in remainingPoints)
            {
                points.Add(point);
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
            questState.RecordSignDrawn();
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
