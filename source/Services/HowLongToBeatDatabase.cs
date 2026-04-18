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

        private static bool DontSetToHtlb { get; set; } = false;


        public HowLongToBeatDatabase(HowLongToBeatSettings pluginSettings, string pluginUserDataPath) : base(pluginSettings, "HowLongToBeat", pluginUserDataPath)
        {
            TagBefore = "[HLTB]";
            PluginWindows = new HowLongToBeatWindows(PluginName, this);
            PluginExportCsv = new HowLongToBeatExport();
        }

        // Change visibility to allow other classes to use the centralized verbose check
        public bool IsVerboseLoggingEnabled => PluginSettings is HowLongToBeatSettings settings && settings.EnableVerboseLogging;

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

                    // Run optional/expensive warm-up work in background so initialization doesn't block.
                    if (HowLongToBeatApi != null)
                    {
                        FireAndForget(Task.Run(() =>
                        {
                            try
                            {
                                // Perform non-critical background warm-up (e.g. refresh cookies or cache).
                                HowLongToBeatApi.UpdatedCookies();
                            }
                            catch (Exception ex)
                            {
                                Common.LogError(ex, false, true, PluginName);
                            }
                        }), "UpdatedCookies warmup");

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
                        Logger.Debug("HowLongToBeatApi not initialized yet during LoadMoreData(); using empty UserHltbData placeholder");
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
                Logger.Debug($"RefreshNoLoader({game?.Name} - {game?.Id})");
            }

            GameHowLongToBeat loadedItem = Get(id, true);
            if (loadedItem.GetData().Id.IsNullOrEmpty())
            {
                if (IsVerboseLoggingEnabled)
                {
                    Logger.Debug($"No data, try to add");
                }
                AddData(game);
                loadedItem = Get(id, true);
                if (loadedItem.GetData().Id.IsNullOrEmpty())
                {
                    if (IsVerboseLoggingEnabled)
                    {
                        Logger.Debug($"No find");
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


        private void SetGameStatusFromHltb()
        {
            try
            {
                DontSetToHtlb = true;
                //API.Instance.Database.Games.BeginBufferUpdate();
                if (UserHltbData?.TitlesList == null)
                {
                    return;
                }

                UserHltbData.TitlesList.ForEach(x =>
                {
                    if (x.GameExist)
                    {
                        bool isCompletionist = x.HltbUserData?.Completionist != 0;
                        bool isCompleted = x.GameStatuses?.Where(y => y.Status == StatusType.Completed)?.Count() != 0;
                        bool isPlaying = x.GameStatuses?.Where(y => y.Status == StatusType.Playing)?.Count() != 0;

                        Game game = API.Instance.Database.Games.Get(x.GameId);

                        if (isCompletionist && PluginSettings.GameStatusCompletionist != default && API.Instance.Database.CompletionStatuses.Get(PluginSettings.GameStatusCompletionist) != null)
                        {
                            game.CompletionStatusId = PluginSettings.GameStatusCompletionist;
                        }
                        else if (isCompleted && PluginSettings.GameStatusCompleted != default && API.Instance.Database.CompletionStatuses.Get(PluginSettings.GameStatusCompleted) != null)
                        {
                            game.CompletionStatusId = PluginSettings.GameStatusCompleted;
                        }
                        else if (isPlaying && PluginSettings.GameStatusPlaying != default && API.Instance.Database.CompletionStatuses.Get(PluginSettings.GameStatusPlaying) != null)
                        {
                            game.CompletionStatusId = PluginSettings.GameStatusPlaying;
                        }

                        API.Instance.Database.Games.Update(game);
                    }
                });
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

        private void SetGameStatusToHltb(Game game)
        {
            if (DontSetToHtlb)
            {
                return;
            }

            try
            {
                bool isCompletionist = game.CompletionStatusId == PluginSettings.GameStatusCompletionist;
                bool isCompleted = game.CompletionStatusId == PluginSettings.GameStatusCompleted;
                bool isPlaying = game.CompletionStatusId == PluginSettings.GameStatusPlaying;

                if (isCompletionist && PluginSettings.GameStatusCompletionist != default && API.Instance.Database.CompletionStatuses.Get(PluginSettings.GameStatusCompletionist) != null)
                {
                    _ = SetCurrentPlayTime(game, true, false, false, false, true);
                }
                else if (isCompleted && PluginSettings.GameStatusCompleted != default && API.Instance.Database.CompletionStatuses.Get(PluginSettings.GameStatusCompleted) != null)
                {
                    _ = SetCurrentPlayTime(game, true, true);
                }
                else if (isPlaying && PluginSettings.GameStatusPlaying != default && API.Instance.Database.CompletionStatuses.Get(PluginSettings.GameStatusPlaying) != null)
                {
                    _ = SetCurrentPlayTime(game, false);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }
        }


        public async Task RefreshUserDataAsync()
        {
            if (IsVerboseLoggingEnabled)
            {
                Logger.Debug("RefreshUserData()");
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
                                Logger.Debug($"Find {userHltbData.TitlesList?.Count ?? 0} games");
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
                                Logger.Debug("Find no data");
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

                        Application.Current.Dispatcher?.Invoke(() =>
                        {
                            _database?.OnCollectionChanged(null, null);
                        });

                        FileSystem.WriteStringToFileSafe(Path.Combine(Paths.PluginUserDataPath, "HltbUserStats.json"), Serialization.ToJson(UserHltbData));
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
            var idsList = ids as IList<Guid> ?? ids.ToList();
            int total = idsList.Count;

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
                                        _ = SetCurrentPlayTime(game, noPlaying, isCompleted, isMain, isMainSide, is100, isSolo, isCoOp, isVs);
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
                        Logger.Debug($"Task SetCurrentPlaytime(){(a.CancelToken.IsCancellationRequested ? " canceled" : string.Empty)} - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {a.CurrentProgressValue}/{total} items");
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

        public bool SetCurrentPlayTime(Game game, bool noPlaying = false, bool isCompleted = false, bool isMain = false, bool isMainSide = false, bool is100 = false, bool isSolo = false, bool isCoOp = false, bool isVs = false)
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

						if(string.IsNullOrWhiteSpace(hltbDataUser.Id))
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

                        Storefront storefront = PluginSettings.StorefrontElements.FirstOrDefault(x => x.SourceId != default && x.SourceId == game.SourceId);
                        if (storefront != null)
                        {
                            storefrontName = storefront.HltbStorefrontName;
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
                                        Logger.Debug($"No existing data in website find for {game.Name}");
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

                        editData.Lists.Playing = false;
                        if (!noPlaying)
                        {
                            editData.Lists.Playing = true;
                        }

                        if (isCompleted)
                        {
                            editData.Lists.Completed = true;

                            if (isMain)
                            {
                                editData.SinglePlayer.CompMain.Time.Hours = time.Hours + (24 * time.Days);
                                editData.SinglePlayer.CompMain.Time.Minutes = time.Minutes;
                                editData.SinglePlayer.CompMain.Time.Seconds = time.Seconds;

                                editData.General.CompletionDate.Day = ((DateTime)game.LastActivity).Day.ToString();
                                editData.General.CompletionDate.Month = ((DateTime)game.LastActivity).Month.ToString();
                                editData.General.CompletionDate.Year = ((DateTime)game.LastActivity).Year.ToString();
                            }

                            if (isMainSide)
                            {
                                editData.SinglePlayer.CompPlus.Time.Hours = time.Hours + (24 * time.Days);
                                editData.SinglePlayer.CompPlus.Time.Minutes = time.Minutes;
                                editData.SinglePlayer.CompPlus.Time.Seconds = time.Seconds;

                                if (editData.General.CompletionDate.Day.IsNullOrEmpty() || editData.General.CompletionDate.Day == "00")
                                {
                                    editData.General.CompletionDate.Day = ((DateTime)game.LastActivity).Day.ToString();
                                    editData.General.CompletionDate.Month = ((DateTime)game.LastActivity).Month.ToString();
                                    editData.General.CompletionDate.Year = ((DateTime)game.LastActivity).Year.ToString();
                                }
                            }

                            if (is100)
                            {
                                editData.SinglePlayer.Comp100.Time.Hours = time.Hours + (24 * time.Days);
                                editData.SinglePlayer.Comp100.Time.Minutes = time.Minutes;
                                editData.SinglePlayer.Comp100.Time.Seconds = time.Seconds;

                                if (editData.General.CompletionDate.Day.IsNullOrEmpty() || editData.General.CompletionDate.Day == "00")
                                {
                                    editData.General.CompletionDate.Day = ((DateTime)game.LastActivity).Day.ToString();
                                    editData.General.CompletionDate.Month = ((DateTime)game.LastActivity).Month.ToString();
                                    editData.General.CompletionDate.Year = ((DateTime)game.LastActivity).Year.ToString();
                                }
                            }
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

                        editData.General.Progress.Hours = time.Hours + (24 * time.Days);
                        editData.General.Progress.Minutes = time.Minutes;
                        editData.General.Progress.Seconds = time.Seconds;

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