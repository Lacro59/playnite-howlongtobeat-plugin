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
        private CancellationTokenSource cts;
        private Task adjustTask;

        public AdaptiveConcurrencyController(int initial, int min, int max, TimeSpan? interval = null)
        {
            targetConcurrency = Math.Max(min, Math.Min(max, initial));
            minConcurrency = min;
            maxConcurrency = max;
            adjustInterval = interval ?? TimeSpan.FromSeconds(2);
            cts = new CancellationTokenSource();
            adjustTask = Task.Run(() => AdjustLoop(cts.Token));
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
            catch (TaskCanceledException) { }
        }

        private void AdjustOnce()
        {
            double latency;
            double error;
            lock (sync)
            {
                latency = emaLatencyMs < 0 ? 0 : emaLatencyMs;
                error = emaError;
            }

            // - If error rate high (>0.15) or latency high (>2000ms): decrease concurrency by 25%
            // - If latency modest (<400ms) and error low (<0.05): increase concurrency by 1 (up to max)
            // - Otherwise keep stable
            int current = TargetConcurrency;
            int newTarget = current;

            if (error > 0.15 || latency > 2000)
            {
                newTarget = Math.Max(minConcurrency, (int)Math.Ceiling(current * 0.75));
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
            try
            {
                cts?.Cancel();
                adjustTask?.Wait(500);
            }
            catch { }
            finally
            {
                cts?.Dispose();
            }
        }
    }
}
