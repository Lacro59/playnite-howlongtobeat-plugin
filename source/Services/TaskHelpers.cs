using Playnite.SDK;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HowLongToBeat.Services
{
    internal static class TaskHelpers
    {
        public static void FireAndForget(Task task, string context, ILogger logger = null)
        {
            try
            {
                if (task == null) return;

                task.ContinueWith(t =>
                {
                    try
                    {
                        if (t.Exception != null)
                        {
                            try
                            {
                                if (logger != null)
                                {
                                    logger.Warn(t.Exception, $"HLTB: FireAndForget faulted ({context})");
                                }
                                else
                                {
                                    CommonPluginsShared.Common.LogError(t.Exception, false);
                                }
                            }
                            catch { }
                        }
                    }
                    catch { }
                }, TaskContinuationOptions.OnlyOnFaulted);
            }
            catch { }
        }

        // Internal helper to run a Task-returning operation with timeout logic shared between overloads.
        private static (bool completed, T result, Exception exception) RunSyncCore<T>(Func<CancellationToken, Task<T>> taskFactory, int timeoutMs, CancellationToken externalToken, ILogger logger)
        {
            var ctsTimeout = new CancellationTokenSource();
            var linked = CancellationTokenSource.CreateLinkedTokenSource(ctsTimeout.Token, externalToken);

            var task = Task.Run(() => taskFactory(linked.Token), linked.Token);
            var delayTask = Task.Delay(timeoutMs, linked.Token);

            try
            {
                // Cancel the timeout token as soon as the main task completes so disposal doesn't wait for the full timeout.
                task.ContinueWith(_ =>
                {
                    try { ctsTimeout.Cancel(); } catch { }
                }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

                // Observe faults to avoid unobserved exception warnings, and dispose CTSes when either completes.
                Task.WhenAll(task.ContinueWith(t => { var _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted), delayTask)
                    .ContinueWith(_ =>
                    {
                        try { ctsTimeout.Dispose(); } catch { }
                        try { linked.Dispose(); } catch { }
                    }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
            catch { }

            try
            {
                var completed = Task.WhenAny(task, delayTask).ConfigureAwait(false).GetAwaiter().GetResult();
                if (completed == task)
                {
                    try
                    {
                        var result = task.GetAwaiter().GetResult();
                        return (true, result, null);
                    }
                    catch (Exception ex)
                    {
                        // Let caller decide how to handle the exception.
                        return (true, default(T), ex);
                    }
                }
                else
                {
                    if (delayTask.IsCanceled || linked.Token.IsCancellationRequested)
                    {
                        try { ctsTimeout.Cancel(); } catch { }
                        try { task.ContinueWith(t => { var _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted); } catch { }
                        return (false, default(T), null);
                    }

                    try { ctsTimeout.Cancel(); } catch { }
                    try { logger?.Warn($"Operation timed out after {timeoutMs}ms"); } catch { }
                    try { task.ContinueWith(t => { var _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted); } catch { }
                    return (false, default(T), null);
                }
            }
            catch (Exception ex)
            {
                try { CommonPluginsShared.Common.LogError(ex, false, true, "TaskHelpers"); } catch { }
                return (false, default(T), ex);
            }
        }

        public static T RunSyncWithTimeout<T>(Func<CancellationToken, Task<T>> taskFactory, int timeoutMs = 15000, CancellationToken externalToken = default, ILogger logger = null)
        {
            var result = RunSyncCore(taskFactory, timeoutMs, externalToken, logger);
            if (result.exception != null)
            {
                CommonPluginsShared.Common.LogError(result.exception, false);
                return default;
            }

            return result.result;
        }

        public static bool TryRunSyncWithTimeout<T>(Func<CancellationToken, Task<T>> taskFactory, out T result, int timeoutMs = 15000, CancellationToken externalToken = default, ILogger logger = null)
        {
            result = default;
            var core = RunSyncCore(taskFactory, timeoutMs, externalToken, logger);
            if (core.exception != null)
            {
                CommonPluginsShared.Common.LogError(core.exception, false);
                return false;
            }

            if (!core.completed)
            {
                result = default;
                return false;
            }

            result = core.result;
            return true;
        }

        // Overload to allow type inference when caller provides a Func<Task<T>>.
        public static T RunSyncWithTimeout<T>(Func<Task<T>> taskFactory, int timeoutMs = 15000, ILogger logger = null)
        {
            return RunSyncWithTimeout(ct => taskFactory(), timeoutMs, CancellationToken.None, logger);
        }
    }
}
