using HowLongToBeat.Models;
using Newtonsoft.Json;
using Playnite.SDK;
using PluginCommon;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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

        private ConcurrentDictionary<Guid, HltbDataUser> Items { get; set; } = new ConcurrentDictionary<Guid, HltbDataUser>();


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
            logger.Info($"HowLongToBeat - LoadOldDB()");

            Parallel.ForEach(Directory.EnumerateFiles(PathActivityDB, "*.json"), (objectFile) =>
            {
                string objectFileManual = string.Empty;

                try
                {
                    var JsonStringData = File.ReadAllText(objectFile);

#if DEBUG
                    logger.Debug(objectFile.Replace(PathActivityDB, "").Replace(".json", "").Replace("\\", ""));
#endif
                    Guid gameId = Guid.Parse(objectFile.Replace(PathActivityDB, "").Replace(".json", "").Replace("\\", ""));

                    HltbDataUser hltbDataUser = JsonConvert.DeserializeObject<HltbDataUser>(JsonStringData);

                    Items.TryAdd(gameId, hltbDataUser);
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "HowLongToBeat", $"Failed to load item from {objectFile} or {objectFileManual}");
                }
            });

            logger.Info($"HowLongToBeat - Find {Items.Count} items");
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

                logger.Info($"HowLongToBeat - ConvertDB()");

                int Converted = 0;

                foreach (var item in Items)
                {
                    try
                    {
                        if (PlayniteApi.Database.Games.Get(item.Key) != null)
                        {
                            GameHowLongToBeat gameHowLongToBeat = HowLongToBeat.PluginDatabase.Get(item.Key, true);

                            gameHowLongToBeat.Items = new List<HltbDataUser> { item.Value };

                            Thread.Sleep(10);
                            HowLongToBeat.PluginDatabase.Add(gameHowLongToBeat);
                            Converted++;
                        }
                        else
                        {
                            logger.Warn($"HowLongToBeat - Game is deleted - {item.Key.ToString()}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, "SuccessStory", $"Failed to load ConvertDB from {item.Key.ToString()}");
                    }
                }

                logger.Info($"HowLongToBeat - Converted {Converted} / {Items.Count}");

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                logger.Info($"HowLongToBeat - Migration - {String.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)}");
            }, globalProgressOptions);

            IsOld = false;
        }
    }
}
