using System;

namespace SimpleSummon.Domain
{
    public sealed class TorchModel
    {
        public TorchModel(
            float fadeDelay,
            float recoveryDelay,
            float fadeRate,
            float recoveryRate)
        {
            ValidateNonNegative(fadeDelay, nameof(fadeDelay));
            ValidateNonNegative(recoveryDelay, nameof(recoveryDelay));
            ValidateNonNegative(fadeRate, nameof(fadeRate));
            ValidateNonNegative(recoveryRate, nameof(recoveryRate));

            FadeDelay = fadeDelay;
            RecoveryDelay = recoveryDelay;
            FadeRate = fadeRate;
            RecoveryRate = recoveryRate;
            Reset();
        }

        public float Strength { get; private set; }
        public float FadeDelay { get; }
        public float RecoveryDelay { get; }
        public float FadeRate { get; }
        public float RecoveryRate { get; }
        public TorchBurnPhase Phase { get; private set; }
        public bool IsExtinguished => Strength <= 0f;
        public bool IsAvailable => !HolderId.HasValue;
        public ulong? HolderId { get; private set; }

        public bool TryTake(ulong holderId)
        {
            if (!IsAvailable)
            {
                return false;
            }

            HolderId = holderId;
            ResetFlame();
            return true;
        }

        public bool IsHeldBy(ulong holderId) => HolderId == holderId;

        public void Release()
        {
            HolderId = null;
            ResetFlame();
        }

        public void Tick(bool isMoving, float deltaTime)
        {
            ValidateNonNegative(deltaTime, nameof(deltaTime));
            if (IsExtinguished || deltaTime <= 0f)
            {
                return;
            }

            switch (Phase)
            {
                case TorchBurnPhase.WaitingToFade:
                    TickWaiting(isMoving, deltaTime);
                    break;
                case TorchBurnPhase.Fading:
                    TickFading(isMoving, deltaTime);
                    break;
                case TorchBurnPhase.RecoveryDelay:
                    TickRecoveryDelay(isMoving, deltaTime);
                    break;
                case TorchBurnPhase.Recovering:
                    TickRecovering(isMoving, deltaTime);
                    break;
            }
        }

        public void Reset()
        {
            HolderId = null;
            ResetFlame();
        }

        private void ResetFlame()
        {
            Strength = 100f;
            Phase = TorchBurnPhase.WaitingToFade;
            phaseElapsed = 0f;
        }

        private float phaseElapsed;

        private void TickWaiting(bool isMoving, float deltaTime)
        {
            if (!isMoving)
            {
                phaseElapsed = 0f;
                return;
            }

            phaseElapsed += deltaTime;
            if (phaseElapsed >= FadeDelay)
            {
                float fadeTime = phaseElapsed - FadeDelay;
                Phase = TorchBurnPhase.Fading;
                phaseElapsed = 0f;
                Fade(fadeTime);
            }
        }

        private void TickFading(bool isMoving, float deltaTime)
        {
            if (isMoving)
            {
                Fade(deltaTime);
                return;
            }

            Phase = TorchBurnPhase.RecoveryDelay;
            phaseElapsed = 0f;
            TickRecoveryDelay(false, deltaTime);
        }

        private void TickRecoveryDelay(bool isMoving, float deltaTime)
        {
            Fade(deltaTime);
            if (IsExtinguished)
            {
                return;
            }

            if (isMoving)
            {
                Phase = TorchBurnPhase.Fading;
                phaseElapsed = 0f;
                return;
            }

            phaseElapsed += deltaTime;
            if (phaseElapsed >= RecoveryDelay)
            {
                Phase = TorchBurnPhase.Recovering;
                phaseElapsed = 0f;
            }
        }

        private void TickRecovering(bool isMoving, float deltaTime)
        {
            if (isMoving)
            {
                return;
            }

            Strength = Math.Min(100f, Strength + RecoveryRate * deltaTime);
            if (Strength >= 100f)
            {
                Reset();
            }
        }

        private void Fade(float deltaTime) =>
            Strength = Math.Max(0f, Strength - FadeRate * deltaTime);

        private static void ValidateNonNegative(float value, string parameterName)
        {
            if (!float.IsFinite(value) || value < 0f)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }
    }
}
