using CommonPluginsShared;
using HowLongToBeat.Models;
using HowLongToBeat.Models.Vndb;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HowLongToBeat.Services
{
    public class VndbApi
    {
        private static ILogger Logger => LogManager.GetLogger();
        private static HowLongToBeatDatabase PluginDatabase => HowLongToBeat.PluginDatabase;

        private static string Url => "https://vndb.org";
        private static string UrlApi => "https://api.vndb.org/kana";
        private static string UrlSearch => UrlApi + "/vn";


        private static List<HltbDataUser> Search(string payload)
        {
            List<HltbDataUser> hltbDataUsers = new List<HltbDataUser>();

            try
            {
                string data = Web.PostStringDataPayload(UrlSearch, payload).GetAwaiter().GetResult();
                _ = Serialization.TryFromJson(data, out VndbSearch vndbSearch);

                vndbSearch?.Results?.ForEach(x =>
                {
                    hltbDataUsers.Add(new HltbDataUser
                    {
                        Id = x.Id,
                        IsVndb = true,
                        Name = x.Title,
                        Url = Url + "/" + x.Id,
                        UrlImg = x.Image?.Url,
                        GameHltbData = new HltbData
                        {
                            MainStory = x.LengthMinutes * 60 ?? (x.Length == null ? 0 : GetTime((int)x.Length))
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            return hltbDataUsers;
        }

        public static List<HltbDataUser> SearchByName(string name)
        {
            Logger.Info($"VndbApi.Search({name})");
            string payload = "{\"filters\":[\"search\", \"=\", \"" + PlayniteTools.NormalizeGameName(name) + "\"], \"fields\":\"id, title, alttitle, image.url, length, length_minutes\"}";
            return Search(payload);
        }

        public static List<HltbDataUser> SearchById(string id)
        {
            Logger.Info($"VndbApi.Search({id})");
            string payload = "{\"filters\":[\"id\", \"=\", \"" + id + "\"], \"fields\":\"id, title, alttitle, image.url, length, length_minutes\"}";
            return Search(payload);
        }


        private static int GetTime(int length)
        {
            switch (length)
            {
                //< 2 hours
                case 1:
                    return 2 * 3600;
                //2 - 10 hours
                case 2:
                    return 6 * 3600;
                //10 - 30 hours
                case 3:
                    return 20 * 3600;
                //30 - 50 hours
                case 4:
                    return 35 * 3600;
                case 5:
                //> 50 hours
                    return 50 * 3600;
                default:
                    return 0;
            }
        }
    }
}
