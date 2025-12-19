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
            if (task == null) return;

            try
            {
                task.ContinueWith(t =>
                {
                    var ex = t.Exception?.GetBaseException();
                    if (ex == null) return;

                    try
                    {
                        if (logger != null)
                        {
                            logger.Warn(ex, $"HLTB: FireAndForget faulted ({context})");
                        }
                        else
                        {
                            CommonPluginsShared.Common.LogError(ex, false);
                        }
                    }
                    catch { }
                }, TaskContinuationOptions.OnlyOnFaulted);
            }
            catch (Exception ex)
            {
                try { CommonPluginsShared.Common.LogError(ex, false, true, "TaskHelpers"); } catch { }
            }
        }

        // Internal helper to run a Task-returning operation with timeout logic shared between overloads.
        private static (bool completed, T result, Exception exception) RunSyncCore<T>(Func<CancellationToken, Task<T>> taskFactory, int timeoutMs, CancellationToken externalToken, ILogger logger)
        {
            CancellationTokenSource ctsTimeout = null;
            CancellationTokenSource linked = null;

            try
            {
                ctsTimeout = new CancellationTokenSource();
                linked = CancellationTokenSource.CreateLinkedTokenSource(ctsTimeout.Token, externalToken);

                Task<T> task;
                try
                {
                    // taskFactory already returns a Task; no need for Task.Run (avoids extra thread-pool scheduling).
                    task = taskFactory(linked.Token);
                }
                catch (OperationCanceledException) when (linked.IsCancellationRequested)
                {
                    task = Task.FromCanceled<T>(linked.Token);
                }
                catch (Exception ex)
                {
                    task = Task.FromException<T>(ex);
                }

                var delayTask = Task.Delay(timeoutMs, linked.Token);

                // Cancel the timeout token as soon as the main task completes so delayTask can be canceled early.
                try
                {
                    _ = task.ContinueWith(
                        _ =>
                        {
                            try { ctsTimeout.Cancel(); } catch { }
                        },
                        CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);
                }
                catch { }

                try
                {
                    var completed = Task.WhenAny(task, delayTask).ConfigureAwait(false).GetAwaiter().GetResult();
                    if (completed == task)
                    {
                        try { ctsTimeout.Cancel(); } catch { }
                        try
                        {
                            // Await/observe the task result (or exception) deterministically.
                            var result = task.GetAwaiter().GetResult();
                            return (true, result, null);
                        }
                        catch (Exception ex)
                        {
                            return (true, default(T), ex);
                        }
                    }

                    // delayTask completed first: timeout or cancellation.
                    if (delayTask.IsCanceled || linked.Token.IsCancellationRequested)
                    {
                        try { ctsTimeout.Cancel(); } catch { }
                        // Observe faults to avoid unobserved exception warnings.
                        try
                        {
                            _ = task.ContinueWith(t => { var _ = t.Exception; },
                                CancellationToken.None,
                                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                                TaskScheduler.Default);
                        }
                        catch { }
                        return (false, default(T), null);
                    }

                    try { ctsTimeout.Cancel(); } catch { }
                    try { logger?.Warn($"Operation timed out after {timeoutMs}ms"); } catch { }

                    // Observe faults to avoid unobserved exception warnings.
                    try
                    {
                        _ = task.ContinueWith(t => { var _ = t.Exception; },
                            CancellationToken.None,
                            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                            TaskScheduler.Default);
                    }
                    catch { }

                    return (false, default(T), null);
                }
                catch (Exception ex)
                {
                    try { CommonPluginsShared.Common.LogError(ex, false, true, "TaskHelpers"); } catch { }
                    return (false, default(T), ex);
                }
            }
            finally
            {
                // Deterministic cleanup to avoid CTS leaks even if setup/continuations throw.
                try { ctsTimeout?.Dispose(); } catch { }
                try { linked?.Dispose(); } catch { }
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
