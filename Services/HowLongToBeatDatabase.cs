using HowLongToBeat.Models;
using HowLongToBeat.Views;
using Newtonsoft.Json;
using Playnite.SDK;
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

namespace HowLongToBeat.Services
{
    public class HowLongToBeatDatabase : PluginDatabaseObject<HowLongToBeatSettings, GameHowLongToBeatCollection, GameHowLongToBeat>
    {
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

        public HowLongToBeatDatabase(HowLongToBeat plugin, IPlayniteAPI PlayniteApi, HowLongToBeatSettings PluginSettings, string PluginUserDataPath) : base(PlayniteApi, PluginSettings, PluginUserDataPath)
        {
            PluginName = "HowLongToBeat";

            ControlAndCreateDirectory(PluginUserDataPath, "HowLongToBeat");

            howLongToBeatClient = new HowLongToBeatClient(plugin, PlayniteApi, PluginSettings);
        }


        protected override bool LoadDatabase()
        {
            IsLoaded = false;
            Database = new GameHowLongToBeatCollection(PluginDatabaseDirectory);

            Database.SetGameInfo<HltbDataUser>(_PlayniteApi);

            Database.UserHltbData = howLongToBeatClient.LoadUserData();

#if DEBUG
            logger.Debug($"{PluginName} [Ignored] - db: {JsonConvert.SerializeObject(Database)}");
#endif

            GameSelectedData = new GameHowLongToBeat();
            GetPluginTags();

            IsLoaded = true;
            return true;
        }


        public override void GetSelectDatas()
        {
            var View = new OptionsDownloadData(_PlayniteApi);
            Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(_PlayniteApi, PluginName + " - " + resources.GetString("LOCCommonSelectData"), View);
            windowExtension.ShowDialog();

            var PlayniteDb = View.GetFilteredGames();

            if (PlayniteDb == null)
            {
                return;
            }

            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                $"{PluginName} - {resources.GetString("LOCCommonGettingData")}",
                true
            );
            globalProgressOptions.IsIndeterminate = false;

            _PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
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

#if DEBUG
                        logger.Debug($"{PluginName} [Ignored] - {gameHowLongToBeat.Name} - {gameHowLongToBeat.HasData} - {gameHowLongToBeat.IsSaved}");
#endif

                        if (!gameHowLongToBeat.HasData && !gameHowLongToBeat.IsSaved)
                        {
                            List<HltbDataUser> dataSearch = HowLongToBeat.PluginDatabase.howLongToBeatClient.Search(game.Name);

                            if (dataSearch.Count == 1 && PluginSettings.AutoAccept)
                            {
                                gameHowLongToBeat.Items = new List<HltbDataUser>() { dataSearch.First() };

                                HowLongToBeat.PluginDatabase.Add(gameHowLongToBeat);
                            }
                            else
                            {
                                if (dataSearch.Count > 0 && PluginSettings.ShowWhenMismatch)
                                {
                                    HowLongToBeat.PluginDatabase.Get(game);
                                }
                            }
                        }

                        activateGlobalProgress.CurrentProgressValue++;
                    }

                    stopWatch.Stop();
                    TimeSpan ts = stopWatch.Elapsed;
                    logger.Info($"{PluginName} - Task GetDatas(){CancelText} - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {activateGlobalProgress.CurrentProgressValue}/{(double)PlayniteDb.Count()} items");
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, PluginName);
                }
            }, globalProgressOptions);
        }

        public override void GetAllDatas()
        {
            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                $"{PluginName} - {resources.GetString("LOCCommonGettingAllDatas")}",
                true
            );
            globalProgressOptions.IsIndeterminate = false;

            _PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                try
                {
                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();

                    var PlayniteDb = _PlayniteApi.Database.Games.Where(x => x.Hidden == false);
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

#if DEBUG
                        logger.Debug($"{PluginName} [Ignored] - {gameHowLongToBeat.Name} - {gameHowLongToBeat.HasData} - {gameHowLongToBeat.IsSaved}");
#endif

                        if (!gameHowLongToBeat.HasData && !gameHowLongToBeat.IsSaved)
                        {
                            List<HltbDataUser> dataSearch = HowLongToBeat.PluginDatabase.howLongToBeatClient.Search(game.Name);

                            if (dataSearch.Count == 1 && PluginSettings.AutoAccept)
                            {
                                gameHowLongToBeat.Items = new List<HltbDataUser>() { dataSearch.First() };

                                HowLongToBeat.PluginDatabase.Add(gameHowLongToBeat);
                            }
                            else
                            {
                                if (dataSearch.Count > 0 && PluginSettings.ShowWhenMismatch)
                                {
                                    HowLongToBeat.PluginDatabase.Get(game);
                                }
                            }
                        }
                        
                        activateGlobalProgress.CurrentProgressValue++;
                    }

                    stopWatch.Stop();
                    TimeSpan ts = stopWatch.Elapsed;
                    logger.Info($"{PluginName} - Task GetAllDatas(){CancelText} - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {activateGlobalProgress.CurrentProgressValue}/{(double)PlayniteDb.Count()} items");
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, PluginName);
                }
            }, globalProgressOptions);
        }


        public override GameHowLongToBeat Get(Guid Id, bool OnlyCache = false)
        {
            GameIsLoaded = false;
            GameHowLongToBeat gameHowLongToBeat = GetOnlyCache(Id);
#if DEBUG
            logger.Debug($"{PluginName} [Ignored] - GetFromDb({Id.ToString()}) - gameHowLongToBeat: {JsonConvert.SerializeObject(gameHowLongToBeat)}");
#endif
            if (gameHowLongToBeat == null && !OnlyCache)
            {
                ControlAndCreateDirectory(PluginUserDataPath, "HowLongToBeat");

                gameHowLongToBeat = howLongToBeatClient.SearchData(_PlayniteApi.Database.Games.Get(Id));

                if (gameHowLongToBeat != null)
                {
#if DEBUG
                    logger.Debug($"{PluginName} [Ignored] - GetFromWeb({Id.ToString()}) - gameHowLongToBeat: {JsonConvert.SerializeObject(gameHowLongToBeat)}");
#endif
                    Add(gameHowLongToBeat);
                }
            }

            if (gameHowLongToBeat == null)
            {
                Game game = _PlayniteApi.Database.Games.Get(Id);
                gameHowLongToBeat = GetDefault(game);
            }

            GameIsLoaded = true;
            return gameHowLongToBeat;
        }


        protected override void GetPluginTags()
        {
#if DEBUG
            logger.Debug($"{PluginName} [Ignored] - GetPluginTags()");
#endif

            try
            {
                // Get tags in playnite database
                PluginTags = new List<Tag>();
                foreach (Tag tag in _PlayniteApi.Database.Tags)
                {
                    if (tag.Name.IndexOf("[HLTB] ") > -1)
                    {
                        PluginTags.Add(tag);
                    }
                }

                // Add missing tags
                if (PluginTags.Count < 13)
                {
                    if (PluginTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCPLaytimeLessThenAnHour")}") == null)
                    {
                        _PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCPLaytimeLessThenAnHour")}" });
                    }
                    if (PluginTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat1to5")}") == null)
                    {
                        _PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCHowLongToBeat1to5")}" });
                    }
                    if (PluginTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat5to10")}") == null)
                    {
                        _PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCHowLongToBeat5to10")}" });
                    }
                    if (PluginTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat10to20")}") == null)
                    {
                        _PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCHowLongToBeat10to20")}" });
                    }
                    if (PluginTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat20to30")}") == null)
                    {
                        _PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCHowLongToBeat20to30")}" });
                    }
                    if (PluginTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat30to40")}") == null)
                    {
                        _PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCHowLongToBeat30to40")}" });
                    }
                    if (PluginTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat40to50")}") == null)
                    {
                        _PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCHowLongToBeat40to50")}" });
                    }
                    if (PluginTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat50to60")}") == null)
                    {
                        _PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCHowLongToBeat50to60")}" });
                    }
                    if (PluginTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat60to70")}") == null)
                    {
                        _PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCHowLongToBeat60to70")}" });
                    }
                    if (PluginTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat70to80")}") == null)
                    {
                        _PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCHowLongToBeat70to80")}" });
                    }
                    if (PluginTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat80to90")}") == null)
                    {
                        _PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCHowLongToBeat80to90")}" });
                    }
                    if (PluginTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat90to100")}") == null)
                    {
                        _PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCHowLongToBeat90to100")}" });
                    }
                    if (PluginTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat100plus")}") == null)
                    {
                        _PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCHowLongToBeat100plus")}" });
                    }

                    foreach (Tag tag in _PlayniteApi.Database.Tags)
                    {
                        if (tag.Name.IndexOf("[HLTB] ") > -1)
                        {
                            PluginTags.Add(tag);
                        }
                    }
                }

#if DEBUG
                logger.Debug($"{PluginName} [Ignored] - PluginTags: {JsonConvert.SerializeObject(PluginTags)}");
#endif
            }
            catch (Exception ex)
            {
                Common.LogError(ex, PluginName);
            }
        }

        public override void AddTag(Game game)
        {
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

                        _PlayniteApi.Database.Games.Update(game);
                    }
                }
                catch (Exception ex)
                {
#if DEBUG
                    Common.LogError(ex, PluginName + " [Ignored]");
#endif
                    logger.Error($"{PluginName} - Tag insert error with {game.Name}");
                    _PlayniteApi.Notifications.Add(new NotificationMessage(
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
            if (Database.UserHltbData.TitlesList == null || Database.UserHltbData.TitlesList.Count == 0)
            {
                return null;
            }

            return Database.UserHltbData.TitlesList.Find(x => x.Id == HltbId);
        }

        public void RefreshUserData()
        {
            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                $"{PluginName} - {resources.GetString("LOCHowLongToBeatPluginGetUserView")}",
                false
            );
            globalProgressOptions.IsIndeterminate = true;

            _PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                HltbUserStats UserHltbData = howLongToBeatClient.GetUserData();

                if (UserHltbData != null)
                {
                    try
                    {
                        string PathHltbUserStats = Path.Combine(PluginUserDataPath, "HltbUserStats.json");
                        File.WriteAllText(PathHltbUserStats, JsonConvert.SerializeObject(UserHltbData));
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, "HowLongToBeat");
                    }

                    Database.UserHltbData = UserHltbData;
                }
            }, globalProgressOptions);
        }


        public void SetCurrentPlayTime(Game game)
        {
            GameHowLongToBeat gameHowLongToBeat = Database.Get(game.Id);

            if (gameHowLongToBeat != null)
            {
                TimeSpan time = TimeSpan.FromSeconds(game.Playtime);

                var platform = hltbPlatforms.FindAll(x => game.Platform.Name.ToLower().Contains(x.Name.ToLower())).First();

                if (platform != null)
                {
                    string Platform = platform.Name;

                    HltbPostData hltbPostData = new HltbPostData
                    {
                        user_id = Database.UserHltbData.UserId,
                        game_id = gameHowLongToBeat.GetData().Id,
                        custom_title = gameHowLongToBeat.GetData().Name,
                        platform = Platform,

                        list_p = "1",

                        protime_h = time.Hours.ToString(),
                        protime_m = time.Minutes.ToString(),
                        protime_s = time.Seconds.ToString(),
                    };

                    howLongToBeatClient.PostData(hltbPostData);
                }
                else
                {
                    logger.Warn($"HowLongToBeat - No platform find for {game.Name}");
                }
            }
        }
    }
}
