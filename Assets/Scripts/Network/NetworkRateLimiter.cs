using System.Collections.Generic;

namespace SimpleSummon.Network
{
    public sealed class NetworkRateLimiter
    {
        private readonly double interval;
        private readonly Dictionary<ulong, double> nextAllowedTimes = new();

        public NetworkRateLimiter(double requestsPerSecond)
        {
            if (!double.IsFinite(requestsPerSecond) || requestsPerSecond <= 0d)
            {
                throw new System.ArgumentOutOfRangeException(nameof(requestsPerSecond));
            }

            interval = 1d / requestsPerSecond;
        }

        public bool TryAcquire(NetworkRequestContext context)
        {
            if (!double.IsFinite(context.ServerTime))
            {
                throw new System.ArgumentOutOfRangeException(nameof(context));
            }

            if (nextAllowedTimes.TryGetValue(
                    context.SenderClientId,
                    out double nextAllowedTime) &&
                context.ServerTime < nextAllowedTime)
            {
                return false;
            }

            nextAllowedTimes[context.SenderClientId] = context.ServerTime + interval;
            return true;
        }

        public void Forget(ulong clientId)
        {
            nextAllowedTimes.Remove(clientId);
        }
    }
}
