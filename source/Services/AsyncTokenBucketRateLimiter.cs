using System;
using System.Threading;
using System.Threading.Tasks;

namespace HowLongToBeat.Services
{
    /// <summary>
    /// Simple async token bucket limiter.
    /// Limits average request rate while allowing short bursts.
    /// </summary>
    public sealed class AsyncTokenBucketRateLimiter
    {
        private readonly object _syncRoot = new object();
        private readonly double _tokensPerSecond;
        private readonly double _capacity;
        private double _tokens;
        private DateTime _lastRefillUtc;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncTokenBucketRateLimiter"/> class.
        /// </summary>
        /// <param name="tokensPerSecond">Average refill rate in tokens per second.</param>
        /// <param name="burstCapacity">Maximum burst size.</param>
        public AsyncTokenBucketRateLimiter(double tokensPerSecond, int burstCapacity)
        {
            if (tokensPerSecond <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tokensPerSecond));
            }

            if (burstCapacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(burstCapacity));
            }

            _tokensPerSecond = tokensPerSecond;
            _capacity = burstCapacity;
            _tokens = burstCapacity;
            _lastRefillUtc = DateTime.UtcNow;
        }

        /// <summary>
        /// Waits asynchronously until one token can be consumed.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Total wait duration in milliseconds.</returns>
        public async Task<int> WaitAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            int totalWaitMs = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int waitMs = 0;
                lock (_syncRoot)
                {
                    DateTime nowUtc = DateTime.UtcNow;
                    double elapsedSeconds = (nowUtc - _lastRefillUtc).TotalSeconds;
                    if (elapsedSeconds > 0)
                    {
                        _tokens = Math.Min(_capacity, _tokens + (elapsedSeconds * _tokensPerSecond));
                        _lastRefillUtc = nowUtc;
                    }

                    if (_tokens >= 1d)
                    {
                        _tokens -= 1d;
                        return totalWaitMs;
                    }

                    double missingTokens = 1d - _tokens;
                    waitMs = (int)Math.Ceiling((missingTokens / _tokensPerSecond) * 1000d);
                    if (waitMs < 5)
                    {
                        waitMs = 5;
                    }
                }

                totalWaitMs += waitMs;
                await Task.Delay(waitMs, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
