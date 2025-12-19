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

        // Helper to run a Task-returning operation with a bounded synchronous wait to avoid indefinite UI blocking.
        public static T RunSyncWithTimeout<T>(Func<CancellationToken, Task<T>> taskFactory, int timeoutMs = 15000, CancellationToken externalToken = default, ILogger logger = null)
        {
            try
            {
                var ctsTimeout = new CancellationTokenSource();
                var linked = CancellationTokenSource.CreateLinkedTokenSource(ctsTimeout.Token, externalToken);

                var task = Task.Run(() => taskFactory(linked.Token), linked.Token);
                var delayTask = Task.Delay(timeoutMs, linked.Token);

                try
                {
                    Task.WhenAll(task.ContinueWith(t => { var _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted), delayTask)
                        .ContinueWith(_ =>
                        {
                            try { ctsTimeout.Dispose(); } catch { }
                            try { linked.Dispose(); } catch { }
                        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                }
                catch { }

                var completed = Task.WhenAny(task, delayTask).ConfigureAwait(false).GetAwaiter().GetResult();
                if (completed == task)
                {
                    try { return task.GetAwaiter().GetResult(); } catch (Exception ex) { CommonPluginsShared.Common.LogError(ex, false); return default; }
                }
                else
                {
                    if (delayTask.IsCanceled || linked.Token.IsCancellationRequested)
                    {
                        try { ctsTimeout.Cancel(); } catch { }
                        try { task.ContinueWith(t => { var _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted); } catch { }
                        return default;
                    }

                    try { ctsTimeout.Cancel(); } catch { }
                    try { logger?.Warn($"Operation timed out after {timeoutMs}ms"); } catch { }
                    try { task.ContinueWith(t => { var _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted); } catch { }
                    return default;
                }
            }
            catch (Exception ex)
            {
                try { CommonPluginsShared.Common.LogError(ex, false, true, "TaskHelpers"); } catch { }
                return default;
            }
        }

        public static bool TryRunSyncWithTimeout<T>(Func<CancellationToken, Task<T>> taskFactory, out T result, int timeoutMs = 15000, CancellationToken externalToken = default, ILogger logger = null)
        {
            result = default;
            try
            {
                var ctsTimeout = new CancellationTokenSource();
                var linked = CancellationTokenSource.CreateLinkedTokenSource(ctsTimeout.Token, externalToken);

                var task = Task.Run(() => taskFactory(linked.Token), linked.Token);
                var delayTask = Task.Delay(timeoutMs, linked.Token);

                try
                {
                    Task.WhenAll(task.ContinueWith(t => { var _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted), delayTask)
                        .ContinueWith(_ =>
                        {
                            try { ctsTimeout.Dispose(); } catch { }
                            try { linked.Dispose(); } catch { }
                        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                }
                catch { }

                var completed = Task.WhenAny(task, delayTask).ConfigureAwait(false).GetAwaiter().GetResult();
                if (completed == task)
                {
                    try
                    {
                        result = task.GetAwaiter().GetResult();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        CommonPluginsShared.Common.LogError(ex, false);
                        return false;
                    }
                }
                else
                {
                    if (delayTask.IsCanceled || linked.Token.IsCancellationRequested)
                    {
                        try { ctsTimeout.Cancel(); } catch { }
                        try { task.ContinueWith(t => { var _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted); } catch { }
                        return false;
                    }

                    try { ctsTimeout.Cancel(); } catch { }
                    try { logger?.Warn($"Operation timed out after {timeoutMs}ms"); } catch { }
                    try { task.ContinueWith(t => { var _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted); } catch { }
                    return false;
                }
            }
            catch (Exception ex)
            {
                try { CommonPluginsShared.Common.LogError(ex, false, true, "TaskHelpers"); } catch { }
                result = default;
                return false;
            }
        }

        // Overload to allow type inference when caller provides a Func<Task<T>>.
        public static T RunSyncWithTimeout<T>(Func<Task<T>> taskFactory, int timeoutMs = 15000, ILogger logger = null)
        {
            return RunSyncWithTimeout(ct => taskFactory(), timeoutMs, CancellationToken.None, logger);
        }
    }
}
