using HowLongToBeat.Models;
using HowLongToBeat.Views;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginCommon;
using PluginCommon.PlayniteResources;
using PluginCommon.PlayniteResources.API;
using PluginCommon.PlayniteResources.Common;
using PluginCommon.PlayniteResources.Converters;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Threading.Tasks;

namespace HowLongToBeat.Services
{
    public class HowLongToBeatData
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        private IPlayniteAPI _PlayniteApi { get; set; }
        private Game _game { get; set; }

        private HltbDataUser hltbDataUser { get; set; } = null;
        public bool hasData = false;
        public bool isEmpty = true;

        private string FileGameData { get; set; }


        public HowLongToBeatData(Game game, string PluginUserDataPath, IPlayniteAPI PlayniteApi, bool mustFind = true)
        {
            _PlayniteApi = PlayniteApi;
            _game = game;

            string PluginDatabasePath = PluginUserDataPath + "\\howlongtobeat";

            if (!Directory.Exists(PluginDatabasePath))
            {
                Directory.CreateDirectory(PluginDatabasePath);
            }

            FileGameData = PluginDatabasePath + "\\" + game.Id.ToString() + ".json";

            // Load data
            if (File.Exists(FileGameData))
            {
#if DEBUG
                logger.Debug($"HowLongToBeat - Load data for {game.Name}");
#endif
                hltbDataUser = JsonConvert.DeserializeObject<HltbDataUser>(File.ReadAllText(FileGameData));
            }
            else
            {
                // Search data
                if (mustFind)
                {
                    SearchData(game);
                }
            }

            if (hltbDataUser != null && hltbDataUser.GameHltbData != null)
            {
                hasData = true;

                if (hltbDataUser.GameHltbData.MainStory != 0 || hltbDataUser.GameHltbData.MainExtra != 0 || hltbDataUser.GameHltbData.Completionist != 0 ||
                    hltbDataUser.GameHltbData.Solo != 0 || hltbDataUser.GameHltbData.CoOp != 0 || hltbDataUser.GameHltbData.Vs != 0)
                {
                    isEmpty = false;
                }
            }
        }

        public void SearchData(Game game)
        {
#if DEBUG
            logger.Debug($"HowLongToBeat - Search data for {game.Name}");
#endif

            var ViewExtension = new HowLongToBeatSelect(null, FileGameData, game.Name);
            Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(_PlayniteApi, resources.GetString("LOCSelection"), ViewExtension);
            windowExtension.ShowDialog();

            if (File.Exists(FileGameData))
            {
                hltbDataUser = JsonConvert.DeserializeObject<HltbDataUser>(File.ReadAllText(FileGameData));
            }
            else
            {
                hltbDataUser = null;
            }

            if (hltbDataUser != null && hltbDataUser.GameHltbData != null)
            {
                hasData = true;

                if (hltbDataUser.GameHltbData.MainStory != 0 || hltbDataUser.GameHltbData.MainExtra != 0 || hltbDataUser.GameHltbData.Completionist != 0 ||
                    hltbDataUser.GameHltbData.Solo != 0 || hltbDataUser.GameHltbData.CoOp != 0 || hltbDataUser.GameHltbData.Vs != 0)
                {
                    isEmpty = false;
                    HowLongToBeat.howLongToBeatUI.RefreshElements(HowLongToBeat.GameSelected);
                }
            }
        }


        public void AddTag()
        {
            RemoveTag();

            try
            {
                List<Tag> HltbTags = GetTagId(_PlayniteApi);

                if (hltbDataUser != null && hltbDataUser != new HltbDataUser())
                {
                    List<Guid> tagIds = SetListHltbTag(_PlayniteApi, hltbDataUser);

                    if (tagIds.Count > 0)
                    {
                        _game.TagIds = tagIds;
                    }

                    _PlayniteApi.Database.Games.Update(_game);
                }
            }
            catch
            {
                logger.Error($"HowLongToBeat - Tag insert error with {_game.Name}");
                _PlayniteApi.Notifications.Add(new NotificationMessage(
                    "HowLongToBeat-Tag-Errors",
                    "HowLongToBeat\r\n" + resources.GetString("LOCCommonNotificationTagError"),
                    NotificationType.Error
                ));
            }
        }

        public void RemoveTag()
        {
            try
            {
                List<Tag> HltbTags = GetTagId(_PlayniteApi);
                foreach (Tag tag in HltbTags)
                {
                    if (_game.TagIds != null && _game.TagIds.Count > 0)
                    {
                        _game.TagIds.Remove(tag.Id);
                        _PlayniteApi.Database.Games.Update(_game);
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "HowLongToBeat", $"Tag remove error with {_game.Name}");
            }
        }


        public HltbDataUser GetData()
        {
            return hltbDataUser;
        }

        public void RemoveData()
        {
            if (File.Exists(FileGameData))
            {
                File.Delete(FileGameData);

                hltbDataUser = null;
                hasData = false;
                isEmpty = true;

                var TaskIntegrationUI = Task.Run(() =>
                {
                    HowLongToBeat.howLongToBeatUI.RefreshElements(HowLongToBeat.GameSelected);
                });
            }
            else
            {
                logger.Warn($"HowLongToBeat - Impossible to remove {_game.Name} in {FileGameData}");
            }
        }


        private static List<Tag> GetTagId(IPlayniteAPI PlayniteApi)
        {
            List<Tag> HltbTags = new List<Tag>();

            try
            {
                // Tags id
                foreach (Tag tag in PlayniteApi.Database.Tags)
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
                        PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCPLaytimeLessThenAnHour")}" });
                    }
                    if (HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat1to5")}") == null)
                    {
                        PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCHowLongToBeat1to5")}" });
                    }
                    if (HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat5to10")}") == null)
                    {
                        PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCHowLongToBeat5to10")}" });
                    }
                    if (HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat10to20")}") == null)
                    {
                        PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCHowLongToBeat10to20")}" });
                    }
                    if (HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat20to30")}") == null)
                    {
                        PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCHowLongToBeat20to30")}" });
                    }
                    if (HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat30to40")}") == null)
                    {
                        PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCHowLongToBeat30to40")}" });
                    }
                    if (HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat40to50")}") == null)
                    {
                        PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCHowLongToBeat40to50")}" });
                    }
                    if (HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat50to60")}") == null)
                    {
                        PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCHowLongToBeat50to60")}" });
                    }
                    if (HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat60to70")}") == null)
                    {
                        PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCHowLongToBeat60to70")}" });
                    }
                    if (HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat70to80")}") == null)
                    {
                        PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCHowLongToBeat70to80")}" });
                    }
                    if (HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat80to90")}") == null)
                    {
                        PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCHowLongToBeat80to90")}" });
                    }
                    if (HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat90to100")}") == null)
                    {
                        PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCHowLongToBeat90to100")}" });
                    }
                    if (HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat100plus")}") == null)
                    {
                        PlayniteApi.Database.Tags.Add(new Tag { Name = $"[HLTB] {resources.GetString("LOCHowLongToBeat100plus")}" });
                    }

                    foreach (Tag tag in PlayniteApi.Database.Tags)
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

        private static List<Guid> SetListHltbTag(IPlayniteAPI PlayniteApi, HltbDataUser data)
        {
            List<Tag> HltbTags = GetTagId(PlayniteApi);
            List<Guid> tagIds = new List<Guid>();
            long hltbTime = 0;
#if DEBUG
            logger.Debug($"HowLongToBeat - HltbTags: {JsonConvert.SerializeObject(HltbTags)}");
#endif
            if (data != null && data != new HltbDataUser() && data.GameHltbData != null && data.GameHltbData != new HltbData() && HltbTags.Count > 1 && HltbTags != null)
            {
                // Get time
                if (data.GameHltbData.MainStory != 0)
                {
                    hltbTime = data.GameHltbData.MainStory;
                }
                else if (data.GameHltbData.MainExtra != 0)
                {
                    hltbTime = data.GameHltbData.MainStory;
                }
                if (data.GameHltbData.Solo != 0)
                {
                    hltbTime = data.GameHltbData.Solo;
                }
                else if (data.GameHltbData.Vs != 0)
                {
                    hltbTime = data.GameHltbData.Vs;
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


        public static void AddAllTag(IPlayniteAPI PlayniteApi, Game game, string PluginUserDataPath)
        {
            RemoveAllTag(PlayniteApi, game);

            if (HaveData(game.Id, PluginUserDataPath))
            {
                try { 
                    HowLongToBeatData howLongToBeatData = new HowLongToBeatData(game, PluginUserDataPath, PlayniteApi, false);
                    HltbDataUser data = howLongToBeatData.GetData();
                    List<Guid> tagIds = SetListHltbTag(PlayniteApi, data);

                    if (tagIds.Count > 0)
                    {
                        game.TagIds = tagIds;
                        PlayniteApi.Database.Games.Update(game);
                    }
                }
                catch (Exception ex)
                {
#if DEBUG
                    Common.LogError(ex, "HowLongToBeat", $"Error on AddAllTag()");
#endif
                    logger.Error($"HowLongToBeat - Tag insert error with {game.Name}");
                    PlayniteApi.Notifications.Add(new NotificationMessage(
                        "HowLongToBeat-Tag-Errors",
                        "HowLongToBeat\r\n" + resources.GetString("LOCCommonNotificationTagError"),
                        NotificationType.Error
                    ));
                }
            }
        }

        public static void RemoveAllTag(IPlayniteAPI PlayniteApi, Game game)
        {
            try
            {
                List<Tag> HltbTags = GetTagId(PlayniteApi);
                if (game.Tags != null && game.Tags.Count > 0)
                {
                    foreach (Tag tag in HltbTags)
                    {
                        game.TagIds.Remove(tag.Id);
                        PlayniteApi.Database.Games.Update(game);
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "HowLongToBeat", $"Tag remove error with {game.Name}");
            }
        }

        public static void RemoveAllTagDb(IPlayniteAPI PlayniteApi)
        {
            try
            {
                List<Tag> HltbTags = GetTagId(PlayniteApi);
                foreach (Tag tag in HltbTags)
                {
                    PlayniteApi.Database.Tags.Remove(tag);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "HowLongToBeat", $"Error on RemoveAllTagDb()");
            }
        }

        public static bool HaveData(Guid GameId, string PluginUserDataPath)
        {
            string FileGameData = PluginUserDataPath + "\\howlongtobeat\\" + GameId.ToString() + ".json";
            return File.Exists(FileGameData);
        }

        public static void SaveData(Guid GameId, HltbData hltbData, string PluginUserDataPath)
        {
            string FileGameData = PluginUserDataPath + "\\howlongtobeat\\" + GameId.ToString() + ".json";
            var SavData = new HltbDataUser { GameHltbData = hltbData };

            try
            {
                File.WriteAllText(FileGameData, JsonConvert.SerializeObject(SavData));
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "HowLongToBeat", $"Error on saving data for {GameId}");
            }
        }

        public static void ClearAllData(string PluginUserDataPath, IPlayniteAPI PlayniteApi)
        {
            string PluginDirectory = PluginUserDataPath + "\\howlongtobeat\\";
            if (Directory.Exists(PluginDirectory))
            {
                try
                {
                    Directory.Delete(PluginDirectory, true);
                    Directory.CreateDirectory(PluginDirectory);
                    PlayniteApi.Dialogs.ShowMessage(resources.GetString("LOCCommonDataRemove"), "HowLongToBeat");
                }
                catch
                {
                    PlayniteApi.Dialogs.ShowErrorMessage(resources.GetString("LOCCommonDataErrorRemove"), "HowLongToBeat");
                }
            }
        }



        public static void AddAllTagFromMain(IPlayniteAPI PlayniteApi, string PluginUserDataPath)
        {
            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                resources.GetString("LOCCommonAddingAllTag"),
                true
            );
            globalProgressOptions.IsIndeterminate = false;

            PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                var db = PlayniteApi.Database.Games.Where(x => x.Hidden == false);
                activateGlobalProgress.ProgressMaxValue = (double)db.Count();

                string CancelText = string.Empty;

                foreach (Game game in db)
                {
                    if (activateGlobalProgress.CancelToken.IsCancellationRequested)
                    {
                        CancelText = " canceled";
                        break;
                    }

                    AddAllTag(PlayniteApi, game, PluginUserDataPath);
                    activateGlobalProgress.CurrentProgressValue++;
                }

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                logger.Info($"HowLongToBeat - Task AddAllTagFromMain(){CancelText} - {String.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)}");
            }, globalProgressOptions);
        }

        public static void RemoveAllTagFromMain(IPlayniteAPI PlayniteApi, string PluginUserDataPath)
        {
            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                resources.GetString("LOCCommonRemovingAllTag"),
                true
            );
            globalProgressOptions.IsIndeterminate = false;

            PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                var db = PlayniteApi.Database.Games.Where(x => x.Hidden == false);
                activateGlobalProgress.ProgressMaxValue = (double)db.Count();

                string CancelText = string.Empty;

                foreach (Game game in db)
                {
                    if (activateGlobalProgress.CancelToken.IsCancellationRequested)
                    {
                        CancelText = " canceled";
                        break;
                    }

                    RemoveAllTag(PlayniteApi, game);
                    activateGlobalProgress.CurrentProgressValue++;
                }

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                logger.Info($"HowLongToBeat - Task RemoveAllTagFromMain(){CancelText} - {String.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)}");
            }, globalProgressOptions);
        }

        public static void GetAllDataFromMain(IPlayniteAPI PlayniteApi, string PluginUserDataPath, HowLongToBeatSettings settings)
        {
            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                resources.GetString("LOCCommonGettingAllDatas"),
                true
            );
            globalProgressOptions.IsIndeterminate = false;

            PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                var db = PlayniteApi.Database.Games.Where(x => x.Hidden == false);
                activateGlobalProgress.ProgressMaxValue = (double)db.Count();

                string CancelText = string.Empty;

                foreach (Game game in db)
                {
                    if (activateGlobalProgress.CancelToken.IsCancellationRequested)
                    {
                        CancelText = " canceled";
                        break;
                    }

                    if (!HowLongToBeatData.HaveData(game.Id, PluginUserDataPath))
                    {
                        List<HltbData> dataSearch = new HowLongToBeatClient().Search(game.Name);

                        if (dataSearch.Count == 1 && settings.AutoAccept)
                        {
                            HowLongToBeatData.SaveData(game.Id, dataSearch[0], PluginUserDataPath);

                            if (settings.EnableTag)
                            {
                                HowLongToBeatData.AddAllTag(PlayniteApi, game, PluginUserDataPath);
                            }
                        }
                        else
                        {
                            if (dataSearch.Count > 0 && settings.ShowWhenMismatch)
                            {
                                Application.Current.Dispatcher.BeginInvoke((Action)delegate
                                {
                                    string FileGameData = PluginUserDataPath + "\\howlongtobeat\\" + game.Id.ToString() + ".json";

                                    var ViewExtension = new HowLongToBeatSelect(dataSearch, FileGameData, game.Name);
                                    Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PlayniteApi, resources.GetString("LOCSelection"), ViewExtension);
                                    windowExtension.ShowDialog();

                                    if (settings.EnableTag)
                                    {
                                        HowLongToBeatData.AddAllTag(PlayniteApi, game, PluginUserDataPath);
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
            }, globalProgressOptions);
        }
    }
}
