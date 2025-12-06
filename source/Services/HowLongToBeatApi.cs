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

namespace HowLongToBeat.Services
{
    public class HowLongToBeatApi : ObservableObject, IDisposable
    {
        private static ILogger Logger => LogManager.GetLogger();

        private static HowLongToBeatDatabase PluginDatabase => HowLongToBeat.PluginDatabase;

        // Helper to centralize verbose logging checks
        private bool IsVerboseLoggingEnabled => PluginDatabase?.PluginSettings?.Settings is HowLongToBeatSettings _vs && _vs.EnableVerboseLogging;

        private void LogDebugVerbose(string message)
        {
            try
            {
                if (IsVerboseLoggingEnabled)
                {
                    Logger.Debug(message);
                }
            }
            catch { }
        }

        /// <summary>
        /// Adjusts a semaphore to match a target limit by releasing extra permits or consuming existing permits.
        /// Returns the new currentLimit value which the caller should persist to the corresponding field.
        /// This is extracted to avoid duplicating complex permit-release/consume logic in multiple places.
        /// </summary>
        private async Task<int> AdjustSemaphoreLimit(SemaphoreSlim semaphore, int currentLimit, int targetLimit, object syncLock, string context = null)
        {
            if (semaphore == null)
            {
                return currentLimit;
            }

            int pendingConsume = 0;
            try
            {
                lock (syncLock)
                {
                    int diff = targetLimit - currentLimit;
                    if (diff > 0)
                    {
                        try
                        {
                            semaphore.Release(diff);
                            currentLimit = targetLimit;
                        }
                        catch { }
                        return currentLimit;
                    }
                    else if (diff < 0)
                    {
                        pendingConsume = -diff;
                    }
                }

                if (pendingConsume > 0)
                {
                    int consumed = 0;
                    for (int i = 0; i < pendingConsume; i++)
                    {
                        try
                        {
                            var delay = Task.Delay(200);
                            var winner = await Task.WhenAny(semaphore.WaitAsync(), delay).ConfigureAwait(false);
                            if (winner == delay)
                            {
                                break;
                            }
                            consumed++;
                        }
                        catch { break; }
                    }

                    lock (syncLock)
                    {
                        int originalLimit = currentLimit;
                        int newLimit = (consumed == pendingConsume) ? targetLimit : Math.Max(0, originalLimit - consumed);
                        newLimit = Math.Min(originalLimit, newLimit);
                        currentLimit = newLimit;
                    }
                }
            }
            catch (Exception ex)
            {
                try { Logger.Warn(ex, $"HLTB: AdjustSemaphoreLimit error ({context})"); } catch { }
            }

            return currentLimit;
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
        private const int ScriptDownloadTimeoutMs = 5000;

        private const int MaxParallelGameDataDownloads = 32;
        private const int GameDataDownloadTimeoutMs = 15000;

        private const int MaxParallelSearches = 96;

        private readonly ConcurrentDictionary<string, string> GamePageCache = new ConcurrentDictionary<string, string>();
        private readonly ConcurrentDictionary<string, SearchResult> SearchCache = new ConcurrentDictionary<string, SearchResult>();
        private SemaphoreSlim SearchSemaphore;
        private AdaptiveConcurrencyController SearchConcurrencyController;
        private readonly object SearchConcurrencySync = new object();
        private int CurrentSearchLimit;
        private int PersistentCacheHits = 0;
        private int InMemoryCacheHits = 0;
        private int PageFetches = 0;
        private readonly PageCache PageCache;
        private AdaptiveConcurrencyController ConcurrencyController;
        private SemaphoreSlim DynamicSemaphore;
        private readonly object ConcurrencySync = new object();
        private int CurrentSemaphoreLimit;
        private const int SemaphoreUpperBound = 128;

        private readonly HttpClient httpClient;

        private readonly ConcurrentQueue<long> RecentSearchSamples = new ConcurrentQueue<long>();
        private const int RecentSamplesWindow = 200;

        private readonly ConcurrentQueue<int> RecentSearchStatusCodes = new ConcurrentQueue<int>();
        private const int RecentStatusWindow = 200;

        private DateTime SearchBackoffUntil = DateTime.MinValue;
        private int SearchBackoffLimit = 0;
        private readonly object BackoffSync = new object();

        private string CachedAuthToken = null;
        private DateTime CachedAuthTokenExpiry = DateTime.MinValue;
        private readonly object AuthTokenSync = new object();

        private CancellationTokenSource monitorCts;
        private Task monitorTask;
        private readonly object monitorSync = new object();
        private bool _disposed = false;


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

        private static string SearchEndPoint => "/api/locate";
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
                    Timeout = TimeSpan.FromMilliseconds(GameDataDownloadTimeoutMs)
                };
                httpClient.DefaultRequestHeaders.Add("User-Agent", Web.UserAgent);
                httpClient.DefaultRequestHeaders.Add("Referer", UrlBase);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "HLTB: HttpClient init failed");
                throw;
            }

            // Cache HtmlAgilityPack availability once to avoid reflection on every request.
            Type hapType = null;
            try
            {
                hapType = Type.GetType("HtmlAgilityPack.HtmlDocument, HtmlAgilityPack");
            }
            catch { }
            HapDocType = hapType;
            HapAvailable = HapDocType != null;

            UserLogin = PluginDatabase.PluginSettings.Settings.UserLogin;

            CookiesDomains = new List<string> { ".howlongtobeat.com", "howlongtobeat.com" };
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
                PageCache = new PageCache(PluginDatabase.Plugin.GetPluginUserDataPath());
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                try { Logger.Warn("HLTB: PageCache init failed; proceeding without persistent cache"); } catch { }
            }

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
                monitorCts?.Cancel();
                try { monitorTask?.Wait(2000); } catch { }
                monitorCts?.Dispose();
                monitorCts = null;
                monitorTask = null;
            }
            catch { }
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
                                try { await Task.Delay(TimeSpan.FromSeconds(10), token); } catch { }
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

                                    int searchAvailable = SearchSemaphore?.CurrentCount ?? 0;
                                    int searchInFlight = Math.Max(0, searchTarget - searchAvailable);

                                    int gameTarget = ConcurrencyController?.TargetConcurrency ?? MaxParallelGameDataDownloads;
                                    int gameAvailable = DynamicSemaphore?.CurrentCount ?? 0;
                                    int gameInFlight = Math.Max(0, gameTarget - gameAvailable);

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

                                    LogDebugVerbose($"HLTB Summary: searchTarget={searchTarget} searchInFlight={searchInFlight} gameTarget={gameTarget} gameInFlight={gameInFlight} avgSearchMs={Math.Round(avg,1)} medianSearchMs={Math.Round(median,1)} p90SearchMs={Math.Round(p90,1)} persistentCacheHits={PersistentCacheHits} inMemoryHits={InMemoryCacheHits} pageFetches={PageFetches} forced={searchForced}");
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
                    if (monitorCts != null)
                    {
                        try { monitorCts.Cancel(); } catch { }
                        try { monitorTask?.Wait(2000); } catch { }
                        try { monitorCts.Dispose(); } catch { }
                    }
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
            }
        }


        /// <summary>
        /// Retrieves game data from HowLongToBeat by game ID.
        /// </summary>
        /// <param name="id">The game ID.</param>
        /// <returns>Returns <see cref="HltbData"/> with game times, or null if not found.</returns>
        private async Task<HltbData> GetGameData(string id, CancellationToken cancellationToken = default)
        {
            try { EnsureMonitoringStarted(); } catch { }
            cancellationToken.ThrowIfCancellationRequested();
            DateTime startTime = DateTime.UtcNow;
            LogDebugVerbose($"GetGameData START id={id} task={Task.CurrentId} thread={Thread.CurrentThread.ManagedThreadId} time={startTime:HH:mm:ss.fff}");

            try
            {
                string jsonData = null;
                try
                {
                    if (PageCache != null && PageCache.TryGetJson(id, out string cachedJson))
                    {
                        jsonData = cachedJson;
                        try { System.Threading.Interlocked.Increment(ref PersistentCacheHits); } catch { }
                        LogDebugVerbose($"GetGameData id={id} - persistent cache hit");
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
                    var rnd = new Random();
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
                                LogDebugVerbose($"GetGameData id={id} - in-memory cache hit");
                            }
                            else
                            {
                                try
                                {
                                    using (var httpResp = await httpClient.GetAsync(string.Format(UrlGame, id), cancellationToken).ConfigureAwait(false))
                                    {
                                        if (!httpResp.IsSuccessStatusCode)
                                        {
                                            var code = (int)httpResp.StatusCode;
                                            LogDebugVerbose($"GetGameData id={id} - HTTP {code} fetching page");
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
                                string maybeJson = Tools.GetJsonInString(response, @"<script[ ]?id=""__NEXT_DATA__""[ ]?type=""application/json"">");
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
                                    Common.LogDebug(true, $"GetGameData id={id} - extracted JSON was empty or incomplete (attempt={attempts})");
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
                            LogDebugVerbose($"GetGameData id={id} - retry {attempts} after {delay}ms");
                            try { await Task.Delay(delay, cancellationToken); } catch (OperationCanceledException) { throw; }
                        }
                    }
                }
                if (string.IsNullOrEmpty(jsonData))
                {
                    Common.LogDebug(true, $"GetGameData id={id} - no JSON extracted after {attempts} attempts");
                    Logger.Warn($"No GameData find with {id}");
                    double elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                    try { ConcurrencyController?.ReportSample(elapsed, false); } catch { }
                    LogDebugVerbose($"GetGameData DONE id={id} task={Task.CurrentId} thread={Thread.CurrentThread.ManagedThreadId} elapsed={elapsed}ms");
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
                    HltbData hltbData = new HltbData
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

                    try
                    {
                        LogDebugVerbose($"GetGameData parsed id={id} mainClassic={hltbData.MainStoryClassic} mainAvg={hltbData.MainStoryAverage} mainMed={hltbData.MainStoryMedian} mainRushed={hltbData.MainStoryRushed} mainLeisure={hltbData.MainStoryLeisure}");
                    }
                    catch { }
                    double elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                    try { ConcurrencyController?.ReportSample(elapsed, true); } catch { }
                    LogDebugVerbose($"GetGameData DONE id={id} task={Task.CurrentId} thread={Thread.CurrentThread.ManagedThreadId} elapsed={elapsed}ms");
                    return hltbData;
                }
                else
                {
                    Logger.Warn($"No GameData find with {id}");
                    double elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                    try { ConcurrencyController?.ReportSample(elapsed, false); } catch { }
                    LogDebugVerbose($"GetGameData DONE id={id} task={Task.CurrentId} thread={Thread.CurrentThread.ManagedThreadId} elapsed={elapsed}ms");
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
                Logger.Info($"GetGameData ERROR id={id} task={Task.CurrentId} thread={Thread.CurrentThread.ManagedThreadId} elapsed={(DateTime.UtcNow - startTime).TotalMilliseconds}ms");
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
            try
            {
                HltbData hltbData = await GetGameData(hltbDataUser.Id);
                hltbDataUser.GameHltbData = hltbData ?? hltbDataUser.GameHltbData;
                return hltbDataUser;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
                return null;
            }
        }


        #region Search

        /// <summary>
        /// Retrieves the search URL from the website scripts.
        /// </summary>
        /// <returns>The search endpoint URL.</returns>
        private async Task<string> GetSearchUrl()
        {
            if (!SearchUrl.IsNullOrEmpty())
            {
                return SearchUrl;
            }

            try
            {
                string url = UrlBase;
                
                string response = null;
                using (var cts = new CancellationTokenSource(ScriptDownloadTimeoutMs))
                {
                    try
                    {
                        using (var httpResp = await httpClient.GetAsync(url, cts.Token).ConfigureAwait(false))
                        {
                            if (!httpResp.IsSuccessStatusCode)
                            {
                                try { Logger.Warn($"HTTP {(int)httpResp.StatusCode} downloading {url}"); } catch { }
                                return "/api/search";
                            }

                            response = await httpResp.Content.ReadAsStringAsync().ConfigureAwait(false);
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        try { Logger.Warn($"Timeout {ScriptDownloadTimeoutMs}ms downloading {url}"); } catch { }
                        return "/api/search";
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, true, PluginDatabase.PluginName);
                        return "/api/search";
                    }
                }

                    List<string> scriptUrls = new List<string>();
                    try
                    {
                        var hapDocType = Type.GetType("HtmlAgilityPack.HtmlDocument, HtmlAgilityPack");
                        if (hapDocType != null)
                        {
                            try
                            {
                                dynamic doc = Activator.CreateInstance(hapDocType);
                                var loadHtml = hapDocType.GetMethod("LoadHtml");
                                loadHtml.Invoke(doc, new object[] { response });
                                var documentNode = hapDocType.GetProperty("DocumentNode").GetValue(doc);
                                var selectNodes = documentNode.GetType().GetMethod("SelectNodes", new Type[] { typeof(string) });
                                var nodes = selectNodes.Invoke(documentNode, new object[] { "//script[@src]" }) as System.Collections.IEnumerable;
                                if (nodes != null)
                                {
                                    foreach (var node in nodes)
                                    {
                                        try
                                        {
                                            var attrsProp = node.GetType().GetProperty("Attributes");
                                            if (attrsProp == null)
                                            {
                                                continue;
                                            }
                                            var attrs = attrsProp.GetValue(node);
                                            if (attrs == null)
                                            {
                                                continue;
                                            }
                                            var getAttr = attrs.GetType().GetMethod("Get", new Type[] { typeof(string) });
                                            if (getAttr == null)
                                            {
                                                continue;
                                            }
                                            var srcAttr = getAttr.Invoke(attrs, new object[] { "src" });
                                            if (srcAttr != null)
                                            {
                                                var valProp = srcAttr.GetType().GetProperty("Value");
                                                var val = valProp != null ? valProp.GetValue(srcAttr) as string : null;
                                                if (!string.IsNullOrEmpty(val)) scriptUrls.Add(val);
                                            }
                                        }
                                        catch (Exception inner)
                                        {
                                            try { Logger.Warn(inner, "HLTB: HAP node parsing failed; continuing with remaining nodes"); } catch { }
                                        }
                                    }
                                }
                                else
                                {
                                    var matches = Regex.Matches(response, "<script[^>]*src=[\"']([^\"']+)[\"'][^>]*>", RegexOptions.IgnoreCase);
                                    foreach (Match match in matches)
                                    {
                                        scriptUrls.Add(match.Groups[1].Value);
                                    }
                                }
                            }
                            catch (Exception hapEx)
                            {
                                try { Logger.Warn(hapEx, "HLTB: HtmlAgilityPack reflection failed; using regex fallback. Consider updating HAP usage."); } catch { }
                                var matches = Regex.Matches(response, "<script[^>]*src=[\"']([^\"']+)[\"'][^>]*>", RegexOptions.IgnoreCase);
                                foreach (Match match in matches)
                                {
                                    scriptUrls.Add(match.Groups[1].Value);
                                }
                            }
                        }
                        else
                        {
                            var matches = Regex.Matches(response, "<script[^>]*src=[\"']([^\"']+)[\"'][^>]*>", RegexOptions.IgnoreCase);
                            foreach (Match match in matches)
                            {
                                scriptUrls.Add(match.Groups[1].Value);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        try { Logger.Warn(ex, "HLTB: script URL extraction encountered an error; using regex fallback"); } catch { }
                        var matches = Regex.Matches(response, "<script[^>]*src=[\"']([^\"']+)[\"'][^>]*>", RegexOptions.IgnoreCase);
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
                            string suffix = searchMatch.Groups[1].Value;
                            if (suffix.Contains("/"))
                            {
                                suffix = suffix.Split('/')[0];
                            }

                            if (suffix != "find")
                            {
                                lock (SearchUrlLock)
                                {
                                    if (SearchUrl.IsNullOrEmpty())
                                    {
                                        SearchUrl = "/api/" + suffix;
                                    }
                                }
                                return SearchUrl;
                            }
                        }
                    }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            return "/api/search";
        }

        /// <summary>
        /// Retrieves the authentication token.
        /// </summary>
        /// <returns>The auth token.</returns>
        private async Task<string> GetAuthToken()
        {
            try
            {
                if (!string.IsNullOrEmpty(CachedAuthToken) && DateTime.UtcNow < CachedAuthTokenExpiry)
                {
                    return CachedAuthToken;
                }

                List<HttpHeader> headers = new List<HttpHeader>
                {
                    new HttpHeader { Key = "Referer", Value = UrlBase }
                };
                string url = UrlBase + "/api/search/init?t=" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                string response = await Web.DownloadStringData(url, headers);

                var data = Serialization.FromJson<Dictionary<string, string>>(response);
                if (data != null && data.TryGetValue("token", out string token))
                {
                    lock (AuthTokenSync)
                    {
                        if (!string.IsNullOrEmpty(CachedAuthToken) && DateTime.UtcNow < CachedAuthTokenExpiry)
                        {
                            return CachedAuthToken;
                        }
                        CachedAuthToken = token;
                        CachedAuthTokenExpiry = DateTime.UtcNow.AddSeconds(90);
                    }
                    return token;
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
                        NeedsDetails = true
                    }
                )?.ToList() ?? new List<HltbDataUser>();

                try
                {
                    LogDebugVerbose($"Api Search mapped {search.Count} items for '{name}' (platform='{platform}'). First item sample: " + (search.Count>0 ? Serialization.ToJson(search[0].GameHltbData) : "none"));
                }
                catch { }

                if (search.Any())
                {
                    var tasks = search.Select(async x =>
                    {
                        bool acquiredGameSemaphore = false;
                        try
                        {
                            int target = ConcurrencyController?.TargetConcurrency ?? MaxParallelGameDataDownloads;
                            CurrentSemaphoreLimit = await AdjustSemaphoreLimit(DynamicSemaphore, CurrentSemaphoreLimit, target, ConcurrencySync, "Search");

                            try
                            {
                                int targetGameLog = ConcurrencyController?.TargetConcurrency ?? MaxParallelGameDataDownloads;
                                int availableGameLog = DynamicSemaphore?.CurrentCount ?? 0;
                                int inFlightGameLog = Math.Max(0, targetGameLog - availableGameLog);
                                LogDebugVerbose($"Search: waiting semaphore for id={x.Id} target={targetGameLog} currentLimit={CurrentSemaphoreLimit} available={availableGameLog} inFlight={inFlightGameLog}");
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
                                LogDebugVerbose($"Search: acquired semaphore for id={x.Id} target={targetGameLog} currentLimit={CurrentSemaphoreLimit} available={availableGameLog} inFlight={inFlightGameLog}");
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

                                if (hasCoreTimes && !x.NeedsDetails)
                                {
                                    LogDebugVerbose($"Search: skipping GetGameData for id={x.Id} (search result has times)");
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
                            LogDebugVerbose($"Search: released semaphore for id={x.Id}");
                        }
                    }).ToArray();

                    await Task.WhenAll(tasks);
                    try
                    {
                        LogDebugVerbose($"Search summary: persistentCacheHits={PersistentCacheHits}, inMemoryHits={InMemoryCacheHits}, pageFetches={PageFetches}");
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
        public async Task<List<HltbSearch>> SearchTwoMethod(string name, string platform = "")
        {
            List<HltbDataUser> dataSearch = await Search(name, platform);
            List<HltbDataUser> dataSearchNormalized = new List<HltbDataUser>();

            if (dataSearch.Count == 0)
            {
                dataSearchNormalized = await Search(PlayniteTools.NormalizeGameName(name, true, true), platform);
            }

            List<HltbDataUser> dataSearchFinal = new List<HltbDataUser>();
            dataSearchFinal.AddRange(dataSearch ?? new List<HltbDataUser>());
            dataSearchFinal.AddRange(dataSearchNormalized ?? new List<HltbDataUser>());

            dataSearchFinal = dataSearchFinal.GroupBy(x => x.Id).Select(x => x.First()).ToList();

            return dataSearchFinal.Select(x => new HltbSearch { MatchPercent = Fuzz.Ratio(name.ToLower(), x.Name.ToLower()), Data = x })
                .OrderByDescending(x => x.MatchPercent)
                .ToList();
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
                    try { LogDebugVerbose($"ApiSearch cache hit for '{name}' platform='" + platform + "'"); } catch { }
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
                string serializedBody = Serialization.ToJson(searchParam);
                string searchUrl = await GetSearchUrl();
                bool tokenReused = !string.IsNullOrEmpty(CachedAuthToken) && DateTime.UtcNow < CachedAuthTokenExpiry;
                string token = await GetAuthToken();
                if (!token.IsNullOrEmpty())
                {
                    httpHeaders.Add(new HttpHeader { Key = "x-auth-token", Value = token });
                }

                bool acquired = false;
                try
                {
                    try
                    {
                        try
                        {
                            int target = GetSearchTarget();
                            CurrentSearchLimit = await AdjustSemaphoreLimit(SearchSemaphore, CurrentSearchLimit, target, SearchConcurrencySync, "ApiSearch+Initial");
                        }
                        catch { }

                            try
                            {
                                int targetLog = GetSearchTarget();
                                int availableLog = SearchSemaphore?.CurrentCount ?? 0;
                                int inFlightLog = Math.Max(0, targetLog - availableLog);
                                LogDebugVerbose($"ApiSearch: waiting search semaphore for '{name}' target={targetLog} currentLimit={CurrentSearchLimit} available={availableLog} inFlight={inFlightLog}");
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
                            LogDebugVerbose($"ApiSearch: acquired search semaphore for '{name}' target={targetLog} currentLimit={CurrentSearchLimit} available={availableLog} inFlight={inFlightLog}");
                        }
                        catch { }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, false, PluginDatabase.PluginName);
                    }

                    var sw = Stopwatch.StartNew();
                    var postResult = await Web.PostJsonWithSharedClientWithStatus(UrlBase + searchUrl, serializedBody, httpHeaders);
                    sw.Stop();
                    string json = postResult?.Item1 ?? string.Empty;
                    int statusCode = postResult?.Item2 ?? 0;
                    string retryAfterHeader = postResult?.Item3;

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

                        LogDebugVerbose($"ApiSearch elapsed={sw.ElapsedMilliseconds}ms tokenReused={tokenReused} status={statusCode}");
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
                            double frac = (double)count429 / (double)codes.Length;
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
                                            int diff = SearchBackoffLimit - CurrentSearchLimit;
                                            if (diff > 0)
                                            {
                                                SearchSemaphore.Release(diff);
                                                CurrentSearchLimit = SearchBackoffLimit;
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

                            CurrentSearchLimit = await AdjustSemaphoreLimit(SearchSemaphore, CurrentSearchLimit, backoffSnapshot, SearchConcurrencySync, "ApiSearch+PostBackoff");
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
                            LogDebugVerbose($"ApiSearch: released search semaphore for '{name}'");
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
            Common.LogDebug(true, $"Search data for {game.Name}");
            if (API.Instance.ApplicationInfo.Mode == ApplicationMode.Desktop)
            {
                HowLongToBeatSelect ViewExtension = null;
                _ = Application.Current.Dispatcher.BeginInvoke((Action)delegate
                {
                    ViewExtension = new HowLongToBeatSelect(game, data);
                    Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(ResourceProvider.GetString("LOCSelection") + " - " + game.Name + " - " + (game.Source?.Name ?? "Playnite"), ViewExtension);
                    _ = windowExtension.ShowDialog();
                }).Wait();

                if (ViewExtension.GameHowLongToBeat?.Items.Count > 0)
                {
                    return ViewExtension.GameHowLongToBeat;
                }
            }
            return null;
        }

        #endregion

        #region user account

        /// <summary>
        /// Checks if the user is currently logged in to HowLongToBeat.
        /// </summary>
        /// <returns>True if logged in, otherwise false.</returns>
        public bool GetIsUserLoggedIn()
        {
            if (UserId == 0)
            {
                _ = SpinWait.SpinUntil(() => PluginDatabase.IsLoaded, -1);
                UserId = PluginDatabase.Database.UserHltbData.UserId;
            }

            if (UserId == 0)
            {
                IsConnected = false;
                return false;
            }

            if (IsConnected == null)
            {
                IsConnected = GetUserId().GetAwaiter().GetResult() != 0;
            }

            IsConnected = (bool)IsConnected;
            return (bool)IsConnected;
        }

        /// <summary>
        /// Initiates the login process for HowLongToBeat.
        /// </summary>
        public void Login()
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate
            {
                Logger.Info("Login()");

                WebViewSettings settings = new WebViewSettings
                {
                    JavaScriptEnabled = true,
                    WindowHeight = 670,
                    WindowWidth = 490,
                    UserAgent = Web.UserAgent
                };

                using (IWebView webView = API.Instance.WebViews.CreateView(settings))
                {
                    webView.LoadingChanged += (s, e) =>
                    {
                        Common.LogDebug(true, $"NavigationChanged - {webView.GetCurrentAddress()}");

                        if (webView.GetCurrentAddress().StartsWith(UrlBase + "/user/"))
                        {
                            UserLogin = WebUtility.HtmlDecode(webView.GetCurrentAddress().Replace(UrlBase + "/user/", string.Empty));
                            IsConnected = true;


                            Thread.Sleep(1500);
                            webView.Close();
                        }
                    };

                    IsConnected = false;
                    webView.Navigate(UrlLogOut);
                    _ = webView.OpenDialog();
                }
            }).Completed += (s, e) =>
            {
                if ((bool)IsConnected)
                {
                    _ = Application.Current.Dispatcher?.BeginInvoke((Action)delegate
                    {
                        try
                        {
                            List<HttpCookie> cookies = CookiesTools.GetWebCookies();
                            _ = CookiesTools.SetStoredCookies(cookies);

                            PluginDatabase.PluginSettings.Settings.UserLogin = UserLogin;

                            PluginDatabase.Plugin.SavePluginSettings(PluginDatabase.PluginSettings.Settings);

                            _ = Task.Run(() =>
                            {
                                UserId = GetUserId().GetAwaiter().GetResult();
                                PluginDatabase.RefreshUserData();
                            });
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false, true, PluginDatabase.PluginName);
                        }
                    });
                }
            };
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
                string response = await Web.DownloadPageText(UrlUser, cookies);
                dynamic t = Serialization.FromJson<dynamic>(response);
                return response == "{}" ? 0 : t?.data[0]?.user_id ?? 0;
            }
            catch (Exception ex)
            {
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

                string json = await Web.PostStringDataPayload(string.Format(UrlUserGamesList, UserId), payload, cookies);
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

                string jsonData = Tools.GetJsonInString(response, @"<script[ ]?id=""__NEXT_DATA__""[ ]?type=""application/json"">");
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
        /// Retrieves the user data from HowLongToBeat.
        /// </summary>
        /// <returns>Returns a <see cref="HltbUserStats"/> object, or null if not logged in.</returns>
        public HltbUserStats GetUserData()
        {
            if (GetIsUserLoggedIn())
            {
                HltbUserStats hltbUserStats = new HltbUserStats
                {
                    Login = UserLogin.IsNullOrEmpty() ? PluginDatabase.Database.UserHltbData.Login : UserLogin,
                    UserId = (UserId == 0) ? PluginDatabase.Database.UserHltbData.UserId : UserId,
                    TitlesList = new List<TitleList>()
                };

                UserGamesList userGamesList = GetUserGamesList().GetAwaiter().GetResult();
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
        /// Retrieves user data for a specific game by game ID.
        /// </summary>
        /// <param name="gameId">The game ID.</param>
        /// <returns>Returns a <see cref="TitleList"/> object, or null if not found.</returns>
        public TitleList GetUserData(string gameId)
        {
            if (GetIsUserLoggedIn())
            {
                try
                {
                    HltbUserStats data = GetUserData();
                    return data?.TitlesList?.Find(x => x.Id == gameId);
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                }

                return null;
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
        /// Checks if a user game ID exists in the user's games list.
        /// </summary>
        /// <param name="userGameId">The user game ID.</param>
        /// <returns>True if the ID exists, otherwise false.</returns>
        public bool EditIdExist(string userGameId)
        {
            return GetUserGamesList()?.GetAwaiter().GetResult()?.Data?.GamesList?.Find(x => x.Id.ToString().IsEqual(userGameId))?.Id != null;
        }

        /// <summary>
        /// Finds the existing user game ID for a given game ID.
        /// </summary>
        /// <param name="gameId">The game ID.</param>
        /// <returns>Returns the user game ID as a string, or null if not found.</returns>
        public string FindIdExisting(string gameId)
        {
            return GetUserGamesList()?.GetAwaiter().GetResult().Data?.GamesList?.Find(x => x.GameId.ToString().IsEqual(gameId))?.Id.ToString() ?? null;
        }

        #endregion


        /// <summary>
        /// Submits the current game data to the HowLongToBeat website.
        /// </summary>
        /// <param name="game">The Playnite game object.</param>
        /// <param name="editData">The data to submit.</param>
        /// <returns>True if submission is successful, otherwise false.</returns>
        public async Task<bool> ApiSubmitData(Game game, EditData editData)
        {
            if (GetIsUserLoggedIn() && editData.UserId != 0 && editData.GameId != 0)
            {
                try
                {
                    List<HttpCookie> cookies = CookiesTools.GetStoredCookies();
                    string payload = Serialization.ToJson(editData);
                    List<KeyValuePair<string, string>> moreHeaders = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36")
                    };
                    string response = await Web.PostStringDataPayload(UrlPostData, payload, cookies);

                    // Check errors
                    // TODO Rewrite
                    if (response.Contains("error"))
                    {
                        _ = Serialization.TryFromJson(response, out dynamic error);
                        API.Instance.Notifications.Add(new NotificationMessage(
                            $"{PluginDatabase.PluginName}-{game.Id}-Error",
                            PluginDatabase.PluginName + Environment.NewLine + game.Name + (error?["error"]?[0] != null ? Environment.NewLine + error["error"][0] : string.Empty),
                            NotificationType.Error
                        ));
                    }
                    else if (response.IsNullOrEmpty())
                    {
                        API.Instance.Notifications.Add(new NotificationMessage(
                              $"{PluginDatabase.PluginName}-{game.Id}-Error",
                              PluginDatabase.PluginName + Environment.NewLine + game.Name,
                              NotificationType.Error
                          ));
                    }
                    else
                    {
                        PluginDatabase.RefreshUserData(editData.GameId.ToString());
                        return true;
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

            return false;
        }

        /// <summary>
        /// Updates the stored cookies for the current user session.
        /// </summary>
        public void UpdatedCookies()
        {
            _ = Task.Run(() =>
            {
                try
                {
                    // Wait extension database are loaded
                    _ = SpinWait.SpinUntil(() => PluginDatabase.IsLoaded, -1);

                    if (PluginDatabase.Database.UserHltbData?.UserId != null && PluginDatabase.Database.UserHltbData.UserId != 0)
                    {
                        Logger.Info($"Refresh HowLongToBeat user cookies");
                        List<HttpCookie> cookies = CookiesTools.GetNewWebCookies(new List<string> { UrlBase, UrlUser }, true);
                        _ = CookiesTools.SetStoredCookies(cookies);
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                }
            });
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
    }
}