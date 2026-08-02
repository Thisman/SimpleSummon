using Unity.Netcode;
using UnityEngine;

namespace SimpleSummon.Network
{
    public sealed class NetworkPlayerInput : NetworkBehaviour
    {
        private const double InputSendRate = 30d;
        private readonly NetworkRateLimiter sendLimiter = new(InputSendRate);
        private Vector3 serverMoveDirection;
        private bool serverJumpRequested;
        private bool serverAttackRequested;
        private Vector3 pendingMoveDirection;
        private bool pendingJumpRequested;
        private bool pendingAttackRequested;

        public void Submit(Vector3 moveDirection, bool jumpRequested, bool attackRequested)
        {
            if (!IsSpawned)
            {
                SetServerInput(moveDirection, jumpRequested, attackRequested);
                return;
            }

            if (!IsOwner)
            {
                return;
            }

            if (IsServer)
            {
                SetServerInput(moveDirection, jumpRequested, attackRequested);
                return;
            }

            pendingMoveDirection = moveDirection;
            pendingJumpRequested |= jumpRequested;
            pendingAttackRequested |= attackRequested;
            NetworkRequestContext request = new(OwnerClientId, Time.unscaledTimeAsDouble);
            if (!sendLimiter.TryAcquire(request))
            {
                return;
            }

            SubmitRpc(
                pendingMoveDirection,
                pendingJumpRequested,
                pendingAttackRequested);
            pendingJumpRequested = false;
            pendingAttackRequested = false;
        }

        public void Read(
            out Vector3 moveDirection,
            out bool jumpRequested,
            out bool attackRequested)
        {
            moveDirection = serverMoveDirection;
            jumpRequested = serverJumpRequested;
            attackRequested = serverAttackRequested;
            serverJumpRequested = false;
            serverAttackRequested = false;
        }

        [Rpc(SendTo.Server)]
        private void SubmitRpc(
            Vector3 moveDirection,
            bool jumpRequested,
            bool attackRequested)
        {
            SetServerInput(moveDirection, jumpRequested, attackRequested);
        }

        private void SetServerInput(
            Vector3 moveDirection,
            bool jumpRequested,
            bool attackRequested)
        {
            serverMoveDirection = Vector3.ClampMagnitude(moveDirection, 1f);
            serverJumpRequested |= jumpRequested;
            serverAttackRequested |= attackRequested;
        }
    }
}
