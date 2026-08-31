using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using FuzzySharp;
using HowLongToBeat.Models;
using HowLongToBeat.Models.Api;
using HowLongToBeat.Models.Enumerations;
using HowLongToBeat.Views;
using HowLongToBeat;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using CommonPluginsShared.Utilities;

namespace HowLongToBeat.Services
{
    public partial class HowLongToBeatApi : ObservableObject, IDisposable
    {
        private static ILogger Logger => LogManager.GetLogger();

        private static HowLongToBeatDatabase PluginDatabase => HowLongToBeat.PluginDatabase;

        // Helper to centralize verbose logging checks
#if DEBUG
        private static bool IsVerboseLoggingEnabled => true;
#else
        private static bool IsVerboseLoggingEnabled => false;
#endif

        private string SafeStr(string s)
        {
            try
            {
                if (s == null)
                {
                    return string.Empty;
                }

                s = s.Replace("\r", " ").Replace("\n", " ");
                return s.Length > 120 ? s.Substring(0, 120) + "..." : s;
            }
            catch
            {
                return string.Empty;
            }
        }

        private DateTime lastLoginCheckUtc = DateTime.MinValue;
        private bool? lastLoginCheckResult = null;

        #region Submit session cookies

        /// <summary>
        /// Logs cookie diagnostics when verbose logging is enabled (counts, domains, session cookies, names).
        /// </summary>
        /// <param name="context">Caller context label used in log messages.</param>
        /// <param name="cookies">Cookie list to summarize.</param>
        private void LogCookieSummary(string context, List<HttpCookie> cookies)
        {
            try
            {
                if (!IsVerboseLoggingEnabled)
                {
                    return;
                }

                cookies = cookies ?? new List<HttpCookie>();
                var domains = cookies.Select(c => c?.Domain ?? string.Empty).Where(d => !string.IsNullOrEmpty(d)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                int expiredCount = 0;
                DateTime? minExpiry = null;
                DateTime? maxExpiry = null;
                foreach (var c in cookies)
                {
                    if (c?.Expires is DateTime dt)
                    {
                        if (dt <= DateTime.Now)
                        {
                            expiredCount++;
                        }

                        if (minExpiry == null || dt < minExpiry) minExpiry = dt;
                        if (maxExpiry == null || dt > maxExpiry) maxExpiry = dt;
                    }
                }

                Common.LogDebug(true, $"HLTB Auth: {context} cookies={cookies.Count} expired={expiredCount} domains=[{string.Join(",", domains)}] minExp={(minExpiry?.ToString("o") ?? "<none>")} maxExp={(maxExpiry?.ToString("o") ?? "<none>")}");

                string[] sessionCookieNames = { "hltb_alive", "hltb_online", "hltb_view_list" };
                var sessionStates = new List<string>();
                foreach (string cookieName in sessionCookieNames)
                {
                    // Reading HLTB session cookies from the stored container for auth diagnostics only (not setting cookies).
                    HttpCookie sessionCookie = cookies.FirstOrDefault(c => string.Equals(c?.Name, cookieName, StringComparison.OrdinalIgnoreCase)); // nosemgrep: csharp.lang.audit.cookies.missing-httponly.missing-httponly, csharp.lang.audit.cookies.missing-secure.missing-secure
                    if (sessionCookie == null)
                    {
                        sessionStates.Add(cookieName + "=missing");
                    }
                    else if (sessionCookie.Expires is DateTime expiry && expiry <= DateTime.Now)
                    {
                        sessionStates.Add(cookieName + "=expired");
                    }
                    else
                    {
                        sessionStates.Add(cookieName + "=present");
                    }
                }

                Common.LogDebug(true, $"HLTB Auth: {context} sessionCookies=[{string.Join(", ", sessionStates)}]");

                string cookieNames = string.Join(", ", cookies.Select(c => c?.Name ?? string.Empty).Where(n => !string.IsNullOrEmpty(n)).OrderBy(n => n, StringComparer.OrdinalIgnoreCase));
                Common.LogDebug(true, $"HLTB Auth: {context} cookieNames=[{cookieNames}]");
            }
            catch { }
        }

        /// <summary>
        /// HowLongToBeat session cookie names injected into a WebView before navigation.
        /// Tracking and consent cookies are excluded to avoid slow SetCookies calls.
        /// </summary>
        private static readonly string[] HltbSessionCookieNames = { "hltb_alive", "hltb_online", "hltb_view_list" };

        /// <summary>
        /// Returns session cookies suitable for WebView injection before navigation.
        /// </summary>
        private static List<HttpCookie> FilterSessionCookiesForInjection(List<HttpCookie> cookies)
        {
            if (cookies == null || cookies.Count == 0)
            {
                return new List<HttpCookie>();
            }

            return cookies
                .Where(c => c != null && HltbSessionCookieNames.Any(n => string.Equals(c.Name, n, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        /// <summary>
        /// Returns whether the cookie list contains the HowLongToBeat session cookie required for profile updates.
        /// </summary>
        /// <param name="cookies">Cookie list to inspect.</param>
        /// <returns><c>true</c> when the <c>hltb_alive</c> cookie is present.</returns>
        private static bool HasHltbSessionCookies(List<HttpCookie> cookies)
        {
            return cookies != null && cookies.Any(c => string.Equals(c?.Name, "hltb_alive", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Post-navigation delay used when refreshing HowLongToBeat session cookies before submit.
        /// </summary>
        private const int SubmitSessionCookieRefreshWaitMs = 400;

        /// <summary>
        /// Builds the minimal list of HowLongToBeat URLs to visit before reading session cookies.
        /// </summary>
        /// <param name="submissionId">User submission id. When greater than zero, the edit page is visited first.</param>
        /// <returns>Ordered URLs to load in the WebView.</returns>
        private List<string> GetSessionCookieRefreshUrls(int submissionId)
        {
            List<string> urls = new List<string>();

            if (submissionId > 0)
            {
                urls.Add(string.Format(UrlPostDataEdit, submissionId));
            }

            string userLogin = UserLogin;
            if (userLogin.IsNullOrEmpty() && PluginDatabase?.PluginSettings != null)
            {
                userLogin = PluginDatabase.PluginSettings.UserLogin;
            }

            if (!userLogin.IsNullOrEmpty())
            {
                urls.Add(UrlBase + "/user/" + userLogin);
            }

            if (urls.Count == 0)
            {
                urls.Add(UrlBase);
            }

            return urls;
        }

        /// <summary>
        /// Visits HowLongToBeat pages in a WebView to refresh session cookies (<c>hltb_alive</c>, <c>hltb_online</c>).
        /// Persisted cookies are updated when extraction succeeds.
        /// Must run on the UI thread.
        /// </summary>
        /// <param name="submissionId">User submission id passed to <see cref="GetSessionCookieRefreshUrls"/>.</param>
        /// <returns>Refreshed cookies ready for API calls.</returns>
        private List<HttpCookie> RefreshSubmitSessionCookies(int submissionId)
        {
            List<string> urls = GetSessionCookieRefreshUrls(submissionId);
            List<HttpCookie> sessionCookies = FilterSessionCookiesForInjection(CookiesTools.GetStoredCookies());
            Logger.Info($"HLTB Auth: RefreshSubmitSessionCookies injecting {sessionCookies.Count} session cookie(s)");
            Common.LogDebug(true, $"RefreshSubmitSessionCookies: urls=[{string.Join(", ", urls)}] injectCount={sessionCookies.Count}");

            List<HttpCookie> cookies = CookiesTools.GetNewWebCookies(urls, false, null, SubmitSessionCookieRefreshWaitMs, sessionCookies);
            if (cookies != null && cookies.Count > 0)
            {
                CookiesTools.SetStoredCookies(cookies);
                LogCookieSummary("RefreshSubmitSessionCookies", cookies);
                return cookies;
            }

            cookies = CookiesTools.GetStoredCookies();
            LogCookieSummary("RefreshSubmitSessionCookies fallback", cookies);
            return cookies ?? new List<HttpCookie>();
        }

        /// <summary>
        /// Returns cookies for profile submission, refreshing the session only when <c>hltb_alive</c> is missing.
        /// When a refresh is required, a global progress dialog is shown while the WebView runs on the UI thread.
        /// </summary>
        /// <param name="submissionId">User submission id used to build refresh URLs.</param>
        /// <returns>Cookie list to send with the submit request.</returns>
        private async Task<List<HttpCookie>> GetCookiesForSubmitAsync(int submissionId)
        {
            List<HttpCookie> stored = CookiesTools.GetStoredCookies();
            if (HasHltbSessionCookies(stored))
            {
                Common.LogDebug(true, "GetCookiesForSubmit: using stored session cookies");
                return stored;
            }

            TaskCompletionSource<List<HttpCookie>> refreshCompleted = new TaskCompletionSource<List<HttpCookie>>();

            try
            {
                GlobalProgressOptions progressOptions = new GlobalProgressOptions(
                    PluginDatabase.PluginName + " - " + ResourceProvider.GetString("LOCHowLongToBeatRefreshSessionCookies"))
                {
                    Cancelable = false,
                    IsIndeterminate = true
                };

                _ = API.Instance.Dialogs.ActivateGlobalProgress(async (GlobalProgressActionArgs args) =>
                {
                    try
                    {
                        args.Text = ResourceProvider.GetString("LOCHowLongToBeatRefreshSessionCookies");
                        List<HttpCookie> cookies = await Task.Run(() =>
                        {
                            if (Application.Current?.Dispatcher != null)
                            {
                                return Application.Current.Dispatcher.Invoke(() => RefreshSubmitSessionCookies(submissionId));
                            }

                            return RefreshSubmitSessionCookies(submissionId);
                        }).ConfigureAwait(false);

                        refreshCompleted.TrySetResult(cookies);
                    }
                    catch (Exception ex)
                    {
                        refreshCompleted.TrySetException(ex);
                    }
                }, progressOptions);

                return await refreshCompleted.Task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, false, PluginDatabase.PluginName);
                return CookiesTools.GetStoredCookies();
            }
        }

        #endregion

        private void FireAndForget(Task task, string context)
        {
            // Delegate to centralized helper to avoid duplication with HowLongToBeatDatabase
            try
            {
                TaskHelpers.FireAndForget(task, context, LogManager.GetLogger());
            }
            catch { }
        }

        /// <summary>
        /// Adjusts a semaphore to match a target limit by releasing extra permits or consuming existing permits.
        /// The helper reads and writes the shared current limit via the provided delegates under the provided syncLock
        /// to avoid races where callers supply stale snapshots. The method returns once adjustments are applied.
        /// </summary>
        private async Task AdjustSemaphoreLimit(
            SemaphoreSlim semaphore,
            Func<int> getCurrentLimit,
            Action<int> setCurrentLimit,
            int targetLimit,
            object syncLock,
            string context = null,
            CancellationToken cancellationToken = default)
        {
            if (semaphore == null) return;

            try
            {
                int pendingConsume = 0;

                targetLimit = Math.Max(0, Math.Min(SemaphoreUpperBound, targetLimit));

                // Compute difference under lock and handle immediate increases (release permits)
                if (syncLock == null)
                {
                    try { Logger.Warn("HLTB: AdjustSemaphoreLimit called with null syncLock; using internal lock"); } catch { }
                    syncLock = new object();
                }

                int current = getCurrentLimit();
                int diff;
                lock (syncLock)
                {
                    current = getCurrentLimit();
                    diff = targetLimit - current;
                    if (diff > 0)
                    {
                        bool released = false;
                        try
                        {
                            semaphore.Release(diff);
                            released = true;
                        }
                        catch (Exception ex)
                        {
                            try { Logger.Warn(ex, $"HLTB: AdjustSemaphoreLimit failed to release {diff} permits (currentLimit={current}) ({context})"); } catch { }
                        }

                        try
                        {
                            if (released)
                            {
                                try { setCurrentLimit(targetLimit); } catch { }
                            }
                        }
                        catch { }

                        if (released) return;
                    }

                    if (diff < 0)
                    {
                        pendingConsume = -diff; // how many permits we need to consume to lower the available count
                    }
                }

                if (pendingConsume > 0)
                {
                    // Try to consume up to pendingConsume permits with short, bounded waits so no orphaned tasks remain.
                    int consumed = 0;
                    TimeSpan tryWait = TimeSpan.FromMilliseconds(200);

                    for (int i = 0; i < pendingConsume; i++)
                    {
                        try
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                break;
                            }
                            bool got = await semaphore.WaitAsync(tryWait, cancellationToken).ConfigureAwait(false);
                            if (!got)
                            {
                                break; // timed out acquiring next permit; stop trying
                            }

                            consumed++;
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch
                        {
                            break;
                        }
                    }

                    // Update the shared currentLimit under lock based on how many permits we actually consumed
                    lock (syncLock)
                    {
                        int originalLimit = getCurrentLimit();
                        int newLimit;

                        if (consumed == pendingConsume)
                        {
                            // Successfully consumed all intended permits
                            newLimit = targetLimit;
                        }
                        else
                        {
                            // We consumed fewer than requested; lower the limit by the number we consumed (but not below 0)
                            newLimit = Math.Max(0, originalLimit - consumed);
                            // Ensure we don't accidentally increase above originalLimit
                            newLimit = Math.Min(originalLimit, newLimit);
                        }

                        setCurrentLimit(newLimit);
                    }
                }
            }
            catch (Exception ex)
            {
                try { Logger.Warn(ex, $"HLTB: AdjustSemaphoreLimit error ({context})"); } catch { }
            }
        }


        /// <summary>
        /// Tool for managing cookies for HowLongToBeat sessions.
        /// </summary>
        protected CookiesTools CookiesTools { get; }
        /// <summary>
        /// List of domains for which cookies are managed.
        /// </summary>
        protected List<string> CookiesDomains { get; }
        /// <summary>
        /// Path to the file where cookies are stored.
        /// </summary>
        internal string FileCookies { get; }

        private readonly Type HapDocType;
        private readonly bool HapAvailable;

        private static string SearchUrl { get; set; } = null;
        private static readonly object SearchUrlLock = new object();
        private static readonly SemaphoreSlim SearchUrlDiscoverySync = new SemaphoreSlim(1, 1);
        private const int ScriptDownloadTimeoutMs = 5000;

        private const int MaxParallelGameDataDownloads = 8;
        private const int GameDataDownloadTimeoutMs = 15000;

        private const int MaxParallelSearches = 24;

        // Replace unbounded dictionaries with bounded LRU caches to avoid unbounded memory growth
        private readonly LruCache<string, string> GamePageCache;
        private readonly LruCache<string, SearchResult> SearchCache;
        private SemaphoreSlim SearchSemaphore;
        private AdaptiveConcurrencyController SearchConcurrencyController;
        private readonly object SearchConcurrencySync = new object();
        private int CurrentSearchLimit;
        private int PersistentCacheHits = 0;
        private int InMemoryCacheHits = 0;
        private int PageFetches = 0;
        private PageCache PageCache;
        private AdaptiveConcurrencyController ConcurrencyController;
        private SemaphoreSlim DynamicSemaphore;
        private readonly object ConcurrencySync = new object();
        private int CurrentSemaphoreLimit;
        private const int SemaphoreUpperBound = 128;

        private readonly HttpClient httpClient;
        private readonly AsyncTokenBucketRateLimiter httpRateLimiter;
        private Task PageCacheInitTask;
        // Thread-local Random to provide jitter without creating many Sequential Random instances
        private static readonly ThreadLocal<Random> _rnd = new ThreadLocal<Random>(() => new Random(Guid.NewGuid().GetHashCode()));

        private readonly ConcurrentQueue<long> RecentSearchSamples = new ConcurrentQueue<long>();
        private const int RecentSamplesWindow = 200;

        private readonly ConcurrentQueue<int> RecentSearchStatusCodes = new ConcurrentQueue<int>();
        private const int RecentStatusWindow = 200;

        private DateTime SearchBackoffUntil = DateTime.MinValue;
        private int SearchBackoffLimit = 0;
        private readonly object BackoffSync = new object();

        private string CachedAuthToken = null;
        private Dictionary<string, string> CachedAuthHeaderParts = null;
        private string CachedAuthEndpoint = null;
        private DateTime CachedAuthTokenExpiry = DateTime.MinValue;
        private readonly object AuthTokenSync = new object();

        private CancellationTokenSource monitorCts;
        private Task monitorTask;
        private readonly object monitorSync = new object();
        private bool _disposed = false;
        private const double DefaultHttpRateTokensPerSecond = 2d;
        private const int DefaultHttpRateBurstCapacity = 3;


        #region Urls

        private static string UrlBase => "https://howlongtobeat.com";

        private static string UrlLogin => UrlBase + "/login";
        private static string UrlLogOut => UrlBase + "/login?t=out";
        private static string UrlSearchWeb => UrlBase + "/?q={0}";

        private static string UrlUser => UrlBase + "/api/user";
        private static string UrlUserStats => UrlUser + "?n={0}&s=stats";
        private static string UrlUserStatsMore => UrlBase + "/user_stats_more";
        private static string UrlUserStatsGamesList => UrlUser + "/{0}/stats";
        private static string UrlUserGamesList => UrlUser + "/{0}/games/list";
        private static string UrlUserStatsGameDetails => UrlBase + "/user_games_detail";

        private static string UrlPostData => UrlBase + "/api/submit";
        private static string UrlPostDataEdit => UrlBase + "/submit/edit/{0}";

        private static string DefaultSearchApiEndpoint => "/api/search/site";

        /// <summary>
        /// Persisted or default HLTB search API path used when script discovery fails.
        /// </summary>
        private static string SearchEndPoint => GetPersistedSearchEndpoint();

        private static string UrlSearch => UrlBase + SearchEndPoint;

        private static string UrlGameImg => UrlBase + "/games/{0}";

        private static string UrlGame => UrlBase + "/game?id={0}";

        private static string UrlExportAll => UrlBase + "/user_export?all=1";

        #endregion


        private bool? _isConnected = null;
        /// <summary>
        /// Indicates if the user is currently connected (logged in).
        /// </summary>
        public bool? IsConnected { get => _isConnected; set => SetValue(ref _isConnected, value); }

        /// <summary>
        /// Raised when the login WebView dialog closes (success or cancel).
        /// </summary>
        public event EventHandler LoginCompleted;

        /// <summary>
        /// The username of the currently logged-in user.
        /// </summary>
        public string UserLogin { get; set; } = string.Empty;
        /// <summary>
        /// The user ID of the currently logged-in user.
        /// </summary>
        public int UserId { get; set; } = 0;

        private bool IsFirst = true;


        /// <summary>
        /// Initializes a new instance of the <see cref="HowLongToBeatApi"/> class.
        /// </summary>
        public HowLongToBeatApi()
        {
            try
            {
                var handler = new HttpClientHandler();
                var prop = handler.GetType().GetProperty("MaxConnectionsPerServer");
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(handler, Math.Max(4, MaxParallelGameDataDownloads));
                }
                else
                {
                    Logger.Warn("HLTB: MaxConnectionsPerServer not available; using default connection limits");
                }

                httpClient = new HttpClient(handler)
                {
                    Timeout = System.Threading.Timeout.InfiniteTimeSpan
                };
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", Web.UserAgent);
                try { httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Referer", UrlBase); } catch { }
                try { httpClient.DefaultRequestHeaders.Add("accept", "application/json, text/javascript, */*; q=0.01"); } catch { }
                httpRateLimiter = new AsyncTokenBucketRateLimiter(DefaultHttpRateTokensPerSecond, DefaultHttpRateBurstCapacity);
                try
                {
                    Logger.Info($"HLTB rate limiter initialized: {DefaultHttpRateTokensPerSecond:0.##} req/s, burst={DefaultHttpRateBurstCapacity}");
                    Common.LogDebug(true, $"HLTB RateLimiter init tokensPerSecond={DefaultHttpRateTokensPerSecond:0.##} burst={DefaultHttpRateBurstCapacity}");
                    Logger.Info($"HLTB concurrency defaults: search={MaxParallelSearches}, gameData={MaxParallelGameDataDownloads}");
                    Common.LogDebug(true, $"HLTB Concurrency init search={MaxParallelSearches} gameData={MaxParallelGameDataDownloads}");
                }
                catch { }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "HLTB: HttpClient init failed");
                throw;
            }

            // Create default alias file if missing (non-fatal)
            try
            {
                GameNameAliases.EnsureAliasFileExists(PluginDatabase?.Plugin?.GetPluginUserDataPath());
            }
            catch { }

            // Cache HtmlAgilityPack availability once to avoid reflection on every request.
            Type hapType = null;
            try
            {
                hapType = Type.GetType("HtmlAgilityPack.HtmlDocument, HtmlAgilityPack");
            }
            catch { }
            HapDocType = hapType;
            HapAvailable = HapDocType != null;

            UserLogin = PluginDatabase.PluginSettings.UserLogin;
            HydrateSearchUrlFromSettings();

            CookiesDomains = new List<string>
            {
                ".howlongtobeat.com",
                "howlongtobeat.com",
                "www.howlongtobeat.com",
                ".www.howlongtobeat.com"
            };
            string pathData = PluginDatabase.Paths.PluginUserDataPath;
            FileCookies = Path.Combine(pathData, CommonPlayniteShared.Common.Paths.GetSafePathName($"HowLongToBeat.dat"));
            CookiesTools = new CookiesTools(
                PluginDatabase.PluginName,
                "HowLongToBeat",
                FileCookies,
                CookiesDomains
            );

            try
            {
                var exists = File.Exists(FileCookies);
                long size = 0;
                if (exists)
                {
                    try { size = new FileInfo(FileCookies).Length; } catch { }
                }

                Logger.Info($"HLTB Auth: cookie file='{FileCookies}' exists={exists} size={size}");
                if (exists)
                {
                    LogCookieSummary("startup stored", CookiesTools.GetStoredCookies());
                }
            }
            catch { }

            try
            {
                PageCacheInitTask = Task.Run(() =>
                {
                    PageCache localPc = null;
                    try
                    {
                        localPc = new PageCache(PluginDatabase.Plugin.GetPluginUserDataPath());
                        if (!_disposed)
                        {
                            PageCache = localPc;
                            localPc = null;
                        }
                        else
                        {
                            try { (localPc as IDisposable)?.Dispose(); } catch { }
                            localPc = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false);
                        try { Logger.Warn("HLTB: PageCache init failed; proceeding without persistent cache"); } catch { }
                        if (localPc != null)
                        {
                            try { (localPc as IDisposable)?.Dispose(); } catch { }
                            localPc = null;
                        }
                    }
                });

                // Observe any exceptions from the init task so they don't go unobserved
                PageCacheInitTask.ContinueWith(t =>
                {
                    try
                    {
                        if (t.IsFaulted && t.Exception != null)
                        {
                            Common.LogError(t.Exception, false);
                        }
                    }
                    catch { }
                }, TaskContinuationOptions.OnlyOnFaulted);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            // Configure bounded in-memory caches to avoid unbounded growth during large imports
            // Sizes are conservative defaults; can be tuned via settings later.
            GamePageCache = new LruCache<string, string>(capacity: 2000, ttl: TimeSpan.FromHours(6));
            SearchCache = new LruCache<string, SearchResult>(capacity: 2000, ttl: TimeSpan.FromHours(6));

            try
            {
                ConcurrencyController = new AdaptiveConcurrencyController(MaxParallelGameDataDownloads, 4, SemaphoreUpperBound, TimeSpan.FromSeconds(2));
                DynamicSemaphore = new SemaphoreSlim(MaxParallelGameDataDownloads, SemaphoreUpperBound);
                CurrentSemaphoreLimit = MaxParallelGameDataDownloads;
                try
                {
                    SearchConcurrencyController = new AdaptiveConcurrencyController(MaxParallelSearches, 2, SemaphoreUpperBound, TimeSpan.FromSeconds(2));
                    CurrentSearchLimit = MaxParallelSearches;
                    SearchSemaphore = new SemaphoreSlim(MaxParallelSearches, SemaphoreUpperBound);
                }
                catch (Exception ex)
                {
                    try { Logger.Warn(ex, "HLTB: Search concurrency controller init failed; using basic semaphore"); } catch { }
                    SearchSemaphore = new SemaphoreSlim(MaxParallelSearches);
                    CurrentSearchLimit = MaxParallelSearches;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                try { Logger.Warn("HLTB: Adaptive concurrency init failed; using basic semaphore defaults"); } catch { }
                DynamicSemaphore = new SemaphoreSlim(MaxParallelGameDataDownloads);
                CurrentSemaphoreLimit = MaxParallelGameDataDownloads;
            }

        }

        public void StopMonitoring()
        {
            try
            {
                var task = monitorTask;
                var cts = monitorCts;

                try { cts?.Cancel(); } catch { }

                if (task != null)
                {
                    try { task.Wait(5000); } catch { }
                }

                try { cts?.Dispose(); } catch { }
                monitorCts = null;
                monitorTask = null;
            }
            catch { }
        }

        private async Task WaitForHttpRateLimitAsync(string operation, string url, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (httpRateLimiter == null)
            {
                return;
            }

            try
            {
                int waitedMs = await httpRateLimiter.WaitAsync(cancellationToken).ConfigureAwait(false);
                if (waitedMs > 0)
                {
                    try { Logger.Info($"HLTB rate limiter: waited {waitedMs}ms before {operation} '{url}'"); } catch { }
                    try { Common.LogDebug(true, $"HLTB RateLimiter wait operation={operation} waitedMs={waitedMs} url='{url}'"); } catch { }
                }
                else
                {
                    try { Common.LogDebug(true, $"HLTB RateLimiter pass operation={operation} waitedMs=0 url='{url}'"); } catch { }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                try { Logger.Warn(ex, $"HLTB rate limiter check failed for {operation} '{url}'"); } catch { }
            }
        }

        private void EnsureMonitoringStarted()
        {
            if (_disposed) return;
            lock (monitorSync)
            {
                if (monitorTask != null && !monitorTask.IsCompleted && monitorCts != null && !monitorCts.IsCancellationRequested)
                {
                    return;
                }

                try { monitorCts?.Dispose(); } catch { }
                monitorCts = new CancellationTokenSource();
                try
                {
                    monitorTask = Task.Run(async () =>
                    {
                        var token = monitorCts.Token;
                        try
                        {
                            while (!token.IsCancellationRequested)
                            {
                                try
                                {
                                    if (token.IsCancellationRequested) break;
                                    await Task.Delay(TimeSpan.FromSeconds(10), token).ConfigureAwait(false);
                                }
                                catch (OperationCanceledException)
                                {
                                    break;
                                }
                                catch (Exception) { }
                                try
                                {
                                    int searchTarget;
                                    bool searchForced = false;
                                    try
                                    {
                                        int fixedTarget = MaxParallelSearches;
                                        lock (BackoffSync)
                                        {
                                            if (SearchBackoffLimit > 0 && DateTime.UtcNow < SearchBackoffUntil)
                                            {
                                                searchTarget = Math.Min(fixedTarget, SearchBackoffLimit);
                                            }
                                            else
                                            {
                                                searchTarget = fixedTarget;
                                            }
                                        }
                                        searchForced = true;
                                    }
                                    catch
                                    {
                                        searchTarget = SearchConcurrencyController?.TargetConcurrency ?? MaxParallelSearches;
                                    }

                                    int searchAvailable = 0;
                                    int searchInFlight = 0;
                                    int gameTarget = 0;
                                    int gameAvailable = 0;
                                    int gameInFlight = 0;
                                    try
                                    {
                                        searchAvailable = SearchSemaphore?.CurrentCount ?? 0;
                                        searchInFlight = Math.Max(0, searchTarget - searchAvailable);

                                        gameTarget = ConcurrencyController?.TargetConcurrency ?? MaxParallelGameDataDownloads;
                                        gameAvailable = DynamicSemaphore?.CurrentCount ?? 0;
                                        gameInFlight = Math.Max(0, gameTarget - gameAvailable);
                                    }
                                    catch (ObjectDisposedException) { break; }
                                    catch (InvalidOperationException) { break; }

                                    var samples = RecentSearchSamples.ToArray();
                                    double avg = samples.Length > 0 ? samples.Average() : 0;
                                    double median = 0;
                                    double p90 = 0;
                                    if (samples.Length > 0)
                                    {
                                        var ordered = samples.OrderBy(x => x).ToArray();
                                        median = ordered[ordered.Length / 2];
                                        p90 = ordered[Math.Max(0, (int)Math.Floor(ordered.Length * 0.9) - 1)];
                                    }

                                    Common.LogDebug(true, $"HLTB Summary: searchTarget={searchTarget} searchInFlight={searchInFlight} gameTarget={gameTarget} gameInFlight={gameInFlight} avgSearchMs={Math.Round(avg, 1)} medianSearchMs={Math.Round(median, 1)} p90SearchMs={Math.Round(p90, 1)} persistentCacheHits={PersistentCacheHits} inMemoryCacheHits={InMemoryCacheHits} pageFetches={PageFetches} forced={searchForced}");
                                }
                                catch (Exception ex)
                                {
                                    try { Logger.Error(ex, "HLTB monitor loop error"); } catch { }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            try { Logger.Error(ex, "HLTB monitor task terminated unexpectedly"); } catch { }
                        }
                    });
                }
                catch (Exception ex)
                {
                    try { Logger.Error(ex, "Failed to start HLTB monitor task"); } catch { }
                }
            }
        }

        ~HowLongToBeatApi()
        {
            try { Dispose(false); } catch { }
        }

        public void Dispose()
        {
            Dispose(true);
            try { GC.SuppressFinalize(this); } catch { }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            _disposed = true;

            if (disposing)
            {
                try
                {
                    // Use StopMonitoring as single shutdown path; it will cancel and cleanup without blocking.
                    try { StopMonitoring(); } catch { }
                }
                catch { }
                finally
                {
                    monitorTask = null;
                    monitorCts = null;
                }

                try { ConcurrencyController?.Dispose(); } catch { }
                ConcurrencyController = null;
                try { SearchConcurrencyController?.Dispose(); } catch { }
                SearchConcurrencyController = null;

                try { DynamicSemaphore?.Dispose(); } catch { }
                DynamicSemaphore = null;
                try { SearchSemaphore?.Dispose(); } catch { }
                SearchSemaphore = null;

                try { httpClient?.Dispose(); } catch { }

                // Dispose page cache if it implements IDisposable
                try
                {
                    if (PageCache is IDisposable disposableCache)
                    {
                        try { disposableCache.Dispose(); } catch { }
                    }
                }
                catch { }
                PageCache = null;
            }
        }


        private static HltbData MapGameDataToHltbData(GameData gameData)
        {
            if (gameData == null)
            {
                return null;
            }

            return new HltbData
            {
                MainStoryClassic = gameData.CompMain,
                MainExtraClassic = gameData.CompPlus,
                CompletionistClassic = gameData.Comp100,
                SoloClassic = gameData.CompAll,
                CoOpClassic = gameData.InvestedCo,
                VsClassic = gameData.InvestedMp,

                MainStoryMedian = gameData.CompMainMed,
                MainExtraMedian = gameData.CompPlusMed,
                CompletionistMedian = gameData.Comp100Med,
                SoloMedian = gameData.CompAllMed,
                CoOpMedian = gameData.InvestedCoMed,
                VsMedian = gameData.InvestedMpMed,

                MainStoryAverage = gameData.CompMainAvg,
                MainExtraAverage = gameData.CompPlusAvg,
                CompletionistAverage = gameData.Comp100Avg,
                SoloAverage = gameData.CompAllAvg,
                CoOpAverage = gameData.InvestedCoAvg,
                VsAverage = gameData.InvestedMpAvg,

                MainStoryRushed = gameData.CompMainL,
                MainExtraRushed = gameData.CompPlusL,
                CompletionistRushed = gameData.Comp100L,
                SoloRushed = gameData.CompAllL,
                CoOpRushed = gameData.InvestedCoL,
                VsRushed = gameData.InvestedMpL,

                MainStoryLeisure = gameData.CompMainH,
                MainExtraLeisure = gameData.CompPlusH,
                CompletionistLeisure = gameData.Comp100H,
                SoloLeisure = gameData.CompAllH,
                CoOpLeisure = gameData.InvestedCoH,
                VsLeisure = gameData.InvestedMpH
            };
        }

        /// <summary>
        /// Fills source URL and cover image URL on <paramref name="entry"/> when they are still empty.
        /// </summary>
        private void ApplyPageMetadataIfMissing(HltbDataUser entry, GameData gameData)
        {
            if (entry == null || gameData == null)
            {
                return;
            }

            bool setUrl = false;
            bool setUrlImg = false;
            bool setName = false;
            bool setPlatform = false;

            string id = entry.Id?.Trim();
            if (!id.IsNullOrEmpty() && entry.Url.IsNullOrEmpty())
            {
                entry.Url = string.Format(UrlGame, id);
                setUrl = true;
            }

            if (entry.UrlImg.IsNullOrEmpty() && !gameData.GameImage.IsNullOrEmpty())
            {
                entry.UrlImg = string.Format(UrlGameImg, gameData.GameImage);
                setUrlImg = true;
            }

            if (entry.Name.IsNullOrEmpty() && !gameData.GameName.IsNullOrEmpty())
            {
                entry.Name = gameData.GameName;
                setName = true;
            }

            if (entry.Platform.IsNullOrEmpty() && !gameData.ProfilePlatform.IsNullOrEmpty())
            {
                entry.Platform = gameData.ProfilePlatform;
                setPlatform = true;
            }

            if (setUrl || setUrlImg || setName || setPlatform)
            {
                Logger.Info(string.Format(
                    "HLTB ApplyPageMetadataIfMissing id={0}: setUrl={1} setUrlImg={2} setName={3} setPlatform={4} url='{5}' urlImg='{6}'",
                    id ?? string.Empty,
                    setUrl,
                    setUrlImg,
                    setName,
                    setPlatform,
                    entry.Url ?? string.Empty,
                    entry.UrlImg ?? string.Empty));
            }
            else
            {
                Common.LogDebug(true, string.Format(
                    "HLTB ApplyPageMetadataIfMissing id={0}: nothing to fill (already present)",
                    id ?? string.Empty));
            }
        }

        /// <summary>
        /// Retrieves game data from HowLongToBeat by game ID.
        /// </summary>
        /// <param name="id">The game ID.</param>
        /// <returns>Returns <see cref="HltbData"/> with game times, or null if not found.</returns>
        private async Task<HltbData> GetGameData(string id, CancellationToken cancellationToken = default)
        {
            Common.LogDebug(true, string.Format("HLTB GetGameData: mapping page data to HltbData for id={0}", id));
            GameData gameData = await GetGamePageDataAsync(id, cancellationToken).ConfigureAwait(false);
            return gameData == null ? null : MapGameDataToHltbData(gameData);
        }

        /// <summary>
        /// Fetches and parses the HLTB game page for a game ID.
        /// </summary>
        /// <param name="id">The HLTB game ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Parsed page <see cref="GameData"/>, or null when not found.</returns>
        private async Task<GameData> GetGamePageDataAsync(string id, CancellationToken cancellationToken = default)
        {
            try { EnsureMonitoringStarted(); } catch { }
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var init = PageCacheInitTask;
                if (init != null && PageCache == null)
                {
                    await Task.WhenAny(init, Task.Delay(500)).ConfigureAwait(false);
                }
            }
            catch { }
            DateTime startTime = DateTime.UtcNow;
            Logger.Info(string.Format("HLTB GetGamePageData START id={0}", id));
            Common.LogDebug(true, string.Format(
                "HLTB GetGamePageData START id={0} task={1} thread={2}",
                id,
                Task.CurrentId,
                Thread.CurrentThread.ManagedThreadId));

            try
            {
                string jsonData = null;
                try
                {
                    if (PageCache != null && PageCache.TryGetJson(id, out string cachedJson))
                    {
                        jsonData = cachedJson;
                        try { System.Threading.Interlocked.Increment(ref PersistentCacheHits); } catch { }
                        Common.LogDebug(true, string.Format("HLTB GetGamePageData id={0} - persistent cache hit (jsonLength={1})", id, cachedJson?.Length ?? 0));
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, false, PluginDatabase.PluginName);
                }

                int attempts = 0;
                if (string.IsNullOrEmpty(jsonData))
                {
                    string response = string.Empty;
                    int maxAttempts = 3;
                    int baseDelayMs = 300;
                    var rnd = _rnd.Value;
                    while (attempts < maxAttempts)
                    {
                        attempts++;
                        try
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            if (GamePageCache.TryGetValue(id, out string cached))
                            {
                                response = cached;
                                try { System.Threading.Interlocked.Increment(ref InMemoryCacheHits); } catch { }
                                Common.LogDebug(true, string.Format("HLTB GetGamePageData id={0} - in-memory HTML cache hit (htmlLength={1})", id, cached?.Length ?? 0));
                            }
                            else
                            {
                                try
                                {
                                    string gameUrl = string.Format(UrlGame, id);
                                    await WaitForHttpRateLimitAsync("GET game page", gameUrl, cancellationToken).ConfigureAwait(false);
                                    using (var httpResp = await httpClient.GetAsync(gameUrl, cancellationToken).ConfigureAwait(false))
                                    {
                                        if (!httpResp.IsSuccessStatusCode)
                                        {
                                            var code = (int)httpResp.StatusCode;
                                            Logger.Warn(string.Format("HLTB GetGamePageData id={0} - HTTP {1}", id, code));
                                            Common.LogDebug(true, string.Format("HLTB GetGamePageData id={0} - HTTP {1} fetching page", id, code));
                                            response = string.Empty;
                                        }
                                        else
                                        {
                                            response = await httpResp.Content.ReadAsStringAsync().ConfigureAwait(false);
                                        }
                                    }
                                }
                                catch (HttpRequestException hre)
                                {
                                    Common.LogError(hre, false, false, PluginDatabase.PluginName);
                                    response = string.Empty;
                                }
                            }

                            if (!response.IsNullOrEmpty())
                            {
                                string maybeJson = UtilityTools.GetJsonInString(response, @"<script[ ]?id=""__NEXT_DATA__""[ ]?type=""application/json"">");
                                if (!maybeJson.IsNullOrEmpty())
                                {
                                    GamePageCache.TryAdd(id, response);
                                    try
                                    {
                                        PageCache?.Set(id, maybeJson);
                                    }
                                    catch (Exception ex)
                                    {
                                        Common.LogError(ex, false, false, PluginDatabase.PluginName);
                                    }
                                    try { System.Threading.Interlocked.Increment(ref PageFetches); } catch { }
                                    jsonData = maybeJson;
                                    break;
                                }
                                else
                                {
                                    Common.LogDebug(true, string.Format("HLTB GetGamePageData id={0} - extracted JSON empty or incomplete (attempt={1})", id, attempts));
                                    response = string.Empty;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false, false, PluginDatabase.PluginName);
                        }

                        if (attempts < maxAttempts)
                        {
                            var jitter = rnd.Next(0, 200);
                            var delay = baseDelayMs * attempts + jitter;
                            Common.LogDebug(true, string.Format("HLTB GetGamePageData id={0} - retry {1} after {2}ms", id, attempts, delay));
                            try { await Task.Delay(delay, cancellationToken); } catch (OperationCanceledException) { throw; }
                        }
                    }
                }
                if (string.IsNullOrEmpty(jsonData))
                {
                    Common.LogDebug(true, string.Format("HLTB GetGamePageData id={0} - no __NEXT_DATA__ JSON after {1} attempt(s)", id, attempts));
                    Logger.Warn(string.Format("HLTB GetGamePageData: no JSON for id={0}", id));
                    double elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                    try { ConcurrencyController?.ReportSample(elapsed, false); } catch { }
                    Logger.Info(string.Format("HLTB GetGamePageData DONE id={0} ok=false elapsed={1:F0}ms", id, elapsed));
                    return null;
                }

                _ = Serialization.TryFromJson(jsonData, out NEXT_DATA next_data, out Exception parseEx);
                if (parseEx != null)
                {
                    Common.LogError(parseEx, false, false, PluginDatabase.PluginName);
                }

                GameData gameData = next_data?.Props?.PageProps?.Game?.Data?.Game != null
                    ? next_data.Props.PageProps.Game.Data.Game.FirstOrDefault()
                    : null;

                if (gameData != null)
                {
                    double elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                    try { ConcurrencyController?.ReportSample(elapsed, true); } catch { }
                    Logger.Info(string.Format(
                        "HLTB GetGamePageData DONE id={0} ok=true elapsed={1:F0}ms name='{2}' compMain={3}s",
                        id,
                        elapsed,
                        gameData.GameName ?? string.Empty,
                        gameData.CompMain));
                    Common.LogDebug(true, string.Format("HLTB GetGamePageData id={0} - parsed gameImage='{1}'", id, gameData.GameImage ?? string.Empty));
                    return gameData;
                }
                else
                {
                    Logger.Warn(string.Format("HLTB GetGamePageData: __NEXT_DATA__ parsed but no game entry for id={0}", id));
                    double elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                    try { ConcurrencyController?.ReportSample(elapsed, false); } catch { }
                    Logger.Info(string.Format("HLTB GetGamePageData DONE id={0} ok=false elapsed={1:F0}ms", id, elapsed));
                    return null;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
                Logger.Info(string.Format("HLTB GetGamePageData ERROR id={0} elapsed={1:F0}ms", id, (DateTime.UtcNow - startTime).TotalMilliseconds));
            }

            return null;
        }

        /// <summary>
        /// Updates the HLTB data for a user game entry.
        /// </summary>
        /// <param name="hltbDataUser">The user game data to update.</param>
        /// <returns>Returns the updated <see cref="HltbDataUser"/>.</returns>
        public async Task<HltbDataUser> UpdateGameData(HltbDataUser hltbDataUser)
        {
            if (hltbDataUser == null || hltbDataUser.Id.IsNullOrEmpty())
            {
                return hltbDataUser;
            }

            Logger.Info(string.Format(
                "HLTB UpdateGameData START: hltbId='{0}' name='{1}' urlBefore='{2}' urlImgBefore='{3}'",
                hltbDataUser.Id,
                hltbDataUser.Name ?? string.Empty,
                hltbDataUser.Url ?? string.Empty,
                hltbDataUser.UrlImg ?? string.Empty));

            try
            {
                GameData gameData = await GetGamePageDataAsync(hltbDataUser.Id).ConfigureAwait(false);
                if (gameData == null)
                {
                    Logger.Warn(string.Format("HLTB UpdateGameData: no page data for hltbId='{0}'", hltbDataUser.Id));
                    return hltbDataUser;
                }

                HltbData hltbData = MapGameDataToHltbData(gameData);
                hltbDataUser.GameHltbData = hltbData ?? hltbDataUser.GameHltbData;
                ApplyPageMetadataIfMissing(hltbDataUser, gameData);

                Logger.Info(string.Format(
                    "HLTB UpdateGameData DONE: hltbId='{0}' main={1}s timeToBeat={2}s url='{3}' urlImg='{4}'",
                    hltbDataUser.Id,
                    hltbDataUser.GameHltbData?.MainStoryClassic ?? 0,
                    hltbDataUser.GameHltbData?.TimeToBeat ?? 0,
                    hltbDataUser.Url ?? string.Empty,
                    hltbDataUser.UrlImg ?? string.Empty));

                return hltbDataUser;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
                Logger.Warn(string.Format("HLTB UpdateGameData FAILED hltbId='{0}': {1}", hltbDataUser.Id, ex.Message));
                return null;
            }
        }


        #region Search

        /// <summary>
        /// Returns the search API path from plugin settings, or <see cref="DefaultSearchApiEndpoint"/>.
        /// </summary>
        private static string GetPersistedSearchEndpoint()
        {
            try
            {
                string fromSettings = PluginDatabase?.PluginSettings?.SearchApiEndpoint;
                if (!fromSettings.IsNullOrWhiteSpace())
                {
                    return NormalizeSearchApiEndpoint(fromSettings);
                }
            }
            catch { }

            return DefaultSearchApiEndpoint;
        }

        /// <summary>
        /// Normalizes a search API path to <c>/api/…</c> form.
        /// </summary>
        private static string NormalizeSearchApiEndpoint(string endpoint)
        {
            if (endpoint.IsNullOrWhiteSpace())
            {
                return DefaultSearchApiEndpoint;
            }

            endpoint = endpoint.Trim();
            if (!endpoint.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
            {
                endpoint = "/api/" + endpoint.TrimStart('/');
            }

            return endpoint;
        }

        /// <summary>
        /// Returns true for obsolete HLTB search paths that must not be persisted.
        /// </summary>
        private static bool IsLegacySearchEndpoint(string endpoint)
        {
            if (endpoint.IsNullOrWhiteSpace())
            {
                return true;
            }

            if (string.Equals(endpoint, "/api/bleed", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(endpoint, "/api/find", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string trimmed = endpoint.Trim('/');
            if (trimmed.StartsWith("api/", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed.Substring(4);
            }

            return string.Equals(trimmed, "find", StringComparison.OrdinalIgnoreCase)
                || string.Equals(trimmed, "bleed", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Loads the persisted search endpoint into the in-session <see cref="SearchUrl"/> cache.
        /// </summary>
        private static void HydrateSearchUrlFromSettings()
        {
            string persisted = GetPersistedSearchEndpoint();
            CacheSearchUrlInMemory(persisted);
            try { Common.LogDebug(true, $"HLTB search: hydrated SearchUrl from settings endpoint='{persisted}'"); } catch { }
        }

        /// <summary>
        /// Updates the in-session search endpoint cache.
        /// </summary>
        private static void CacheSearchUrlInMemory(string endpoint)
        {
            if (endpoint.IsNullOrWhiteSpace())
            {
                return;
            }

            lock (SearchUrlLock)
            {
                SearchUrl = endpoint;
            }
        }

        /// <summary>
        /// Uses the persisted search endpoint as fallback and refreshes the in-session cache.
        /// </summary>
        private static string UsePersistedSearchEndpointFallback(string reason)
        {
            string endpoint = SearchEndPoint;
            CacheSearchUrlInMemory(endpoint);
            try { Logger.Warn($"HLTB search: {reason}; using persisted endpoint '{endpoint}'"); } catch { }
            return endpoint;
        }

        /// <summary>
        /// Saves a search endpoint to plugin settings after a successful auth init.
        /// </summary>
        private void PersistSearchApiEndpoint(string apiEndpoint)
        {
            try
            {
                if (apiEndpoint.IsNullOrWhiteSpace() || IsLegacySearchEndpoint(apiEndpoint))
                {
                    return;
                }

                string normalized = NormalizeSearchApiEndpoint(apiEndpoint);
                var settings = PluginDatabase?.PluginSettings;
                var plugin = PluginDatabase?.Plugin;
                if (settings == null || plugin == null)
                {
                    return;
                }

                if (string.Equals(settings.SearchApiEndpoint ?? string.Empty, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                settings.SearchApiEndpoint = normalized;
                plugin.SavePluginSettings(settings);
                CacheSearchUrlInMemory(normalized);
                try { Logger.Info($"HLTB search: persisted endpoint '{normalized}' to settings"); } catch { }
                Common.LogDebug(true, $"HLTB search: persisted endpoint='{normalized}'");
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        /// <summary>
        /// Clears the in-session discovered search endpoint so the next lookup can re-scrape or use <see cref="SearchEndPoint"/>.
        /// </summary>
        /// <param name="reason">Short explanation for logs.</param>
        private void ClearDiscoveredSearchUrl(string reason)
        {
            lock (SearchUrlLock)
            {
                if (SearchUrl.IsNullOrEmpty())
                {
                    return;
                }

                try { Logger.Warn($"HLTB search: clearing discovered endpoint '{SearchUrl}' ({reason})"); } catch { }
                SearchUrl = null;
            }
        }

        /// <summary>
        /// Retrieves the search URL from the website scripts, or falls back to <see cref="SearchEndPoint"/>.
        /// Concurrent callers share a single in-flight discovery (single-flight).
        /// </summary>
        /// <param name="forceRediscover">When true, ignores the cached discovered endpoint and re-scrapes the site.</param>
        /// <returns>The search API path (for example <c>/api/search/site</c>).</returns>
        private async Task<string> GetSearchUrl(bool forceRediscover = false)
        {
            if (!forceRediscover && !SearchUrl.IsNullOrEmpty() && !SearchUrl.Contains("error"))
            {
                try { Logger.Info($"HLTB search: using cached discovered endpoint '{SearchUrl}'"); } catch { }
                Common.LogDebug(true, $"GetSearchUrl: cache hit endpoint='{SearchUrl}'");
                return SearchUrl;
            }

            await SearchUrlDiscoverySync.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!forceRediscover && !SearchUrl.IsNullOrEmpty() && !SearchUrl.Contains("error"))
                {
                    Common.LogDebug(true, $"GetSearchUrl: cache hit after discovery wait endpoint='{SearchUrl}'");
                    return SearchUrl;
                }

                if (forceRediscover)
                {
                    SearchUrl = null;
                    try { Logger.Info("HLTB search: re-discovering search endpoint (forced)"); } catch { }
                }

                return await DiscoverSearchUrlAsync().ConfigureAwait(false);
            }
            finally
            {
                try { SearchUrlDiscoverySync.Release(); } catch { }
            }
        }

        /// <summary>
        /// Scrapes HLTB scripts for the search API path. Must run under <see cref="SearchUrlDiscoverySync"/>.
        /// </summary>
        private async Task<string> DiscoverSearchUrlAsync()
        {
            try
            {
                string url = UrlBase;

                string response = null;
                using (var cts = new CancellationTokenSource(ScriptDownloadTimeoutMs))
                {
                    try
                    {
                        await WaitForHttpRateLimitAsync("GET homepage", url, cts.Token).ConfigureAwait(false);
                        using (var httpResp = await httpClient.GetAsync(url, cts.Token).ConfigureAwait(false))
                        {
                            if (!httpResp.IsSuccessStatusCode)
                            {
                                return UsePersistedSearchEndpointFallback($"homepage HTTP {(int)httpResp.StatusCode}");
                            }

                            response = await httpResp.Content.ReadAsStringAsync().ConfigureAwait(false);
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        return UsePersistedSearchEndpointFallback($"homepage timeout ({ScriptDownloadTimeoutMs}ms)");
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, true, PluginDatabase.PluginName);
                        return UsePersistedSearchEndpointFallback("homepage download error");
                    }
                }

                List<string> scriptUrls = ExtractScriptUrlsWithHap(response) ?? new List<string>();
                if (scriptUrls.Count == 0)
                {
                    var matches = MyRegex().Matches(response);
                    foreach (Match match in matches)
                    {
                        scriptUrls.Add(match.Groups[1].Value);
                    }
                }

                var ordered = scriptUrls.Where(s => s.Contains("_app-")).Concat(scriptUrls).Where(s => !string.IsNullOrEmpty(s)).Distinct();
                foreach (string sUrl in ordered)
                {
                    string scriptUrl = sUrl;
                    if (!scriptUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        scriptUrl = UrlBase + scriptUrl;
                    }

                    string scriptContent = null;
                    using (var ctsScript = new CancellationTokenSource(ScriptDownloadTimeoutMs))
                    {
                        try
                        {
                            await WaitForHttpRateLimitAsync("GET script", scriptUrl, ctsScript.Token).ConfigureAwait(false);
                            using (var scriptResp = await httpClient.GetAsync(scriptUrl, ctsScript.Token).ConfigureAwait(false))
                            {
                                if (!scriptResp.IsSuccessStatusCode)
                                {
                                    try { Logger.Warn($"HTTP {(int)scriptResp.StatusCode} downloading {scriptUrl}"); } catch { }
                                    continue;
                                }

                                scriptContent = await scriptResp.Content.ReadAsStringAsync().ConfigureAwait(false);
                            }
                        }
                        catch (TaskCanceledException)
                        {
                            try { Logger.Warn($"Timeout {ScriptDownloadTimeoutMs}ms downloading {scriptUrl}"); } catch { }
                            continue;
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false, true, PluginDatabase.PluginName);
                            continue;
                        }
                    }

                    string pattern = "fetch\\s*\\(\\s*[\"']\\/api\\/([a-zA-Z0-9_\\/]+)[^\"']*[\"']\\s*,\\s*\\{[^}]*method:\\s*[\"']POST[\"'][^}]*\\}";
                    var searchMatch = Regex.Match(scriptContent, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
                    if (searchMatch.Success)
                    {
                        // Keep the full path (e.g. "search/site"); truncating to the first segment
                        // produced a dead "/api/search" after HLTB moved the POST endpoint.
                        string suffix = searchMatch.Groups[1].Value?.Trim('/');
                        if (suffix.IsNullOrEmpty())
                        {
                            continue;
                        }

                        string firstSegment = suffix.Contains("/") ? suffix.Split('/')[0] : suffix;
                        if (!string.Equals(firstSegment, "find", StringComparison.OrdinalIgnoreCase))
                        {
                            string discovered = "/api/" + suffix;
                            bool newlyCached = false;
                            lock (SearchUrlLock)
                            {
                                if (SearchUrl.IsNullOrEmpty())
                                {
                                    SearchUrl = discovered;
                                    newlyCached = true;
                                }
                            }

                            if (newlyCached)
                            {
                                try { Logger.Info($"HLTB search: discovered endpoint '{SearchUrl}' from script '{scriptUrl}'"); } catch { }
                            }

                            Common.LogDebug(true, $"GetSearchUrl: discovered suffix='{suffix}' endpoint='{SearchUrl}' newlyCached={newlyCached}");
                            return SearchUrl;
                        }

                        Common.LogDebug(true, $"GetSearchUrl: ignoring legacy endpoint suffix 'find' in '{scriptUrl}'");
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            return UsePersistedSearchEndpointFallback("no endpoint discovered from scripts");
        }

        /// <summary>
        /// Resolves auth headers for an API search, with cache invalidation and <see cref="SearchEndPoint"/> fallback.
        /// </summary>
        /// <param name="gameName">Game name used for diagnostic logs.</param>
        /// <returns>
        /// Item1: header dictionary; Item2: API path used for the search POST.
        /// Returns null when auth cannot be obtained.
        /// </returns>
        private async Task<Tuple<Dictionary<string, string>, string>> ResolveSearchAuthHeadersAsync(string gameName)
        {
            string searchUrl = await GetSearchUrl().ConfigureAwait(false);
            Dictionary<string, string> headerParts = await GetAuthToken(searchUrl).ConfigureAwait(false);
            if (headerParts != null)
            {
                Common.LogDebug(true, $"ResolveSearchAuthHeaders: auth ok endpoint='{searchUrl}' game='{gameName}'");
                return Tuple.Create(headerParts, searchUrl);
            }

            try { Logger.Warn($"HLTB search: auth init failed for endpoint='{searchUrl}' game='{gameName}'"); } catch { }

            if (string.Equals(searchUrl, SearchEndPoint, StringComparison.OrdinalIgnoreCase))
            {
                try { Logger.Warn($"HLTB search: auth unavailable on SearchEndPoint '{SearchEndPoint}'; search aborted for '{gameName}'"); } catch { }
                return null;
            }

            ClearDiscoveredSearchUrl("auth init failed for discovered endpoint");
            string rediscoveredUrl = await GetSearchUrl(forceRediscover: true).ConfigureAwait(false);
            if (!string.Equals(rediscoveredUrl, searchUrl, StringComparison.OrdinalIgnoreCase))
            {
                headerParts = await GetAuthToken(rediscoveredUrl).ConfigureAwait(false);
                if (headerParts != null)
                {
                    try { Logger.Info($"HLTB search: auth ok after re-discover endpoint='{rediscoveredUrl}' game='{gameName}'"); } catch { }
                    return Tuple.Create(headerParts, rediscoveredUrl);
                }

                try { Logger.Warn($"HLTB search: auth init failed after re-discover endpoint='{rediscoveredUrl}' game='{gameName}'"); } catch { }
            }

            if (!string.Equals(rediscoveredUrl, SearchEndPoint, StringComparison.OrdinalIgnoreCase))
            {
                try { Logger.Warn($"HLTB search: retrying auth with SearchEndPoint '{SearchEndPoint}' game='{gameName}'"); } catch { }
                headerParts = await GetAuthToken(SearchEndPoint).ConfigureAwait(false);
                if (headerParts != null)
                {
                    try { Logger.Info($"HLTB search: auth ok using SearchEndPoint '{SearchEndPoint}' game='{gameName}'"); } catch { }
                    return Tuple.Create(headerParts, SearchEndPoint);
                }

                try { Logger.Warn($"HLTB search: auth init failed on SearchEndPoint '{SearchEndPoint}' game='{gameName}'"); } catch { }
            }

            return null;
        }

        /// <summary>
        /// Retrieves the authentication token.
        /// </summary>
        /// <returns>The auth token.</returns>
        private async Task<Dictionary<string, string>> GetAuthToken(string apiEndpoint)
        {
            try
            {
                // Double-checked locking: quick snapshot first to avoid locking in common case
                var snapshotToken = CachedAuthToken;
                var snapshotExpiry = CachedAuthTokenExpiry;
                var snapshotHeaders = CachedAuthHeaderParts;
                var snapshotEndpoint = CachedAuthEndpoint;
                if (!string.IsNullOrEmpty(snapshotToken)
                    && DateTime.UtcNow < snapshotExpiry
                    && snapshotHeaders != null
                    && string.Equals(snapshotEndpoint ?? string.Empty, apiEndpoint ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        Common.LogDebug(true, $"HLTB auth cache hit (snapshot) endpoint='{apiEndpoint}' ttlMs={(int)Math.Max(0, (snapshotExpiry - DateTime.UtcNow).TotalMilliseconds)}");
                    }
                    catch { }
                    return new Dictionary<string, string>(snapshotHeaders, StringComparer.Ordinal);
                }

                // Re-check under lock to avoid race with a concurrent writer
                lock (AuthTokenSync)
                {
                    if (!string.IsNullOrEmpty(CachedAuthToken)
                        && DateTime.UtcNow < CachedAuthTokenExpiry
                        && CachedAuthHeaderParts != null
                        && string.Equals(CachedAuthEndpoint ?? string.Empty, apiEndpoint ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            Common.LogDebug(true, $"HLTB auth cache hit (lock) endpoint='{apiEndpoint}' ttlMs={(int)Math.Max(0, (CachedAuthTokenExpiry - DateTime.UtcNow).TotalMilliseconds)}");
                        }
                        catch { }
                        return new Dictionary<string, string>(CachedAuthHeaderParts, StringComparer.Ordinal);
                    }
                }

                try { Common.LogDebug(true, $"HLTB auth cache miss endpoint='{apiEndpoint}'"); } catch { }

                List<HttpHeader> headers = new List<HttpHeader>
                {
                    new HttpHeader { Key = "Referer", Value = UrlBase }
                };
                string url = $"{UrlBase}{apiEndpoint}/init?t={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
                string response = null;
                try
                {
                    using (var cts = new CancellationTokenSource(ScriptDownloadTimeoutMs))
                    using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                    {
                        try { request.Headers.Add("Referer", UrlBase); } catch { }

                        await WaitForHttpRateLimitAsync("GET auth init", url, cts.Token).ConfigureAwait(false);
                        using (var resp = await httpClient.SendAsync(request, cts.Token).ConfigureAwait(false))
                        {
                            if (!resp.IsSuccessStatusCode)
                            {
                                try { Logger.Warn($"HLTB search: auth init HTTP {(int)resp.StatusCode} for endpoint='{apiEndpoint}' url={url}"); } catch { }
                                return null;
                            }

                            response = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    try { Logger.Warn($"HLTB search: auth init timeout ({ScriptDownloadTimeoutMs}ms) for endpoint='{apiEndpoint}' url={url}"); } catch { }
                    return null;
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                    return null;
                }

                var data = Serialization.FromJson<Dictionary<string, string>>(response);
                if (data != null && data.TryGetValue("token", out string token))
                {
                    Dictionary<string, string> headerParts = null;
                    if (data.TryGetValue("hpKey", out string hpKey) && data.TryGetValue("hpVal", out string hpVal))
                    {
                        headerParts = new Dictionary<string, string>(StringComparer.Ordinal)
                        {
                            { "Token", token },
                            { "Hpkey", hpKey },
                            { "Hpval", hpVal }
                        };
                    }

                    lock (AuthTokenSync)
                    {
                        CachedAuthToken = token;
                        CachedAuthTokenExpiry = DateTime.UtcNow.AddSeconds(90);
                        CachedAuthEndpoint = apiEndpoint;
                        CachedAuthHeaderParts = headerParts;
                    }

                    if (headerParts != null)
                    {
                        try { Common.LogDebug(true, $"HLTB auth cache store endpoint='{apiEndpoint}' ttlSec=90 hasHp=1"); } catch { }
                        PersistSearchApiEndpoint(apiEndpoint);
                        return headerParts;
                    }

                    try { Logger.Warn($"HLTB search: auth init missing hpKey/hpVal for endpoint='{apiEndpoint}'"); } catch { }
                }
                else
                {
                    try { Logger.Warn($"HLTB search: auth init response missing token for endpoint='{apiEndpoint}'"); } catch { }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
            return null;
        }


        /// <summary>
        /// Searches for games on HowLongToBeat by name and platform.
        /// </summary>
        /// <param name="name">Game name to search for.</param>
        /// <param name="platform">Optional platform filter.</param>
        /// <returns>Returns a list of <see cref="HltbDataUser"/> matching the search.</returns>
        private async Task<List<HltbDataUser>> Search(string name, string platform = "")
        {
            try
            {
                SearchResult searchResult = await ApiSearch(name, platform);

                List<HltbDataUser> search = searchResult?.Data?.Select(x =>
                    new HltbDataUser
                    {
                        Name = x.GameName,
                        Id = x.GameId.ToString(),
                        UrlImg = string.Format(UrlGameImg, x.GameImage),
                        Url = string.Format(UrlGame, x.GameId),
                        Platform = x.ProfilePlatform,
                        GameType = x.GameType.IsEqual("game") ? GameType.Game : x.GameType.IsEqual("multi") ? GameType.Multi : GameType.Compil,
                        GameHltbData = new HltbData
                        {
                            GameType = x.GameType.IsEqual("game") ? GameType.Game : x.GameType.IsEqual("multi") ? GameType.Multi : GameType.Compil,
                            MainStoryClassic = x.CompMain,
                            MainExtraClassic = x.CompPlus,
                            CompletionistClassic = x.Comp100,
                            SoloClassic = x.CompAll,
                            CoOpClassic = x.InvestedCo,
                            VsClassic = x.InvestedMp
                        },
                        NeedsDetails = !((x.CompMain > 0) || (x.CompAll > 0) || (x.CompPlus > 0) || (x.Comp100 > 0))
                    }
                )?.ToList() ?? new List<HltbDataUser>();

                if (search.Count != 0)
                {
                    try
                    {
                        int target = ConcurrencyController?.TargetConcurrency ?? MaxParallelGameDataDownloads;
                        await AdjustSemaphoreLimit(DynamicSemaphore, () => CurrentSemaphoreLimit, l => CurrentSemaphoreLimit = l, target, ConcurrencySync, "Search");
                    }
                    catch { }

                    var tasks = search.Select(async x =>
                    {
                        bool acquiredGameSemaphore = false;
                        try
                        {
                            try
                            {
                                try
                                {
                                    int targetGameLog = ConcurrencyController?.TargetConcurrency ?? MaxParallelGameDataDownloads;
                                    int availableGameLog = DynamicSemaphore?.CurrentCount ?? 0;
                                    int inFlightGameLog = Math.Max(0, targetGameLog - availableGameLog);
                                    Common.LogDebug(true, $"Search: waiting semaphore for id={x.Id} target={targetGameLog} currentLimit={CurrentSemaphoreLimit} available={availableGameLog} inFlight={inFlightGameLog}");
                                }
                                catch { }
                                var acquired = await DynamicSemaphore.WaitAsync(TimeSpan.FromSeconds(10));
                                if (!acquired)
                                {
                                    Logger.Warn($"Search: timeout waiting for game data semaphore for id={x.Id}");
                                    return;
                                }
                                acquiredGameSemaphore = true;
                                try
                                {
                                    int targetGameLog = ConcurrencyController?.TargetConcurrency ?? MaxParallelGameDataDownloads;
                                    int availableGameLog = DynamicSemaphore?.CurrentCount ?? 0;
                                    int inFlightGameLog = Math.Max(0, targetGameLog - availableGameLog);
                                    Common.LogDebug(true, $"Search: acquired semaphore for id={x.Id} target={targetGameLog} currentLimit={CurrentSemaphoreLimit} available={availableGameLog} inFlight={inFlightGameLog}");
                                }
                                catch { }
                            }
                            catch (Exception ex)
                            {
                                Common.LogError(ex, false, false, PluginDatabase.PluginName);
                            }
                            try
                            {
                                try
                                {
                                    bool hasCoreTimes = (x.GameHltbData != null) && (
                                        (x.GameHltbData.MainStoryClassic > 0) ||
                                        (x.GameHltbData.MainStoryAverage > 0) ||
                                        (x.GameHltbData.MainStoryMedian > 0)
                                    );

                                    if (hasCoreTimes)
                                    {
                                        Common.LogDebug(true, string.Format("HLTB Search: skipping GetGamePageData for id={0} (search result already has times)", x.Id));
                                        x.NeedsDetails = false;
                                    }
                                    else
                                    {
                                        using (var ctsGame = new CancellationTokenSource(GameDataDownloadTimeoutMs))
                                        {
                                            try
                                            {
                                                x.GameHltbData = await GetGameData(x.Id, ctsGame.Token);
                                                x.NeedsDetails = false;
                                            }
                                            catch (OperationCanceledException)
                                            {
                                                Logger.Warn($"Timeout {GameDataDownloadTimeoutMs}ms getting game data for {x.Id}");
                                                x.GameHltbData = null;
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Common.LogError(ex, false, false, PluginDatabase.PluginName);
                                    x.GameHltbData = null;
                                }
                            }
                            finally
                            {
                                if (acquiredGameSemaphore)
                                {
                                    try
                                    {
                                        DynamicSemaphore.Release();
                                    }
                                    catch { }
                                }
                                Common.LogDebug(true, $"Search: released semaphore for id={x.Id}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false, false, PluginDatabase.PluginName);
                        }
                    }).ToArray();

                    await Task.WhenAll(tasks);
                    try
                    {
                        Common.LogDebug(true, $"Search summary: persistentCacheHits={PersistentCacheHits}, inMemoryHits={InMemoryCacheHits}, pageFetches={PageFetches}");
                    }
                    catch { }
                }
                return search;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
                return new List<HltbDataUser>();
            }
        }

        /// <summary>
        /// Performs two search methods (normalized and original name) and merges results.
        /// </summary>
        /// <param name="name">Game name to search for.</param>
        /// <param name="platform">Optional platform filter.</param>
        /// <returns>Returns a list of <see cref="HltbSearch"/> with match percentages.</returns>
        public async Task<List<HltbSearch>> SearchTwoMethod(string name, string platform = "", bool includeExtendedTimes = false)
        {
            // Apply manual aliases (exact normalized match) to support paired releases (e.g. Pokemon versions).
            try
            {
                var settings = PluginDatabase?.PluginSettings;
                var userDataPath = PluginDatabase?.Plugin?.GetPluginUserDataPath();
                var aliased = GameNameAliases.ApplyAlias(name, settings, userDataPath);
                if (!string.IsNullOrEmpty(aliased) && !aliased.IsEqual(name))
                {
                    Common.LogDebug(true, $"HLTB aliases: '{SafeStr(name)}' -> '{SafeStr(aliased)}'");
                    name = aliased;
                }
            }
            catch { }

            string normalized = PlayniteTools.NormalizeGameName(name, true, true);

            List<HltbDataUser> dataSearch = null;
            List<HltbDataUser> dataSearchNormalized = null;

            try
            {
                if (!string.IsNullOrEmpty(normalized) && !normalized.Equals(name, StringComparison.Ordinal))
                {
                    var t1 = Search(name, platform);
                    var t2 = Search(normalized, platform);
                    await Task.WhenAll(t1, t2).ConfigureAwait(false);
                    dataSearch = t1.Result ?? new List<HltbDataUser>();
                    dataSearchNormalized = t2.Result ?? new List<HltbDataUser>();
                }
                else
                {
                    dataSearch = await Search(name, platform);
                    dataSearchNormalized = new List<HltbDataUser>();
                }
            }
            catch (Exception ex)
            {
                try { Common.LogError(ex, false, true, PluginDatabase.PluginName); } catch { }
                dataSearch = dataSearch ?? new List<HltbDataUser>();
                dataSearchNormalized = dataSearchNormalized ?? new List<HltbDataUser>();
            }

            var dataSearchFinal = new List<HltbDataUser>();
            if (dataSearch != null) dataSearchFinal.AddRange(dataSearch);
            if (dataSearchNormalized != null) dataSearchFinal.AddRange(dataSearchNormalized);

            dataSearchFinal = dataSearchFinal.GroupBy(x => x.Id).Select(x => x.First()).ToList();

            string searchNameLower = (name ?? string.Empty).ToLower();
            var results = dataSearchFinal
                .Where(x => x != null)
                .Select(x => new HltbSearch
                {
                    MatchPercent = Fuzz.Ratio(searchNameLower, (x.Name ?? string.Empty).ToLower()),
                    Data = x
                })
                .OrderByDescending(x => x.MatchPercent)
                .ToList();

            if (includeExtendedTimes)
            {
                try
                {
                    const int maxDetails = 20;
                    var targets = results
                        .Select(r => r?.Data)
                        .Where(x => x != null && !string.IsNullOrEmpty(x.Id))
                        .Take(maxDetails)
                        .ToList();

                    if (targets.Count > 0)
                    {
                        try
                        {
                            int target = ConcurrencyController?.TargetConcurrency ?? MaxParallelGameDataDownloads;
                            await AdjustSemaphoreLimit(DynamicSemaphore, () => CurrentSemaphoreLimit, l => CurrentSemaphoreLimit = l, target, ConcurrencySync, "SearchTwoMethod+Details");
                        }
                        catch { }

                        var tasks = targets.Select(async x =>
                        {
                            bool acquiredGameSemaphore = false;
                            try
                            {
                                var acquired = await DynamicSemaphore.WaitAsync(TimeSpan.FromSeconds(10));
                                if (!acquired)
                                {
                                    return;
                                }
                                acquiredGameSemaphore = true;

                                using (var ctsGame = new CancellationTokenSource(GameDataDownloadTimeoutMs))
                                {
                                    try
                                    {
                                        var details = await GetGameData(x.Id, ctsGame.Token);
                                        if (details != null)
                                        {
                                            x.GameHltbData = details;
                                            x.NeedsDetails = false;
                                        }
                                    }
                                    catch (OperationCanceledException)
                                    {
                                        // Ignore timeout; keep basic search data
                                    }
                                    catch
                                    {
                                        // Ignore; keep basic search data
                                    }
                                }
                            }
                            finally
                            {
                                if (acquiredGameSemaphore)
                                {
                                    try { DynamicSemaphore.Release(); } catch { }
                                }
                            }
                        }).ToArray();

                        await Task.WhenAll(tasks).ConfigureAwait(false);
                    }
                }
                catch
                {
                    // Best-effort enrichment; never fail the whole search.
                }
            }

            return results;
        }

        /// <summary>
        /// Performs an API search for games.
        /// </summary>
        /// <param name="name">Game name to search for.</param>
        /// <param name="platform">Optional platform filter.</param>
        /// <returns>Returns a <see cref="SearchResult"/> object.</returns>
        private async Task<SearchResult> ApiSearch(string name, string platform = "")
        {
            try { EnsureMonitoringStarted(); } catch { }
            int GetSearchTarget()
            {
                try
                {
                    int baseTarget = MaxParallelSearches;
                    lock (BackoffSync)
                    {
                        if (SearchBackoffLimit > 0 && DateTime.UtcNow < SearchBackoffUntil)
                        {
                            return Math.Min(baseTarget, SearchBackoffLimit);
                        }
                    }
                    return baseTarget;
                }
                catch
                {
                    return SearchConcurrencyController?.TargetConcurrency ?? MaxParallelSearches;
                }
            }

            try
            {
                string cacheKey = (name ?? string.Empty) + "|" + (platform ?? string.Empty);
                if (SearchCache.TryGetValue(cacheKey, out SearchResult cachedResult))
                {
                    try { Common.LogDebug(true, $"ApiSearch cache hit for '{name}' platform='" + platform + "'"); } catch { }
                    return cachedResult;
                }

                List<HttpHeader> httpHeaders = new List<HttpHeader>
                {
                    new HttpHeader { Key = "User-Agent", Value = Web.UserAgent },
                    new HttpHeader { Key = "Origin", Value = UrlBase },
                    new HttpHeader { Key = "Referer", Value = UrlBase }
                };

                SearchParam searchParam = new SearchParam
                {
                    SearchTerms = name.Split(' ').ToList(),
                    SearchOptions = new SearchOptions { Games = new Games { Platform = platform } }
                };

                SearchResult searchResult = null;
                string baseJson = Serialization.ToJson(searchParam);
                var dict = Serialization.FromJson<Dictionary<string, object>>(baseJson);
                bool tokenReused = !string.IsNullOrEmpty(CachedAuthToken) && DateTime.UtcNow < CachedAuthTokenExpiry;
                Tuple<Dictionary<string, string>, string> authResolution = await ResolveSearchAuthHeadersAsync(name).ConfigureAwait(false);
                if (authResolution == null)
                {
                    return null;
                }

                Dictionary<string, string> headerParts = authResolution.Item1;
                string searchUrl = authResolution.Item2;
                Common.LogDebug(true, $"ApiSearch: POST endpoint='{searchUrl}' game='{name}' platform='{platform}'");
                string token = headerParts.TryGetValue("Token", out string tokenValue) ? tokenValue : null;
                string hpKey = headerParts.TryGetValue("Hpkey", out string hpKeyValue) ? hpKeyValue : null;
                string hpVal = headerParts.TryGetValue("Hpval", out string hpValValue) ? hpValValue : null;
                if (!token.IsNullOrEmpty())
                {
                    httpHeaders.Add(new HttpHeader { Key = "x-auth-token", Value = token });
                }

                if (!hpKey.IsNullOrEmpty() && !hpVal.IsNullOrEmpty())
                {
                    httpHeaders.Add(new HttpHeader { Key = "x-hp-key", Value = hpKey });
                    httpHeaders.Add(new HttpHeader { Key = "x-hp-val", Value = hpVal });
                    dict[hpKey] = hpVal;
                }

                string serializedBody = Serialization.ToJson(dict);

                bool acquired = false;
                try
                {
                    try
                    {
                        try
                        {
                            int target = GetSearchTarget();
                            await AdjustSemaphoreLimit(SearchSemaphore, () => CurrentSearchLimit, l => CurrentSearchLimit = l, target, SearchConcurrencySync, "ApiSearch+Initial");
                        }
                        catch { }

                        try
                        {
                            int targetLog = GetSearchTarget();
                            int availableLog = SearchSemaphore?.CurrentCount ?? 0;
                            int inFlightLog = Math.Max(0, targetLog - availableLog);
                            Common.LogDebug(true, $"ApiSearch: waiting search semaphore for '{name}' target={targetLog} currentLimit={CurrentSearchLimit} available={availableLog} inFlight={inFlightLog}");
                        }
                        catch { }
                        bool waitOk = true;
                        if (SearchSemaphore != null)
                        {
                            waitOk = await SearchSemaphore.WaitAsync(TimeSpan.FromSeconds(10));
                        }
                        if (!waitOk)
                        {
                            Logger.Warn($"ApiSearch: timeout waiting search semaphore for '{name}'");
                            return null;
                        }
                        acquired = true;
                        try
                        {
                            int targetLog = GetSearchTarget();
                            int availableLog = SearchSemaphore?.CurrentCount ?? 0;
                            int inFlightLog = Math.Max(0, targetLog - availableLog);
                            Common.LogDebug(true, $"ApiSearch: acquired search semaphore for '{name}' target={targetLog} currentLimit={CurrentSearchLimit} available={availableLog} inFlight={inFlightLog}");
                        }
                        catch { }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, false, PluginDatabase.PluginName);
                    }

                    var sw = Stopwatch.StartNew();
                    // Use local helper to avoid dependency on optional CommonPluginsShared submodule.
                    // Allow the POST to be cancelled/timeout independently of the shared HttpClient timeout by using a bounded CTS.
                    (string body, int status, string retry) postResult;
                    using (var postCts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                    {
                        postResult = await PostJsonWithSharedClientWithStatus(UrlBase + searchUrl, serializedBody, httpHeaders, postCts.Token);
                    }
                    sw.Stop();
                    string json = postResult.body ?? string.Empty;
                    int statusCode = postResult.status;
                    string retryAfterHeader = postResult.retry;

                    try
                    {
                        if (statusCode == 429)
                        {
                            int backoffSeconds = 30;
                            if (!string.IsNullOrEmpty(retryAfterHeader) && int.TryParse(retryAfterHeader, out int parsedSeconds))
                            {
                                backoffSeconds = Math.Max(1, parsedSeconds);
                            }

                            lock (BackoffSync)
                            {
                                int newLimit = Math.Max(1, CurrentSearchLimit / 2);
                                SearchBackoffLimit = newLimit;
                                SearchBackoffUntil = DateTime.UtcNow.AddSeconds(backoffSeconds);
                            }

                            Logger.Warn($"ApiSearch: received 429 for '{name}'. Immediate backoff -> limit={SearchBackoffLimit} until {SearchBackoffUntil:HH:mm:ss}");
                        }
                    }
                    catch { }

                    try
                    {
                        try
                        {
                            RecentSearchSamples.Enqueue(sw.ElapsedMilliseconds);
                            while (RecentSearchSamples.Count > RecentSamplesWindow)
                            {
                                RecentSearchSamples.TryDequeue(out _);
                            }
                        }
                        catch { }

                        try
                        {
                            RecentSearchStatusCodes.Enqueue(statusCode);
                            while (RecentSearchStatusCodes.Count > RecentStatusWindow)
                            {
                                RecentSearchStatusCodes.TryDequeue(out _);
                            }
                        }
                        catch { }

                        Common.LogDebug(true, $"ApiSearch elapsed={sw.ElapsedMilliseconds}ms tokenReused={tokenReused} status={statusCode}");
                    }
                    catch { }

                    _ = Serialization.TryFromJson(json, out searchResult);

                    try
                    {
                        bool successSample = searchResult != null && searchResult.Data != null && statusCode != 429;
                        SearchConcurrencyController?.ReportSample(sw.ElapsedMilliseconds, successSample);
                    }
                    catch { }

                    int postLockPendingConsume = 0;
                    try
                    {
                        var codes = RecentSearchStatusCodes.ToArray();
                        if (codes.Length > 0)
                        {
                            int count429 = codes.Count(c => c == 429);
                            double frac = count429 / (double)codes.Length;
                            if (count429 >= 3 && frac >= 0.05)
                            {
                                lock (BackoffSync)
                                {
                                    if (SearchBackoffLimit == 0 || DateTime.UtcNow >= SearchBackoffUntil)
                                    {
                                        int newLimit = Math.Max(1, CurrentSearchLimit / 2);
                                        SearchBackoffLimit = newLimit;

                                        int backoffSeconds = 30;
                                        try
                                        {
                                            if (!string.IsNullOrEmpty(retryAfterHeader) && int.TryParse(retryAfterHeader, out int parsed))
                                            {
                                                backoffSeconds = Math.Max(1, parsed);
                                            }
                                        }
                                        catch { }
                                        SearchBackoffUntil = DateTime.UtcNow.AddSeconds(backoffSeconds);
                                        Logger.Warn($"ApiSearch: detected elevated 429 rate ({count429}/{codes.Length}). Applying temporary search backoff -> limit={SearchBackoffLimit} until {SearchBackoffUntil:HH:mm:ss}");
                                        try
                                        {
                                            int currentLimitSnapshot;
                                            lock (SearchConcurrencySync)
                                            {
                                                currentLimitSnapshot = CurrentSearchLimit;
                                            }
                                            int diff = SearchBackoffLimit - currentLimitSnapshot;
                                            if (diff > 0)
                                            {
                                                try { SearchSemaphore.Release(diff); } catch { }
                                                lock (SearchConcurrencySync)
                                                {
                                                    CurrentSearchLimit = SearchBackoffLimit;
                                                }
                                            }
                                            else if (diff < 0)
                                            {
                                                postLockPendingConsume = -diff;
                                            }
                                        }
                                        catch { }
                                    }
                                }
                            }
                        }
                    }
                    catch { }

                    try
                    {
                        if (postLockPendingConsume > 0)
                        {
                            int backoffSnapshot;
                            lock (BackoffSync)
                            {
                                backoffSnapshot = SearchBackoffLimit;
                            }

                            await AdjustSemaphoreLimit(SearchSemaphore, () => CurrentSearchLimit, l => CurrentSearchLimit = l, backoffSnapshot, SearchConcurrencySync, "ApiSearch+PostBackoff");
                        }
                    }
                    catch { }

                    try
                    {
                        if (searchResult != null)
                        {
                            SearchCache.TryAdd(cacheKey, searchResult);
                        }
                    }
                    catch { }

                    return searchResult;
                }
                finally
                {
                    if (acquired)
                    {
                        try
                        {
                            SearchSemaphore.Release();
                            Common.LogDebug(true, $"ApiSearch: released search semaphore for '{name}'");
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
                return null;
            }
        }

        /// <summary>
        /// Opens a selection window for the user to choose the correct game data.
        /// </summary>
        /// <param name="game">The Playnite game object.</param>
        /// <param name="data">Optional list of search results.</param>
        /// <returns>Returns a <see cref="GameHowLongToBeat"/> object if a selection is made, otherwise null.</returns>
        public GameHowLongToBeat SearchData(Game game, List<HltbDataUser> data = null)
        {
            string openMode = data == null ? "SearchDialog-AutoSearch" : string.Format("SearchDialog-PreloadedResults({0})", data.Count);
            Logger.Info(string.Format("HLTB SearchData OPEN: playniteGame='{0}' mode={1}", game?.Name ?? string.Empty, openMode));
            Common.LogDebug(true, string.Format("HLTB SearchData OPEN: gameId={0} mode={1}", game?.Id, openMode));

            if (API.Instance.ApplicationInfo.Mode == ApplicationMode.Desktop)
            {
                HowLongToBeatSelect ViewExtension = null;
                _ = Application.Current.Dispatcher.BeginInvoke((Action)delegate
                {
                    WindowOptions windowOptions = new WindowOptions
                    {
                        ShowMinimizeButton = false,
                        ShowMaximizeButton = false,
                        ShowCloseButton = true,
                        CanBeResizable = false,
                        Height = 600,
                        Width = 700
                    };

                    ViewExtension = new HowLongToBeatSelect(game, data);
                    Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(ResourceProvider.GetString("LOCSelection") + " - " + game.Name + " - " + (game.Source?.Name ?? "Playnite"), ViewExtension, windowOptions);
                    _ = windowExtension.ShowDialog();
                }).Wait();

                if (ViewExtension?.GameHowLongToBeat?.Items.Count > 0)
                {
                    var picked = ViewExtension.GameHowLongToBeat.Items.FirstOrDefault();
                    Logger.Info(string.Format(
                        "HLTB SearchData RESULT: playniteGame='{0}' hltbId='{1}' title='{2}' url='{3}' urlImg='{4}' main={5}s",
                        game?.Name ?? string.Empty,
                        picked?.Id ?? string.Empty,
                        picked?.Name ?? string.Empty,
                        picked?.Url ?? string.Empty,
                        picked?.UrlImg ?? string.Empty,
                        picked?.GameHltbData?.MainStoryClassic ?? 0));
                    return ViewExtension.GameHowLongToBeat;
                }

                Logger.Info(string.Format("HLTB SearchData RESULT: playniteGame='{0}' cancelled or empty", game?.Name ?? string.Empty));
            }

            return null;
        }

        public HltbDataUser SearchDataAuto(string gameName, string platform = "")
        {
            string traceId = null;
            try
            {
                if (string.IsNullOrEmpty(gameName))
                {
                    return null;
                }

                // Apply manual aliases (exact normalized match) early so all subsequent matching uses the aliased title.
                try
                {
                    var settings = PluginDatabase?.PluginSettings;
                    var userDataPath = PluginDatabase?.Plugin?.GetPluginUserDataPath();
                    var aliased = GameNameAliases.ApplyAlias(gameName, settings, userDataPath);
                    if (!string.IsNullOrEmpty(aliased) && !aliased.IsEqual(gameName))
                    {
                        Common.LogDebug(true, $"HLTB aliases: '{SafeStr(gameName)}' -> '{SafeStr(aliased)}'");
                        gameName = aliased;
                    }
                }
                catch { }

                traceId = Guid.NewGuid().ToString("N").Substring(0, 8);

                var hltbSettings = PluginDatabase?.PluginSettings;

                if (IsVerboseLoggingEnabled)
                {
                    try
                    {
                        Common.LogDebug(true,$"SearchDataAuto[{traceId}]: start name='{SafeStr(gameName)}' platform='{SafeStr(platform)}' UseMatchValue={hltbSettings?.UseMatchValue} MatchValue={hltbSettings?.MatchValue}");
                    }
                    catch { }
                }

                List<HltbSearch> results = null;
                bool gotResults = false;
                try
                {
                    // 1) First pass: fast search (no details fetch)
                    gotResults = TaskHelpers.TryRunSyncWithTimeout(() => SearchTwoMethod(gameName, platform), out results, 15000, Logger);
                    if (!gotResults)
                    {
                        if (IsVerboseLoggingEnabled)
                        {
                            try { Logger.Warn($"SearchDataAuto[{traceId}]: SearchTwoMethod timed out after 15000ms; retrying once"); } catch { }
                        }

                        gotResults = TaskHelpers.TryRunSyncWithTimeout(() => SearchTwoMethod(gameName, platform), out results, 30000, Logger);
                    }
                }
                catch (Exception ex)
                {
                    if (IsVerboseLoggingEnabled)
                    {
                        try { Logger.Warn(ex, $"SearchDataAuto[{traceId}]: SearchTwoMethod threw for name='{SafeStr(gameName)}' platform='{SafeStr(platform)}'"); } catch { }
                    }
                    results = null;
                }

                if (!gotResults)
                {
                    if (IsVerboseLoggingEnabled)
                    {
                        try { Common.LogDebug(true,$"SearchDataAuto[{traceId}]: no results (timeout)"); } catch { }
                    }
                    return null;
                }

                if (results == null || results.Count == 0)
                {
                    if (IsVerboseLoggingEnabled)
                    {
                        try { Common.LogDebug(true,$"SearchDataAuto[{traceId}]: no results"); } catch { }
                    }
                    return null;
                }

                if (IsVerboseLoggingEnabled)
                {
                    try
                    {
                        Common.LogDebug(true,$"SearchDataAuto[{traceId}]: results count={results.Count}");
                        int take = Math.Min(8, results.Count);
                        for (int i = 0; i < take; i++)
                        {
                            var r = results[i];
                            var d = r?.Data;
                            long ttb = 0;
                            bool hasAnyTime = false;
                            try
                            {
                                ttb = d?.GameHltbData?.TimeToBeat ?? 0;
                                hasAnyTime = ttb > 0 || (d?.GameHltbData?.MainStoryClassic ?? 0) > 0 || (d?.GameHltbData?.SoloClassic ?? 0) > 0;
                            }
                            catch { }

                            Common.LogDebug(true,$"SearchDataAuto[{traceId}]: candidate[{i}] score={r?.MatchPercent} id={SafeStr(d?.Id)} title='{SafeStr(d?.Name)}' platform='{SafeStr(d?.Platform)}' hasTime={hasAnyTime} ttb={ttb} needsDetails={d?.NeedsDetails}");
                        }
                    }
                    catch { }
                }

                try
                {
                    var nQuery = PlayniteTools.NormalizeGameName(gameName ?? string.Empty, true, true);
                    if (!string.IsNullOrEmpty(nQuery))
                    {
                        var exact = results
                            .Where(r => r?.Data != null)
                            .Select(r => new { r.MatchPercent, Data = r.Data, Norm = PlayniteTools.NormalizeGameName(r.Data?.Name ?? string.Empty, true, true) })
                            .Where(x => !string.IsNullOrEmpty(x.Norm) && x.Norm.IsEqual(nQuery))
                            .OrderByDescending(x => x.MatchPercent)
                            .FirstOrDefault();

                        if (exact?.Data != null)
                        {
                            if (hltbSettings != null && hltbSettings.UseMatchValue && exact.MatchPercent < hltbSettings.MatchValue && exact.MatchPercent < 98)
                            {
                                if (IsVerboseLoggingEnabled)
                                {
                                    try { Common.LogDebug(true,$"SearchDataAuto[{traceId}]: exact normalized match rejected by MatchValue score={exact.MatchPercent} threshold={hltbSettings.MatchValue}"); } catch { }
                                }
                                return null;
                            }

                            if (IsVerboseLoggingEnabled)
                            {
                                try { Common.LogDebug(true,$"SearchDataAuto[{traceId}]: selected exact normalized match id={SafeStr(exact.Data?.Id)} title='{SafeStr(exact.Data?.Name)}' score={exact.MatchPercent}"); } catch { }
                            }

                            return exact.Data;
                        }
                    }
                }
                catch { }

                var best = results[0];
                if (best?.Data == null)
                {
                    if (IsVerboseLoggingEnabled)
                    {
                        try { Common.LogDebug(true,$"SearchDataAuto[{traceId}]: best result had null data"); } catch { }
                    }
                    return null;
                }

                try
                {
                    if (hltbSettings != null && hltbSettings.UseMatchValue && best.MatchPercent < hltbSettings.MatchValue)
                    {
                        if (best.MatchPercent >= 98)
                        {
                            if (IsVerboseLoggingEnabled)
                            {
                                try { Common.LogDebug(true,$"SearchDataAuto[{traceId}]: best below MatchValue but accepted via near-perfect safety net score={best.MatchPercent} threshold={hltbSettings.MatchValue}"); } catch { }
                            }
                            return best.Data;
                        }

                        if (IsVerboseLoggingEnabled)
                        {
                            try { Common.LogDebug(true,$"SearchDataAuto[{traceId}]: rejected by MatchValue bestScore={best.MatchPercent} threshold={hltbSettings.MatchValue}"); } catch { }
                        }
                        return null;
                    }
                }
                catch { }

                // 2) Ambiguity handling: if multiple top results are close in score,
                // fetch details for top candidates and prefer one that actually has times.
                try
                {
                    if (results.Count > 1)
                    {
                        var second = results[1];
                        bool ambiguous = false;
                        try
                        {
                            ambiguous = (best.MatchPercent < 100 && second != null && (best.MatchPercent - second.MatchPercent) <= 3);
                        }
                        catch { }

                        if (IsVerboseLoggingEnabled)
                        {
                            try { Common.LogDebug(true,$"SearchDataAuto[{traceId}]: ambiguity check best={best.MatchPercent} second={second?.MatchPercent} ambiguous={ambiguous}"); } catch { }
                        }

                        if (ambiguous)
                        {
                            List<HltbSearch> enriched = null;
                            try
                            {
                                enriched = TaskHelpers.RunSyncWithTimeout(() => SearchTwoMethod(gameName, platform, includeExtendedTimes: true), 20000);
                            }
                            catch (Exception ex)
                            {
                                if (IsVerboseLoggingEnabled)
                                {
                                    try { Logger.Warn(ex, $"SearchDataAuto[{traceId}]: enrichment SearchTwoMethod threw for name='{SafeStr(gameName)}' platform='{SafeStr(platform)}'"); } catch { }
                                }
                                enriched = null;
                            }

                            if (enriched != null && enriched.Count > 0)
                            {
                                if (IsVerboseLoggingEnabled)
                                {
                                    try
                                    {
                                        Common.LogDebug(true,$"SearchDataAuto[{traceId}]: enriched count={enriched.Count}");
                                        int take = Math.Min(8, enriched.Count);
                                        for (int i = 0; i < take; i++)
                                        {
                                            var r = enriched[i];
                                            var d = r?.Data;
                                            long ttb = 0;
                                            bool hasAnyTime = false;
                                            try
                                            {
                                                ttb = d?.GameHltbData?.TimeToBeat ?? 0;
                                                hasAnyTime = ttb > 0 || (d?.GameHltbData?.MainStoryClassic ?? 0) > 0 || (d?.GameHltbData?.SoloClassic ?? 0) > 0;
                                            }
                                            catch { }

                                            Common.LogDebug(true,$"SearchDataAuto[{traceId}]: enriched[{i}] score={r?.MatchPercent} id={SafeStr(d?.Id)} title='{SafeStr(d?.Name)}' platform='{SafeStr(d?.Platform)}' hasTime={hasAnyTime} ttb={ttb} needsDetails={d?.NeedsDetails}");
                                        }
                                    }
                                    catch { }
                                }

                                var withTimes = enriched
                                    .Where(r => r?.Data?.GameHltbData != null)
                                    .Where(r =>
                                    {
                                        try
                                        {
                                            return r.Data.GameHltbData.TimeToBeat > 0 || r.Data.GameHltbData.MainStoryClassic > 0 || r.Data.GameHltbData.MainExtraClassic > 0 || r.Data.GameHltbData.CompletionistClassic > 0 || r.Data.GameHltbData.SoloClassic > 0;
                                        }
                                        catch { return false; }
                                    })
                                    .OrderByDescending(r => r.MatchPercent)
                                    .FirstOrDefault();

                                if (withTimes?.Data != null)
                                {
                                    if (hltbSettings != null && hltbSettings.UseMatchValue && withTimes.MatchPercent < hltbSettings.MatchValue && withTimes.MatchPercent < 98)
                                    {
                                        if (IsVerboseLoggingEnabled)
                                        {
                                            try { Common.LogDebug(true,$"SearchDataAuto[{traceId}]: enriched withTimes rejected by MatchValue score={withTimes.MatchPercent} threshold={hltbSettings.MatchValue}"); } catch { }
                                        }
                                        return null;
                                    }

                                    if (IsVerboseLoggingEnabled)
                                    {
                                        try { Common.LogDebug(true,$"SearchDataAuto[{traceId}]: selected enriched withTimes id={SafeStr(withTimes.Data?.Id)} title='{SafeStr(withTimes.Data?.Name)}' score={withTimes.MatchPercent}"); } catch { }
                                    }
                                    return withTimes.Data;
                                }

                                var bestEnriched = enriched[0];
                                if (bestEnriched?.Data != null)
                                {
                                    if (hltbSettings != null && hltbSettings.UseMatchValue && bestEnriched.MatchPercent < hltbSettings.MatchValue && bestEnriched.MatchPercent < 98)
                                    {
                                        if (IsVerboseLoggingEnabled)
                                        {
                                            try { Common.LogDebug(true,$"SearchDataAuto[{traceId}]: bestEnriched rejected by MatchValue score={bestEnriched.MatchPercent} threshold={hltbSettings.MatchValue}"); } catch { }
                                        }
                                        return null;
                                    }

                                    if (IsVerboseLoggingEnabled)
                                    {
                                        try { Common.LogDebug(true,$"SearchDataAuto[{traceId}]: selected bestEnriched id={SafeStr(bestEnriched.Data?.Id)} title='{SafeStr(bestEnriched.Data?.Name)}' score={bestEnriched.MatchPercent}"); } catch { }
                                    }
                                    return bestEnriched.Data;
                                }
                            }

                            if (best.MatchPercent >= 98)
                            {
                                if (IsVerboseLoggingEnabled)
                                {
                                    try { Common.LogDebug(true,$"SearchDataAuto[{traceId}]: ambiguous but fell back to near-perfect best score={best.MatchPercent} id={SafeStr(best.Data?.Id)}"); } catch { }
                                }
                                return best.Data;
                            }
                        }
                    }
                }
                catch { }

                if (IsVerboseLoggingEnabled)
                {
                    try { Common.LogDebug(true,$"SearchDataAuto[{traceId}]: selected best (default) id={SafeStr(best.Data?.Id)} title='{SafeStr(best.Data?.Name)}' score={best.MatchPercent}"); } catch { }
                }

                return best.Data;
            }
            catch (Exception ex)
            {
                try { Common.LogError(ex, false, true, PluginDatabase?.PluginName); } catch { }
                return null;
            }
        }

        #endregion

        #region user account

        /// <summary>
        /// Checks if the user is currently logged in to HowLongToBeat (async).
        /// </summary>
        /// <returns>True if logged in, otherwise false.</returns>
        public async Task<bool> GetIsUserLoggedInAsync()
        {
            // Avoid getting stuck in a "logged out" state if this method is called before PluginDatabase is loaded.
            // Use stored cookies directly to check the current session.
            try
            {
                var now = DateTime.UtcNow;
                if (lastLoginCheckResult != null)
                {
                    var ttl = lastLoginCheckResult == true ? TimeSpan.FromMinutes(10) : TimeSpan.FromMinutes(1);
                    if (now - lastLoginCheckUtc < ttl)
                    {
                        return lastLoginCheckResult.Value;
                    }
                }

                int userId = await GetUserId().ConfigureAwait(false);
                UserId = userId;
                IsConnected = userId != 0;

                lastLoginCheckUtc = now;
                lastLoginCheckResult = userId != 0;

                Logger.Info($"HLTB Auth: session check userId={userId} loggedIn={userId != 0}");
                Common.LogDebug(true, $"HLTB Auth: GetIsUserLoggedInAsync userId={userId} IsConnected={IsConnected} cached={(lastLoginCheckResult != null)}");

                return userId != 0;
            }
            catch (Exception ex)
            {
                try { Common.LogError(ex, false, true, PluginDatabase.PluginName); } catch { }
                UserId = 0;
                IsConnected = false;
                lastLoginCheckUtc = DateTime.UtcNow;
                lastLoginCheckResult = false;
                return false;
            }
        }

        public bool GetIsUserLoggedIn()
        {
            try
            {
                return Task.Run(() => GetIsUserLoggedInAsync()).Result;
            }
            catch (Exception ex)
            {
                try { Common.LogError(ex, false, false, PluginDatabase?.PluginName); } catch { }
                return false;
            }
        }

        /// <summary>
        /// Initiates the login process for HowLongToBeat.
        /// </summary>
        public void Login()
        {
            try
            {
                if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
                {
                    Application.Current.Dispatcher.Invoke((Action)RunLoginWebViewDialog);
                }
                else
                {
                    RunLoginWebViewDialog();
                }
            }
            catch (Exception ex)
            {
                try { Common.LogError(ex, false, true, PluginDatabase.PluginName); } catch { }
            }
        }

        /// <summary>
        /// Opens the login WebView dialog, captures cookies after it closes, then refreshes user data.
        /// Cookie extraction must run after <see cref="IWebView.OpenDialog"/> returns; reading cookies while the dialog is open can block indefinitely.
        /// </summary>
        private void RunLoginWebViewDialog()
        {
            Logger.Info("HLTB Auth: opening login WebView");

            bool loginRedirectDetected = false;
            IWebView webView = null;
            EventHandler<WebViewLoadingChangedEventArgs> loadingHandler = null;
            System.Windows.Threading.DispatcherTimer redirectPollTimer = null;

            try
            {
                WebViewSettings settings = new WebViewSettings
                {
                    JavaScriptEnabled = true,
                    WindowHeight = 670,
                    WindowWidth = 490,
                    UserAgent = Web.UserAgent
                };

                webView = API.Instance.WebViews.CreateView(settings);
                loadingHandler = (sender, e) =>
                {
                    if (!loginRedirectDetected)
                    {
                        loginRedirectDetected = TryCompleteLoginRedirect(webView);
                    }
                };
                webView.LoadingChanged += loadingHandler;

                redirectPollTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(500)
                };
                redirectPollTimer.Tick += (sender, e) =>
                {
                    if (!loginRedirectDetected)
                    {
                        loginRedirectDetected = TryCompleteLoginRedirect(webView);
                    }
                };
                redirectPollTimer.Start();

                IsConnected = false;
                webView.Navigate(UrlLogOut);
                webView.OpenDialog();
            }
            catch (Exception ex)
            {
                try { Common.LogError(ex, false, true, PluginDatabase.PluginName); } catch { }
            }
            finally
            {
                if (redirectPollTimer != null)
                {
                    redirectPollTimer.Stop();
                }

                if (webView != null)
                {
                    if (loadingHandler != null)
                    {
                        webView.LoadingChanged -= loadingHandler;
                    }

                    try
                    {
                        if (loginRedirectDetected)
                        {
                            Logger.Info($"HLTB Auth: login dialog closed, user='{UserLogin}'");
                            Common.LogDebug(true, "HLTB Auth: extracting cookies after dialog close");

                            List<HttpCookie> cookies = ExtractLoginWebViewCookies(webView);
                            bool saved = CookiesTools.SetStoredCookies(cookies);
                            Logger.Info($"HLTB Auth: captured cookies from login webview count={cookies?.Count ?? 0} saved={saved}");
                            Common.LogDebug(true, $"HLTB Auth: login capture saved={saved}");
                            LogCookieSummary("login captured", cookies);

                            lastLoginCheckResult = null;
                            lastLoginCheckUtc = DateTime.MinValue;

                            PluginDatabase.PluginSettings.UserLogin = UserLogin;
                            PluginDatabase.Plugin.SavePluginSettings(PluginDatabase.PluginSettings);

                            FireAndForget(Task.Run(async () =>
                            {
                                try
                                {
                                    UserId = await GetUserId().ConfigureAwait(false);
                                    IsConnected = UserId != 0;
                                    Logger.Info($"HLTB Auth: post-login userId={UserId}");
                                    Common.LogDebug(true, $"HLTB Auth: post-login IsConnected={IsConnected} UserLogin='{UserLogin}'");
                                }
                                catch (Exception ex)
                                {
                                    try { Logger.Warn(ex, "HLTB Auth: post-login GetUserId failed"); } catch { }
                                }

                                try { PluginDatabase.RefreshUserData(); }
                                catch (Exception ex)
                                {
                                    try { Logger.Warn(ex, "HLTB Auth: post-login RefreshUserData failed"); } catch { }
                                }
                            }), "post-login refresh");
                        }
                        else
                        {
                            Logger.Info("HLTB Auth: login dialog closed without authentication");
                        }
                    }
                    catch (Exception ex)
                    {
                        try { Logger.Warn(ex, "HLTB Auth: post-login cookie capture failed"); } catch { }
                        try { Common.LogError(ex, false, true, PluginDatabase.PluginName); } catch { }
                    }
                    finally
                    {
                        try { webView.Dispose(); } catch { }
                    }
                }

                try { LoginCompleted?.Invoke(this, EventArgs.Empty); } catch { }
            }
        }

        /// <summary>
        /// Detects a successful login redirect and closes the login WebView on the next dispatcher frame.
        /// </summary>
        /// <returns><c>true</c> when a login redirect was detected.</returns>
        private bool TryCompleteLoginRedirect(IWebView webView)
        {
            if (webView == null)
            {
                return false;
            }

            try
            {
                string address = webView.GetCurrentAddress();
                Common.LogDebug(true, $"HLTB Auth Login: checking url='{address}'");

                string userLogin;
                if (!TryParseLoginUserUrl(address, out userLogin))
                {
                    return false;
                }

                UserLogin = userLogin;
                IsConnected = true;
                Logger.Info($"HLTB Auth: login redirect detected user='{userLogin}', scheduling WebView close");

                _ = Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        webView.Close();
                        Logger.Info("HLTB Auth: login WebView close requested");
                    }
                    catch (Exception ex)
                    {
                        try { Logger.Warn(ex, "HLTB Auth: webView.Close failed after login redirect"); } catch { }
                    }
                }), System.Windows.Threading.DispatcherPriority.Normal);

                return true;
            }
            catch (Exception ex)
            {
                try { Logger.Warn(ex, "HLTB Auth: login redirect check failed"); } catch { }
                Common.LogDebug(true, $"HLTB Auth Login: redirect check error url='{webView?.GetCurrentAddress() ?? string.Empty}'");
                return false;
            }
        }

        /// <summary>
        /// Reads cookies from the login WebView after the dialog has closed.
        /// </summary>
        private List<HttpCookie> ExtractLoginWebViewCookies(IWebView webView)
        {
            try
            {
                List<HttpCookie> cookies = webView.GetCookies();
                if (cookies == null || cookies.Count == 0)
                {
                    Logger.Warn("HLTB Auth: login WebView returned no cookies");
                    return new List<HttpCookie>();
                }

                if (CookiesDomains == null || CookiesDomains.Count == 0)
                {
                    return cookies;
                }

                return cookies
                    .Where(c => c != null && CookiesDomains.Any(d => d.Contains(c.Domain ?? string.Empty, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }
            catch (Exception ex)
            {
                try { Logger.Warn(ex, "HLTB Auth: failed to read cookies from login WebView"); } catch { }
                return new List<HttpCookie>();
            }
        }

        /// <summary>
        /// Returns whether the address points to a HowLongToBeat user profile page and extracts the login name.
        /// </summary>
        private static bool TryParseLoginUserUrl(string address, out string userLogin)
        {
            userLogin = string.Empty;
            if (address.IsNullOrEmpty())
            {
                return false;
            }

            string[] prefixes =
            {
                "https://howlongtobeat.com/user/",
                "https://www.howlongtobeat.com/user/",
                "http://howlongtobeat.com/user/",
                "http://www.howlongtobeat.com/user/"
            };

            foreach (string prefix in prefixes)
            {
                if (!address.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string loginPart = address.Substring(prefix.Length);
                loginPart = WebUtility.HtmlDecode(loginPart);

                int queryIndex = loginPart.IndexOf('?');
                if (queryIndex >= 0)
                {
                    loginPart = loginPart.Substring(0, queryIndex);
                }

                int hashIndex = loginPart.IndexOf('#');
                if (hashIndex >= 0)
                {
                    loginPart = loginPart.Substring(0, hashIndex);
                }

                userLogin = loginPart.Trim().TrimEnd('/');
                return !userLogin.IsNullOrEmpty();
            }

            return false;
        }

        /// <summary>
        /// Returns whether the API payload looks like JSON.
        /// </summary>
        private static bool LooksLikeJson(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string trimmed = text.TrimStart();
            return trimmed.StartsWith("{", StringComparison.Ordinal) || trimmed.StartsWith("[", StringComparison.Ordinal);
        }

        /// <summary>
        /// Downloads the current user payload from the HTTP API using stored cookies.
        /// </summary>
        private async Task<string> FetchUserIdHttpResponseAsync(List<HttpCookie> cookies)
        {
            List<HttpHeader> headers = new List<HttpHeader>
            {
                new HttpHeader { Key = "Accept", Value = "application/json, text/javascript, */*; q=0.01" },
                new HttpHeader { Key = "Referer", Value = UrlBase }
            };

            return await Web.DownloadStringData(UrlUser, headers, cookies).ConfigureAwait(false);
        }

        /// <summary>
        /// Parses a HowLongToBeat <c>/api/user</c> JSON response and extracts the user id.
        /// </summary>
        private bool TryParseUserIdFromApiResponse(string response, out int userId)
        {
            userId = 0;

            if (string.IsNullOrWhiteSpace(response) || response == "{}")
            {
                return true;
            }

            if (!LooksLikeJson(response))
            {
                try { Logger.Warn($"HLTB Auth: GetUserId unexpected response (not JSON): {SafeStr(response)}"); } catch { }
                Common.LogDebug(true, $"HLTB Auth: GetUserId non-JSON response='{response}'");
                return false;
            }

            try
            {
                dynamic parsed = Serialization.FromJson<dynamic>(response);
                userId = parsed?.data[0]?.user_id ?? 0;
                Common.LogDebug(true, $"HLTB Auth: GetUserId parsed userId={userId} responseLen={response.Length}");
                return true;
            }
            catch (Exception ex)
            {
                try { Logger.Warn(ex, "HLTB Auth: failed to parse GetUserId JSON response"); } catch { }
                Common.LogDebug(true, $"HLTB Auth: GetUserId JSON parse error preview='{SafeStr(response)}'");
                return false;
            }
        }

        /// <summary>
        /// Retrieves the user ID of the currently logged-in user.
        /// </summary>
        /// <returns>User ID as integer, or 0 if not logged in.</returns>
        private async Task<int> GetUserId()
        {
            try
            {
                List<HttpCookie> cookies = CookiesTools.GetStoredCookies();
                LogCookieSummary("GetUserId stored", cookies);

                if (cookies == null || cookies.Count == 0)
                {
                    try { Logger.Info("HLTB Auth: no stored cookies available"); } catch { }
                    return 0;
                }

                string httpResponse = await FetchUserIdHttpResponseAsync(cookies).ConfigureAwait(false);
                Common.LogDebug(true, $"HLTB Auth: GetUserId HTTP responseLen={httpResponse?.Length ?? 0} preview='{SafeStr(httpResponse)}'");

                int userId;
                if (TryParseUserIdFromApiResponse(httpResponse, out userId))
                {
                    if (userId > 0)
                    {
                        Logger.Info($"HLTB Auth: GetUserId ok userId={userId}");
                        return userId;
                    }

                    if (string.IsNullOrWhiteSpace(httpResponse) || httpResponse == "{}")
                    {
                        Logger.Warn("HLTB Auth: GetUserId HTTP session empty; trying WebView fallback");
                        Common.LogDebug(true, "HLTB Auth: GetUserId HTTP empty response with stored cookies");
                    }
                    else
                    {
                        Logger.Info("HLTB Auth: GetUserId: no active session");
                        return 0;
                    }
                }
                else
                {
                    Logger.Warn("HLTB Auth: GetUserId HTTP returned unexpected content; trying WebView fallback");
                }

                string webViewResponse = await Web.DownloadPageText(UrlUser, cookies).ConfigureAwait(false);
                Common.LogDebug(true, $"HLTB Auth: GetUserId WebView responseLen={webViewResponse?.Length ?? 0} preview='{SafeStr(webViewResponse)}'");

                if (TryParseUserIdFromApiResponse(webViewResponse, out userId))
                {
                    if (userId > 0)
                    {
                        Logger.Info($"HLTB Auth: GetUserId ok via WebView fallback userId={userId}");
                    }
                    else
                    {
                        Logger.Warn("HLTB Auth: GetUserId failed after WebView fallback");
                    }

                    return userId;
                }

                Logger.Warn("HLTB Auth: GetUserId could not parse WebView response");
                return 0;
            }
            catch (Exception ex)
            {
                try { Logger.Warn(ex, "HLTB Auth: GetUserId failed"); } catch { }
                Common.LogError(ex, false);
                return 0;
            }
        }

        /// <summary>
        /// Retrieves the list of games for the current user.
        /// </summary>
        /// <returns>Returns a <see cref="UserGamesList"/> object.</returns>
        private async Task<UserGamesList> GetUserGamesList()
        {
            try
            {
                List<HttpCookie> cookies = CookiesTools.GetStoredCookies();

                UserGamesListParam userGamesListParam = new UserGamesListParam { UserId = UserId };
                string payload = Serialization.ToJson(userGamesListParam);

                (string json, _) = await PostJson(string.Format(UrlUserGamesList, UserId), payload, cookies);
                _ = Serialization.TryFromJson(json, out UserGamesList userGamesList);

                return userGamesList;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
                return null;
            }
        }

        /// <summary>
        /// Converts a <see cref="GamesList"/> entry to a <see cref="TitleList"/>.
        /// </summary>
        /// <param name="gamesList">The games list entry.</param>
        /// <returns>Returns a <see cref="TitleList"/> object.</returns>
        private TitleList GetTitleList(GamesList gamesList)
        {
            try
            {
                _ = DateTime.TryParse(gamesList.DateUpdated, out DateTime lastUpdate);
                _ = DateTime.TryParse(gamesList.DateComplete, out DateTime completion);
                _ = DateTime.TryParse(gamesList.DateStart, out DateTime dateStart);
                DateTime? completionFinal = null;
                if (completion != default)
                {
                    completionFinal = completion;
                }

                TitleList titleList = new TitleList
                {
                    UserGameId = gamesList.Id.ToString(),
                    GameName = gamesList.CustomTitle,
                    Platform = gamesList.Platform,
                    Id = gamesList.GameId.ToString(),
                    CurrentTime = gamesList.InvestedPro,
                    IsReplay = gamesList.PlayCount == 2,
                    IsIncludesDlc = gamesList.PlayDlc == 1,
                    IsRetired = gamesList.ListRetired == 1,
                    Storefront = gamesList.PlayStorefront,
                    StartDate = dateStart,
                    LastUpdate = lastUpdate,
                    Completion = completionFinal,
                    HltbUserData = new HltbData
                    {
                        GameType = gamesList.GameType.IsEqual("game") ? GameType.Game : gamesList.GameType.IsEqual("multi") ? GameType.Multi : GameType.Compil,
                        MainStoryClassic = gamesList.CompMain,
                        MainExtraClassic = gamesList.CompPlus,
                        CompletionistClassic = gamesList.Comp100,
                        SoloClassic = 0,
                        CoOpClassic = gamesList.InvestedCo,
                        VsClassic = gamesList.InvestedMp
                    },
                    GameStatuses = new List<GameStatus>()
                };

                if (gamesList.ListBacklog == 1)
                {
                    titleList.GameStatuses.Add(new GameStatus { Status = StatusType.Backlog });
                }

                if (gamesList.ListComp == 1)
                {
                    titleList.GameStatuses.Add(new GameStatus { Status = StatusType.Completed });
                }

                if (gamesList.ListCustom == 1)
                {
                    titleList.GameStatuses.Add(new GameStatus { Status = StatusType.CustomTab });
                }

                if (gamesList.ListPlaying == 1)
                {
                    titleList.GameStatuses.Add(new GameStatus { Status = StatusType.Playing });
                }

                if (gamesList.ListReplay == 1)
                {
                    titleList.GameStatuses.Add(new GameStatus { Status = StatusType.Replays });
                }

                if (gamesList.ListRetired == 1)
                {
                    titleList.GameStatuses.Add(new GameStatus { Status = StatusType.Retired });
                }

                return titleList;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
                return null;
            }
        }

        /// <summary>
        /// Retrieves the edit data for a specific user game entry.
        /// </summary>
        /// <param name="gameName">The name of the game.</param>
        /// <param name="userGameId">The user game ID.</param>
        /// <returns>Returns an <see cref="EditData"/> object.</returns>
        public async Task<EditData> GetEditData(string gameName, string userGameId)
        {
            Logger.Info($"GetEditData({gameName}, {userGameId})");
            try
            {
                List<HttpCookie> cookies = CookiesTools.GetStoredCookies();

                string response = await Web.DownloadStringData(string.Format(UrlPostDataEdit, userGameId), cookies);
                if (string.IsNullOrEmpty(response) || !response.Contains("__NEXT_DATA__"))
                {
                    Logger.Warn($"No EditData for {gameName} - {userGameId}");
                    return null;
                }

                string jsonData = UtilityTools.GetJsonInString(response, @"<script[ ]?id=""__NEXT_DATA__""[ ]?type=""application/json"">");
                _ = Serialization.TryFromJson(jsonData, out NEXT_DATA next_data, out Exception parseEx);
                if (parseEx != null)
                {
                    Common.LogError(parseEx, false, false, PluginDatabase.PluginName);
                }

                return next_data?.Props?.PageProps?.EditData?.UserId != null
                    ? next_data.Props.PageProps.EditData
                    : throw new Exception($"No EditData find for {gameName} - {userGameId}");
            }
            catch (Exception ex)
            {
                if (IsFirst)
                {
                    IsFirst = false;
                    return await GetEditData(gameName, userGameId);
                }
                else
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                    return null;
                }
            }
        }

        /// <summary>
        /// Loads user stats from the local file.
        /// </summary>
        /// <returns>Returns a <see cref="HltbUserStats"/> object.</returns>
        public HltbUserStats LoadUserData()
        {
            string pathHltbUserStats = Path.Combine(PluginDatabase.Plugin.GetPluginUserDataPath(), "HltbUserStats.json");
            HltbUserStats hltbDataUser = new HltbUserStats();

            if (File.Exists(pathHltbUserStats))
            {
                try
                {
                    if (!Serialization.TryFromJsonFile(pathHltbUserStats, out hltbDataUser))
                    {
                        return new HltbUserStats();
                    }
                    hltbDataUser.TitlesList = hltbDataUser.TitlesList.Where(x => x != null).ToList();
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                }
            }

            return hltbDataUser;
        }

        /// <summary>
        /// Retrieves the user data from HowLongToBeat (async).
        /// </summary>
        /// <returns>Returns a <see cref="HltbUserStats"/> object, or null if not logged in.</returns>
        public async Task<HltbUserStats> GetUserDataAsync()
        {
            if (await GetIsUserLoggedInAsync().ConfigureAwait(false))
            {
                HltbUserStats hltbUserStats = new HltbUserStats
                {
                    Login = UserLogin.IsNullOrEmpty() ? PluginDatabase.UserHltbData.Login : UserLogin,
                    UserId = (UserId == 0) ? PluginDatabase.UserHltbData.UserId : UserId,
                    TitlesList = new List<TitleList>()
                };

                UserGamesList userGamesList = null;
                try { userGamesList = await GetUserGamesList().ConfigureAwait(false); } catch { userGamesList = null; }
                if (userGamesList == null)
                {
                    return null;
                }

                try
                {
                    userGamesList.Data.GamesList.ForEach(x =>
                    {
                        TitleList titleList = GetTitleList(x);
                        hltbUserStats.TitlesList.Add(titleList);
                    });
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                    return null;
                }

                return hltbUserStats;
            }
            else
            {
                API.Instance.Notifications.Add(new NotificationMessage(
                    $"{PluginDatabase.PluginName}-Import-Error",
                    PluginDatabase.PluginName + Environment.NewLine + ResourceProvider.GetString("LOCCommonNotLoggedIn"),
                    NotificationType.Error,
                    () => PluginDatabase.Plugin.OpenSettingsView()
                ));
                return null;
            }
        }

        /// <summary>
        /// Synchronous wrapper for backwards compatibility; runs async method on thread-pool.
        /// </summary>
        public HltbUserStats GetUserData()
        {
            try
            {
                var task = Task.Run(() => GetUserDataAsync());
                if (!task.Wait(15000))
                {
                    try { Logger.Warn("GetUserData timed out"); } catch { }
                    return null;
                }
                return task.Result;
            }
            catch (Exception ex)
            {
                try { Common.LogError(ex, false, false, PluginDatabase?.PluginName); } catch { }
                return null;
            }
        }

        /// <summary>
        /// Finds the existing user game ID for a given game ID (async).
        /// </summary>
        public async Task<string> FindIdExistingAsync(string gameId)
        {
            try
            {
                var ug = await GetUserGamesList().ConfigureAwait(false);
                return ug?.Data?.GamesList?.Find(x => x.GameId.ToString().IsEqual(gameId))?.Id.ToString() ?? null;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
                return null;
            }
        }

        /// <summary>
        /// Synchronous wrapper for backwards compatibility.
        /// </summary>
        public string FindIdExisting(string gameId)
        {
            try
            {
                var task = Task.Run(() => FindIdExistingAsync(gameId));
                if (!task.Wait(15000))
                {
                    try { Logger.Warn($"FindIdExisting({gameId}) timed out"); } catch { }
                    return null;
                }
                return task.Result;
            }
            catch (Exception ex)
            {
                try { Common.LogError(ex, false, true, PluginDatabase.PluginName); } catch { }
                return null;
            }
        }

        /// <summary>
        /// Synchronous helper: get TitleList for a specific game id from user data.
        /// </summary>
        public TitleList GetUserData(string gameId)
        {
            try
            {
                var task = Task.Run(() => GetUserDataAsync());
                if (!task.Wait(15000))
                {
                    try { Logger.Warn($"GetUserData (sync) timed out"); } catch { }
                    return null;
                }
                var userData = task.Result;
                return userData?.TitlesList?.Find(x => x.Id == gameId);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public bool EditIdExist(string userGameId)
        {
            try
            {
                var task = Task.Run(() => GetUserGamesList());
                if (!task.Wait(15000))
                {
                    try { Logger.Warn($"EditIdExist({userGameId}) timed out"); } catch { }
                    return false;
                }
                var ug = task.Result;
                return ug?.Data?.GamesList?.Find(x => x.Id.ToString().IsEqual(userGameId))?.Id != null;
            }
            catch
            {
                return false;
            }
        }

        #endregion


        /// <summary>
        /// Builds a localized detail message for a failed profile submission.
        /// </summary>
        /// <param name="statusCode">HTTP status code returned by the submit endpoint (0 when unavailable).</param>
        /// <param name="response">Raw response body, used to extract server-side error messages.</param>
        /// <returns>Localized detail text shown in the user notification.</returns>
        private string BuildSubmitFailureDetail(int statusCode, string response)
        {
            if (statusCode == 401 || statusCode == 403)
            {
                return ResourceProvider.GetString("LOCHowLongToBeatErrorSubmitUnauthorized");
            }

            string serverMessage = GetSubmitResponseErrorMessage(response);
            if (!serverMessage.IsNullOrEmpty())
            {
                return serverMessage;
            }

            if (statusCode > 0)
            {
                return string.Format(ResourceProvider.GetString("LOCHowLongToBeatErrorSubmitHttpStatus"), statusCode);
            }

            return ResourceProvider.GetString("LOCHowLongToBeatErrorSubmitNoResponse");
        }

        /// <summary>
        /// Extracts an error message from a HowLongToBeat submit API JSON response, when present.
        /// </summary>
        /// <param name="response">Raw JSON response body.</param>
        /// <returns>Server error text, or <c>null</c> when none could be parsed.</returns>
        private static string GetSubmitResponseErrorMessage(string response)
        {
            if (response.IsNullOrEmpty())
            {
                return null;
            }

            try
            {
                if (Serialization.TryFromJson(response, out dynamic respObj) && respObj != null)
                {
                    if (respObj.error != null)
                    {
                        try
                        {
                            return respObj.error[0]?.ToString() ?? respObj.error.ToString();
                        }
                        catch
                        {
                            return respObj.error.ToString();
                        }
                    }

                    if (respObj.errors != null)
                    {
                        return respObj.errors.ToString();
                    }
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// Shows a user-facing notification when a HowLongToBeat profile submission fails.
        /// </summary>
        /// <param name="game">Playnite game associated with the failed submission.</param>
        /// <param name="detailMessage">Localized detail shown below the summary line.</param>
        /// <param name="openSettings">When <c>true</c>, clicking the notification opens plugin settings.</param>
        private void NotifySubmitError(Game game, string detailMessage, bool openSettings = false)
        {
            string summary = string.Format(ResourceProvider.GetString("LOCHowLongToBeatErrorSubmitFailed"), game.Name);
            string message = PluginDatabase.PluginName + Environment.NewLine + summary;
            if (!detailMessage.IsNullOrEmpty())
            {
                message += Environment.NewLine + detailMessage;
            }

            Action openSettingsAction = null;
            if (openSettings)
            {
                openSettingsAction = () => PluginDatabase.Plugin.OpenSettingsView();
            }

            API.Instance.Notifications.Add(new NotificationMessage(
                $"{PluginDatabase.PluginName}-{game.Id}-SubmitError-{Guid.NewGuid()}",
                message,
                NotificationType.Error,
                openSettingsAction
            ));
        }

        /// <summary>
        /// Submits the current game data to the HowLongToBeat website.
        /// Ensures session cookies (<c>hltb_alive</c>) are present, sends <c>Origin</c>/<c>Referer</c> headers,
        /// and surfaces localized errors for HTTP failures or missing session state.
        /// </summary>
        /// <param name="game">The Playnite game object.</param>
        /// <param name="editData">The data to submit.</param>
        /// <returns><c>true</c> when submission succeeds; otherwise <c>false</c>.</returns>
        public async Task<bool> ApiSubmitData(Game game, EditData editData)
        {
            if (GetIsUserLoggedIn() && editData.UserId != 0 && editData.GameId != 0)
            {
                try
                {
                    List<HttpCookie> cookies = await GetCookiesForSubmitAsync(editData.SubmissionId).ConfigureAwait(false);
                    if (!HasHltbSessionCookies(cookies))
                    {
                        Logger.Warn("ApiSubmitData: hltb_alive cookie missing after session refresh");
                        NotifySubmitError(game, ResourceProvider.GetString("LOCHowLongToBeatErrorSubmitSessionCookies"), openSettings: true);
                        return false;
                    }

                    string payload = Serialization.ToJson(editData);
                    string referer = editData.SubmissionId > 0
                        ? string.Format(UrlPostDataEdit, editData.SubmissionId)
                        : UrlBase + "/submit";
                    List<KeyValuePair<string, string>> submitHeaders = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("Origin", UrlBase),
                        new KeyValuePair<string, string>("Referer", referer)
                    };

                    Common.LogDebug(true, $"ApiSubmitData: POST {UrlPostData} submissionId={editData.SubmissionId} referer={referer}");

                    (string response, int statusCode) = await PostJson(UrlPostData, payload, cookies, submitHeaders);

                    if (statusCode < 200 || statusCode >= 300)
                    {
                        bool openSettings = statusCode == 401 || statusCode == 403;
                        NotifySubmitError(game, BuildSubmitFailureDetail(statusCode, response), openSettings);
                        Logger.Warn($"ApiSubmitData failed for game {game.Id}: HTTP {statusCode}");
                        return false;
                    }


                    if (string.IsNullOrEmpty(response))
                    {
                        NotifySubmitError(game, BuildSubmitFailureDetail(statusCode, response));
                        Logger.Warn($"ApiSubmitData: empty response when posting data for game {game.Id}");
                        return false;
                    }

                    try
                    {
                        var success = false;
                        _ = Serialization.TryFromJson(response, out dynamic respObj);
                        if (respObj != null)
                        {
                            try
                            {
                                string msg = GetSubmitResponseErrorMessage(response);
                                if (!msg.IsNullOrEmpty())
                                {
                                    NotifySubmitError(game, msg);
                                    Logger.Warn($"ApiSubmitData error for game {game.Id}: {msg}");
                                }
                                else
                                {
                                    success = true;
                                }
                            }
                            catch (Exception ex)
                            {
                                Common.LogError(ex, false, false, PluginDatabase.PluginName);
                            }
                        }
                        else
                        {
                            Common.LogDebug(true,"ApiSubmitData: non-JSON response received; treating as success");
                            success = true;
                        }

                        if (success)
                        {
                            PluginDatabase.RefreshUserData(editData.GameId.ToString());
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, true, PluginDatabase.PluginName);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                    return false;
                }
            }
            else
            {
                API.Instance.Notifications.Add(new NotificationMessage(
                    $"{PluginDatabase.PluginName}-DataUpdate-Error",
                    PluginDatabase.PluginName + Environment.NewLine + ResourceProvider.GetString("LOCCommonNotLoggedIn"),
                    NotificationType.Error,
                    () => PluginDatabase.Plugin.OpenSettingsView()
                ));
                return false;
            }
        }

        /// <summary>
        /// Try to extract script src URLs using HtmlAgilityPack via reflection. Returns null on failure.
        /// Reflection is used because HAP is an optional dependency for the host application.
        /// </summary>
        private List<string> ExtractScriptUrlsWithHap(string html)
        {
            if (!HapAvailable || HapDocType == null || string.IsNullOrEmpty(html))
            {
                return null;
            }

            try
            {
                // Create HtmlDocument instance
                dynamic doc = Activator.CreateInstance(HapDocType);
                var loadHtml = HapDocType.GetMethod("LoadHtml");
                loadHtml.Invoke(doc, new object[] { html });

                var documentNode = HapDocType.GetProperty("DocumentNode").GetValue(doc);
                var selectNodes = documentNode.GetType().GetMethod("SelectNodes", new Type[] { typeof(string) });
                var nodes = selectNodes.Invoke(documentNode, new object[] { "//script[@src]" }) as System.Collections.IEnumerable;
                if (nodes == null) return new List<string>();

                var urls = new List<string>();
                foreach (var node in nodes)
                {
                    try
                    {
                        var attrsProp = node.GetType().GetProperty("Attributes");
                        if (attrsProp == null) continue;
                        var attrs = attrsProp.GetValue(node);
                        if (attrs == null) continue;
                        var getAttr = attrs.GetType().GetMethod("Get", new Type[] { typeof(string) });
                        if (getAttr == null) continue;
                        var srcAttr = getAttr.Invoke(attrs, new object[] { "src" });
                        if (srcAttr != null)
                        {
                            var valProp = srcAttr.GetType().GetProperty("Value");
                            var val = valProp != null ? valProp.GetValue(srcAttr) as string : null;
                            if (!string.IsNullOrEmpty(val)) urls.Add(val);
                        }
                    }
                    catch { /* ignore per-node errors */ }
                }

                return urls;
            }
            catch (Exception ex)
            {
                try { Logger.Warn(ex, "HLTB: HtmlAgilityPack reflection extraction failed"); } catch { }
                return null;
            }
        }

        /// <summary>
        /// Posts a JSON payload to the specified URL, optionally with cookies and extra request headers.
        /// </summary>
        /// <param name="url">Target URL.</param>
        /// <param name="payload">JSON request body.</param>
        /// <param name="cookies">Optional cookies attached to the request.</param>
        /// <param name="requestHeaders">Optional extra headers (for example <c>Origin</c> and <c>Referer</c>).</param>
        /// <param name="cancellationToken">Cancellation token for the HTTP request.</param>
        /// <returns>
        /// A tuple containing the response body and HTTP status code.
        /// Status code is <c>0</c> when the request could not be sent.
        /// </returns>
        private async Task<(string body, int statusCode)> PostJson(
            string url,
            string payload,
            List<HttpCookie> cookies = null,
            IEnumerable<KeyValuePair<string, string>> requestHeaders = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                HttpClientHandler handler = null;
                if (cookies != null)
                {
                    try
                    {
                        var mi = typeof(CommonPluginsShared.Web).GetMethod("CreateCookiesContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                        if (mi != null)
                        {
                            var container = mi.Invoke(null, new object[] { cookies }) as CookieContainer;
                            handler = new HttpClientHandler { CookieContainer = container };
                        }
                    }
                    catch { handler = new HttpClientHandler(); }
                }

                HttpClient client = null;
                bool disposeClient = false;
                try
                {
                    if (handler != null)
                    {
                        client = new HttpClient(handler)
                        {
                            Timeout = System.Threading.Timeout.InfiniteTimeSpan
                        };
                        disposeClient = true;
                    }
                    else
                    {
                        // Reuse the shared instance-level httpClient to avoid socket exhaustion
                        client = httpClient ?? new HttpClient();
                        disposeClient = false;
                    }

                    using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                    {
                        request.Content = new StringContent(payload ?? string.Empty, Encoding.UTF8, "application/json");

                        if (handler != null)
                        {
                            try { request.Headers.TryAddWithoutValidation("User-Agent", Web.UserAgent); } catch { }
                            try { request.Headers.TryAddWithoutValidation("Accept", "application/json, text/javascript, */*; q=0.01"); } catch { }
                        }

                        if (requestHeaders != null)
                        {
                            foreach (KeyValuePair<string, string> header in requestHeaders)
                            {
                                if (string.IsNullOrWhiteSpace(header.Key) || header.Value == null)
                                {
                                    continue;
                                }

                                try { request.Headers.TryAddWithoutValidation(header.Key, header.Value); } catch { }
                            }
                        }

                        await WaitForHttpRateLimitAsync("POST json", url, cancellationToken).ConfigureAwait(false);
                        using (var resp = await client.SendAsync(request, cancellationToken).ConfigureAwait(false))
                        {
                            int statusCode = (int)resp.StatusCode;
                            string body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false) ?? string.Empty;

                            if (!resp.IsSuccessStatusCode)
                            {
                                try { Logger.Warn($"HTTP {statusCode} posting to {url}"); } catch { }
                            }

                            return (body, statusCode);
                        }
                    }
                }
                finally
                {
                    if (disposeClient && client != null)
                    {
                        try { client.Dispose(); } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Error posting to {url}");
                return (string.Empty, 0);
            }
        }

        /// <summary>
        /// Posts JSON payload using shared HttpClient and returns tuple (responseBody, statusCode, retryAfterHeader).
        /// This is a local replacement for CommonPluginsShared.Web.PostJsonWithSharedClientWithStatus when the submodule isn't available.
        /// </summary>
        private async Task<(string body, int status, string retry)> PostJsonWithSharedClientWithStatus(string url, string payload, List<HttpHeader> headers = null, CancellationToken cancellationToken = default)
        {
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    request.Content = new StringContent(payload ?? string.Empty, Encoding.UTF8, "application/json");

                    if (headers != null)
                    {
                        foreach (var h in headers)
                        {
                            try
                            {
                                // Try to add to request headers; if invalid, try content headers
                                if (!request.Headers.TryAddWithoutValidation(h.Key, h.Value))
                                {
                                    try { request.Content?.Headers.TryAddWithoutValidation(h.Key, h.Value); } catch { }
                                }
                            }
                            catch { }
                        }
                    }

                    await WaitForHttpRateLimitAsync("POST shared json", url, cancellationToken).ConfigureAwait(false);
                    using (var resp = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false))
                    {
                        int status = (int)resp.StatusCode;
                        string retry = null;
                        try
                        {
                            if (resp.Headers.TryGetValues("Retry-After", out var vals))
                            {
                                retry = vals.FirstOrDefault();
                            }
                        }
                        catch { }

                        string body = string.Empty;
                        try { body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false); } catch { body = string.Empty; }

                        if (!resp.IsSuccessStatusCode)
                        {
                            try { Logger.Warn($"HTTP {status} posting to {url}"); } catch { }
                        }

                        return (body, status, retry);
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Error posting to {url}");
                return (string.Empty, 0, (string)null);
            }
        }
        private static readonly Regex _scriptSrcRegex = new Regex("<script[^>]*src=[\\\"']([^\\\"']+)[\\\"'][^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static Regex MyRegex() => _scriptSrcRegex;
    }
}
