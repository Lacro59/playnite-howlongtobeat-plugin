﻿using HowLongToBeat.Models;
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

namespace HowLongToBeat.Services
{
    public class HowLongToBeatDatabase : PluginDatabaseObject<HowLongToBeatSettingsViewModel, GameHowLongToBeatCollection, GameHowLongToBeat, HltbDataUser>
    {
        public HowLongToBeat Plugin;
        public HowLongToBeatClient howLongToBeatClient;


        public HowLongToBeatDatabase(HowLongToBeatSettingsViewModel PluginSettings, string PluginUserDataPath) : base(PluginSettings, "HowLongToBeat", PluginUserDataPath)
        {
            TagBefore = "[HLTB]";
        }


        public void InitializeClient(HowLongToBeat Plugin)
        {
            this.Plugin = Plugin;
            howLongToBeatClient = new HowLongToBeatClient();
        }


        protected override bool LoadDatabase()
        {
            try
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                Database = new GameHowLongToBeatCollection(Paths.PluginDatabasePath);
                Database.SetGameInfo<HltbDataUser>();

                Database.UserHltbData = howLongToBeatClient.LoadUserData();

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


        public override void GetSelectData()
        {
            OptionsDownloadData View = new OptionsDownloadData();
            Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PluginName + " - " + ResourceProvider.GetString("LOCCommonSelectData"), View);
            _ = windowExtension.ShowDialog();

            List<Game> PlayniteDb = View.GetFilteredGames();
            bool OnlyMissing = View.GetOnlyMissing();

            if (PlayniteDb == null)
            {
                return;
            }

            if (OnlyMissing)
            {
                PlayniteDb = PlayniteDb.FindAll(x => !Get(x.Id, true).HasData);
            }

            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                $"{PluginName} - {ResourceProvider.GetString("LOCCommonGettingData")}",
                true
            );
            globalProgressOptions.IsIndeterminate = false;

            _ = API.Instance.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                try
                {
                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();

                    activateGlobalProgress.ProgressMaxValue = (double)PlayniteDb.Count();

                    string CancelText = string.Empty;

                    foreach (Game game in PlayniteDb)
                    {
                        if (activateGlobalProgress.CancelToken.IsCancellationRequested)
                        {
                            CancelText = " canceled";
                            break;
                        }

                        Thread.Sleep(10);
                        AddData(game);

                        activateGlobalProgress.CurrentProgressValue++;
                    }

                    stopWatch.Stop();
                    TimeSpan ts = stopWatch.Elapsed;
                    Logger.Info($"Task GetSelectData(){CancelText} - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {activateGlobalProgress.CurrentProgressValue}/{(double)PlayniteDb.Count()} items");
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginName);
                }
            }, globalProgressOptions);
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
                gameHowLongToBeat = howLongToBeatClient.SearchData(API.Instance.Database.Games.Get(Id));

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

            List<HltbDataUser> data = HowLongToBeat.PluginDatabase.howLongToBeatClient.SearchTwoMethod(game.Name);
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


        public override void Refresh(Guid Id)
        {
            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                $"{PluginName} - {ResourceProvider.GetString("LOCCommonProcessing")}",
                false
            );
            globalProgressOptions.IsIndeterminate = true;

            _ = API.Instance.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                RefreshElement(Id);
            }, globalProgressOptions);
        }

        public override void Refresh(List<Guid> Ids)
        {
            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                $"{PluginName} - {ResourceProvider.GetString("LOCCommonProcessing")}",
                true
            );
            globalProgressOptions.IsIndeterminate = false;

            _ = API.Instance.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                activateGlobalProgress.ProgressMaxValue = Ids.Count;

                string CancelText = string.Empty;

                foreach (Guid Id in Ids)
                {
                    if (activateGlobalProgress.CancelToken.IsCancellationRequested)
                    {
                        CancelText = " canceled";
                        break;
                    }

                    RefreshElement(Id);
                    activateGlobalProgress.CurrentProgressValue++;
                }

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                Logger.Info($"Task Refresh(){CancelText} - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {activateGlobalProgress.CurrentProgressValue}/{Ids.Count} items");
            }, globalProgressOptions);
        }

        public void RefreshAll()
        {
            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                $"{PluginName} - {ResourceProvider.GetString("LOCCommonProcessing")}",
                true
            );
            globalProgressOptions.IsIndeterminate = false;

            _ = API.Instance.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                string CancelText = string.Empty;

                IEnumerable<GameHowLongToBeat> db = Database.Where(x => x.HasData);
                activateGlobalProgress.ProgressMaxValue = (double)db.Count();

                foreach (GameHowLongToBeat item in db)
                {
                    if (activateGlobalProgress.CancelToken.IsCancellationRequested)
                    {
                        CancelText = " canceled";
                        break;
                    }

                    if (item.DateLastRefresh.AddMonths(1) < DateTime.Now)
                    {
                        RefreshElement(item.Id);
                    }

                    activateGlobalProgress.CurrentProgressValue++;
                }

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                Logger.Info($"Task RefreshAll(){CancelText} - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {activateGlobalProgress.CurrentProgressValue}/{(double)db.Count()} items");
            }, globalProgressOptions);
        }

        private void RefreshElement(Guid Id)
        {
            GameHowLongToBeat loadedItem = Get(Id, true);
            List<HltbDataUser> dataSearch = howLongToBeatClient.Search(loadedItem.GetData().Name);

            HltbDataUser webDataSearch = dataSearch.Find(x => x.Id == loadedItem.GetData().Id);
            if (webDataSearch != null)
            {
                if (!ReferenceEquals(loadedItem.GetData(), webDataSearch))
                {
                    loadedItem.Items = new List<HltbDataUser> { webDataSearch };
                    loadedItem.DateLastRefresh = DateTime.Now;
                    Update(loadedItem);
                }
            }
        }

        public override void RefreshWithNoData(List<Guid> Ids)
        {
            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                $"{PluginName} - {ResourceProvider.GetString("LOCCommonProcessing")}",
                true
            );
            globalProgressOptions.IsIndeterminate = false;

            _ = API.Instance.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                activateGlobalProgress.ProgressMaxValue = Ids.Count;

                string CancelText = string.Empty;

                foreach (Guid Id in Ids)
                {
                    if (activateGlobalProgress.CancelToken.IsCancellationRequested)
                    {
                        CancelText = " canceled";
                        break;
                    }

                    Game game = API.Instance.Database.Games.Get(Id);
                    if (game != null)
                    {
                        AddData(game);
                    }

                    activateGlobalProgress.CurrentProgressValue++;
                }

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                Logger.Info($"Task Refresh(){CancelText} - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {activateGlobalProgress.CurrentProgressValue}/{Ids.Count} items");
            }, globalProgressOptions);
        }


        #region Tag
        public override void AddTag(Game game, bool noUpdate = false)
        {
            GetPluginTags();
            GameHowLongToBeat gameHowLongToBeat = Get(game, true);

            if (gameHowLongToBeat.HasData)
            {
                try
                {
                    HltbDataUser hltbDataUser = gameHowLongToBeat.GetData();
                    Guid? TagId = FindGoodPluginTags(hltbDataUser);

                    if (TagId != null)
                    {
                        if (game.TagIds != null)
                        {
                            game.TagIds.Add((Guid)TagId);
                        }
                        else
                        {
                            game.TagIds = new List<Guid> { (Guid)TagId };
                        }

                        if (!noUpdate)
                        {
                            Application.Current.Dispatcher?.Invoke(() =>
                            {
                                API.Instance.Database.Games.Update(game);
                                game.OnPropertyChanged();
                            }, DispatcherPriority.Send);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Tag insert error with {game.Name}", true, PluginName, string.Format(ResourceProvider.GetString("LOCCommonNotificationTagError"), game.Name));
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

                if (!noUpdate)
                {
                    Application.Current.Dispatcher?.Invoke(() =>
                    {
                        API.Instance.Database.Games.Update(game);
                        game.OnPropertyChanged();
                    }, DispatcherPriority.Send);
                }

            }
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
        public TitleList GetUserHltbData(int HltbId)
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

        public TitleList GetUserHltbDataCurrent(int HltbId, string UserGameId = "")
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

        public List<TitleList> GetUserHltbDataAll(int HltbId)
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
                    HltbUserStats UserHltbData = howLongToBeatClient.GetUserData();

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

        public void RefreshUserData(int game_id)
        {
            _ = Task.Run(() => 
            {
                try
                {
                    TitleList titleList = howLongToBeatClient.GetUserData(game_id);
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

        public bool SetCurrentPlayTime(Game game, ulong ElapsedSeconds = 0, bool NoPlaying = false, bool IsCompleted = false, bool IsMain = false, bool IsMainSide = false, bool Is100 = false)
        {
            try
            {
                if (howLongToBeatClient.GetIsUserLoggedIn())
                {
                    GameHowLongToBeat gameHowLongToBeat = Database.Get(game.Id);

                    if (gameHowLongToBeat != null)
                    {
                        TimeSpan time = TimeSpan.FromSeconds(game.Playtime + ElapsedSeconds);

                        // TODO Enough?
                        string platform = string.Empty;
                        Platform gamePlatform = game.Platforms?.FirstOrDefault();
                        if (gamePlatform == default(Platform)) {
                            Logger.Warn($"Cannot submit data for a game without platform ({game.Name})");
                            API.Instance.Notifications.Add(new NotificationMessage(
                               $"{PluginName}-NoPlatform-Error-{new Guid()}",
                               PluginName + Environment.NewLine + string.Format(ResourceProvider.GetString("LOCHowLongToBeatErrorNoPlatform"), game.Name),
                               NotificationType.Error,
                               () => Plugin.OpenSettingsView()
                            ));
                            return false;
                        }

                        HltbPlatform? match = PluginSettings.Settings.Platforms.Where(p => p.Platform.Equals(gamePlatform)).FirstOrDefault()?.HltbPlatform;
                        if (match != null) 
                        {
                            platform = match.GetDescription();
                        }
                        else
                        {
                            platform = HltbPlatform.PC.GetDescription();

                            Logger.Warn($"No platform find for {game.Name} - Default \"PC\" used");
                            API.Instance.Notifications.Add(new NotificationMessage(
                               $"{PluginName}-NoPlatformDefined-Error-{new Guid()}",
                               PluginName + Environment.NewLine + string.Format(ResourceProvider.GetString("LOCHowLongToBeatErrorNoPlatformDefaultUsed"), gamePlatform.Name, game.Name),
                               NotificationType.Error,
                               () => Plugin.OpenSettingsView()
                            ));
                        }

                        string StorefrontName = string.Empty;
                        Storefront storefront = PluginSettings.Settings.StorefrontElements.Where(x => x.SourceId != default && x.SourceId == game.SourceId).FirstOrDefault();
                        if (storefront != null)
                        {
                            StorefrontName = storefront.HltbStorefrontName;
                        }

                        TitleList HltbData = GetUserHltbDataCurrent(gameHowLongToBeat.GetData().Id, gameHowLongToBeat.UserGameId);
                        int edit_id = 0;
                        HltbPostData hltbPostData = new HltbPostData();
                        if (HltbData != null)
                        {
                            if (howLongToBeatClient.EditIdExist(HltbData.UserGameId))
                            {
                                edit_id = int.Parse(HltbData.UserGameId);
                                hltbPostData = howLongToBeatClient.GetSubmitData(gameHowLongToBeat.Name, edit_id.ToString());
                            }
                        }
                        else
                        {
                            // Find existing in website
                            HltbDataUser hltbDataUser = gameHowLongToBeat.GetData();
                            if (hltbDataUser != null)
                            {
                                string tmpEditId = howLongToBeatClient.FindIdExisting(hltbDataUser.Id.ToString());
                                if (!tmpEditId.IsNullOrEmpty())
                                {
                                    edit_id = int.Parse(tmpEditId);
                                    hltbPostData = howLongToBeatClient.GetSubmitData(gameHowLongToBeat.Name, tmpEditId);
                                }
                                else
                                {
                                    Logger.Info($"No existing data in website find for {game.Name}");
                                }
                            }
                        }

                        if (hltbPostData == null)
                        {
                            Logger.Warn($"No hltbPostData for {game.Name}");
                            return false;
                        }

                        hltbPostData.user_id = Database.UserHltbData.UserId;
                        hltbPostData.edit_id = edit_id;
                        hltbPostData.game_id = gameHowLongToBeat.GetData().Id;
                        hltbPostData.custom_title = gameHowLongToBeat.GetData().Name;
                        hltbPostData.platform = WebUtility.HtmlEncode(platform);
                        hltbPostData.storefront = hltbPostData.storefront.IsNullOrEmpty() ? StorefrontName : hltbPostData.storefront;

                        if (!NoPlaying)
                        {
                            hltbPostData.list_p = "1";
                        }
                        else if (hltbPostData.list_b.IsNullOrEmpty() && hltbPostData.list_c.IsNullOrEmpty() && hltbPostData.list_cp.IsNullOrEmpty() 
                            && hltbPostData.list_p.IsNullOrEmpty() && hltbPostData.list_r.IsNullOrEmpty() &&hltbPostData.list_rt.IsNullOrEmpty())
                        {
                            hltbPostData.list_p = "1";
                        }

                        if (IsCompleted)
                        {
                            hltbPostData.list_cp = "1";

                            if (IsMain)
                            {
                                hltbPostData.c_main_h = (time.Hours + (24 * time.Days)).ToString();
                                hltbPostData.c_main_m = time.Minutes.ToString();
                                hltbPostData.c_main_s = time.Seconds.ToString();

                                hltbPostData.compday = ((DateTime)game.LastActivity).Day.ToString();
                                hltbPostData.compmonth = ((DateTime)game.LastActivity).Month.ToString();
                                hltbPostData.compyear = ((DateTime)game.LastActivity).Year.ToString();
                            }

                            if (IsMainSide)
                            {
                                hltbPostData.c_plus_h = (time.Hours + (24 * time.Days)).ToString();
                                hltbPostData.c_plus_m = time.Minutes.ToString();
                                hltbPostData.c_plus_s = time.Seconds.ToString();

                                if (hltbPostData.compday.IsNullOrEmpty() || hltbPostData.compday == "00")
                                {
                                    hltbPostData.compday = ((DateTime)game.LastActivity).Day.ToString();
                                    hltbPostData.compmonth = ((DateTime)game.LastActivity).Month.ToString();
                                    hltbPostData.compyear = ((DateTime)game.LastActivity).Year.ToString();
                                }
                            }

                            if (Is100)
                            {
                                hltbPostData.c_100_h = (time.Hours + (24 * time.Days)).ToString();
                                hltbPostData.c_100_m = time.Minutes.ToString();
                                hltbPostData.c_100_s = time.Seconds.ToString();

                                if (hltbPostData.compday.IsNullOrEmpty() || hltbPostData.compday == "00")
                                {
                                    hltbPostData.compday = ((DateTime)game.LastActivity).Day.ToString();
                                    hltbPostData.compmonth = ((DateTime)game.LastActivity).Month.ToString();
                                    hltbPostData.compyear = ((DateTime)game.LastActivity).Year.ToString();
                                }
                            }
                        }

                        hltbPostData.protime_h = (time.Hours + (24 * time.Days)).ToString();
                        hltbPostData.protime_m = time.Minutes.ToString();
                        hltbPostData.protime_s = time.Seconds.ToString();


                        return howLongToBeatClient.PostData(game, hltbPostData).GetAwaiter().GetResult(); 
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
                if (howLongToBeatClient.GetIsUserLoggedIn())
                {
                    using (IWebView WebViewOffscreen = API.Instance.WebViews.CreateOffscreenView())
                    {
                        WebViewOffscreen.NavigateAndWait("https://howlongtobeat.com");
                    }
                    List<HttpCookie> Cookies = howLongToBeatClient.GetWebCookies();
                    _ = howLongToBeatClient.SetStoredCookies(Cookies);
                }
            });
        }
    }
}
