using CommonPluginsShared;
using FuzzySharp;
using HowLongToBeat.Models;
using HowLongToBeat.Models.Vndb;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace HowLongToBeat.Services
{
    public class VndbApi
    {
        private static ILogger Logger => LogManager.GetLogger();
        private static HowLongToBeatDatabase PluginDatabase => HowLongToBeat.PluginDatabase;

        private static string Url => "https://vndb.org";
        private static string UrlApi => "https://api.vndb.org/kana";
        private static string UrlSearch => UrlApi + "/vn";

        private static readonly HttpClient httpClient;

        static VndbApi()
        {
            httpClient = new HttpClient();
            try
            {
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", CommonPluginsShared.Web.UserAgent);
            }
            catch { }
        }

        private static async Task<string> PostJson(string url, string payload)
        {
            try
            {
                var content = new StringContent(payload ?? string.Empty, Encoding.UTF8, "application/json");
                using (content)
                {
                    using (var resp = await httpClient.PostAsync(url, content).ConfigureAwait(false))
                    {
                        if (!resp.IsSuccessStatusCode)
                        {
                            try { Logger.Error($"VNDB error status {(int)resp.StatusCode}"); } catch { }
                            return string.Empty;
                        }
                        return await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Error posting to {url}");
                return string.Empty;
            }
        }

        private static async Task<List<HltbDataUser>> Search(string payload)
        {
            List<HltbDataUser> hltbDataUsers = new List<HltbDataUser>();

            try
            {
                string data = await PostJson(UrlSearch, payload).ConfigureAwait(false);
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
                            MainStoryClassic = x.LengthMinutes * 60 ?? (x.Length == null ? 0 : GetTime((int)x.Length))
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


        public static async Task<List<HltbSearch>> SearchByNameAsync(string name)
        {
            Logger.Info($"VndbApi.Search({name})");
            string payload = "{\"filters\":[\"search\", \"=\", \"" + PlayniteTools.NormalizeGameName(name) + "\"], \"fields\":\"id, title, alttitle, image.url, length, length_minutes\"}";
            List<HltbDataUser> search = await Search(payload).ConfigureAwait(false);
            return search.Select(x => new HltbSearch { MatchPercent = Fuzz.Ratio(name.ToLower(), x.Name.ToLower()), Data = x })
                .OrderByDescending(x => x.MatchPercent)
                .ToList();
        }

        public static async Task<List<HltbDataUser>> SearchByIdAsync(string id)
        {
            Logger.Info($"VndbApi.Search({id})");
            string payload = "{\"filters\":[\"id\", \"=\", \"" + id + "\"], \"fields\":\"id, title, alttitle, image.url, length, length_minutes\"}";
            return await Search(payload).ConfigureAwait(false);
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
