using HowLongToBeat.Models;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using CommonPluginsShared.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommonPluginsShared;
using CommonPlayniteShared.Common;
using CommonPluginsShared.Extensions;
using HowLongToBeat.Models.Api;
using HowLongToBeat.Models.GameActivity;
using HowLongToBeat.Models.Enumerations;

namespace HowLongToBeat.Services
{
    public class HowLongToBeatDatabase : PluginDatabaseObject<HowLongToBeatSettingsViewModel, GameHowLongToBeatCollection, GameHowLongToBeat, HltbDataUser>
    {
        public HowLongToBeat Plugin { get; set; }
        public HowLongToBeatApi HowLongToBeatApi { get; set; }

        private static bool DontSetToHtlb { get; set; } = false;


        public HowLongToBeatDatabase(HowLongToBeatSettingsViewModel pluginSettings, string pluginUserDataPath) : base(pluginSettings, "HowLongToBeat", pluginUserDataPath)
        {
            TagBefore = "[HLTB]";
        }


        public void InitializeClient(HowLongToBeat plugin)
        {
            Plugin = plugin;
            HowLongToBeatApi = new HowLongToBeatApi();
        }

        protected override void LoadMoreData()
        {
            Database.UserHltbData = HowLongToBeatApi.LoadUserData();
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


        public void AddData(Game game)
        {
            GameHowLongToBeat gameHowLongToBeat = Get(game, true);

            if (gameHowLongToBeat.Items.Count > 0)
            {
                Logger.Warn($"Data is already added for {game.Name}");
                return;
            }

            List<HltbSearch> data = HowLongToBeatApi.SearchTwoMethod(game.Name).GetAwaiter().GetResult();
            if (data.Count == 1 && PluginSettings.Settings.AutoAccept)
            {
                gameHowLongToBeat.Items = new List<HltbDataUser>() { data.First().Data };
                AddOrUpdate(gameHowLongToBeat);
            }
            else
            {
                if (data.Count > 0 && PluginSettings.Settings.UseMatchValue)
                {
                    if (data.First().MatchPercent >= PluginSettings.Settings.MatchValue)
                    {
                        gameHowLongToBeat.Items = new List<HltbDataUser>() { data.First().Data };
                        AddOrUpdate(gameHowLongToBeat);
                        return;
                    }
                }

                if (data.Count > 0 && PluginSettings.Settings.ShowWhenMismatch)
                {
                    gameHowLongToBeat = HowLongToBeatApi.SearchData(game, data?.Select(x => x.Data).ToList());
                    AddOrUpdate(gameHowLongToBeat);
                }
            }
        }

        public override void RefreshNoLoader(Guid id)
        {
            Game game = API.Instance.Database.Games.Get(id);
            Logger.Info($"RefreshNoLoader({game?.Name} - {game?.Id})");

            GameHowLongToBeat loadedItem = Get(id, true);
            if (loadedItem.GetData().Id.IsNullOrEmpty())
            {
                Logger.Info($"No data, try to add");
                AddData(game);
                loadedItem = Get(id, true);
                if (loadedItem.GetData().Id.IsNullOrEmpty())
                {
                    Logger.Info($"No find");
                }
            }
            else
            {
                if (loadedItem.GetData().IsVndb)
                {
                    List<HltbDataUser> dataSearch = VndbApi.SearchById(loadedItem.GetData().Id);
                    HltbDataUser webDataSearch = dataSearch.Find(x => x.Id == loadedItem.GetData().Id);
                    if (webDataSearch != null)
                    {
                        loadedItem.Items = new List<HltbDataUser> { webDataSearch };
                        loadedItem.DateLastRefresh = DateTime.Now;
                        Update(loadedItem);
                    }
                }
                else
                {
                    loadedItem.Items = new List<HltbDataUser> { HowLongToBeatApi.UpdateGameData(loadedItem.Items.First()).GetAwaiter().GetResult() };
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


        #region Tag

        public override void AddTag(Game game)
        {
            GameHowLongToBeat item = Get(game, true);
            if (item.HasData)
            {
                try
                {
                    HltbDataUser hltbDataUser = item.GetData();
                    Guid? tagId = FindGoodPluginTags(hltbDataUser);
                    if (tagId != null)
                    {
                        if (game.TagIds != null)
                        {
                            game.TagIds.Add((Guid)tagId);
                        }
                        else
                        {
                            game.TagIds = new List<Guid> { (Guid)tagId };
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Tag insert error with {game.Name}", true, PluginName, string.Format(ResourceProvider.GetString("LOCCommonNotificationTagError"), game.Name));
                    return;
                }
            }
            else if (TagMissing)
            {
                if (game.TagIds != null)
                {
                    game.TagIds.Add((Guid)AddNoDataTag());
                }
                else
                {
                    game.TagIds = new List<Guid> { (Guid)AddNoDataTag() };
                }
            }

            API.Instance.MainView.UIDispatcher?.Invoke(() =>
            {
                API.Instance.Database.Games.Update(game);
                game.OnPropertyChanged();
            });
        }

        private Guid? FindGoodPluginTags(HltbDataUser hltbDataUser)
        {
            long hltbTime = hltbDataUser.GameHltbData.TimeToBeat;

            // Add tag
            if (hltbTime != 0)
            {
                if (hltbTime < 3600)
                {
                    return CheckTagExist($"{ResourceProvider.GetString("LOCCommon0to1")}");
                }
                if (hltbTime < 18000)
                {
                    return CheckTagExist($"{ResourceProvider.GetString("LOCCommon1to5")}");
                }
                if (hltbTime < 36000)
                {
                    return CheckTagExist($"{ResourceProvider.GetString("LOCCommon5to10")}");
                }
                if (hltbTime < 72000)
                {
                    return CheckTagExist($"{ResourceProvider.GetString("LOCCommon10to20")}");
                }
                if (hltbTime < 108000)
                {
                    return CheckTagExist($"{ResourceProvider.GetString("LOCCommon20to30")}");
                }
                if (hltbTime < 144000)
                {
                    return CheckTagExist($"{ResourceProvider.GetString("LOCCommon30to40")}");
                }
                if (hltbTime < 180000)
                {
                    return CheckTagExist($"{ResourceProvider.GetString("LOCCommon40to50")}");
                }
                if (hltbTime < 216000)
                {
                    return CheckTagExist($"{ResourceProvider.GetString("LOCCommon50to60")}");
                }
                if (hltbTime < 252000)
                {
                    return CheckTagExist($"{ResourceProvider.GetString("LOCCommon60to70")}");
                }
                if (hltbTime < 288000)
                {
                    return CheckTagExist($"{ResourceProvider.GetString("LOCCommon70to80")}");
                }
                if (hltbTime < 324000)
                {
                    return CheckTagExist($"{ResourceProvider.GetString("LOCCommon80to90")}");
                }
                if (hltbTime < 360000)
                {
                    return CheckTagExist($"{ResourceProvider.GetString("LOCCommon90to100")}");
                }
                if (hltbTime >= 360000)
                {
                    return CheckTagExist($"{ResourceProvider.GetString("LOCCommon100plus")}");
                }
            }

            return null;
        }

        #endregion

        #region User data

        public TitleList GetUserHltbData(string hltbId)
        {
            try
            {
                return Database.UserHltbData.TitlesList == null || Database.UserHltbData.TitlesList.Count == 0
                    ? null
                    : Database.UserHltbData.TitlesList.Find(x => x.Id == hltbId);
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
                return Database.UserHltbData?.TitlesList == null || Database.UserHltbData.TitlesList.Count == 0
                    ? null
                    : Database.UserHltbData.TitlesList.FindAll(x => x.Id == hltbId).ToList();
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
                Database.UserHltbData.TitlesList.ForEach(x =>
                {
                    if (x.GameExist)
                    {
                        bool isCompletionist = x.HltbUserData?.Completionist != 0;
                        bool isCompleted = x.GameStatuses?.Where(y => y.Status == StatusType.Completed)?.Count() != 0;
                        bool isPlaying = x.GameStatuses?.Where(y => y.Status == StatusType.Playing)?.Count() != 0;

                        Game game = API.Instance.Database.Games.Get(x.GameId);

                        if (isCompletionist && PluginSettings.Settings.GameStatusCompletionist != default && API.Instance.Database.CompletionStatuses.Get(PluginSettings.Settings.GameStatusCompletionist) != null)
                        {
                            game.CompletionStatusId = PluginSettings.Settings.GameStatusCompletionist;
                        }
                        else if (isCompleted && PluginSettings.Settings.GameStatusCompleted != default && API.Instance.Database.CompletionStatuses.Get(PluginSettings.Settings.GameStatusCompleted) != null)
                        {
                            game.CompletionStatusId = PluginSettings.Settings.GameStatusCompleted;
                        }
                        else if (isPlaying && PluginSettings.Settings.GameStatusPlaying != default && API.Instance.Database.CompletionStatuses.Get(PluginSettings.Settings.GameStatusPlaying) != null)
                        {
                            game.CompletionStatusId = PluginSettings.Settings.GameStatusPlaying;
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
                bool isCompletionist = game.CompletionStatusId == PluginSettings.Settings.GameStatusCompletionist;
                bool isCompleted = game.CompletionStatusId == PluginSettings.Settings.GameStatusCompleted;
                bool isPlaying = game.CompletionStatusId == PluginSettings.Settings.GameStatusPlaying;

                if (isCompletionist && PluginSettings.Settings.GameStatusCompletionist != default && API.Instance.Database.CompletionStatuses.Get(PluginSettings.Settings.GameStatusCompletionist) != null)
                {
                    _ = SetCurrentPlayTime(game, true, false, false, false, true);
                }
                else if (isCompleted && PluginSettings.Settings.GameStatusCompleted != default && API.Instance.Database.CompletionStatuses.Get(PluginSettings.Settings.GameStatusCompleted) != null)
                {
                    _ = SetCurrentPlayTime(game, true, true);
                }
                else if (isPlaying && PluginSettings.Settings.GameStatusPlaying != default && API.Instance.Database.CompletionStatuses.Get(PluginSettings.Settings.GameStatusPlaying) != null)
                {
                    _ = SetCurrentPlayTime(game, false);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }
        }


        public void RefreshUserData()
        {
            Logger.Info("RefreshUserData()");

            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions($"{PluginName} - {ResourceProvider.GetString("LOCHowLongToBeatPluginGetUserView")}")
            {
                Cancelable = false,
                IsIndeterminate = true
            };

            _ = API.Instance.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                try
                {
                    HltbUserStats UserHltbData = HowLongToBeatApi.GetUserData();

                    if (UserHltbData != null)
                    {
                        Logger.Info($"Find {UserHltbData.TitlesList?.Count ?? 0} games");
                        FileSystem.WriteStringToFileSafe(Path.Combine(Paths.PluginUserDataPath, "HltbUserStats.json"), Serialization.ToJson(UserHltbData));
                        Database.UserHltbData = UserHltbData;

                        if (PluginSettings.Settings.AutoSetGameStatus)
                        {
                            SetGameStatusFromHltb();
                        }

                        Application.Current.Dispatcher?.Invoke(() =>
                        {
                            Database.OnCollectionChanged(null, null);
                        });
                    }
                    else
                    {
                        Logger.Info($"Find no data");
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginName);
                }
            }, globalProgressOptions);
        }

        public void RefreshUserData(string gameId)
        {
            _ = Task.Run(() =>
            {
                try
                {
                    TitleList titleList = HowLongToBeatApi.GetUserData(gameId);
                    if (titleList != null)
                    {
                        int index = Database.UserHltbData.TitlesList.FindIndex(x => x.Id == gameId);
                        if (index > -1)
                        {
                            Database.UserHltbData.TitlesList[index] = titleList;
                        }
                        else
                        {
                            Database.UserHltbData.TitlesList.Add(titleList);
                        }

                        Application.Current.Dispatcher?.Invoke(() =>
                        {
                            Database.OnCollectionChanged(null, null);
                        });

                        FileSystem.WriteStringToFileSafe(Path.Combine(Paths.PluginUserDataPath, "HltbUserStats.json"), Serialization.ToJson(Database.UserHltbData));
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
            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions($"{PluginName} - {ResourceProvider.GetString("LOCCommonProcessing")}")
            {
                Cancelable = true,
                IsIndeterminate = ids.Count() == 1
            };

            _ = API.Instance.Dialogs.ActivateGlobalProgress((a) =>
            {
                API.Instance.Database.BeginBufferUpdate();
                //Database.BeginBufferUpdate();

                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                a.ProgressMaxValue = ids.Count();

                foreach (Guid id in ids)
                {
                    Game game = API.Instance.Database.Games.Get(id);
                    a.Text = $"{PluginName} - {ResourceProvider.GetString("LOCCommonProcessing")}"
                        + (ids.Count() == 1 ? string.Empty : "\n\n" + $"{a.CurrentProgressValue}/{a.ProgressMaxValue}")
                        + "\n" + game?.Name + (game?.Source == null ? string.Empty : $" ({game?.Source.Name})");

                    if (a.CancelToken.IsCancellationRequested)
                    {
                        break;
                    }

                    try
                    {
                        Thread.Sleep(100);
                        _ = SetCurrentPlayTime(game, noPlaying, isCompleted, isMain, isMainSide, is100, isSolo, isCoOp, isVs);
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, true, PluginName);
                    }

                    a.CurrentProgressValue++;
                }

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                Logger.Info($"Task SetCurrentPlaytime(){(a.CancelToken.IsCancellationRequested ? " canceled" : string.Empty)} - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {a.CurrentProgressValue}/{ids.Count()} items");

                //Database.EndBufferUpdate();
                API.Instance.Database.EndBufferUpdate();
            }, globalProgressOptions);
        }

        public bool SetCurrentPlayTime(Game game, bool noPlaying = false, bool isCompleted = false, bool isMain = false, bool isMainSide = false, bool is100 = false, bool isSolo = false, bool isCoOp = false, bool isVs = false)
        {
            try
            {
                if (HowLongToBeatApi.GetIsUserLoggedIn())
                {
                    GameHowLongToBeat gameHowLongToBeat = Database.Get(game.Id);
                    if (gameHowLongToBeat != null && (!gameHowLongToBeat.GetData()?.IsVndb ?? false))
                    {
                        TimeSpan time = TimeSpan.FromSeconds(game.Playtime);
                        string platformName = HltbPlatform.PC.GetDescription();
                        string storefrontName = string.Empty;

                        #region Search platform

                        Platform platform = game.Platforms?.FirstOrDefault();
                        if (platform == default)
                        {
                            Logger.Warn($"Cannot submit data for a game without platform ({game.Name})");
                            API.Instance.Notifications.Add(new NotificationMessage(
                               $"{PluginName}-NoPlatform-Error-{new Guid()}",
                               PluginName + Environment.NewLine + string.Format(ResourceProvider.GetString("LOCHowLongToBeatErrorNoPlatform"), game.Name),
                               NotificationType.Error,
                               () => Plugin.OpenSettingsView()
                            ));
                            return false;
                        }

                        HltbPlatform? match = PluginSettings.Settings.Platforms.FirstOrDefault(p => p.Platform.Equals(platform))?.HltbPlatform;
                        if (match != null)
                        {
                            platformName = match.GetDescription();
                        }
                        else
                        {
                            Logger.Warn($"No platform find for {game.Name} - Default \"PC\" used");
                            API.Instance.Notifications.Add(new NotificationMessage(
                               $"{PluginName}-NoPlatformDefined-Error-{new Guid()}",
                               PluginName + Environment.NewLine + string.Format(ResourceProvider.GetString("LOCHowLongToBeatErrorNoPlatformDefaultUsed"), platform.Name, game.Name),
                               NotificationType.Error,
                               () => Plugin.OpenSettingsView()
                            ));
                        }

                        #endregion

                        #region Search storefront

                        Storefront storefront = PluginSettings.Settings.StorefrontElements.FirstOrDefault(x => x.SourceId != default && x.SourceId == game.SourceId);
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

                        HltbDataUser hltbDataUser = gameHowLongToBeat.GetData();
                        TitleList HltbData = GetUserHltbDataCurrent(hltbDataUser.Id, gameHowLongToBeat.UserGameId);
                        EditData editData = new EditData();
                        string submissionId = "0";

                        if (HltbData != null && HowLongToBeatApi.EditIdExist(HltbData.UserGameId))
                        {
                            submissionId = HltbData.UserGameId;
                            editData = HowLongToBeatApi.GetEditData(gameHowLongToBeat.Name, submissionId).GetAwaiter().GetResult();
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
                                    editData = HowLongToBeatApi.GetEditData(gameHowLongToBeat.Name, submissionId).GetAwaiter().GetResult();
                                }
                                else
                                {
                                    Logger.Info($"No existing data in website find for {game.Name}");
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

                        editData.UserId = Database.UserHltbData.UserId;
                        editData.SubmissionId = int.Parse(submissionId);
                        editData.GameId = int.Parse(hltbDataUser.Id);
                        editData.Title = editData.Title.IsNullOrEmpty() ? hltbDataUser.Name : editData.Title;
                        editData.Platform = platformName;
                        editData.Storefront = editData.Storefront.IsNullOrEmpty() ? storefrontName : editData.Storefront;

                        if (PluginSettings.Settings.UsedStartDateFromGameActivity)
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
                        else if (!editData.Lists.Backlog && !editData.Lists.Completed && !editData.Lists.Custom && !editData.Lists.Playing && !editData.Lists.Replay && !editData.Lists.Retired)
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

                        return HowLongToBeatApi.ApiSubmitData(game, editData).GetAwaiter().GetResult();
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
                PluginSettings.Settings.HasData = false;
                PluginSettings.Settings.HasDataEmpty = true;
                PluginSettings.Settings.MainStory = 0;
                PluginSettings.Settings.MainStoryFormat = "--";
                PluginSettings.Settings.MainExtra = 0;
                PluginSettings.Settings.MainExtraFormat = "--";
                PluginSettings.Settings.Completionist = 0;
                PluginSettings.Settings.CompletionistFormat = "--";
                PluginSettings.Settings.Solo = 0;
                PluginSettings.Settings.SoloFormat = "--";
                PluginSettings.Settings.CoOp = 0;
                PluginSettings.Settings.CoOpFormat = "--";
                PluginSettings.Settings.Vs = 0;
                PluginSettings.Settings.VsFormat = "--";

                PluginSettings.Settings.TimeToBeat = 0;
                PluginSettings.Settings.TimeToBeatFormat = "--";

                return;
            }

            PluginSettings.Settings.HasData = gameHowLongToBeat.HasData;
            PluginSettings.Settings.HasDataEmpty = gameHowLongToBeat.HasDataEmpty;
            PluginSettings.Settings.MainStory = gameHowLongToBeat.GetData().GameHltbData.MainStory;
            PluginSettings.Settings.MainStoryFormat = gameHowLongToBeat.GetData().GameHltbData.MainStoryFormat;
            PluginSettings.Settings.MainExtra = gameHowLongToBeat.GetData().GameHltbData.MainExtra;
            PluginSettings.Settings.MainExtraFormat = gameHowLongToBeat.GetData().GameHltbData.MainExtraFormat;
            PluginSettings.Settings.Completionist = gameHowLongToBeat.GetData().GameHltbData.Completionist;
            PluginSettings.Settings.CompletionistFormat = gameHowLongToBeat.GetData().GameHltbData.CompletionistFormat;
            PluginSettings.Settings.Solo = gameHowLongToBeat.GetData().GameHltbData.Solo;
            PluginSettings.Settings.SoloFormat = gameHowLongToBeat.GetData().GameHltbData.SoloFormat;
            PluginSettings.Settings.CoOp = gameHowLongToBeat.GetData().GameHltbData.CoOp;
            PluginSettings.Settings.CoOpFormat = gameHowLongToBeat.GetData().GameHltbData.CoOpFormat;
            PluginSettings.Settings.Vs = gameHowLongToBeat.GetData().GameHltbData.Vs;
            PluginSettings.Settings.VsFormat = gameHowLongToBeat.GetData().GameHltbData.VsFormat;

            PluginSettings.Settings.TimeToBeat = gameHowLongToBeat.GetData().GameHltbData.TimeToBeat;
            PluginSettings.Settings.TimeToBeatFormat = gameHowLongToBeat.GetData().GameHltbData.TimeToBeatFormat;
        }

        public override void ActionAfterGames_ItemUpdated(Game gameOld, Game gameNew)
        {
            _ = Task.Run(() =>
            {
                if (PluginSettings.Settings.AutoSetGameStatusToHltb && gameOld.CompletionStatusId != gameNew.CompletionStatusId)
                {
                    SetGameStatusToHltb(gameNew);
                }
            });
        }
    }
}