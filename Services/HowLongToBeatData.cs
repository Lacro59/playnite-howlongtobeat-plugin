using HowLongToBeat.Models;
using HowLongToBeat.Views;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace HowLongToBeat.Services
{
    public class HowLongToBeatData
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private HltbDataUser data { get; set; }

        private string FileGameData { get; set; }

        public HowLongToBeatData(Game game, string PluginUserDataPath)
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
                logger.Debug("HowLongToBeat - Load data");

                data = JsonConvert.DeserializeObject<HltbDataUser>(File.ReadAllText(FileGameData));
            }
            else
            {
                // Create data
                logger.Debug("HowLongToBeat - Create data");

                List<HltbData> dataSearch = new HowLongToBeatClient().Search(game.Name);
                new HowLongToBeatSelect(dataSearch, FileGameData).ShowDialog();

                if (File.Exists(FileGameData))
                {
                    data = JsonConvert.DeserializeObject<HltbDataUser>(File.ReadAllText(FileGameData));
                }
                else
                {

                }
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
