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
using System.Collections.Generic;
using System.IO;

namespace HowLongToBeat.Services
{
    public class HowLongToBeatData
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        private IPlayniteAPI _PlayniteApi { get; set; }
        private Game _game { get; set; }

        private HltbDataUser data { get; set; } = new HltbDataUser();
        public readonly bool hasData = false;

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

                data = JsonConvert.DeserializeObject<HltbDataUser>(File.ReadAllText(FileGameData));
            }
            else
            {
                // Search data
                if (mustFind)
                {
#if DEBUG  
                    logger.Debug($"HowLongToBeat - Search data for {game.Name}");
#endif

                    List<HltbData> dataSearch = new HowLongToBeatClient().Search(game.Name);
                    new HowLongToBeatSelect(dataSearch, FileGameData, game.Name).ShowDialog();

                    if (File.Exists(FileGameData))
                    {
                        data = JsonConvert.DeserializeObject<HltbDataUser>(File.ReadAllText(FileGameData));
                    }
                    else
                    {
                        data = null;
                    }
                }
            }

            if (data != null && data != new HltbDataUser())
            {
                hasData = true;
            }
        }


        public void AddTag()
        {
            RemoveTag();

            try
            {
                List<Tag> HltbTags = GetTagId(_PlayniteApi);

                if (data != null && data != new HltbDataUser())
                {
                    List<Guid> tagIds = SetListHltbTag(_PlayniteApi, data);

                    if (tagIds.Count > 0)
                    {
                        _game.TagIds = tagIds;
                    }

                    _PlayniteApi.Database.Games.Update(_game);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "HowLongToBeat", $"Tag insert error with {_game.Name}");
            }
        }

        public void RemoveTag()
        {
            try
            {
                List<Tag> HltbTags = GetTagId(_PlayniteApi);
                foreach (Tag tag in HltbTags)
                {
                    _game.TagIds.Remove(tag.Id);
                    _PlayniteApi.Database.Games.Update(_game);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "HowLongToBeat", $"Tag remove error with {_game.Name}");
            }
        }


        public HltbDataUser GetData()
        {
            return data;
        }

        public void RemoveData()
        {
            if (File.Exists(FileGameData))
            {
                File.Delete(FileGameData);
            }
            else
            {
                logger.Warn($"HowLongToBeat - Impossible to remove");
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

            if (data != null && data != new HltbDataUser() && data.GameHltbData != null && data.GameHltbData != new HltbData() && HltbTags.Count > 1 && HltbTags != null)
            {
                try
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
                catch (Exception ex)
                {
                    Common.LogError(ex, "HowLongToBeat", $"Tag insert error with {hltbTime}");
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
                    Common.LogError(ex, "HowLongToBeat", $"Tag insert error with {game.Name}");
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
                }
                catch
                {
                    PlayniteApi.Dialogs.ShowErrorMessage(resources.GetString("LOCSystemCheckerErrorRemove"), "HowLongToBeat error");
                }
            }
        }
    }
}
