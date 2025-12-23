using CommonPluginsShared;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HowLongToBeat.Services
{
    internal class AdaptiveConcurrencyController : IDisposable
    {
        private readonly object sync = new object();
        private readonly int minConcurrency;
        private readonly int maxConcurrency;
        private readonly TimeSpan adjustInterval;

        private double emaLatencyMs = -1;
        private double emaError = 0;
        private readonly double latencyAlpha = 0.2;
        private readonly double errorAlpha = 0.1;

        private int targetConcurrency;
        private readonly CancellationTokenSource cts;
        private readonly Task adjustTask;

        public AdaptiveConcurrencyController(int initial, int min, int max, TimeSpan? interval = null)
        {
            // Validate inputs
             if (min < 1)
             {
                 throw new ArgumentOutOfRangeException(nameof(min), "min must be >= 1");
             }
             
             if (max < 1)
             {
                 throw new ArgumentOutOfRangeException(nameof(max), "max must be >= 1");
             }

             if (min > max)
             {
                 throw new ArgumentException("min must be less than or equal to max", nameof(min));
             }

             if (initial < min || initial > max)
             {
                 throw new ArgumentOutOfRangeException(nameof(initial), $"initial must be within [{min}, {max}]");
             }

             if (interval.HasValue && interval.Value <= TimeSpan.Zero)
             {
                 throw new ArgumentOutOfRangeException(nameof(interval), "interval must be null or greater than TimeSpan.Zero");
             }

             // After validation assign fields
             targetConcurrency = initial;
             minConcurrency = min;
             maxConcurrency = max;
             adjustInterval = interval ?? TimeSpan.FromSeconds(2);
             cts = new CancellationTokenSource();
             adjustTask = Task.Factory.StartNew(() => AdjustLoop(cts.Token),
                 cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
         }

        public int TargetConcurrency
        {
            get
            {
                lock (sync) { return targetConcurrency; }
            }
        }

        public void ReportSample(double latencyMs, bool success)
        {
            lock (sync)
            {
                if (emaLatencyMs < 0) emaLatencyMs = latencyMs;
                else emaLatencyMs = latencyAlpha * latencyMs + (1 - latencyAlpha) * emaLatencyMs;

                double err = success ? 0.0 : 1.0;
                emaError = errorAlpha * err + (1 - errorAlpha) * emaError;
            }
        }

        private async Task AdjustLoop(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(adjustInterval, token).ConfigureAwait(false);
                    try
                    {
                        AdjustOnce();
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false);
                    }
                }
            }
            catch (OperationCanceledException) { }
        }

        private void AdjustOnce()
        {
            double latency;
            double error;
            int current;
            lock (sync)
            {
                if (emaLatencyMs < 0)
                {
                    return;
                }
                latency = emaLatencyMs;
                error = emaError;
                current = targetConcurrency;
            }

            // - If error rate high (>0.15) or latency high (>2000ms): decrease concurrency by 25%
            // - If latency modest (<400ms) and error low (<0.05): increase concurrency by 1 (up to max)
            // - Otherwise keep stable
            int newTarget = current;

            if (error > 0.15 || latency > 2000)
            {
                if (current > minConcurrency)
                {
                    int decreased = (int)Math.Floor(current * 0.75);
                    if (decreased >= current)
                    {
                        decreased = current - 1;
                    }
                    newTarget = Math.Max(minConcurrency, decreased);
                }
                else
                {
                    newTarget = current;
                }
            }
            else if (latency < 400 && error < 0.05)
            {
                newTarget = Math.Min(maxConcurrency, current + 1);
            }

            if (newTarget != current)
            {
                lock (sync)
                {
                    targetConcurrency = newTarget;
                }
                Common.LogDebug(true, $"AdaptiveConcurrency: adjusted target {current} -> {newTarget} (latency={latency:F0}ms, error={error:F2})");
            }
        }

        public void Dispose()
        {
            try { cts?.Cancel(); } catch { }
            try
            {
                var t = adjustTask;
                if (t != null && !t.Wait(0))
                {
                    _ = Task.Run(() => { try { t.Wait(500); } catch { } });
                 }
             }
             catch { }
             finally
             {
                try { cts?.Dispose(); } catch { }
             }
        }
    }
}
