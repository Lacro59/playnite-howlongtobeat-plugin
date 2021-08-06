using HowLongToBeat.Models;
using HowLongToBeat.Views;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using CommonPluginsShared.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommonPluginsShared;
using System.Net;
using CommonPluginsControls.Controls;
using System.Net.Http;
using System.Windows.Threading;

namespace HowLongToBeat.Services
{
    public class HowLongToBeatDatabase : PluginDatabaseObject<HowLongToBeatSettingsViewModel, GameHowLongToBeatCollection, GameHowLongToBeat>
    {
        public HowLongToBeat Plugin;
        public HowLongToBeatClient howLongToBeatClient;

        public List<HltbPlatform> hltbPlatforms = new List<HltbPlatform>
        {
            new HltbPlatform() { Name = "3DO", Category = "All Platforms" },
            new HltbPlatform() { Name = "Amiga", Category = "All Platforms" },
            new HltbPlatform() { Name = "Amstrad CPC", Category = "All Platforms" },
            new HltbPlatform() { Name = "Android", Category = "All Platforms" },
            new HltbPlatform() { Name = "Apple II", Category = "All Platforms" },
            new HltbPlatform() { Name = "Arcade", Category = "All Platforms" },
            new HltbPlatform() { Name = "Atari 2600", Category = "All Platforms" },
            new HltbPlatform() { Name = "Atari 5200", Category = "All Platforms" },
            new HltbPlatform() { Name = "Atari 7800", Category = "All Platforms" },
            new HltbPlatform() { Name = "Atari 8-bit Family", Category = "All Platforms" },
            new HltbPlatform() { Name = "Atari Jaguar", Category = "All Platforms" },
            new HltbPlatform() { Name = "Atari Jaguar CD", Category = "All Platforms" },
            new HltbPlatform() { Name = "Atari Lynx", Category = "All Platforms" },
            new HltbPlatform() { Name = "Atari ST", Category = "All Platforms" },
            new HltbPlatform() { Name = "BBC Micro", Category = "All Platforms" },
            new HltbPlatform() { Name = "Browser", Category = "All Platforms" },
            new HltbPlatform() { Name = "ColecoVision", Category = "All Platforms" },
            new HltbPlatform() { Name = "Commodore 64", Category = "All Platforms" },
            new HltbPlatform() { Name = "Dreamcast", Category = "All Platforms" },
            new HltbPlatform() { Name = "Emulated", Category = "All Platforms" },
            new HltbPlatform() { Name = "FM Towns", Category = "All Platforms" },
            new HltbPlatform() { Name = "Game & Watch", Category = "All Platforms" },
            new HltbPlatform() { Name = "Game Boy", Category = "All Platforms" },
            new HltbPlatform() { Name = "Game Boy Advance", Category = "All Platforms" },
            new HltbPlatform() { Name = "Game Boy Color", Category = "All Platforms" },
            new HltbPlatform() { Name = "Gear VR", Category = "All Platforms" },
            new HltbPlatform() { Name = "Google Stadia", Category = "All Platforms" },
            new HltbPlatform() { Name = "Intellivision", Category = "All Platforms" },
            new HltbPlatform() { Name = "Interactive Movie", Category = "All Platforms" },
            new HltbPlatform() { Name = "iOS", Category = "All Platforms" },
            new HltbPlatform() { Name = "Linux", Category = "All Platforms" },
            new HltbPlatform() { Name = "Mac", Category = "All Platforms" },
            new HltbPlatform() { Name = "Mobile", Category = "All Platforms" },
            new HltbPlatform() { Name = "MSX", Category = "All Platforms" },
            new HltbPlatform() { Name = "N-Gage", Category = "All Platforms" },
            new HltbPlatform() { Name = "NEC PC-8800", Category = "All Platforms" },
            new HltbPlatform() { Name = "NEC PC-9801/21", Category = "All Platforms" },
            new HltbPlatform() { Name = "NEC PC-FX", Category = "All Platforms" },
            new HltbPlatform() { Name = "Neo Geo", Category = "All Platforms" },
            new HltbPlatform() { Name = "Neo Geo CD", Category = "All Platforms" },
            new HltbPlatform() { Name = "Neo Geo Pocket", Category = "All Platforms" },
            new HltbPlatform() { Name = "NES", Category = "All Platforms" },
            new HltbPlatform() { Name = "Nintendo 3DS", Category = "All Platforms" },
            new HltbPlatform() { Name = "Nintendo 64", Category = "All Platforms" },
            new HltbPlatform() { Name = "Nintendo DS", Category = "All Platforms" },
            new HltbPlatform() { Name = "Nintendo GameCube", Category = "All Platforms" },
            new HltbPlatform() { Name = "Nintendo Switch", Category = "All Platforms" },
            new HltbPlatform() { Name = "Oculus Go", Category = "All Platforms" },
            new HltbPlatform() { Name = "Oculus Quest", Category = "All Platforms" },
            new HltbPlatform() { Name = "OnLive", Category = "All Platforms" },
            new HltbPlatform() { Name = "Ouya", Category = "All Platforms" },
            new HltbPlatform() { Name = "PC", Category = "All Platforms" },
            new HltbPlatform() { Name = "PC VR", Category = "All Platforms" },
            new HltbPlatform() { Name = "Philips CD-i", Category = "All Platforms" },
            new HltbPlatform() { Name = "Philips Videopac G7000", Category = "All Platforms" },
            new HltbPlatform() { Name = "PlayStation", Category = "All Platforms" },
            new HltbPlatform() { Name = "PlayStation 2", Category = "All Platforms" },
            new HltbPlatform() { Name = "PlayStation 3", Category = "All Platforms" },
            new HltbPlatform() { Name = "PlayStation 4", Category = "All Platforms" },
            new HltbPlatform() { Name = "PlayStation 5", Category = "All Platforms" },
            new HltbPlatform() { Name = "PlayStation Mobile", Category = "All Platforms" },
            new HltbPlatform() { Name = "PlayStation Now", Category = "All Platforms" },
            new HltbPlatform() { Name = "PlayStation Portable", Category = "All Platforms" },
            new HltbPlatform() { Name = "PlayStation Vita", Category = "All Platforms" },
            new HltbPlatform() { Name = "PlayStation VR", Category = "All Platforms" },
            new HltbPlatform() { Name = "Plug & Play", Category = "All Platforms" },
            new HltbPlatform() { Name = "Sega 32X", Category = "All Platforms" },
            new HltbPlatform() { Name = "Sega CD", Category = "All Platforms" },
            new HltbPlatform() { Name = "Sega Game Gear", Category = "All Platforms" },
            new HltbPlatform() { Name = "Sega Master System", Category = "All Platforms" },
            new HltbPlatform() { Name = "Sega Mega Drive/Genesis", Category = "All Platforms" },
            new HltbPlatform() { Name = "Sega Saturn", Category = "All Platforms" },
            new HltbPlatform() { Name = "SG-1000", Category = "All Platforms" },
            new HltbPlatform() { Name = "Sharp X68000", Category = "All Platforms" },
            new HltbPlatform() { Name = "Super Nintendo", Category = "All Platforms" },
            new HltbPlatform() { Name = "Tiger Handheld", Category = "All Platforms" },
            new HltbPlatform() { Name = "TurboGrafx-16", Category = "All Platforms" },
            new HltbPlatform() { Name = "TurboGrafx-CD", Category = "All Platforms" },
            new HltbPlatform() { Name = "Virtual Boy", Category = "All Platforms" },
            new HltbPlatform() { Name = "Wii", Category = "All Platforms" },
            new HltbPlatform() { Name = "Wii U", Category = "All Platforms" },
            new HltbPlatform() { Name = "Windows Phone", Category = "All Platforms" },
            new HltbPlatform() { Name = "WonderSwan", Category = "All Platforms" },
            new HltbPlatform() { Name = "Xbox", Category = "All Platforms" },
            new HltbPlatform() { Name = "Xbox 360", Category = "All Platforms" },
            new HltbPlatform() { Name = "Xbox One", Category = "All Platforms" },
            new HltbPlatform() { Name = "Xbox Series X/S", Category = "All Platforms" },
            new HltbPlatform() { Name = "ZX Spectrum", Category = "All Platforms" }
        };


        public HowLongToBeatDatabase(IPlayniteAPI PlayniteApi, HowLongToBeatSettingsViewModel PluginSettings, string PluginUserDataPath) : base(PlayniteApi, PluginSettings, "HowLongToBeat", PluginUserDataPath)
        {
            
        }


        public void InitializeClient(HowLongToBeat Plugin)
        {
            this.Plugin = Plugin;
            howLongToBeatClient = new HowLongToBeatClient(Plugin, PlayniteApi, PluginSettings.Settings);
        }


        protected override bool LoadDatabase()
        {
            Database = new GameHowLongToBeatCollection(Paths.PluginDatabasePath);
            Database.SetGameInfo<HltbDataUser>(PlayniteApi);

            Database.UserHltbData = howLongToBeatClient.LoadUserData();

            return true;
        }


        public override void GetSelectData()
        {
            var View = new OptionsDownloadData(PlayniteApi);
            Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PlayniteApi, PluginName + " - " + resources.GetString("LOCCommonSelectData"), View);
            windowExtension.ShowDialog();

            var PlayniteDb = View.GetFilteredGames();
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
                $"{PluginName} - {resources.GetString("LOCCommonGettingData")}",
                true
            );
            globalProgressOptions.IsIndeterminate = false;

            PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
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
                        GameHowLongToBeat gameHowLongToBeat = Get(game, true);

                        List<HltbDataUser> dataSearch = HowLongToBeat.PluginDatabase.howLongToBeatClient.Search(game.Name);

                        if (dataSearch.Count == 1 && PluginSettings.Settings.AutoAccept)
                        {
                            gameHowLongToBeat.Items = new List<HltbDataUser>() { dataSearch.First() };
                            AddOrUpdate(gameHowLongToBeat);
                        }
                        else
                        {
                            if (dataSearch.Count > 0 && PluginSettings.Settings.ShowWhenMismatch)
                            {
                                Get(game, false, true);
                            }
                        }

                        activateGlobalProgress.CurrentProgressValue++;
                    }

                    stopWatch.Stop();
                    TimeSpan ts = stopWatch.Elapsed;
                    logger.Info($"Task GetSelectData(){CancelText} - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {activateGlobalProgress.CurrentProgressValue}/{(double)PlayniteDb.Count()} items");
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false);
                }
            }, globalProgressOptions);
        }


        public override GameHowLongToBeat Get(Guid Id, bool OnlyCache = false, bool Force = false)
        {
            GameHowLongToBeat gameHowLongToBeat = GetOnlyCache(Id);

            if ((gameHowLongToBeat == null && !OnlyCache) || Force)
            {
                gameHowLongToBeat = howLongToBeatClient.SearchData(PlayniteApi.Database.Games.Get(Id));

                if (gameHowLongToBeat != null)
                {
                    AddOrUpdate(gameHowLongToBeat);
                }
            }

            if (gameHowLongToBeat == null)
            {
                Game game = PlayniteApi.Database.Games.Get(Id);
                if (game != null)
                {
                    gameHowLongToBeat = GetDefault(game);
                }
            }

            return gameHowLongToBeat;
        }


        public override void Refresh(Guid Id)
        {
            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                $"{PluginName} - {resources.GetString("LOCCommonProcessing")}",
                false
            );
            globalProgressOptions.IsIndeterminate = true;

            PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                RefreshElement(Id);
            }, globalProgressOptions);
        }

        public void RefreshAll()
        {
            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                $"{PluginName} - {resources.GetString("LOCCommonProcessing")}",
                true
            );
            globalProgressOptions.IsIndeterminate = false;

            PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                string CancelText = string.Empty;

                var db = Database.Where(x => x.HasData);
                activateGlobalProgress.ProgressMaxValue = (double)db.Count();

                foreach (var item in db)
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
                logger.Info($"Task RefreshAll(){CancelText} - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {activateGlobalProgress.CurrentProgressValue}/{(double)db.Count()} items");
            }, globalProgressOptions);
        }

        private void RefreshElement(Guid Id)
        {
            var loadedItem = Get(Id, true);
            List<HltbDataUser> dataSearch = HowLongToBeat.PluginDatabase.howLongToBeatClient.Search(loadedItem.GetData().Name);

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


        protected override void GetPluginTags()
        {
            try
            {
                // Get tags in playnite database
                PluginTags = new List<Tag>();

                foreach (Tag tag in PlayniteApi.Database.Tags)
                {
                    if (tag.Name?.IndexOf("[HLTB] ") > -1)
                    {
                        PluginTags.Add(tag);
                    }
                }

                // Add missing tags
                if (PluginTags.Count < 13)
                {
                    if (PluginTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCCommon0to5")}") == null)
                    {
                        PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCCommon0to5")}" });
                    }
                    if (PluginTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCCommon1to5")}") == null)
                    {
                        PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCCommon1to5")}" });
                    }
                    if (PluginTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCCommon5to10")}") == null)
                    {
                        PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCCommon5to10")}" });
                    }
                    if (PluginTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCCommon10to20")}") == null)
                    {
                        PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCCommon10to20")}" });
                    }
                    if (PluginTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCCommon20to30")}") == null)
                    {
                        PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCCommon20to30")}" });
                    }
                    if (PluginTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCCommon30to40")}") == null)
                    {
                        PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCCommon30to40")}" });
                    }
                    if (PluginTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCCommon40to50")}") == null)
                    {
                        PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCCommon40to50")}" });
                    }
                    if (PluginTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCCommon50to60")}") == null)
                    {
                        PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCCommon50to60")}" });
                    }
                    if (PluginTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCCommon60to70")}") == null)
                    {
                        PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCCommon60to70")}" });
                    }
                    if (PluginTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCCommon70to80")}") == null)
                    {
                        PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCCommon70to80")}" });
                    }
                    if (PluginTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCCommon80to90")}") == null)
                    {
                        PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCCommon80to90")}" });
                    }
                    if (PluginTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCCommon90to100")}") == null)
                    {
                        PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCCommon90to100")}" });
                    }
                    if (PluginTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCCommon100plus")}") == null)
                    {
                        PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCCommon100plus")}" });
                    }

                    foreach (Tag tag in PlayniteApi.Database.Tags)
                    {
                        if (tag.Name.IndexOf("[HLTB] ") > -1)
                        {
                            PluginTags.Add(tag);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }
        }

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
                                PlayniteApi.Database.Games.Update(game);
                                game.OnPropertyChanged();
                            }, DispatcherPriority.Send);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, true);
                    logger.Error($"Tag insert error with {game.Name}");
                    PlayniteApi.Notifications.Add(new NotificationMessage(
                        $"{PluginName}-Tag-Errors",
                        $"{PluginName}\r\n" + resources.GetString("LOCCommonNotificationTagError"),
                        NotificationType.Error
                    ));
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
                    return (PluginTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCPLaytimeLessThenAnHour")}")).Id;
                }
                if (hltbTime < 18000)
                {
                    return (PluginTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat1to5")}")).Id;
                }
                if (hltbTime < 36000)
                {
                    return (PluginTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat5to10")}")).Id;
                }
                if (hltbTime < 72000)
                {
                    return (PluginTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat10to20")}")).Id;
                }
                if (hltbTime < 108000)
                {
                    return (PluginTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat20to30")}")).Id;
                }
                if (hltbTime < 144000)
                {
                    return (PluginTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat30to40")}")).Id;
                }
                if (hltbTime < 180000)
                {
                    return (PluginTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat40to50")}")).Id;
                }
                if (hltbTime < 216000)
                {
                    return (PluginTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat50to60")}")).Id;
                }
                if (hltbTime < 252000)
                {
                    return (PluginTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat60to70")}")).Id;
                }
                if (hltbTime < 288000)
                {
                    return (PluginTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat70to80")}")).Id;
                }
                if (hltbTime < 324000)
                {
                    return (PluginTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat80to90")}")).Id;
                }
                if (hltbTime < 360000)
                {
                    return (PluginTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat90to100")}")).Id;
                }
                if (hltbTime >= 360000)
                {
                    return (PluginTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat100plus")}")).Id;
                }
            }

            return null;
        }


        public TitleList GetUserHltbData(int HltbId)
        {
            try
            {
                if (Database.UserHltbData.TitlesList == null || Database.UserHltbData.TitlesList.Count == 0)
                {
                    return null;
                }

                return Database.UserHltbData.TitlesList.Find(x => x.Id == HltbId);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, true);
                logger.Warn($"No HltbData for {HltbId}");
                return null;
            }
        }

        public void RefreshUserData()
        {
            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                $"{PluginName} - {resources.GetString("LOCHowLongToBeatPluginGetUserView")}",
                false
            );
            globalProgressOptions.IsIndeterminate = true;

            PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                HltbUserStats UserHltbData = howLongToBeatClient.GetUserData();

                if (UserHltbData != null)
                {
                    try
                    {
                        Serialization.ToFile(UserHltbData, Path.Combine(Paths.PluginUserDataPath, "HltbUserStats.json"), Format.Json);
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false);
                    }

                    Database.UserHltbData = UserHltbData;
                }
            }, globalProgressOptions);
        }

        public void RefreshUserDataTask()
        {
            Task.Run(() =>
            {
                HltbUserStats UserHltbData = howLongToBeatClient.GetUserData();

                if (UserHltbData != null)
                {
                    try
                    {
                        Serialization.ToFile(UserHltbData, Path.Combine(Paths.PluginUserDataPath, "HltbUserStats.json"), Format.Json);
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false);
                    }

                    Database.UserHltbData = UserHltbData;
                }
            });
        }

        public void RefreshUserData(int game_id)
        {
            Task.Run(() => 
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

                    try
                    {
                        Serialization.ToFile(Database.UserHltbData, Path.Combine(Paths.PluginUserDataPath, "HltbUserStats.json"), Format.Json);
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false);
                    }
                }
            });
        }


        public void SetCurrentPlayTime(Game game, long elapsedSeconds)
        {
            try
            {
                if (howLongToBeatClient.GetIsUserLoggedIn())
                {
                    GameHowLongToBeat gameHowLongToBeat = Database.Get(game.Id);

                    if (gameHowLongToBeat != null)
                    {
                        TimeSpan time = TimeSpan.FromSeconds(game.Playtime + elapsedSeconds);

                        var platform = hltbPlatforms.FindAll(x => game.Platform.Name.ToLower().Contains(x.Name.ToLower())).First();

                        if (platform != null)
                        {
                            string Platform = platform.Name;

                            string StorefrontName = string.Empty;
                            Storefront storefront = PluginSettings.Settings.Storefronts.Where(x => x.SourceId == game.SourceId).FirstOrDefault();
                            if (storefront != null)
                            {
                                StorefrontName = storefront.HltbStorefrontName;
                            }

                            var HltbData = GetUserHltbData(gameHowLongToBeat.GetData().Id);
                            int edit_id = 0;
                            HltbPostData hltbPostData = new HltbPostData();
                            if (HltbData != null)
                            {
                                if (howLongToBeatClient.EditIdExist(HltbData.UserGameId))
                                {
                                    edit_id = int.Parse(HltbData.UserGameId);
                                    hltbPostData = howLongToBeatClient.GetSubmitData(edit_id.ToString());
                                }
                            }
                            else
                            {
                                if (HltbData != null)
                                {
                                    string tmpEditId = howLongToBeatClient.FindIdExisting(edit_id.ToString());
                                    if (!tmpEditId.IsNullOrEmpty())
                                    {
                                        edit_id = int.Parse(tmpEditId);
                                        hltbPostData = howLongToBeatClient.GetSubmitData(tmpEditId);
                                    }
                                }
                            }

                            if (hltbPostData == null)
                            {
                                logger.Warn($"No hltbPostData for {game.Name}");
                                return;
                            }

                            hltbPostData.user_id = Database.UserHltbData.UserId;
                            hltbPostData.edit_id = edit_id;
                            hltbPostData.game_id = gameHowLongToBeat.GetData().Id;
                            hltbPostData.custom_title = gameHowLongToBeat.GetData().Name;
                            hltbPostData.platform = Platform;
                            hltbPostData.storefront = StorefrontName;

                            hltbPostData.list_p = "1";

                            hltbPostData.protime_h = (time.Hours + (24 * time.Days)).ToString();
                            hltbPostData.protime_m = time.Minutes.ToString();
                            hltbPostData.protime_s = time.Seconds.ToString();


                            howLongToBeatClient.PostData(hltbPostData);
                        }
                        else
                        {
                            logger.Warn($"No platform find for {game.Name}");
                        }
                    }
                }
                else
                {
                    PlayniteApi.Notifications.Add(new NotificationMessage(
                        "HowLongToBeat-Import-Error",
                        "HowLongToBeat" + System.Environment.NewLine +
                        resources.GetString("LOCCommonNotLoggedIn"),
                        NotificationType.Error,
                        () => Plugin.OpenSettingsView()));
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                PlayniteApi.Notifications.Add(new NotificationMessage(
                    "HowLongToBeat-Import-Error",
                    "HowLongToBeat" + System.Environment.NewLine +
                    ex.Message,
                    NotificationType.Error,
                    () => Plugin.OpenSettingsView()));
            }
        }


        public override void SetThemesResources(Game game)
        {
            GameHowLongToBeat gameHowLongToBeat = Get(game, true);

            if (gameHowLongToBeat == null)
            {
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

        public override void Games_ItemUpdated(object sender, ItemUpdatedEventArgs<Game> e)
        {
            foreach (var GameUpdated in e.UpdatedItems)
            {
                Database.SetGameInfo<HltbDataUser>(PlayniteApi, GameUpdated.NewData.Id);
            }
        }
    }
}
