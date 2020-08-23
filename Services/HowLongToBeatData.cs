using HowLongToBeat.Models;
using HowLongToBeat.Views;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginCommon;
using System;
using System.Collections.Generic;
using System.IO;

namespace HowLongToBeat.Services
{
    public class HowLongToBeatData
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        private HltbDataUser data { get; set; }

        private string FileGameData { get; set; }

        public HowLongToBeatData(Game game, string PluginUserDataPath, IPlayniteAPI PlayniteApi, bool EnableTag, bool mustFind = true)
        {
            string PluginDatabasePath = PluginUserDataPath + "\\howlongtobeat";

            if (!Directory.Exists(PluginDatabasePath))
            {
                Directory.CreateDirectory(PluginDatabasePath);
            }

            FileGameData = PluginDatabasePath + "\\" + game.Id.ToString() + ".json";

            if (File.Exists(FileGameData))
            {
                // Load data
                logger.Info("HowLongToBeat - Load data");

                data = JsonConvert.DeserializeObject<HltbDataUser>(File.ReadAllText(FileGameData));
            }
            else
            {
                if (mustFind)
                {
                    // Create data
                    logger.Info("HowLongToBeat - Create data");

                    List<HltbData> dataSearch = new HowLongToBeatClient().Search(game.Name);
                    new HowLongToBeatSelect(dataSearch, FileGameData, game.Name).ShowDialog();

                    if (File.Exists(FileGameData))
                    {
                        data = JsonConvert.DeserializeObject<HltbDataUser>(File.ReadAllText(FileGameData));
                    }
                    else
                    {

                    }
                }
            }

            AddTag(data, game, PlayniteApi, EnableTag);
        }

        public void AddTag(HltbDataUser data, Game game, IPlayniteAPI PlayniteApi, bool EnableTag)
        {
            // Tags id
            List<Tag> HltbTags = new List<Tag>();
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

            // Add or remove tag
            if (data != null && data != new HltbDataUser())
            {
                if (game.Tags != null && game.Tags.Count > 0)
                {
                    foreach (Tag tag in HltbTags)
                    {
                        game.TagIds.Remove(tag.Id);
                    }
                }

                List<Guid> tagIds = new List<Guid>();
                if (game.TagIds != null)
                {
                    tagIds = game.TagIds;
                }

                if (EnableTag)
                {
                    long hltbTime = 0;
                    try
                    {
                        // Get time
                        if (data.GameHltbData.MainStory != 0)
                        {
                            hltbTime = data.GameHltbData.MainStory;
                        }
                        if (data.GameHltbData.Solo != 0)
                        {
                            hltbTime = data.GameHltbData.Solo;
                        }

                        // Add tag
                        bool isFind = false;
                        if (hltbTime < 3600 && !isFind)
                        {
                            isFind = true;
                            logger.Info($"HowLongToBeat - Add tag [HLTB] {resources.GetString("LOCPLaytimeLessThenAnHour")} for {game.Name}");
                            tagIds.Add((HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCPLaytimeLessThenAnHour")}")).Id);
                        }
                        if (hltbTime < 18000 && !isFind)
                        {
                            isFind = true;
                            logger.Info($"HowLongToBeat - Add tag [HLTB] {resources.GetString("LOCHowLongToBeat1to5")} for {game.Name}");
                            tagIds.Add((HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat1to5")}")).Id);
                        }
                        if (hltbTime < 36000 && !isFind)
                        {
                            isFind = true;
                            logger.Info($"HowLongToBeat - Add tag [HLTB] {resources.GetString("LOCHowLongToBeat5to10")} for {game.Name}");
                            tagIds.Add((HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat5to10")}")).Id);
                        }
                        if (hltbTime < 72000 && !isFind)
                        {
                            isFind = true;
                            logger.Info($"HowLongToBeat - Add tag [HLTB] {resources.GetString("LOCHowLongToBeat10to20")} for {game.Name}");
                            tagIds.Add((HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat10to20")}")).Id);
                        }
                        if (hltbTime < 108000 && !isFind)
                        {
                            isFind = true;
                            logger.Info($"HowLongToBeat - Add tag [HLTB] {resources.GetString("LOCHowLongToBeat20to30")} for {game.Name}");
                            tagIds.Add((HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat20to30")}")).Id);
                        }
                        if (hltbTime < 144000 && !isFind)
                        {
                            isFind = true;
                            logger.Info($"HowLongToBeat - Add tag [HLTB] {resources.GetString("LOCHowLongToBeat30to40")} for {game.Name}");
                            tagIds.Add((HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat30to40")}")).Id);
                        }
                        if (hltbTime < 180000 && !isFind)
                        {
                            isFind = true;
                            logger.Info($"HowLongToBeat - Add tag [HLTB] {resources.GetString("LOCHowLongToBeat40to50")} for {game.Name}");
                            tagIds.Add((HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat40to50")}")).Id);
                        }
                        if (hltbTime < 216000 && !isFind)
                        {
                            isFind = true;
                            logger.Info($"HowLongToBeat - Add tag [HLTB] {resources.GetString("LOCHowLongToBeat50to60")} for {game.Name}");
                            tagIds.Add((HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat50to60")}")).Id);
                        }
                        if (hltbTime < 252000 && !isFind)
                        {
                            isFind = true;
                            logger.Info($"HowLongToBeat - Add tag [HLTB] {resources.GetString("LOCHowLongToBeat60to70")} for {game.Name}");
                            tagIds.Add((HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat60to70")}")).Id);
                        }
                        if (hltbTime < 288000 && !isFind)
                        {
                            isFind = true;
                            logger.Info($"HowLongToBeat - Add tag [HLTB] {resources.GetString("LOCHowLongToBeat70to80")} for {game.Name}");
                            tagIds.Add((HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat70to80")}")).Id);
                        }
                        if (hltbTime < 324000 && !isFind)
                        {
                            isFind = true;
                            logger.Info($"HowLongToBeat - Add tag [HLTB] {resources.GetString("LOCHowLongToBeat80to90")} for {game.Name}");
                            tagIds.Add((HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat80to90")}")).Id);
                        }
                        if (hltbTime < 360000 && !isFind)
                        {
                            isFind = true;
                            logger.Info($"HowLongToBeat - Add tag [HLTB] {resources.GetString("LOCHowLongToBeat90to100")} for {game.Name}");
                            tagIds.Add((HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat90to100")}")).Id);
                        }
                        if (hltbTime >= 360000 && !isFind)
                        {
                            isFind = true;
                            logger.Info($"HowLongToBeat - Add tag [HLTB] {resources.GetString("LOCHowLongToBeat100plus")} for {game.Name}");
                            tagIds.Add((HltbTags.Find(x => x.Name == $"[HLTB] {resources.GetString("LOCHowLongToBeat100plus")}")).Id);
                        }
                    }
                    catch(Exception ex)
                    {
                        Common.LogError(ex, "HowLongToBeat", $"Tag insert error with {hltbTime} in {game.Name}");
                    }
                }

                if (tagIds.Count > 0)
                {
                    game.TagIds = tagIds;
                }
                PlayniteApi.Database.Games.Update(game);
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
        }
    }
}
