using HowLongToBeat.Models;
using HowLongToBeat.Views;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using CommonPluginsShared.Collections;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommonPluginsShared;
using CommonPluginsControls.Controls;
using System.Windows.Threading;
using FuzzySharp;
using CommonPlayniteShared.Common;
using CommonPluginsShared.Extensions;
using System.Net;
using HowLongToBeat.Models.Api;

namespace HowLongToBeat.Services
{
    public class HowLongToBeatDatabase : PluginDatabaseObject<HowLongToBeatSettingsViewModel, GameHowLongToBeatCollection, GameHowLongToBeat, HltbDataUser>
    {
        public HowLongToBeat Plugin { get; set; }
        public HowLongToBeatClient HowLongToBeatClient { get; set; }


        public HowLongToBeatDatabase(HowLongToBeatSettingsViewModel PluginSettings, string PluginUserDataPath) : base(PluginSettings, "HowLongToBeat", PluginUserDataPath)
        {
            TagBefore = "[HLTB]";
        }


        public void InitializeClient(HowLongToBeat plugin)
        {
            Plugin = plugin;
            HowLongToBeatClient = new HowLongToBeatClient();
        }


        protected override bool LoadDatabase()
        {
            try
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                Database = new GameHowLongToBeatCollection(Paths.PluginDatabasePath);
                Database.SetGameInfo<HltbDataUser>();

                Database.UserHltbData = HowLongToBeatClient.LoadUserData();

                DeleteDataWithDeletedGame();

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                Logger.Info($"LoadDatabase with {Database.Count} items - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)}");
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, "SuccessStory");
                return false;
            }

            return true;
        }


        public override GameHowLongToBeat Get(Guid Id, bool OnlyCache = false, bool Force = false)
        {
            GameHowLongToBeat gameHowLongToBeat = GetOnlyCache(Id);

            if (!OnlyCache && gameHowLongToBeat != null && !gameHowLongToBeat.HasData && !gameHowLongToBeat.HasDataEmpty)
            {
                gameHowLongToBeat = null;
            }

            if ((gameHowLongToBeat == null && !OnlyCache) || Force)
            {
                gameHowLongToBeat = HowLongToBeatClient.SearchData(API.Instance.Database.Games.Get(Id));

                if (gameHowLongToBeat != null)
                {
                    AddOrUpdate(gameHowLongToBeat);
                }
            }

            if (gameHowLongToBeat == null)
            {
                Game game = API.Instance.Database.Games.Get(Id);
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

            List<HltbDataUser> data = HowLongToBeatClient.SearchTwoMethod(game.Name).GetAwaiter().GetResult();
            if (data.Count == 1 && PluginSettings.Settings.AutoAccept)
            {
                gameHowLongToBeat.Items = new List<HltbDataUser>() { data.First() };
                AddOrUpdate(gameHowLongToBeat);
            }
            else
            {
                if (data.Count > 0 && PluginSettings.Settings.UseMatchValue)
                {
                    var FuzzList = data.Select(x => new { MatchPercent = Fuzz.Ratio(game.Name.ToLower(), x.Name.ToLower()), Data = x })
                        .OrderByDescending(x => x.MatchPercent)
                        .ToList();

                    if (FuzzList.First().MatchPercent >= PluginSettings.Settings.MatchValue)
                    {
                        gameHowLongToBeat.Items = new List<HltbDataUser>() { FuzzList.First().Data };
                        AddOrUpdate(gameHowLongToBeat);
                        return;
                    }
                }

                if (data.Count > 0 && PluginSettings.Settings.ShowWhenMismatch)
                {
                    _ = Get(game, false, true);
                }
            }
        }

        public void RefreshAll()
        {
            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions($"{PluginName} - {ResourceProvider.GetString("LOCCommonProcessing")}")
            {
                Cancelable = true,
                IsIndeterminate = false
            };

            _ = API.Instance.Dialogs.ActivateGlobalProgress((a) =>
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                string CancelText = string.Empty;

                IEnumerable<GameHowLongToBeat> db = Database.Where(x => x.HasData);
                a.ProgressMaxValue = (double)db.Count();

                foreach (GameHowLongToBeat item in db)
                {
                    Game game = item.Game;
                    a.Text = $"{PluginName} - {ResourceProvider.GetString("LOCCommonProcessing")}"
                        + "\n\n" + $"{a.CurrentProgressValue}/{a.ProgressMaxValue}"
                        + "\n" + game?.Name + (game?.Source == null ? string.Empty : $" ({game?.Source.Name})");

                    if (a.CancelToken.IsCancellationRequested)
                    {
                        break;
                    }

                    if (item.DateLastRefresh.AddMonths(1) < DateTime.Now)
                    {
                        RefreshNoLoader(item.Id);
                    }

                    a.CurrentProgressValue++;
                }

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                Logger.Info($"Task RefreshAll(){CancelText} - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {a.CurrentProgressValue}/{(double)db.Count()} items");
            }, globalProgressOptions);
        }

        public override void RefreshNoLoader(Guid id)
        {
            Game game = API.Instance.Database.Games.Get(id);
            Logger.Info($"RefreshNoLoader({game?.Name} - {game?.Id})");

            GameHowLongToBeat loadedItem = Get(id, true);
            List<HltbDataUser> dataSearch = loadedItem.GetData().IsVndb
                ? VndbApi.SearchById(loadedItem.GetData().Id)
                : HowLongToBeatClient.SearchTwoMethod(loadedItem.GetData().Name).GetAwaiter().GetResult();

            HltbDataUser webDataSearch = dataSearch.Find(x => x.Id == loadedItem.GetData().Id);
            if (webDataSearch != null)
            {
                loadedItem.Items = new List<HltbDataUser> { webDataSearch };
                loadedItem.DateLastRefresh = DateTime.Now;
                Update(loadedItem);
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
        public TitleList GetUserHltbData(string HltbId)
        {
            try
            {
                return Database.UserHltbData.TitlesList == null || Database.UserHltbData.TitlesList.Count == 0
                    ? null
                    : Database.UserHltbData.TitlesList.Find(x => x.Id == HltbId);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, true);
                Logger.Warn($"No HltbData for {HltbId}");
                return null;
            }
        }

        public TitleList GetUserHltbDataCurrent(string HltbId, string UserGameId = "")
        {
            try
            {
                List<TitleList> all = GetUserHltbDataAll(HltbId);
                return all == null || all.Count == 0
                    ? null
                    : UserGameId.IsNullOrEmpty()
                    ? all.OrderByDescending(x => x.GameStatuses.Where(y => y.Status == StatusType.Playing)?.Count() > 0).ThenByDescending(x => x.LastUpdate).First()
                    : all.FirstOrDefault(x => x.UserGameId.IsEqual(UserGameId));
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"No HltbData for {HltbId}");
                return null;
            }
        }

        public List<TitleList> GetUserHltbDataAll(string HltbId)
        {
            try
            {
                return Database.UserHltbData.TitlesList == null || Database.UserHltbData.TitlesList.Count == 0
                    ? null
                    : Database.UserHltbData.TitlesList.FindAll(x => x.Id == HltbId).ToList();
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"No HltbData for {HltbId}");
                return null;
            }
        }


        private void SetGameStatus()
        {
            try
            {
                API.Instance.Database.Games.BeginBufferUpdate();
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
                API.Instance.Database.Games.EndBufferUpdate();
            }
        }


        public void RefreshUserData()
        {
            Logger.Info("RefreshUserData()");

            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                $"{PluginName} - {ResourceProvider.GetString("LOCHowLongToBeatPluginGetUserView")}",
                false
            );
            globalProgressOptions.IsIndeterminate = true;

            _ = API.Instance.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                try
                {
                    HltbUserStats UserHltbData = HowLongToBeatClient.GetUserData();

                    if (UserHltbData != null)
                    {
                        Logger.Info($"Find {UserHltbData.TitlesList?.Count ?? 0} games");
                        FileSystem.WriteStringToFileSafe(Path.Combine(Paths.PluginUserDataPath, "HltbUserStats.json"), Serialization.ToJson(UserHltbData));
                        Database.UserHltbData = UserHltbData;

                        if (PluginSettings.Settings.AutoSetGameStatus)
                        {
                            SetGameStatus();
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

        public void RefreshUserData(string game_id)
        {
            _ = Task.Run(() =>
            {
                try
                {
                    TitleList titleList = HowLongToBeatClient.GetUserData(game_id);
                    if (titleList != null)
                    {
                        int index = Database.UserHltbData.TitlesList.FindIndex(x => x.Id == game_id);
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

        public bool SetCurrentPlayTime(Game game, ulong ElapsedSeconds = 0, bool NoPlaying = false, bool IsCompleted = false, bool IsMain = false, bool IsMainSide = false, bool Is100 = false, bool IsSolo = false, bool IsCoOp = false, bool IsVs = false)
        {
            try
            {
                if (HowLongToBeatClient.GetIsUserLoggedIn())
                {
                    GameHowLongToBeat gameHowLongToBeat = Database.Get(game.Id);
                    if (gameHowLongToBeat != null && (!gameHowLongToBeat.GetData()?.IsVndb ?? false))
                    {
                        TimeSpan time = TimeSpan.FromSeconds(game.Playtime + ElapsedSeconds);
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

                        if (HltbData != null && HowLongToBeatClient.EditIdExist(HltbData.UserGameId))
                        {
                            submissionId = HltbData.UserGameId;
                            editData = HowLongToBeatClient.GetEditData(gameHowLongToBeat.Name, submissionId).GetAwaiter().GetResult();
                        }
                        else
                        {
                            // Find existing in website
                            if (hltbDataUser != null)
                            {
                                string tmpEditId = HowLongToBeatClient.FindIdExisting(hltbDataUser.Id.ToString());
                                if (!tmpEditId.IsNullOrEmpty())
                                {
                                    submissionId = tmpEditId;
                                    editData = HowLongToBeatClient.GetEditData(gameHowLongToBeat.Name, submissionId).GetAwaiter().GetResult();
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
                        editData.Title = hltbDataUser.Name;
                        editData.Platform = platformName;
                        editData.Storefront = editData.Storefront.IsNullOrEmpty() ? storefrontName : editData.Storefront;

                        if (!NoPlaying)
                        {
                            editData.Lists.Playing = true;
                        }
                        else if (!editData.Lists.Backlog && !editData.Lists.Completed && !editData.Lists.Custom && !editData.Lists.Playing && !editData.Lists.Replay && !editData.Lists.Retired)
                        {
                            editData.Lists.Playing = true;
                        }

                        if (IsCompleted)
                        {
                            editData.Lists.Completed = true;

                            if (IsMain)
                            {
                                editData.SinglePlayer.CompMain.Time.Hours = time.Hours + (24 * time.Days);
                                editData.SinglePlayer.CompMain.Time.Minutes = time.Minutes;
                                editData.SinglePlayer.CompMain.Time.Seconds = time.Seconds;

                                editData.General.CompletionDate.Day = ((DateTime)game.LastActivity).Day.ToString();
                                editData.General.CompletionDate.Month = ((DateTime)game.LastActivity).Month.ToString();
                                editData.General.CompletionDate.Year = ((DateTime)game.LastActivity).Year.ToString();
                            }

                            if (IsMainSide)
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

                            if (Is100)
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

                        if (IsCoOp)
                        {
                            editData.MultiPlayer.CoOp.Time.Hours = time.Hours + (24 * time.Days);
                            editData.MultiPlayer.CoOp.Time.Minutes = time.Minutes;
                            editData.MultiPlayer.CoOp.Time.Seconds = time.Seconds;
                        }

                        if (IsVs)
                        {
                            editData.MultiPlayer.Vs.Time.Hours = time.Hours + (24 * time.Days);
                            editData.MultiPlayer.Vs.Time.Minutes = time.Minutes;
                            editData.MultiPlayer.Vs.Time.Seconds = time.Seconds;
                        }

                        editData.General.Progress.Hours = time.Hours + (24 * time.Days);
                        editData.General.Progress.Minutes = time.Minutes;
                        editData.General.Progress.Seconds = time.Seconds;
                        #endregion

                        return HowLongToBeatClient.ApiSubmitData(game, editData).GetAwaiter().GetResult(); 
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


        public double GetAvgGameByMonth()
        {
            double result = 0;

            Dictionary<string, int> DataByMonth = new Dictionary<string, int>();
            foreach (TitleList titleList in Database.UserHltbData.TitlesList)
            {
                string Month = titleList.Completion?.ToString("yyyy-MM");
                if (!Month.IsNullOrEmpty())
                {
                    if (DataByMonth.ContainsKey(Month))
                    {
                        DataByMonth[Month]++;
                    }
                    else
                    {
                        DataByMonth.Add(Month, 1);
                    }
                }
            }

            if (DataByMonth.Count > 0)
            {
                foreach (KeyValuePair<string, int> data in DataByMonth)
                {
                    result += data.Value;
                }
                result /= DataByMonth.Count;
            }

            return result;
        }

        public long GetAvgTimeByGame()
        {
            long result = 0;
            double count = 0;

            foreach (TitleList titleList in Database.UserHltbData.TitlesList)
            {
                if (titleList.Completion != null && titleList.HltbUserData.TimeToBeat != 0)
                {
                    count++;
                    result += titleList.HltbUserData.TimeToBeat;
                }
            }

            if (count > 0)
            {
                result = (long)(result / count);
            }

            return result;
        }

        public int GetCountGameBeatenBeforeTime()
        {
            return Database.UserHltbData.TitlesList
                .Where(x => x.HltbUserData.TimeToBeat != 0 && x.Completion != null 
                            && Get(x.GameId, true)?.GetData()?.GameHltbData?.TimeToBeat > x.HltbUserData?.TimeToBeat).Count();
        }

        public int GetCountGameBeatenAfterTime()
        {
            return Database.UserHltbData.TitlesList
                .Where(x => x.HltbUserData.TimeToBeat != 0 && x.Completion != null
                        && Get(x.GameId, true)?.GetData()?.GameHltbData?.TimeToBeat <= x.HltbUserData?.TimeToBeat).Count();
        }

        public int GetCountGameBeatenReplays()
        {
            return Database.UserHltbData.TitlesList.Where(x => x.IsReplay).Count();
        }

        public int GetCountGameRetired()
        {
            return Database.UserHltbData.TitlesList.Where(x => x.IsRetired).Count();
        }
        #endregion


        public override void SetThemesResources(Game game)
        {
            GameHowLongToBeat gameHowLongToBeat = Get(game, true);

            if (gameHowLongToBeat == null || !gameHowLongToBeat.HasData)
            {
                PluginSettings.Settings.HasData = false;
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


        public void UpdatedCookies()
        {
            _ = Task.Run(() =>
            {
                try
                {
                    _ = SpinWait.SpinUntil(() => API.Instance.Database.IsOpen, -1);
                    _ = SpinWait.SpinUntil(() => IsLoaded, -1);
                    if (HowLongToBeatClient.GetIsUserLoggedIn())
                    {
                        using (IWebView WebViewOffscreen = API.Instance.WebViews.CreateOffscreenView())
                        {
                            WebViewOffscreen.NavigateAndWait("https://howlongtobeat.com");
                            List<HttpCookie> Cookies = HowLongToBeatClient.GetWebCookies(WebViewOffscreen);
                            _ = HowLongToBeatClient.SetStoredCookies(Cookies);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginName);
                }
            });
        }
    }
}
