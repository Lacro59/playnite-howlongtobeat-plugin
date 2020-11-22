using HowLongToBeat.Models;
using HowLongToBeat.Views;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginCommon;
using PluginCommon.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace HowLongToBeat.Services
{
    public class HowLongToBeatDatabase : PluginDatabaseObject
    {
        private GameHowLongToBeatCollection db;

        private GameHowLongToBeat _GameSelectedData = new GameHowLongToBeat();
        public GameHowLongToBeat GameSelectedData
        {
            get
            {
                return _GameSelectedData;
            }

            set
            {
                _GameSelectedData = value;
                OnPropertyChanged();
            }
        }

        public HowLongToBeatClient howLongToBeatClient;


        public HowLongToBeatDatabase(IPlayniteAPI PlayniteApi, HowLongToBeatSettings PluginSettings, string PluginUserDataPath) : base(PlayniteApi, PluginSettings, PluginUserDataPath)
        {
            PluginName = "HowLongToBeat";

            ControlAndCreateDirectory(PluginUserDataPath, "HowLongToBeat");

            howLongToBeatClient = new HowLongToBeatClient(PlayniteApi);
        }


        protected override bool LoadDatabase()
        {
            IsLoaded = false;
            db = new GameHowLongToBeatCollection(PluginDatabaseDirectory);

            db.SetGameInfo<HltbDataUser>(_PlayniteApi);
#if DEBUG
            logger.Debug($"{PluginName} - db: {JsonConvert.SerializeObject(db)}");
#endif

            IsLoaded = true;
            return true;
        }


        public GameHowLongToBeat Get(Guid Id, bool OnlyCache = false)
        {
            GameIsLoaded = false;
            GameHowLongToBeat gameHowLongToBeat = db.Get(Id);
#if DEBUG
            logger.Debug($"{PluginName} - GetFromDb({Id.ToString()}) - gameHowLongToBeat: {JsonConvert.SerializeObject(gameHowLongToBeat)}");
#endif
            if (gameHowLongToBeat == null && !OnlyCache)
            {
                ControlAndCreateDirectory(PluginUserDataPath, "HowLongToBeat");

                gameHowLongToBeat = howLongToBeatClient.SearchData(_PlayniteApi.Database.Games.Get(Id));

                if (gameHowLongToBeat != null)
                {
#if DEBUG
                    logger.Debug($"{PluginName} - GetFromWeb({Id.ToString()}) - gameHowLongToBeat: {JsonConvert.SerializeObject(gameHowLongToBeat)}");
#endif
                    Add(gameHowLongToBeat);
                }
            }

            if (gameHowLongToBeat == null)
            {
                Game game = _PlayniteApi.Database.Games.Get(Id);

                gameHowLongToBeat = new GameHowLongToBeat
                {
                    Id = game.Id,
                    Name = game.Name,
                    Hidden = game.Hidden,
                    Icon = game.Icon,
                    CoverImage = game.CoverImage,
                    GenreIds = game.GenreIds,
                    Genres = game.Genres,
                    Playtime = game.Playtime
                };
            }

            GameIsLoaded = true;
            return gameHowLongToBeat;
        }

        public GameHowLongToBeat Get(Game game, bool OnlyCache = false)
        {
            return Get(game.Id, OnlyCache);
        }

        public void Add(GameHowLongToBeat itemToAdd)
        {
            db.Add(itemToAdd);
        }

        public void Update(GameHowLongToBeat itemToUpdate)
        {
            db.Update(itemToUpdate);
        }

        public bool Remove(Guid Id)
        {
            return db.Remove(Id);
        }


        public void GetAllDataFromMain()
        {
            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                            resources.GetString("LOCCommonGettingAllDatas"),
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

                        GameHowLongToBeat gameHowLongToBeat = Get(game, true);

                        if (!gameHowLongToBeat.HasData && !gameHowLongToBeat.IsSaved)
                        {
                            List<HltbData> dataSearch = howLongToBeatClient.Search(game.Name);

                            if (dataSearch.Count == 1 && ((HowLongToBeatSettings)PluginSettings).AutoAccept)
                            {
                                gameHowLongToBeat = new GameHowLongToBeat
                                {
                                    Items = new List<HltbDataUser>() {
                                    new HltbDataUser
                                    {
                                        GameHltbData = dataSearch.First()
                                    }
                                }
                                };

                                Thread.Sleep(10);
                                Add(gameHowLongToBeat);

                                if (((HowLongToBeatSettings)PluginSettings).EnableTag)
                                {
                                    AddTag(game);
                                }
                            }
                            else
                            {
                                if (dataSearch.Count > 0 && ((HowLongToBeatSettings)PluginSettings).ShowWhenMismatch)
                                {
                                    Application.Current.Dispatcher.BeginInvoke((Action)delegate
                                    {
                                        Get(game);

                                        if (((HowLongToBeatSettings)PluginSettings).EnableTag)
                                        {
                                            AddTag(game);
                                        }
                                    }).Wait();
                                }
                            }
                        }

                        activateGlobalProgress.CurrentProgressValue++;
                    }

                    stopWatch.Stop();
                    TimeSpan ts = stopWatch.Elapsed;
                    logger.Info($"HowLongToBeat - Task GetAllDataFromMain(){CancelText} - {String.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)}");
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, PluginName);
                }
            }, globalProgressOptions);
        }

        public override bool ClearDatabase()
        {
            if (!base.ClearDatabase())
            {
                return false;
            }

            return LoadDatabase();
        }


        public void AddAllTagFromMain()
        {
            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                resources.GetString("LOCCommonAddingAllTag"),
                true
            );
            globalProgressOptions.IsIndeterminate = false;

            _PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
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
                    AddTag(game);
                    activateGlobalProgress.CurrentProgressValue++;
                }

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                logger.Info($"HowLongToBeat - Task AddAllTagFromMain(){CancelText} - {String.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)}");
            }, globalProgressOptions);
        }

        public void RemoveAllTagFromMain()
        {
            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                resources.GetString("LOCCommonRemovingAllTag"),
                true
            );
            globalProgressOptions.IsIndeterminate = false;

            _PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
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
                    RemoveTag(game);
                    activateGlobalProgress.CurrentProgressValue++;
                }

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                logger.Info($"HowLongToBeat - Task RemoveAllTagFromMain(){CancelText} - {String.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)}");
            }, globalProgressOptions);
        }

        public void AddTag(Game game)
        {
            RemoveTag(game);

            GameHowLongToBeat gameHowLongToBeat = Get(game, true);

            if (gameHowLongToBeat.HasData)
            {
                try
                {
                    HltbDataUser hltbDataUser = gameHowLongToBeat.GetData();
                    List<Guid> tagIds = SetPluginTags(hltbDataUser);

                    if (tagIds.Count > 0)
                    {
                        game.TagIds = tagIds;
                        _PlayniteApi.Database.Games.Update(game);
                    }
                }
                catch (Exception ex)
                {
#if DEBUG
                    Common.LogError(ex, "HowLongToBeat");
#endif
                    logger.Error($"HowLongToBeat - Tag insert error with {game.Name}");
                    _PlayniteApi.Notifications.Add(new NotificationMessage(
                        "HowLongToBeat-Tag-Errors",
                        "HowLongToBeat\r\n" + resources.GetString("LOCCommonNotificationTagError"),
                        NotificationType.Error
                    ));
                }
            }
        }

        public void RemoveTag(Game game)
        {
            try
            {
                List<Tag> HltbTags = GetPluginTags();
                if (game.Tags != null && game.Tags.Count > 0)
                {
                    foreach (Tag tag in HltbTags)
                    {
                        game.TagIds.Remove(tag.Id);
                        _PlayniteApi.Database.Games.Update(game);
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "HowLongToBeat", $"Tag remove error for {game.Name}");
            }
        }

        public void RemoveAllTagInDatabase()
        {
            try
            {
                List<Tag> HltbTags = GetPluginTags();
                foreach (Tag tag in HltbTags)
                {
                    _PlayniteApi.Database.Tags.Remove(tag);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, PluginName);
            }
        }




        private List<Tag> GetPluginTags()
        {
            List<Tag> HltbTags = new List<Tag>();

            try
            {
                // Tags id
                foreach (Tag tag in _PlayniteApi.Database.Tags)
                {
                    if (tag.Name.IndexOf("[HLTB]") > -1)
                    {
                        HltbTags.Add(tag);
                    }
                }

                // Add missing tags in database
                if (HltbTags.Count < 13)
                {
                    if (HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCPLaytimeLessThenAnHour")}") == null)
                    {
                        _PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCPLaytimeLessThenAnHour")}" });
                    }
                    if (HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat1to5")}") == null)
                    {
                        _PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCHowLongToBeat1to5")}" });
                    }
                    if (HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat5to10")}") == null)
                    {
                        _PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCHowLongToBeat5to10")}" });
                    }
                    if (HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat10to20")}") == null)
                    {
                        _PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCHowLongToBeat10to20")}" });
                    }
                    if (HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat20to30")}") == null)
                    {
                        _PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCHowLongToBeat20to30")}" });
                    }
                    if (HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat30to40")}") == null)
                    {
                        _PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCHowLongToBeat30to40")}" });
                    }
                    if (HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat40to50")}") == null)
                    {
                        _PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCHowLongToBeat40to50")}" });
                    }
                    if (HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat50to60")}") == null)
                    {
                        _PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCHowLongToBeat50to60")}" });
                    }
                    if (HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat60to70")}") == null)
                    {
                        _PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCHowLongToBeat60to70")}" });
                    }
                    if (HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat70to80")}") == null)
                    {
                        _PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCHowLongToBeat70to80")}" });
                    }
                    if (HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat80to90")}") == null)
                    {
                        _PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCHowLongToBeat80to90")}" });
                    }
                    if (HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat90to100")}") == null)
                    {
                        _PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCHowLongToBeat90to100")}" });
                    }
                    if (HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat100plus")}") == null)
                    {
                        _PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCHowLongToBeat100plus")}" });
                    }

                    foreach (Tag tag in _PlayniteApi.Database.Tags)
                    {
                        if (tag.Name.IndexOf("[HLTB]") > -1)
                        {
                            HltbTags.Add(tag);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "HowLongToBeat", $"Error on GetTagId()");
            }

            return HltbTags;
        }

        private List<Guid> SetPluginTags(HltbDataUser hltbDataUser)
        {
            List<Tag> HltbTags = GetPluginTags();
            List<Guid> tagIds = new List<Guid>();
            long hltbTime = 0;
#if DEBUG
            logger.Debug($"HowLongToBeat - HltbTags: {JsonConvert.SerializeObject(HltbTags)}");
#endif
            if (hltbDataUser != null && hltbDataUser != new HltbDataUser() && hltbDataUser.GameHltbData != null && hltbDataUser.GameHltbData != new HltbData() && HltbTags.Count > 1 && HltbTags != null)
            {
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
                    bool isFind = false;
                    if (hltbTime < 3600 && !isFind)
                    {
                        isFind = true;
                        tagIds.Add((HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCPLaytimeLessThenAnHour")}")).Id);
                    }
                    if (hltbTime < 18000 && !isFind)
                    {
                        isFind = true;
                        tagIds.Add((HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat1to5")}")).Id);
                    }
                    if (hltbTime < 36000 && !isFind)
                    {
                        isFind = true;
                        tagIds.Add((HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat5to10")}")).Id);
                    }
                    if (hltbTime < 72000 && !isFind)
                    {
                        isFind = true;
                        tagIds.Add((HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat10to20")}")).Id);
                    }
                    if (hltbTime < 108000 && !isFind)
                    {
                        isFind = true;
                        tagIds.Add((HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat20to30")}")).Id);
                    }
                    if (hltbTime < 144000 && !isFind)
                    {
                        isFind = true;
                        tagIds.Add((HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat30to40")}")).Id);
                    }
                    if (hltbTime < 180000 && !isFind)
                    {
                        isFind = true;
                        tagIds.Add((HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat40to50")}")).Id);
                    }
                    if (hltbTime < 216000 && !isFind)
                    {
                        isFind = true;
                        tagIds.Add((HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat50to60")}")).Id);
                    }
                    if (hltbTime < 252000 && !isFind)
                    {
                        isFind = true;
                        tagIds.Add((HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat60to70")}")).Id);
                    }
                    if (hltbTime < 288000 && !isFind)
                    {
                        isFind = true;
                        tagIds.Add((HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat70to80")}")).Id);
                    }
                    if (hltbTime < 324000 && !isFind)
                    {
                        isFind = true;
                        tagIds.Add((HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat80to90")}")).Id);
                    }
                    if (hltbTime < 360000 && !isFind)
                    {
                        isFind = true;
                        tagIds.Add((HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat90to100")}")).Id);
                    }
                    if (hltbTime >= 360000 && !isFind)
                    {
                        isFind = true;
                        tagIds.Add((HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat100plus")}")).Id);
                    }
                }
            }
            return tagIds;
        }



        public void SetCurrent(Guid Id)
        {
            GameSelectedData = Get(Id);
        }

        public void SetCurrent(Game game)
        {
            GameSelectedData = Get(game.Id);
        }

        public void SetCurrent(GameHowLongToBeat gameLocalizations)
        {
            GameSelectedData = gameLocalizations;
        }
    }
}
