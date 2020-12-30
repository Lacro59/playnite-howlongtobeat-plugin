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

namespace HowLongToBeat.Services
{
    public class HowLongToBeatDatabase : PluginDatabaseObject<HowLongToBeatSettings, GameHowLongToBeatCollection, GameHowLongToBeat>
    {
        public HowLongToBeatClient howLongToBeatClient;


        public HowLongToBeatDatabase(HowLongToBeat plugin, IPlayniteAPI PlayniteApi, HowLongToBeatSettings PluginSettings, string PluginUserDataPath) : base(PlayniteApi, PluginSettings, PluginUserDataPath)
        {
            PluginName = "HowLongToBeat";

            ControlAndCreateDirectory(PluginUserDataPath, "HowLongToBeat");

            howLongToBeatClient = new HowLongToBeatClient(plugin, PlayniteApi);
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
            long hltbTime = 0;

            // Get time
            if (hltbDataUser.GameHltbData.MainStory != 0)
            {
                hltbTime = hltbDataUser.GameHltbData.MainStory;
            }
            else if (hltbDataUser.GameHltbData.MainExtra != 0)
            {
                hltbTime = hltbDataUser.GameHltbData.MainStory;
            }
            if (hltbDataUser.GameHltbData.Solo != 0)
            {
                hltbTime = hltbDataUser.GameHltbData.Solo;
            }
            else if (hltbDataUser.GameHltbData.Vs != 0)
            {
                hltbTime = hltbDataUser.GameHltbData.Vs;
            }

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
    }
}
