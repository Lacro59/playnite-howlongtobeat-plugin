using CommonPlayniteShared.Converters;
using CommonPluginsShared;
using HowLongToBeat.Models;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HowLongToBeat.Services
{
    public class OldToNew
    {
        private ILogger logger = LogManager.GetLogger();

        public bool IsOld = false;

        private string PathActivityDB = "howlongtobeat";

        private ConcurrentDictionary<Guid, HltbDataUserOld> Items { get; set; } = new ConcurrentDictionary<Guid, HltbDataUserOld>();


        public OldToNew(string PluginUserDataPath)
        {
            PathActivityDB = Path.Combine(PluginUserDataPath, PathActivityDB);

            if (Directory.Exists(PathActivityDB))
            {
                // Test is old
                CheckIsOld();

                if (IsOld)
                {
                    Directory.Move(PathActivityDB, PathActivityDB + "_old");

                    PathActivityDB += "_old";

                    LoadOldDB();
                }
            }
        }

        public void CheckIsOld()
        {
            Parallel.ForEach(Directory.EnumerateFiles(PathActivityDB, "*.json"), (objectFile) =>
            {
                string objectFileManual = string.Empty;

                try
                {
                    var JsonStringData = File.ReadAllText(objectFile);
                    IsOld = !JsonStringData.Contains("\"Items\"");
                    return;
                }
                catch
                {
                    IsOld = false;
                    return;
                }
            });
        }

        public void LoadOldDB()
        {
            logger.Info($"LoadOldDB()");

            Parallel.ForEach(Directory.EnumerateFiles(PathActivityDB, "*.json"), (objectFile) =>
            {
                string objectFileManual = string.Empty;

                try
                {
                    Guid gameId = Guid.Parse(objectFile.Replace(PathActivityDB, "").Replace(".json", "").Replace("\\", ""));
                    HltbDataUserOld hltbDataUser = Serialization.FromJsonFile<HltbDataUserOld>(objectFile);

                    Items.TryAdd(gameId, hltbDataUser);
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Failed to load item from {objectFile} or {objectFileManual}");
                }
            });

            logger.Info($"Find {Items.Count} items");
        }

        public void ConvertDB(IPlayniteAPI PlayniteApi)
        {
            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                "HowLongToBeat - Database migration",
                false
            );
            globalProgressOptions.IsIndeterminate = true;

            PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                logger.Info($"ConvertDB()");

                int Converted = 0;

                foreach (var item in Items)
                {
                    try
                    {
                        if (PlayniteApi.Database.Games.Get(item.Key) != null)
                        {
                            GameHowLongToBeat gameHowLongToBeat = HowLongToBeat.PluginDatabase.Get(item.Key, true);

                            HltbDataUser hltbDataUser = new HltbDataUser
                            {
                                Id = item.Value.GameHltbData.Id,
                                Name = item.Value.GameHltbData.Name,
                                Url = item.Value.GameHltbData.Url,
                                UrlImg = item.Value.GameHltbData.UrlImg,
                                GameHltbData = new HltbData
                                {
                                    MainStory = item.Value.GameHltbData.MainStory,
                                    MainExtra = item.Value.GameHltbData.MainExtra,
                                    Completionist = item.Value.GameHltbData.Completionist,
                                    Solo = item.Value.GameHltbData.Solo,
                                    CoOp = item.Value.GameHltbData.CoOp,
                                    Vs = item.Value.GameHltbData.Vs
                                }
                            };

                            gameHowLongToBeat.Items = new List<HltbDataUser> { hltbDataUser };

                            Thread.Sleep(10);
                            HowLongToBeat.PluginDatabase.Add(gameHowLongToBeat);
                            Converted++;
                        }
                        else
                        {
                            logger.Warn($"Game is deleted - {item.Key.ToString()}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, $"Failed to load ConvertDB from {item.Key.ToString()}");
                    }
                }

                logger.Info($"Converted {Converted} / {Items.Count}");

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                logger.Info($"Migration - {String.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)}");
            }, globalProgressOptions);

            IsOld = false;
        }
    }


    public class HltbDataUserOld
    {
        public long UserMainStory { get; set; } = 0;
        public long UserMainExtra { get; set; } = 0;
        public long UserCompletionist { get; set; } = 0;

        public long UserSolo { get; set; } = 0;
        public long UserCoOp { get; set; } = 0;
        public long UserVs { get; set; } = 0;

        public HltbDataOld GameHltbData { get; set; }
    }

    public class HltbDataOld
    {
        private PlayTimeToStringConverter converter = new PlayTimeToStringConverter();

        public string Name { get; set; }
        public int Id { get; set; }
        public string UrlImg { get; set; }
        public string Url { get; set; } = string.Empty;

        public long MainStory { get; set; }
        [DontSerialize]
        public string MainStoryFormat
        {
            get
            {
                if (MainStory == 0)
                {
                    return "--";
                }
                return (string)converter.Convert((long)MainStory, null, null, CultureInfo.CurrentCulture);
            }
        }
        public long MainExtra { get; set; }
        [DontSerialize]
        public string MainExtraFormat
        {
            get
            {
                if (MainExtra == 0)
                {
                    return "--";
                }
                return (string)converter.Convert((long)MainExtra, null, null, CultureInfo.CurrentCulture);
            }
        }
        public long Completionist { get; set; }
        [DontSerialize]
        public string CompletionistFormat
        {
            get
            {
                if (Completionist == 0)
                {
                    return "--";
                }
                return (string)converter.Convert((long)Completionist, null, null, CultureInfo.CurrentCulture);
            }
        }


        public long Solo { get; set; } = 0;
        [DontSerialize]
        public string SoloFormat
        {
            get
            {
                if (Solo == 0)
                {
                    return "--";
                }
                return (string)converter.Convert((long)Solo, null, null, CultureInfo.CurrentCulture);
            }
        }
        public long CoOp { get; set; } = 0;
        [DontSerialize]
        public string CoOpFormat
        {
            get
            {
                if (CoOp == 0)
                {
                    return "--";
                }
                return (string)converter.Convert((long)CoOp, null, null, CultureInfo.CurrentCulture);
            }
        }
        public long Vs { get; set; } = 0;
        [DontSerialize]
        public string VsFormat
        {
            get
            {
                if (Vs == 0)
                {
                    return "--";
                }
                return (string)converter.Convert((long)Vs, null, null, CultureInfo.CurrentCulture);
            }
        }
    }
}
