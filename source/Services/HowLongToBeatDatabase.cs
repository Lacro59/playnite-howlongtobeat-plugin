using CommonPlayniteShared.Common;
using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Extensions;
using HowLongToBeat.Models;
using HowLongToBeat.Models.Api;
using HowLongToBeat.Models.Enumerations;
using HowLongToBeat.Models.GameActivity;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace HowLongToBeat.Services
{
    public class HowLongToBeatDatabase : PluginDatabaseObject<HowLongToBeatSettings, GameHowLongToBeat, HltbDataUser>
    {
        public HowLongToBeat Plugin { get; set; }
        public HowLongToBeatApi HowLongToBeatApi { get; set; }
        public HltbUserStats UserHltbData { get; set; } = new HltbUserStats();
        private readonly object UserHltbDataSync = new object();

        private static bool DontSetToHtlb { get; set; } = false;


        public HowLongToBeatDatabase(HowLongToBeatSettings pluginSettings, string pluginUserDataPath) : base(pluginSettings, "HowLongToBeat", pluginUserDataPath)
        {
            TagBefore = "[HLTB]";
            PluginWindows = new HowLongToBeatWindows(PluginName, this);
            PluginExportCsv = new HowLongToBeatExport();
        }

        // Verbose logs follow the settings toggle; DEBUG builds always enable them for local development.
        public bool IsVerboseLoggingEnabled
        {
            get
            {
#if DEBUG
                return true;
#else
                return PluginSettings?.EnableVerboseLogging ?? false;
#endif
            }
        }

        private void FireAndForget(Task task, string context)
        {
            // Delegate to centralized helper to avoid duplication with HowLongToBeatApi
            try
            {
                TaskHelpers.FireAndForget(task, context, LogManager.GetLogger());
            }
            catch { }
        }

        // Run synchronous Task helpers are centralized in Services.TaskHelpers to avoid duplication and ensure consistent behavior.
        // Use TaskHelpers.RunSyncWithTimeout(...) or TaskHelpers.TryRunSyncWithTimeout(...) where needed.

        public void InitializeClient(HowLongToBeat plugin)
        {
            Plugin = plugin;
            try
            {
                if (HowLongToBeatApi == null)
                {
                    try
                    {
                        // Create the API instance synchronously so callers do not observe a null reference.
                        HowLongToBeatApi = new HowLongToBeatApi();
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, true, PluginName);
                        HowLongToBeatApi = null;
                    }

                    // Session cookies are refreshed on demand before profile submit (GetCookiesForSubmitAsync).
                    if (HowLongToBeatApi != null)
                    {
                        // Load cached user stats as soon as the API exists.
                        // LoadMoreData can run before InitializeClient and will then set an empty placeholder.
                        FireAndForget(Task.Run(() =>
                        {
                            try
                            {
                                var data = HowLongToBeatApi.LoadUserData();
                                if (data == null)
                                {
                                    return;
                                }

                                try { Logger.Info($"HLTB UserData: loaded cached stats titles={data.TitlesList?.Count ?? 0}"); } catch { }

                                try
                                {
                                    Application.Current?.Dispatcher?.BeginInvoke((Action)(() =>
                                    {
                                        try
                                        {
                                            UserHltbData = data;
                                            _database?.OnCollectionChanged(null, null);
                                        }
                                        catch (Exception innerEx)
                                        {
                                            Common.LogError(innerEx, false, true, PluginName);
                                        }
                                    }));
                                }
                                catch (Exception ex)
                                {
                                    Common.LogError(ex, false, true, PluginName);
                                }
                            }
                            catch (Exception ex)
                            {
                                Common.LogError(ex, false, true, PluginName);
                            }
                        }), "Load cached user data");
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }
        }

        protected override void LoadMoreData()
        {
            try
            {
                if (HowLongToBeatApi == null)
                {
                    if (IsVerboseLoggingEnabled)
                    {
                        Common.LogDebug(true,"HowLongToBeatApi not initialized yet during LoadMoreData(); using empty UserHltbData placeholder");
                    }
                    UserHltbData = new HltbUserStats();
                    return;
                }

                UserHltbData = new HltbUserStats();
                FireAndForget(Task.Run(() =>
                {
                    try
                    {
                        var data = HowLongToBeatApi.LoadUserData();
                        if (data != null)
                        {
                            try
                            {
                                var dispatcher = Application.Current?.Dispatcher;
                                if (dispatcher != null)
                                {
                                    // Use BeginInvoke to avoid blocking the background thread and keep behavior consistent.
                                    dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        try
                                        {
                                            UserHltbData = data;
                                            _database?.OnCollectionChanged(null, null);
                                        }
                                        catch (Exception innerEx)
                                        {
                                            Common.LogError(innerEx, false, true, PluginName);
                                        }
                                    }));
                                }
                            }
                            catch (Exception ex)
                            {
                                Common.LogError(ex, false, true, PluginName);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, true, PluginName);
                    }
                }), "LoadMoreData LoadUserData");
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
                UserHltbData = new HltbUserStats();
            }
        }

        public override GameHowLongToBeat Get(Guid id, bool onlyCache = false, bool force = false)
        {
            GameHowLongToBeat gameHowLongToBeat = GetOnlyCache(id);

            if (!onlyCache && gameHowLongToBeat != null && !gameHowLongToBeat.HasData)
            {
                gameHowLongToBeat = null;
            }

            if ((gameHowLongToBeat == null && !onlyCache) || force)
            {
                if (HowLongToBeatApi == null)
                {
                    // Clear, descriptive exception to avoid null-reference later on.
                    throw new InvalidOperationException("HowLongToBeatApi is not initialized. Call InitializeClient before using the database.");
                }

                gameHowLongToBeat = HowLongToBeatApi.SearchData(API.Instance.Database.Games.Get(id));

                if (gameHowLongToBeat != null)
                {
                    AddOrUpdate(gameHowLongToBeat);
                }
            }

            if (gameHowLongToBeat == null)
            {
                Game game = API.Instance.Database.Games.Get(id);
                if (game != null)
                {
                    gameHowLongToBeat = GetDefault(game);
                }
            }

            return gameHowLongToBeat;
        }

        private string GetSearchPlatform(Game game)
        {
            try
            {
                if (game?.Platforms == null || game.Platforms.Count == 0)
                {
                    return string.Empty;
                }

                var platform = game.Platforms.FirstOrDefault();
                if (platform == null)
                {
                    return string.Empty;
                }

                var match = PluginSettings?.Platforms?.FirstOrDefault(p => p?.Platform != null && p.Platform.Equals(platform))?.HltbPlatform;
                if (match != null)
                {
                    return match.GetDescription();
                }

                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }


        public void AddData(Game game)
        {
            if (game == null)
            {
                return;
            }

            GameHowLongToBeat gameHowLongToBeat = Get(game, true);

            if (gameHowLongToBeat.Items.Count > 0)
            {
                Logger.Warn($"Data is already added for {game.Name}");
                return;
            }

            if (HowLongToBeatApi == null)
            {
                Logger.Warn("HowLongToBeatApi not initialized yet; cannot perform AddData");
                return;
            }

            string platform = GetSearchPlatform(game);

            Func<string, bool> tryAddWithPlatform = (platformFilter) =>
            {
                try
                {
                    HltbDataUser auto = HowLongToBeatApi.SearchDataAuto(game.Name, platformFilter);
                    if (auto != null)
                    {
                        gameHowLongToBeat.Items = new List<HltbDataUser> { auto };
                        gameHowLongToBeat.DateLastRefresh = DateTime.Now;
                        AddOrUpdate(gameHowLongToBeat);
                        return true;
                    }

                    if (PluginSettings?.UseMatchValue == true)
                    {
                        var results = TaskHelpers.RunSyncWithTimeout(() => HowLongToBeatApi.SearchTwoMethod(game.Name, platformFilter), 15000);
                        if (results != null && results.Count == 1 && results[0]?.Data != null)
                        {
                            var single = results[0];
                            bool accept = false;

                            try
                            {
                                if (single.MatchPercent >= 80)
                                {
                                    accept = true;
                                }
                                else
                                {
                                    var n1 = PlayniteTools.NormalizeGameName(game?.Name ?? string.Empty, true, true);
                                    var n2 = PlayniteTools.NormalizeGameName(single.Data?.Name ?? string.Empty, true, true);
                                    if (!string.IsNullOrEmpty(n1) && !string.IsNullOrEmpty(n2) && n1.IsEqual(n2))
                                    {
                                        accept = true;
                                    }
                                }
                            }
                            catch
                            {
                                accept = false;
                            }

                            if (accept)
                            {
                                gameHowLongToBeat.Items = new List<HltbDataUser> { single.Data };
                                gameHowLongToBeat.DateLastRefresh = DateTime.Now;
                                AddOrUpdate(gameHowLongToBeat);
                                return true;
                            }
                        }
                    }

                    List<HltbSearch> data = TaskHelpers.RunSyncWithTimeout(() => HowLongToBeatApi.SearchTwoMethod(game.Name, platformFilter), 15000) ?? new List<HltbSearch>();
                    if (data.Count == 1 && PluginSettings.AutoAccept)
                    {
                        gameHowLongToBeat.Items = new List<HltbDataUser>() { data.First().Data };
                        gameHowLongToBeat.DateLastRefresh = DateTime.Now;
                        AddOrUpdate(gameHowLongToBeat);
                        return true;
                    }

                    if (data.Count > 0 && PluginSettings.UseMatchValue)
                    {
                        if (data.First().MatchPercent >= PluginSettings.MatchValue)
                        {
                            gameHowLongToBeat.Items = new List<HltbDataUser>() { data.First().Data };
                            gameHowLongToBeat.DateLastRefresh = DateTime.Now;
                            AddOrUpdate(gameHowLongToBeat);
                            return true;
                        }
                    }

                    if (data.Count > 0 && PluginSettings.ShowWhenMismatch)
                    {
                        var picked = HowLongToBeatApi.SearchData(game, data.Select(x => x.Data).ToList());
                        if (picked != null)
                        {
                            picked.DateLastRefresh = DateTime.Now;
                            AddOrUpdate(picked);
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginName);
                }

                return false;
            };

            // 1) Try with platform filter
            if (tryAddWithPlatform(platform))
            {
                return;
            }

            // 2) Fall back to no platform filter (more permissive; matches manual search behavior)
            if (!platform.IsNullOrEmpty())
            {
                if (tryAddWithPlatform(string.Empty))
                {
                    return;
                }
            }
        }

        public override void RefreshNoLoader(Guid id, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            Game game = API.Instance.Database.Games.Get(id);
            if (IsVerboseLoggingEnabled)
            {
                Common.LogDebug(true,$"RefreshNoLoader({game?.Name} - {game?.Id})");
            }

            GameHowLongToBeat loadedItem = Get(id, true);
            if (loadedItem.GetData().Id.IsNullOrEmpty())
            {
                if (IsVerboseLoggingEnabled)
                {
                    Common.LogDebug(true,$"No data, try to add");
                }
                AddData(game);
                loadedItem = Get(id, true);
                if (loadedItem.GetData().Id.IsNullOrEmpty())
                {
                    if (IsVerboseLoggingEnabled)
                    {
                        Common.LogDebug(true,$"No find");
                    }
                }
            }
            else
            {
                if (loadedItem.GetData().IsVndb)
                {
                    var dataSearch = TaskHelpers.RunSyncWithTimeout(() => VndbApi.SearchByIdAsync(loadedItem.GetData().Id), 15000) ?? new List<HltbDataUser>();
                    HltbDataUser webDataSearch = dataSearch.Find(x => x.Id == loadedItem.GetData().Id);
                    if (webDataSearch != null)
                    {
                        var previousVndb = loadedItem.GetData();
                        webDataSearch.ApplyVndbSpeedSelection(HltbDataUser.InferVndbSpeedAfterRefresh(previousVndb, webDataSearch));
                        loadedItem.Items = new List<HltbDataUser> { webDataSearch };
                        loadedItem.DateLastRefresh = DateTime.Now;
                        Update(loadedItem);
                    }
                }
                else
                {
                    string hltbId = loadedItem.GetData()?.Id ?? string.Empty;
                    Logger.Info(string.Format(
                        "HLTB RefreshNoLoader path=HltbPageFetch: playniteGame='{0}' hltbId='{1}' urlBefore='{2}' urlImgBefore='{3}'",
                        game?.Name ?? string.Empty,
                        hltbId,
                        loadedItem.GetData()?.Url ?? string.Empty,
                        loadedItem.GetData()?.UrlImg ?? string.Empty));

                    if (HowLongToBeatApi != null)
                    {
                        HltbDataUser updated = TaskHelpers.RunSyncWithTimeout(() => HowLongToBeatApi.UpdateGameData(loadedItem.Items.First()), 15000);
                        loadedItem.Items = new List<HltbDataUser> { updated != null ? updated : loadedItem.Items.First() };
                    }
                    else
                    {
                        Logger.Warn("HowLongToBeatApi not initialized; skipping UpdateGameData in RefreshNoLoader");
                        loadedItem.Items = new List<HltbDataUser> { loadedItem.Items.First() };
                    }
                    loadedItem.DateLastRefresh = DateTime.Now;
                    Update(loadedItem);

                    var after = loadedItem.GetData();
                    Logger.Info(string.Format(
                        "HLTB RefreshNoLoader path=HltbPageFetch DONE: playniteGame='{0}' hltbId='{1}' url='{2}' urlImg='{3}' main={4}s",
                        game?.Name ?? string.Empty,
                        after?.Id ?? string.Empty,
                        after?.Url ?? string.Empty,
                        after?.UrlImg ?? string.Empty,
                        after?.GameHltbData?.MainStoryClassic ?? 0));

                    // Refresh User data
                    List<TitleList> titleLists = GetUserHltbDataAll(loadedItem.GetData().Id);
                    if (titleLists != null && titleLists.Count > 0)
                    {
                        RefreshUserData(loadedItem.GetData().Id);
                    }
                }
            }

            ActionAfterRefresh(loadedItem);
        }

        public Guid ResolveGameIdFromUserTitle(string hltbId, string userGameId = "")
        {
            try
            {
                if (hltbId.IsNullOrEmpty())
                {
                    return default;
                }

                return GetAllCache()
                    .Where(x => x?.Game != null
                        && !x.Game.Hidden
                        && x.GetData()?.Id == hltbId
                        && (x.UserGameId.IsNullOrEmpty() || x.UserGameId.IsEqual(userGameId)))
                    .Select(x => x.Id)
                    .FirstOrDefault();
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
                return default;
            }
        }

        public List<Guid> ResolveGameIdsFromUserTitle(string hltbId, string userGameId = "")
        {
            try
            {
                if (hltbId.IsNullOrEmpty())
                {
                    return new List<Guid>();
                }

                return GetAllCache()
                    .Where(x => x != null
                        && x.GetData()?.Id == hltbId
                        && (x.UserGameId.IsNullOrEmpty() || x.UserGameId.IsEqual(userGameId)))
                    .Select(x => x.Id)
                    .ToList();
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
                return new List<Guid>();
            }
        }


        #region Tag

        // Ignore-sync tag name comes from LOCHowLongToBeatIgnoreSyncTag (localized), prefixed with TagBefore ([HLTB]).
        // If the user changes Playnite UI language, ResourceProvider may resolve a different label and CheckTagExist
        // creates a new tag; games tagged under the previous language no longer match FindExistingIgnoreSyncTagId
        // until re-tagged. Same limitation as playtime range tags. A fixed English label would avoid duplicates.

        /// <summary>
        /// Resolves the ignore-sync tag ID without creating it when missing.
        /// </summary>
        /// <returns>Existing tag ID, or <c>null</c>.</returns>
        private Guid? FindExistingIgnoreSyncTagId()
        {
            string tagLabel = ResourceProvider.GetString("LOCHowLongToBeatIgnoreSyncTag");
            string fullName = TagBefore.IsNullOrEmpty()
                ? tagLabel
                : string.Format("{0} {1}", TagBefore, tagLabel);

            Tag existing = API.Instance?.Database?.Tags?
                .FirstOrDefault(t => t != null && string.Equals(t.Name, fullName, StringComparison.Ordinal));

            return existing?.Id;
        }

        /// <summary>
        /// Returns the Playnite tag ID used to exclude a game from automatic playtime sync, creating the tag if needed.
        /// </summary>
        /// <returns>Tag ID, or <c>null</c> if the tag could not be created.</returns>
        public Guid? GetIgnoreSyncTagId()
        {
            return CheckTagExist(ResourceProvider.GetString("LOCHowLongToBeatIgnoreSyncTag"));
        }

        /// <inheritdoc/>
        protected override IEnumerable<Guid> GetProtectedPluginTagIds()
        {
            Guid? ignoreTagId = FindExistingIgnoreSyncTagId();
            if (ignoreTagId != null)
            {
                yield return ignoreTagId.Value;
            }
        }

        /// <summary>
        /// Indicates whether the game is tagged to skip automatic HowLongToBeat playtime sync.
        /// </summary>
        /// <param name="game">Playnite game.</param>
        /// <returns><c>true</c> when the ignore-sync tag is present.</returns>
        public bool IsGameIgnoredForPlaytimeSync(Game game)
        {
            if (game?.TagIds == null || game.TagIds.Count == 0)
            {
                return false;
            }

            Guid? ignoreTagId = FindExistingIgnoreSyncTagId();
            return ignoreTagId != null && game.TagIds.Contains(ignoreTagId.Value);
        }

        /// <summary>
        /// Adds the ignore-sync tag to the game and persists the change.
        /// </summary>
        /// <param name="game">Playnite game.</param>
        public void AddIgnoreSyncTag(Game game)
        {
            if (game == null)
            {
                return;
            }

            Guid? ignoreTagId = GetIgnoreSyncTagId();
            if (ignoreTagId == null)
            {
                return;
            }

            AppendTagId(game, ignoreTagId.Value);
            PersistGameUpdate(game);
            Common.LogDebug(true, $"Added ignore playtime sync tag for {game.Name}");
        }

        /// <summary>
        /// Removes the ignore-sync tag from the game and persists the change.
        /// </summary>
        /// <param name="game">Playnite game.</param>
        public void RemoveIgnoreSyncTag(Game game)
        {
            if (game?.TagIds == null)
            {
                return;
            }

            Guid? ignoreTagId = FindExistingIgnoreSyncTagId();
            if (ignoreTagId == null || !game.TagIds.Contains(ignoreTagId.Value))
            {
                return;
            }

            game.TagIds.Remove(ignoreTagId.Value);
            PersistGameUpdate(game);
            Common.LogDebug(true, $"Removed ignore playtime sync tag for {game.Name}");
        }

        /// <summary>
        /// Returns library games that have the ignore-sync tag, ordered by name.
        /// </summary>
        /// <returns>Ignored games.</returns>
        public List<Game> GetGamesIgnoredForPlaytimeSync()
        {
            Guid? ignoreTagId = FindExistingIgnoreSyncTagId();
            if (ignoreTagId == null || API.Instance?.Database?.Games == null)
            {
                return new List<Game>();
            }

            Guid tagId = ignoreTagId.Value;
            return API.Instance.Database.Games
                .Where(g => g?.TagIds != null && g.TagIds.Contains(tagId))
                .OrderBy(g => g.Name)
                .ToList();
        }

        /// <summary>
        /// Returns visible library games that are not tagged for ignore-sync, ordered by name.
        /// </summary>
        /// <returns>Games available to add to the ignore list.</returns>
        public List<Game> GetGamesAvailableForIgnoreSync()
        {
            if (API.Instance?.Database?.Games == null)
            {
                return new List<Game>();
            }

            Guid? ignoreTagId = FindExistingIgnoreSyncTagId();
            Guid tagId = ignoreTagId ?? Guid.Empty;

            return API.Instance.Database.Games
                .Where(g => g != null && !g.Hidden && (ignoreTagId == null || g.TagIds == null || !g.TagIds.Contains(tagId)))
                .OrderBy(g => g.Name)
                .ToList();
        }

        /// <summary>
        /// Asks for confirmation when submitting playtime manually for an ignored game.
        /// </summary>
        /// <param name="game">Playnite game.</param>
        /// <returns><c>true</c> if sync may proceed.</returns>
        public bool ConfirmManualPlaytimeSyncIfIgnored(Game game)
        {
            if (!IsGameIgnoredForPlaytimeSync(game))
            {
                return true;
            }

            MessageBoxResult result = MessageBoxResult.No;
            Application.Current.Dispatcher?.Invoke(() =>
            {
                result = API.Instance.Dialogs.ShowMessage(
                    string.Format(ResourceProvider.GetString("LOCHowLongToBeatIgnoreSyncManualConfirm"), game?.Name),
                    PluginName,
                    MessageBoxButton.YesNo);
            });

            if (result == MessageBoxResult.Yes)
            {
                Common.LogDebug(true, $"Manual playtime sync confirmed for ignored game {game?.Name}");
            }

            return result == MessageBoxResult.Yes;
        }

        protected override bool AppendPluginTag(Game game)
        {
            GameHowLongToBeat item = Get(game, true);

            if (item.HasData)
            {
                try
                {
                    HltbDataUser hltbDataUser = item.GetData();
                    Guid? tagId = ResolvePlaytimeTag(hltbDataUser);
                    if (tagId != null)
                    {
                        AppendTagId(game, tagId.Value);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Tag insert error {game.Name}", true, PluginName,
                        string.Format(ResourceProvider.GetString("LOCCommonNotificationTagError"), game.Name));
                }
                return false;
            }

            if (TagMissing)
            {
                Guid? noDataTagId = AddNoDataTag();
                if (noDataTagId != null)
                {
                    AppendTagId(game, noDataTagId.Value);
                    return true;
                }
            }

            return false;
        }

        private Guid? ResolvePlaytimeTag(HltbDataUser hltbDataUser)
        {
            long hltbTime = hltbDataUser?.GameHltbData?.TimeToBeat ?? 0;
            if (hltbTime <= 0)
            {
                return null;
            }

            if (hltbTime < 3600)
            {
                return CheckTagExist(ResourceProvider.GetString("LOCCommon0to1"));
            }
            if (hltbTime < 18000)
            {
                return CheckTagExist(ResourceProvider.GetString("LOCCommon1to5"));
            }
            if (hltbTime < 36000)
            {
                return CheckTagExist(ResourceProvider.GetString("LOCCommon5to10"));
            }
            if (hltbTime < 72000)
            {
                return CheckTagExist(ResourceProvider.GetString("LOCCommon10to20"));
            }
            if (hltbTime < 108000)
            {
                return CheckTagExist(ResourceProvider.GetString("LOCCommon20to30"));
            }
            if (hltbTime < 144000)
            {
                return CheckTagExist(ResourceProvider.GetString("LOCCommon30to40"));
            }
            if (hltbTime < 180000)
            {
                return CheckTagExist(ResourceProvider.GetString("LOCCommon40to50"));
            }
            if (hltbTime < 216000)
            {
                return CheckTagExist(ResourceProvider.GetString("LOCCommon50to60"));
            }
            if (hltbTime < 252000)
            {
                return CheckTagExist(ResourceProvider.GetString("LOCCommon60to70"));
            }
            if (hltbTime < 288000)
            {
                return CheckTagExist(ResourceProvider.GetString("LOCCommon70to80"));
            }
            if (hltbTime < 324000)
            {
                return CheckTagExist(ResourceProvider.GetString("LOCCommon80to90"));
            }
            if (hltbTime < 360000)
            {
                return CheckTagExist(ResourceProvider.GetString("LOCCommon90to100"));
            }

            return CheckTagExist(ResourceProvider.GetString("LOCCommon100plus"));
        }

        #endregion

        #region User data
        public TitleList GetUserHltbData(string hltbId)
        {
            try
            {
                if (UserHltbData?.TitlesList == null || UserHltbData.TitlesList.Count == 0)
                {
                    return null;
                }
                return UserHltbData.TitlesList == null || UserHltbData.TitlesList.Count == 0
                    ? null
                    : UserHltbData.TitlesList.Find(x => x.Id == hltbId);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, true);
                Logger.Warn($"No HltbData for {hltbId}");
                return null;
            }
        }

        public TitleList GetUserHltbDataCurrent(string hltbId, string userGameId = "")
        {
            try
            {
                List<TitleList> all = GetUserHltbDataAll(hltbId);
                return all == null || all.Count == 0
                    ? null
                    : userGameId.IsNullOrEmpty()
                    ? all.OrderByDescending(x => x.GameStatuses.Where(y => y.Status == StatusType.Playing)?.Count() > 0).ThenByDescending(x => x.LastUpdate).First()
                    : all.FirstOrDefault(x => x.UserGameId.IsEqual(userGameId));
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"No HltbData for {hltbId}");
                return null;
            }
        }

        public List<TitleList> GetUserHltbDataAll(string hltbId)
        {
            try
            {
                if (UserHltbData?.TitlesList == null || UserHltbData.TitlesList.Count == 0)
                {
                    return null;
                }
                return UserHltbData?.TitlesList == null || UserHltbData.TitlesList.Count == 0
                    ? null
                    : UserHltbData.TitlesList.FindAll(x => x.Id == hltbId).ToList();
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"No HltbData for {hltbId}");
                return null;
            }
        }


        /// <summary>
        /// Applies HowLongToBeat list statuses to Playnite completion statuses using the configured Guid mapping.
        /// </summary>
        private void SetGameStatusFromHltb()
        {
            try
            {
                DontSetToHtlb = true;
                //API.Instance.Database.Games.BeginBufferUpdate();
                if (UserHltbData?.TitlesList == null)
                {
                    Logger.Info("Status sync from HLTB skipped: no user titles list");
                    return;
                }

                int appliedCount = 0;
                int unchangedCount = 0;
                int linkedCount = 0;

                UserHltbData.TitlesList.ForEach(x =>
                {
                    if (x.GameExist)
                    {
                        linkedCount++;
                        bool isCompletionist = x.HltbUserData?.Completionist != 0;
                        bool isCompleted = x.HasHltbListStatus(StatusType.Completed);
                        bool isPlaying = x.HasHltbListStatus(StatusType.Playing);
                        bool isBacklog = x.HasHltbListStatus(StatusType.Backlog);
                        bool isReplays = x.HasHltbListStatus(StatusType.Replays);
                        bool isRetired = x.HasHltbListStatus(StatusType.Retired);

                        Game game = API.Instance.Database.Games.Get(x.GameId);
                        Guid previousStatusId = game.CompletionStatusId;
                        string appliedStatus = null;

                        if (isCompletionist && PluginSettings.GameStatusCompletionist != default && API.Instance.Database.CompletionStatuses.Get(PluginSettings.GameStatusCompletionist) != null)
                        {
                            game.CompletionStatusId = PluginSettings.GameStatusCompletionist;
                            appliedStatus = "Completionist";
                        }
                        else if (isCompleted && PluginSettings.GameStatusCompleted != default && API.Instance.Database.CompletionStatuses.Get(PluginSettings.GameStatusCompleted) != null)
                        {
                            game.CompletionStatusId = PluginSettings.GameStatusCompleted;
                            appliedStatus = "Completed";
                        }
                        else if (isPlaying && PluginSettings.GameStatusPlaying != default && API.Instance.Database.CompletionStatuses.Get(PluginSettings.GameStatusPlaying) != null)
                        {
                            game.CompletionStatusId = PluginSettings.GameStatusPlaying;
                            appliedStatus = "Playing";
                        }
                        else if (isBacklog && PluginSettings.GameStatusBacklog != default && API.Instance.Database.CompletionStatuses.Get(PluginSettings.GameStatusBacklog) != null)
                        {
                            game.CompletionStatusId = PluginSettings.GameStatusBacklog;
                            appliedStatus = "Backlog";
                        }
                        else if (isReplays && PluginSettings.GameStatusReplays != default && API.Instance.Database.CompletionStatuses.Get(PluginSettings.GameStatusReplays) != null)
                        {
                            game.CompletionStatusId = PluginSettings.GameStatusReplays;
                            appliedStatus = "Replays";
                        }
                        else if (isRetired && PluginSettings.GameStatusRetired != default && API.Instance.Database.CompletionStatuses.Get(PluginSettings.GameStatusRetired) != null)
                        {
                            game.CompletionStatusId = PluginSettings.GameStatusRetired;
                            appliedStatus = "Retired";
                        }

                        if (appliedStatus != null)
                        {
                            if (game.CompletionStatusId != previousStatusId)
                            {
                                appliedCount++;
                                Logger.Info($"Status sync from HLTB ({appliedStatus}) for {game.Name}: {previousStatusId} -> {game.CompletionStatusId}");
                            }
                            else
                            {
                                unchangedCount++;
                                Common.LogDebug(true, $"Status sync from HLTB ({appliedStatus}) for {game.Name}: already set");
                            }
                        }

                        API.Instance.Database.Games.Update(game);
                    }
                });

                Logger.Info($"Status sync from HLTB finished: linked={linkedCount}, applied={appliedCount}, unchanged={unchangedCount}");
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }
            finally
            {
                //API.Instance.Database.Games.EndBufferUpdate();
                DontSetToHtlb = false;
            }
        }

        /// <summary>
        /// Syncs the Playnite completion status of one game to HowLongToBeat list flags.
        /// </summary>
        /// <param name="game">Playnite game.</param>
        /// <param name="fromManualMenu">When true, skips the playtime-sync ignore list (status sync is not playtime upload).</param>
        public void SetGameStatusToHltb(Game game, bool fromManualMenu = false)
        {
            if (DontSetToHtlb)
            {
                return;
            }

            string syncSource = fromManualMenu ? "Manual" : "Auto";

            if (!fromManualMenu && IsGameIgnoredForPlaytimeSync(game))
            {
                Logger.Info($"Skipping {syncSource} status sync to HLTB for ignored game {game?.Name}");
                return;
            }

            try
            {
                Action<bool, bool> notifySync = (playtimeSent, dateSent) =>
                {
                    if (!PluginSettings.EnableSucessNotification)
                    {
                        return;
                    }

                    API.Instance.Notifications.Add(new NotificationMessage(
                        $"{PluginName}-AutoSetGameStatusToHltb-{game.Id}-{Guid.NewGuid()}",
                        PluginName + Environment.NewLine + string.Format(
                            ResourceProvider.GetString("LOCHowLongToBeatAutoSetGameStatusToHltbNotification"),
                            game.Name,
                            playtimeSent ? "sent" : "kept on HLTB",
                            dateSent ? "sent" : "kept on HLTB"),
                        NotificationType.Info));
                };

                bool isCompletionist = game.CompletionStatusId == PluginSettings.GameStatusCompletionist;
                bool isCompleted = game.CompletionStatusId == PluginSettings.GameStatusCompleted;
                bool isPlaying = game.CompletionStatusId == PluginSettings.GameStatusPlaying;
                bool isBacklog = game.CompletionStatusId == PluginSettings.GameStatusBacklog;
                bool isReplays = game.CompletionStatusId == PluginSettings.GameStatusReplays;
                bool isRetired = game.CompletionStatusId == PluginSettings.GameStatusRetired;

                if (isCompletionist && PluginSettings.GameStatusCompletionist != default && API.Instance.Database.CompletionStatuses.Get(PluginSettings.GameStatusCompletionist) != null)
                {
                    bool sendPlaytime = PluginSettings.AutoSetToHltbCompletionistSendPlaytime;
                    bool sendCompletionDate = sendPlaytime && PluginSettings.AutoSetToHltbCompletionistSendCompletionDate;
                    HltbStatusToHltbSyncOptions listSyncOptions = PluginSettings.ToHltbCompletionistListSync;
                    bool isUpdated = SetCurrentPlayTime(
                        game,
                        noPlaying: true,
                        isCompleted: true,
                        is100: sendPlaytime,
                        sendCompletedPlaytime: sendPlaytime,
                        sendCompletionDateFromLastActivity: sendCompletionDate,
                        listSyncOptions: listSyncOptions);

                    Logger.Info($"{syncSource} status sync to HLTB (Completionist) for {game?.Name}: success={isUpdated}, sendPlaytime={sendPlaytime}, sendCompletionDate={sendCompletionDate}, {FormatListSyncOptionsLog(listSyncOptions, PluginSettings.ToHltbAlwaysKeepLists)}");

                    if (isUpdated)
                    {
                        notifySync(sendPlaytime, sendCompletionDate);
                    }
                }
                else if (isCompleted && PluginSettings.GameStatusCompleted != default && API.Instance.Database.CompletionStatuses.Get(PluginSettings.GameStatusCompleted) != null)
                {
                    bool sendPlaytime = PluginSettings.AutoSetToHltbCompletedSendPlaytime;
                    bool sendCompletionDate = sendPlaytime && PluginSettings.AutoSetToHltbCompletedSendCompletionDate;
                    HltbStatusToHltbSyncOptions listSyncOptions = PluginSettings.ToHltbCompletedListSync;
                    bool isUpdated = SetCurrentPlayTime(
                        game,
                        noPlaying: true,
                        isCompleted: true,
                        isMain: sendPlaytime,
                        sendCompletedPlaytime: sendPlaytime,
                        sendCompletionDateFromLastActivity: sendCompletionDate,
                        listSyncOptions: listSyncOptions);

                    Logger.Info($"{syncSource} status sync to HLTB (Completed) for {game?.Name}: success={isUpdated}, sendPlaytime={sendPlaytime}, sendCompletionDate={sendCompletionDate}, {FormatListSyncOptionsLog(listSyncOptions, PluginSettings.ToHltbAlwaysKeepLists)}");

                    if (isUpdated)
                    {
                        notifySync(sendPlaytime, sendCompletionDate);
                    }
                }
                else if (isPlaying && PluginSettings.GameStatusPlaying != default && API.Instance.Database.CompletionStatuses.Get(PluginSettings.GameStatusPlaying) != null)
                {
                    HltbStatusToHltbSyncOptions listSyncOptions = PluginSettings.ToHltbPlayingListSync;
                    bool sendPlaytime = PluginSettings.AutoSetToHltbPlayingSendPlaytime;
                    bool isUpdated = SetCurrentPlayTime(
                        game,
                        noPlaying: false,
                        sendProgressPlaytime: sendPlaytime,
                        listSyncOptions: listSyncOptions);
                    Logger.Info($"{syncSource} status sync to HLTB (Playing) for {game?.Name}: success={isUpdated}, sendPlaytime={sendPlaytime}, {FormatListSyncOptionsLog(listSyncOptions, PluginSettings.ToHltbAlwaysKeepLists)}");

                    if (isUpdated)
                    {
                        notifySync(sendPlaytime, false);
                    }
                }
                else if (isBacklog && PluginSettings.GameStatusBacklog != default && API.Instance.Database.CompletionStatuses.Get(PluginSettings.GameStatusBacklog) != null)
                {
                    HltbStatusToHltbSyncOptions listSyncOptions = PluginSettings.ToHltbBacklogListSync;
                    bool isUpdated = SetCurrentPlayTime(game, noPlaying: true, isBacklog: true, listSyncOptions: listSyncOptions);
                    Logger.Info($"{syncSource} status sync to HLTB (Backlog) for {game?.Name}: success={isUpdated}, {FormatListSyncOptionsLog(listSyncOptions, PluginSettings.ToHltbAlwaysKeepLists)}");

                    if (isUpdated)
                    {
                        notifySync(true, false);
                    }
                }
                else if (isReplays && PluginSettings.GameStatusReplays != default && API.Instance.Database.CompletionStatuses.Get(PluginSettings.GameStatusReplays) != null)
                {
                    HltbStatusToHltbSyncOptions listSyncOptions = PluginSettings.ToHltbReplaysListSync;
                    bool isUpdated = SetCurrentPlayTime(game, noPlaying: true, isReplay: true, listSyncOptions: listSyncOptions);
                    Logger.Info($"{syncSource} status sync to HLTB (Replays) for {game?.Name}: success={isUpdated}, {FormatListSyncOptionsLog(listSyncOptions, PluginSettings.ToHltbAlwaysKeepLists)}");

                    if (isUpdated)
                    {
                        notifySync(true, false);
                    }
                }
                else if (isRetired && PluginSettings.GameStatusRetired != default && API.Instance.Database.CompletionStatuses.Get(PluginSettings.GameStatusRetired) != null)
                {
                    HltbStatusToHltbSyncOptions listSyncOptions = PluginSettings.ToHltbRetiredListSync;
                    bool isUpdated = SetCurrentPlayTime(game, noPlaying: true, isRetired: true, listSyncOptions: listSyncOptions);
                    Logger.Info($"{syncSource} status sync to HLTB (Retired) for {game?.Name}: success={isUpdated}, {FormatListSyncOptionsLog(listSyncOptions, PluginSettings.ToHltbAlwaysKeepLists)}");

                    if (isUpdated)
                    {
                        notifySync(true, false);
                    }
                }
                else
                {
                    Logger.Info($"{syncSource} status sync to HLTB skipped for {game?.Name}: CompletionStatusId={game?.CompletionStatusId} is not mapped");
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }
        }

        /// <summary>
        /// Syncs Playnite completion status to HowLongToBeat for the selected games (game menu).
        /// </summary>
        /// <param name="ids">Playnite game identifiers.</param>
        public void SyncGameStatusToHltb(IEnumerable<Guid> ids)
        {
            List<Guid> idsList = new List<Guid>();
            foreach (Guid id in ids ?? Enumerable.Empty<Guid>())
            {
                Game game = API.Instance.Database.Games.Get(id);
                if (game == null)
                {
                    continue;
                }

                idsList.Add(id);
            }

            int total = idsList.Count;
            if (total == 0)
            {
                Logger.Info("Manual status sync to HLTB skipped: no games selected");
                return;
            }

            Logger.Info($"Manual status sync to HLTB starting for {total} game(s)");

            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions($"{PluginName} - {ResourceProvider.GetString("LOCCommonProcessing")}")
            {
                Cancelable = true,
                IsIndeterminate = total == 1
            };

            _ = API.Instance.Dialogs.ActivateGlobalProgress((a) =>
            {
                a.ProgressMaxValue = total;

                foreach (Guid id in idsList)
                {
                    if (a.CancelToken.IsCancellationRequested)
                    {
                        break;
                    }

                    Game game = API.Instance.Database.Games.Get(id);
                    try
                    {
                        Application.Current.Dispatcher?.BeginInvoke(new Action(() =>
                        {
                            a.Text = PluginName + " - " + ResourceProvider.GetString("LOCCommonProcessing")
                                + (total == 1 ? string.Empty : "\n\n" + $"{a.CurrentProgressValue}/{a.ProgressMaxValue}")
                                + "\n" + game?.Name + (game?.Source == null ? string.Empty : $" ({game?.Source.Name})");
                        }));
                    }
                    catch { }

                    try
                    {
                        SetGameStatusToHltb(game, fromManualMenu: true);
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, true, PluginName);
                    }

                    try
                    {
                        Application.Current.Dispatcher?.BeginInvoke(new Action(() => { a.CurrentProgressValue++; }));
                    }
                    catch { }
                }

                Logger.Info($"Manual status sync to HLTB finished: processed={a.CurrentProgressValue}/{total}, cancelled={a.CancelToken.IsCancellationRequested}");
            }, globalProgressOptions);
        }


        public async Task RefreshUserDataAsync()
        {
            if (IsVerboseLoggingEnabled)
            {
                Common.LogDebug(true,"RefreshUserData()");
            }

            if (HowLongToBeatApi == null)
            {
                try { Logger.Warn("HowLongToBeatApi not initialized; cannot refresh user data"); } catch { }
                return;
            }

            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions($"{PluginName} - {ResourceProvider.GetString("LOCHowLongToBeatPluginGetUserView")}")
            {
                Cancelable = false,
                IsIndeterminate = true
            };

            try
            {
                // Fire-and-forget: activate global progress and run the async callback
                _ = API.Instance.Dialogs.ActivateGlobalProgress(async (a) =>
                {
                    var ct = a?.CancelToken ?? CancellationToken.None;

                    try
                    {
                        if (ct.IsCancellationRequested)
                        {
                            return;
                        }

                        HltbUserStats userHltbData = null;
                        try
                        {
                            // Start the async operation and wait for completion or cancellation
                            var userTask = HowLongToBeatApi.GetUserDataAsync();
                            var completed = await Task.WhenAny(userTask, Task.Delay(Timeout.Infinite, ct)).ConfigureAwait(false);
                            if (completed == userTask)
                            {
                                userHltbData = await userTask.ConfigureAwait(false);
                            }
                            else
                            {
                                // Cancelled via progress token
                                return;
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            return;
                        }
                        catch (Exception)
                        {
                            if (ct.IsCancellationRequested)
                            {
                                return;
                            }
                            throw;
                        }

                        if (userHltbData != null)
                        {
                            if (IsVerboseLoggingEnabled)
                            {
                                Common.LogDebug(true,$"Find {userHltbData.TitlesList?.Count ?? 0} games");
                            }
                            FileSystem.WriteStringToFileSafe(Path.Combine(Paths.PluginUserDataPath, "HltbUserStats.json"), Serialization.ToJson(userHltbData));
                            UserHltbData = userHltbData;

                            if (PluginSettings.AutoSetGameStatus)
                            {
                                SetGameStatusFromHltb();
                            }

                            Application.Current.Dispatcher?.BeginInvoke(new Action(() =>
                            {
                                _database?.OnCollectionChanged(null, null);
                            }));
                        }
                        else
                        {
                            if (IsVerboseLoggingEnabled)
                            {
                                Common.LogDebug(true,"Find no data");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        try { Common.LogError(ex, false, true, PluginName); } catch { }
                    }
                }, globalProgressOptions);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            // Small delay to allow UI notifications to settle when caller awaited this Task; keep minimal
            await Task.Delay(200).ConfigureAwait(false);
        }

        public void RefreshUserData()
        {
            var t = RefreshUserDataAsync();
            try
            {
                t.ContinueWith(task =>
                {
                    try
                    {
                        var ex = task.Exception?.GetBaseException() ?? task.Exception;
                        if (ex != null)
                        {
                            Common.LogError(ex, false, true, PluginName);
                        }
                    }
                    catch { }
                    try { var _ = task.Exception; } catch { }
                }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
            }
            catch (Exception ex)
            {
                try { Common.LogError(ex, false, true, PluginName); } catch { }
            }
        }

        public void RefreshUserData(string gameId)
        {
            _ = Task.Run(() =>
            {
                try
                {
                    if (HowLongToBeatApi == null)
                    {
                        Logger.Warn("HowLongToBeatApi not initialized; cannot refresh specific user data");
                        return;
                    }

                    TitleList titleList = HowLongToBeatApi.GetUserData(gameId);
                    if (titleList != null)
                    {
                        string userDataJson = null;
                        lock (UserHltbDataSync)
                        {
                            if (UserHltbData?.TitlesList == null)
                            {
                                return;
                            }

                            int index = UserHltbData.TitlesList.FindIndex(x => x.Id == gameId);
                            if (index > -1)
                            {
                                UserHltbData.TitlesList[index] = titleList;
                            }
                            else
                            {
                                UserHltbData.TitlesList.Add(titleList);
                            }

                            // Serialize while protected from concurrent list mutations.
                            userDataJson = Serialization.ToJson(UserHltbData);
                        }

                        Application.Current.Dispatcher?.Invoke(() =>
                        {
                            _database?.OnCollectionChanged(null, null);
                        });

                        FileSystem.WriteStringToFileSafe(Path.Combine(Paths.PluginUserDataPath, "HltbUserStats.json"), userDataJson ?? string.Empty);
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginName);
                }
            });
        }

        public void SetCurrentPlaytime(IEnumerable<Guid> ids, bool noPlaying = false, bool isCompleted = false, bool isMain = false, bool isMainSide = false, bool is100 = false, bool isSolo = false, bool isCoOp = false, bool isVs = false)
        {
            List<Guid> idsList = new List<Guid>();
            foreach (Guid id in ids ?? Enumerable.Empty<Guid>())
            {
                Game game = API.Instance.Database.Games.Get(id);
                if (game == null)
                {
                    continue;
                }

                if (!ConfirmManualPlaytimeSyncIfIgnored(game))
                {
                    Logger.Info($"Manual playtime sync cancelled for ignored game {game.Name}");
                    continue;
                }

                idsList.Add(id);
            }

            int total = idsList.Count;
            if (total == 0)
            {
                return;
            }

            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions($"{PluginName} - {ResourceProvider.GetString("LOCCommonProcessing")}")
            {
                Cancelable = true,
                IsIndeterminate = total == 1
            };

            // Use a producer/consumer worker pool instead of creating one Task per id for better scalability
            _ = API.Instance.Dialogs.ActivateGlobalProgress(async (a) =>
            {
                API.Instance.Database.BeginBufferUpdate();
                BlockingCollection<Guid> queue = new BlockingCollection<Guid>();
                var workers = new List<Task>();
                try
                {
                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();

                    a.ProgressMaxValue = total;

                    int parallelism = Math.Min(16, Math.Max(1, Environment.ProcessorCount * 2));

                    // Start worker tasks
                    for (int w = 0; w < parallelism; ++w)
                    {
                        workers.Add(Task.Run(() =>
                        {
                            while (true)
                            {
                                if (a.CancelToken.IsCancellationRequested) break;

                                Guid id;
                                try
                                {
                                    id = queue.Take(a.CancelToken);
                                }
                                catch (OperationCanceledException)
                                {
                                    break;
                                }
                                catch (InvalidOperationException)
                                {
                                    // Thrown when collection is completed
                                    break;
                                }

                                try
                                {
                                    if (a.CancelToken.IsCancellationRequested) break;

                                    Game game = API.Instance.Database.Games.Get(id);

                                    try
                                    {
                                        Application.Current.Dispatcher?.BeginInvoke(new Action(() =>
                                        {
                                            a.Text = PluginName + " - " + ResourceProvider.GetString("LOCCommonProcessing")
                                                + (total == 1 ? string.Empty : "\n\n" + $"{a.CurrentProgressValue}/{a.ProgressMaxValue}")
                                                + "\n" + game?.Name + (game?.Source == null ? string.Empty : $" ({game?.Source.Name})");
                                        }));
                                    }
                                    catch { }

                                    try
                                    {
                                        // Call synchronously; SetCurrentPlayTime is synchronous and may perform network work via RunSyncWithTimeout
                                        _ = SetCurrentPlayTime(
                                            game,
                                            noPlaying: noPlaying,
                                            isCompleted: isCompleted,
                                            isMain: isMain,
                                            isMainSide: isMainSide,
                                            is100: is100,
                                            isSolo: isSolo,
                                            isCoOp: isCoOp,
                                            isVs: isVs);
                                    }
                                    catch (Exception ex)
                                    {
                                        Common.LogError(ex, false, true, PluginName);
                                    }

                                    try
                                    {
                                        Application.Current.Dispatcher?.BeginInvoke(new Action(() => { a.CurrentProgressValue++; }));
                                    }
                                    catch { }
                                }
                                catch (Exception ex)
                                {
                                    Common.LogError(ex, false, true, PluginName);
                                }
                            }
                        }, a.CancelToken));
                    }

                    // Enqueue items
                    try
                    {
                        foreach (Guid id in idsList)
                        {
                            if (a.CancelToken.IsCancellationRequested) break;
                            queue.Add(id, a.CancelToken);
                        }
                    }
                    catch (OperationCanceledException) { }
                    finally
                    {
                        queue.CompleteAdding();
                    }

                    try
                    {
                        await Task.WhenAll(workers.ToArray()).ConfigureAwait(false);
                    }
                    catch (AggregateException ex)
                    {
                        Common.LogError(ex, false, true, PluginName);
                    }

                    stopWatch.Stop();
                    TimeSpan ts = stopWatch.Elapsed;
                    if (IsVerboseLoggingEnabled)
                    {
                        Common.LogDebug(true,$"Task SetCurrentPlaytime(){(a.CancelToken.IsCancellationRequested ? " canceled" : string.Empty)} - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {a.CurrentProgressValue}/{total} items");
                    }
                }
                finally
                {
                    try
                    {
                        queue?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        try { Common.LogError(ex, false, true, PluginName); } catch { }
                    }

                    try
                    {
                        API.Instance.Database.EndBufferUpdate();
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, true, PluginName);
                    }
                }
            }, globalProgressOptions);
        }

        /// <summary>
        /// Submits playtime and list status for a game to HowLongToBeat.
        /// </summary>
        /// <param name="isBacklog">When true, sets <c>Lists.Backlog</c> on the submit payload.</param>
        /// <param name="isReplay">When true, sets <c>Lists.Replay</c> on the submit payload.</param>
        /// <param name="isRetired">When true, sets <c>Lists.Retired</c> on the submit payload.</param>
        /// <param name="sendProgressPlaytime">When true, updates <c>General.Progress</c> from Playnite playtime.</param>
        /// <param name="listSyncOptions">When set, controls clearing of other HowLongToBeat lists before applying the target status.</param>
        public bool SetCurrentPlayTime(
            Game game,
            bool noPlaying = false,
            bool isCompleted = false,
            bool isMain = false,
            bool isMainSide = false,
            bool is100 = false,
            bool isSolo = false,
            bool isCoOp = false,
            bool isVs = false,
            bool sendCompletedPlaytime = true,
            bool sendCompletionDateFromLastActivity = true,
            bool isBacklog = false,
            bool isReplay = false,
            bool isRetired = false,
            bool sendProgressPlaytime = true,
            HltbStatusToHltbSyncOptions listSyncOptions = null)
        {
            try
            {
                if (game == null)
                {
                    Common.LogDebug(true, "SetCurrentPlayTime called with null game");
                    return false;
                }

                if (HowLongToBeatApi == null)
                {
                    Common.LogError(new NullReferenceException("HowLongToBeatApi is null"), false, true, PluginName);
                    return false;
                }

                if (HowLongToBeatApi.GetIsUserLoggedIn())
                {
                    var db = _database;
                    if (db == null)
                    {
                        Common.LogDebug(true, "Database is not loaded, cannot set current playtime.");
                        return false;
                    }

                    GameHowLongToBeat gameHowLongToBeat = db.Get(game.Id);
                    if (gameHowLongToBeat != null && (!gameHowLongToBeat.GetData()?.IsVndb ?? false))
                    {
                        TimeSpan time = TimeSpan.FromSeconds(game.Playtime);
                        HltbDataUser hltbDataUser = gameHowLongToBeat.GetData();
                        string platformName = HltbPlatform.PC.GetDescription();
                        string storefrontName = string.Empty;

                        #region Validate Id

                        if (string.IsNullOrWhiteSpace(hltbDataUser.Id))
                        {
                            Logger.Warn($"Cannot submit data for a game without HLTB ID ({game.Name})");
                            API.Instance.Notifications.Add(new NotificationMessage(
                                $"{PluginName}-NoHltbId-Error-{Guid.NewGuid()}",
                                PluginName + Environment.NewLine + string.Format(ResourceProvider.GetString("LOCHowLongToBeatErrorNoHltbId"), game.Name),
                                NotificationType.Error
                            ));
                            return false;
                        }

                        #endregion

                        #region Search platform

                        Platform platform = game.Platforms?.FirstOrDefault();
                        if (platform == default)
                        {
                            Logger.Warn($"Cannot submit data for a game without platform ({game.Name})");
                            API.Instance.Notifications.Add(new NotificationMessage(
                               $"{PluginName}-NoPlatform-Error-{Guid.NewGuid()}",
                               PluginName + Environment.NewLine + string.Format(ResourceProvider.GetString("LOCHowLongToBeatErrorNoPlatform"), game.Name),
                               NotificationType.Error,
                               () => Plugin.OpenSettingsView()
                            ));
                            return false;
                        }

                        HltbPlatform? match = PluginSettings.Platforms.FirstOrDefault(p => p.Platform.Equals(platform))?.HltbPlatform;
                        if (match != null)
                        {
                            platformName = match.GetDescription();
                        }
                        else
                        {
                            Logger.Warn($"No platform find for {game.Name} - Default \"PC\" used");
                            API.Instance.Notifications.Add(new NotificationMessage(
                               $"{PluginName}-NoPlatformDefined-Error-{Guid.NewGuid()}",
                               PluginName + Environment.NewLine + string.Format(ResourceProvider.GetString("LOCHowLongToBeatErrorNoPlatformDefaultUsed"), platform.Name, game.Name),
                               NotificationType.Error,
                               () => Plugin.OpenSettingsView()
                            ));
                        }

                        #endregion

                        #region Search storefront

                        string resolvedStorefrontName = PluginSettings.GetHltbStorefrontNameForSource(game.SourceId);
                        if (!resolvedStorefrontName.IsNullOrEmpty())
                        {
                            storefrontName = resolvedStorefrontName;
                        }
                        else
                        {
                            Logger.Warn($"No storefront find for {game.Name}");
                        }

                        #endregion

                        #region Get current data from HowLongToBeat

                        TitleList HltbData = GetUserHltbDataCurrent(hltbDataUser.Id, gameHowLongToBeat.UserGameId);
                        EditData editData = new EditData();
                        string submissionId = "0";

                        if (HltbData != null && HowLongToBeatApi.EditIdExist(HltbData.UserGameId))
                        {
                            submissionId = HltbData.UserGameId;
                            editData = TaskHelpers.RunSyncWithTimeout(() => HowLongToBeatApi.GetEditData(gameHowLongToBeat.Name, submissionId), 15000);
                        }
                        else
                        {
                            // Find existing in website
                            if (hltbDataUser != null)
                            {
                                string tmpEditId = HowLongToBeatApi.FindIdExisting(hltbDataUser.Id.ToString());
                                if (!tmpEditId.IsNullOrEmpty())
                                {
                                    submissionId = tmpEditId;
                                    editData = TaskHelpers.RunSyncWithTimeout(() => HowLongToBeatApi.GetEditData(gameHowLongToBeat.Name, submissionId), 15000);
                                }
                                else
                                {
                                    if (IsVerboseLoggingEnabled)
                                    {
                                        Common.LogDebug(true,$"No existing data in website find for {game.Name}");
                                    }
                                }
                            }
                        }

                        if (editData == null)
                        {
                            Logger.Warn($"No editData for {game.Name}");
                            return false;
                        }

                        #endregion

                        #region Data

                        if (UserHltbData == null)
                        {
                            Common.LogDebug(true, $"User HLTB data is null, cannot submit data for {game.Name}");
                            return false;
                        }

                        editData.UserId = UserHltbData.UserId;
                        editData.SubmissionId = int.Parse(submissionId);
                        editData.GameId = int.Parse(hltbDataUser.Id);
                        editData.Title = editData.Title.IsNullOrEmpty() ? hltbDataUser.Name : editData.Title;
                        editData.Platform = platformName;
                        editData.Storefront = editData.Storefront.IsNullOrEmpty() ? storefrontName : editData.Storefront;

                        if (PluginSettings.UsedStartDateFromGameActivity)
                        {
                            string pathGameActivityData = Path.Combine(Paths.PluginUserDataPath, "..", PlayniteTools.GetPluginId(PlayniteTools.ExternalPlugin.GameActivity).ToString(), "GameActivity", game.Id.ToString() + ".json");
                            if (File.Exists(pathGameActivityData))
                            {
                                if (Serialization.TryFromJsonFile(pathGameActivityData, out dynamic gameActivity, out Exception ex))
                                {
                                    if (Serialization.TryFromJson(Serialization.ToJson(gameActivity["Items"]), out List<Activity> activities, out ex))
                                    {
                                        if (activities?.Count > 0)
                                        {
                                            DateTime dt = (DateTime)(activities?.Where(x => x.DateSession != null).OrderBy(x => (DateTime)x.DateSession)?.FirstOrDefault().DateSession);
                                            editData.General.StartDate = new Date { Year = dt.ToString("yyyy"), Month = dt.ToString("MM"), Day = dt.ToString("dd") };
                                        }
                                    }
                                }
                                if (ex != null)
                                {
                                    Common.LogError(ex, false, false, PluginName);
                                }
                            }
                            else
                            {
                                Logger.Warn($"No GameActivity for {game.Name} in {pathGameActivityData}");
                            }
                        }

                        if (listSyncOptions != null)
                        {
                            ApplyHltbListClearing(editData.Lists, listSyncOptions.ClearOtherLists, PluginSettings.ToHltbAlwaysKeepLists, game.Name);
                        }

                        editData.Lists.Playing = false;
                        if (!noPlaying)
                        {
                            editData.Lists.Playing = true;
                        }

                        if (isCompleted)
                        {
                            editData.Lists.Completed = true;

                            if (isMain && sendCompletedPlaytime)
                            {
                                editData.SinglePlayer.CompMain.Time.Hours = time.Hours + (24 * time.Days);
                                editData.SinglePlayer.CompMain.Time.Minutes = time.Minutes;
                                editData.SinglePlayer.CompMain.Time.Seconds = time.Seconds;

                                if (sendCompletionDateFromLastActivity && game.LastActivity != null)
                                {
                                    editData.General.CompletionDate.Day = ((DateTime)game.LastActivity).Day.ToString();
                                    editData.General.CompletionDate.Month = ((DateTime)game.LastActivity).Month.ToString();
                                    editData.General.CompletionDate.Year = ((DateTime)game.LastActivity).Year.ToString();
                                }
                            }

                            if (isMainSide && sendCompletedPlaytime)
                            {
                                editData.SinglePlayer.CompPlus.Time.Hours = time.Hours + (24 * time.Days);
                                editData.SinglePlayer.CompPlus.Time.Minutes = time.Minutes;
                                editData.SinglePlayer.CompPlus.Time.Seconds = time.Seconds;

                                if (sendCompletionDateFromLastActivity && game.LastActivity != null && (editData.General.CompletionDate.Day.IsNullOrEmpty() || editData.General.CompletionDate.Day == "00"))
                                {
                                    editData.General.CompletionDate.Day = ((DateTime)game.LastActivity).Day.ToString();
                                    editData.General.CompletionDate.Month = ((DateTime)game.LastActivity).Month.ToString();
                                    editData.General.CompletionDate.Year = ((DateTime)game.LastActivity).Year.ToString();
                                }
                            }

                            if (is100 && sendCompletedPlaytime)
                            {
                                editData.SinglePlayer.Comp100.Time.Hours = time.Hours + (24 * time.Days);
                                editData.SinglePlayer.Comp100.Time.Minutes = time.Minutes;
                                editData.SinglePlayer.Comp100.Time.Seconds = time.Seconds;

                                if (sendCompletionDateFromLastActivity && game.LastActivity != null && (editData.General.CompletionDate.Day.IsNullOrEmpty() || editData.General.CompletionDate.Day == "00"))
                                {
                                    editData.General.CompletionDate.Day = ((DateTime)game.LastActivity).Day.ToString();
                                    editData.General.CompletionDate.Month = ((DateTime)game.LastActivity).Month.ToString();
                                    editData.General.CompletionDate.Year = ((DateTime)game.LastActivity).Year.ToString();
                                }
                            }

                            if (isCompleted && sendCompletionDateFromLastActivity && game.LastActivity == null && IsVerboseLoggingEnabled)
                            {
                                Common.LogDebug(true,$"No LastActivity found for {game.Name}, completion date is not sent to HLTB.");
                            }
                        }

                        if (isBacklog)
                        {
                            editData.Lists.Backlog = true;
                        }

                        if (isReplay)
                        {
                            editData.Lists.Replay = true;
                        }

                        if (isRetired)
                        {
                            editData.Lists.Retired = true;
                        }

                        // Apply the default only after all explicit status flags are set.
                        // This prevents sending both Playing and Completed when auto-syncing a completed game.
                        if (!editData.Lists.Backlog && !editData.Lists.Completed && !editData.Lists.Custom && !editData.Lists.Playing && !editData.Lists.Replay && !editData.Lists.Retired)
                        {
                            editData.Lists.Playing = true;
                        }

                        if (isCoOp)
                        {
                            editData.MultiPlayer.CoOp.Time.Hours = time.Hours + (24 * time.Days);
                            editData.MultiPlayer.CoOp.Time.Minutes = time.Minutes;
                            editData.MultiPlayer.CoOp.Time.Seconds = time.Seconds;
                        }

                        if (isVs)
                        {
                            editData.MultiPlayer.Vs.Time.Hours = time.Hours + (24 * time.Days);
                            editData.MultiPlayer.Vs.Time.Minutes = time.Minutes;
                            editData.MultiPlayer.Vs.Time.Seconds = time.Seconds;
                        }

                        if (sendProgressPlaytime)
                        {
                            editData.General.Progress.Hours = time.Hours + (24 * time.Days);
                            editData.General.Progress.Minutes = time.Minutes;
                            editData.General.Progress.Seconds = time.Seconds;
                        }

                        if (listSyncOptions != null && IsVerboseLoggingEnabled)
                        {
                            Common.LogDebug(true, $"Status sync lists for {game.Name}: listsSubmitted={FormatHltbListsSubmitted(editData.Lists)}");
                        }

                        #endregion

                        return TaskHelpers.RunSyncWithTimeout(() => HowLongToBeatApi.ApiSubmitData(game, editData), 15000);
                    }
                }
                else
                {
                    API.Instance.Notifications.Add(new NotificationMessage(
                        $"{PluginName}-NotLoggedIn-Error",
                        PluginName + Environment.NewLine + ResourceProvider.GetString("LOCCommonNotLoggedIn"),
                        NotificationType.Error,
                        () => Plugin.OpenSettingsView()
                    ));
                    return false;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return false;
        }

        /// <summary>
        /// Clears HowLongToBeat profile list flags on the submit payload, optionally preserving globally configured lists that are already set.
        /// </summary>
        /// <param name="lists">List flags from edit data.</param>
        /// <param name="clearOtherLists">When true, clears lists before applying the target status.</param>
        /// <param name="alwaysKeep">Global always-keep rules applied after clearing.</param>
        /// <param name="gameName">Playnite game name used for verbose logging.</param>
        private void ApplyHltbListClearing(Lists lists, bool clearOtherLists, HltbListAlwaysKeepOptions alwaysKeep, string gameName)
        {
            if (lists == null)
            {
                return;
            }

            if (!clearOtherLists)
            {
                if (IsVerboseLoggingEnabled)
                {
                    Common.LogDebug(true, $"Status sync lists for {gameName}: listsBefore={FormatActiveHltbLists(lists)}, clearOtherLists=false, listsAfterClear=skipped");
                }

                return;
            }

            if (alwaysKeep == null)
            {
                alwaysKeep = new HltbListAlwaysKeepOptions();
            }

            string listsBefore = FormatActiveHltbLists(lists);
            bool keepPlaying = alwaysKeep.AlwaysKeepPlayingIfPresent && lists.Playing;
            bool keepBacklog = alwaysKeep.AlwaysKeepBacklogIfPresent && lists.Backlog;
            bool keepReplay = alwaysKeep.AlwaysKeepReplayIfPresent && lists.Replay;
            bool keepCompleted = alwaysKeep.AlwaysKeepCompletedIfPresent && lists.Completed;
            bool keepRetired = alwaysKeep.AlwaysKeepRetiredIfPresent && lists.Retired;

            ClearHltbLists(lists);

            var kept = new List<string>();
            if (keepPlaying)
            {
                lists.Playing = true;
                kept.Add("Playing");
            }

            if (keepBacklog)
            {
                lists.Backlog = true;
                kept.Add("Backlog");
            }

            if (keepReplay)
            {
                lists.Replay = true;
                kept.Add("Replay");
            }

            if (keepCompleted)
            {
                lists.Completed = true;
                kept.Add("Completed");
            }

            if (keepRetired)
            {
                lists.Retired = true;
                kept.Add("Retired");
            }

            if (IsVerboseLoggingEnabled)
            {
                string keptSummary = kept.Count == 0 ? "none" : string.Join(",", kept);
                Common.LogDebug(true, $"Status sync lists for {gameName}: listsBefore={listsBefore}, clearOtherLists=true, listsAfterClear={FormatActiveHltbLists(lists)}, kept={keptSummary}");
            }
        }

        /// <summary>
        /// Clears all HowLongToBeat profile list flags on the submit payload.
        /// </summary>
        /// <param name="lists">List flags from edit data.</param>
        private static void ClearHltbLists(Lists lists)
        {
            if (lists == null)
            {
                return;
            }

            lists.Playing = false;
            lists.Backlog = false;
            lists.Replay = false;
            lists.Custom = false;
            lists.Custom2 = false;
            lists.Custom3 = false;
            lists.Completed = false;
            lists.Retired = false;
        }

        private static string FormatListSyncOptionsLog(HltbStatusToHltbSyncOptions options, HltbListAlwaysKeepOptions alwaysKeep)
        {
            if (options == null)
            {
                return "listSync=none";
            }

            if (!options.ClearOtherLists)
            {
                return "clearOtherLists=false";
            }

            if (alwaysKeep == null)
            {
                return "clearOtherLists=true, alwaysKeep=none";
            }

            var alwaysKeepLists = new List<string>();
            if (alwaysKeep.AlwaysKeepPlayingIfPresent)
            {
                alwaysKeepLists.Add("Playing");
            }

            if (alwaysKeep.AlwaysKeepBacklogIfPresent)
            {
                alwaysKeepLists.Add("Backlog");
            }

            if (alwaysKeep.AlwaysKeepReplayIfPresent)
            {
                alwaysKeepLists.Add("Replay");
            }

            if (alwaysKeep.AlwaysKeepCompletedIfPresent)
            {
                alwaysKeepLists.Add("Completed");
            }

            if (alwaysKeep.AlwaysKeepRetiredIfPresent)
            {
                alwaysKeepLists.Add("Retired");
            }

            string alwaysKeepSummary = alwaysKeepLists.Count == 0 ? "none" : string.Join(",", alwaysKeepLists);
            return $"clearOtherLists=true, alwaysKeep={alwaysKeepSummary}";
        }

        /// <summary>
        /// Formats active HowLongToBeat list names for logging.
        /// </summary>
        private static string FormatActiveHltbLists(Lists lists)
        {
            if (lists == null)
            {
                return "none";
            }

            var active = new List<string>();
            if (lists.Playing)
            {
                active.Add("Playing");
            }

            if (lists.Backlog)
            {
                active.Add("Backlog");
            }

            if (lists.Replay)
            {
                active.Add("Replay");
            }

            if (lists.Completed)
            {
                active.Add("Completed");
            }

            if (lists.Retired)
            {
                active.Add("Retired");
            }

            if (lists.Custom)
            {
                active.Add("Custom");
            }

            if (lists.Custom2)
            {
                active.Add("Custom2");
            }

            if (lists.Custom3)
            {
                active.Add("Custom3");
            }

            return active.Count == 0 ? "none" : string.Join(",", active);
        }

        /// <summary>
        /// Formats all HowLongToBeat list flags for submit payload logging.
        /// </summary>
        private static string FormatHltbListsSubmitted(Lists lists)
        {
            if (lists == null)
            {
                return "none";
            }

            return string.Format(
                "Playing={0},Backlog={1},Replay={2},Completed={3},Retired={4},Custom={5},Custom2={6},Custom3={7}",
                lists.Playing,
                lists.Backlog,
                lists.Replay,
                lists.Completed,
                lists.Retired,
                lists.Custom,
                lists.Custom2,
                lists.Custom3);
        }

        #endregion


        public override void SetThemesResources(Game game)
        {
            GameHowLongToBeat gameHowLongToBeat = Get(game, true);

            if (gameHowLongToBeat == null || !gameHowLongToBeat.HasData)
            {
                PluginSettings.HasData = false;
                PluginSettings.HasDataEmpty = true;
                PluginSettings.MainStory = 0;
                PluginSettings.MainStoryFormat = "--";
                PluginSettings.MainExtra = 0;
                PluginSettings.MainExtraFormat = "--";
                PluginSettings.Completionist = 0;
                PluginSettings.CompletionistFormat = "--";
                PluginSettings.Solo = 0;
                PluginSettings.SoloFormat = "--";
                PluginSettings.CoOp = 0;
                PluginSettings.CoOpFormat = "--";
                PluginSettings.Vs = 0;
                PluginSettings.VsFormat = "--";

                PluginSettings.TimeToBeat = 0;
                PluginSettings.TimeToBeatFormat = "--";

                return;
            }

            PluginSettings.HasData = gameHowLongToBeat.HasData;
            PluginSettings.HasDataEmpty = gameHowLongToBeat.HasDataEmpty;
            PluginSettings.MainStory = gameHowLongToBeat.GetData().GameHltbData.MainStory;
            PluginSettings.MainStoryFormat = gameHowLongToBeat.GetData().GameHltbData.MainStoryFormat;
            PluginSettings.MainExtra = gameHowLongToBeat.GetData().GameHltbData.MainExtra;
            PluginSettings.MainExtraFormat = gameHowLongToBeat.GetData().GameHltbData.MainExtraFormat;
            PluginSettings.Completionist = gameHowLongToBeat.GetData().GameHltbData.Completionist;
            PluginSettings.CompletionistFormat = gameHowLongToBeat.GetData().GameHltbData.CompletionistFormat;
            PluginSettings.Solo = gameHowLongToBeat.GetData().GameHltbData.Solo;
            PluginSettings.SoloFormat = gameHowLongToBeat.GetData().GameHltbData.SoloFormat;
            PluginSettings.CoOp = gameHowLongToBeat.GetData().GameHltbData.CoOp;
            PluginSettings.CoOpFormat = gameHowLongToBeat.GetData().GameHltbData.CoOpFormat;
            PluginSettings.Vs = gameHowLongToBeat.GetData().GameHltbData.Vs;
            PluginSettings.VsFormat = gameHowLongToBeat.GetData().GameHltbData.VsFormat;

            PluginSettings.TimeToBeat = gameHowLongToBeat.GetData().GameHltbData.TimeToBeat;
            PluginSettings.TimeToBeatFormat = gameHowLongToBeat.GetData().GameHltbData.TimeToBeatFormat;
        }

        public override void ActionAfterGames_ItemUpdated(Game gameOld, Game gameNew)
        {
            _ = Task.Run(() =>
            {
                if (PluginSettings.AutoSetGameStatusToHltb && gameOld.CompletionStatusId != gameNew.CompletionStatusId)
                {
                    SetGameStatusToHltb(gameNew);
                }
            });
        }
    }
}