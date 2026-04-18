using CommonPluginsShared;
using FuzzySharp;
using HowLongToBeat.Models;
using HowLongToBeat.Models.Enumerations;
using HowLongToBeat.Models.Vndb;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading;

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
        private static readonly Regex SpeedRowRegex = new Regex(
            "<tr><td>(Slow|Normal|Fast|Total)</td><td>(.*?)</td><td>(.*?)</td><td>(.*?)</td><td>(\\d+)</td></tr>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

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

                await EnrichWithLengthVotesAsync(hltbDataUsers).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            return hltbDataUsers;
        }

        private static async Task EnrichWithLengthVotesAsync(List<HltbDataUser> items)
        {
            if (items == null || items.Count == 0)
            {
                return;
            }

            // Keep VNDB requests bounded to avoid slow search UI.
            using (var throttler = new SemaphoreSlim(4))
            {
                var tasks = items.Select(async item =>
                {
                    if (item == null || string.IsNullOrEmpty(item.Id))
                    {
                        return;
                    }

                    await throttler.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        await FillLengthVotesAsync(item).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, $"Unable to read VNDB length votes for {item.Id}");
                    }
                    finally
                    {
                        throttler.Release();
                    }
                });

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
        }

        private static async Task FillLengthVotesAsync(HltbDataUser item)
        {
            string data = await GetString($"{Url}/{item.Id}/lengthvotes").ConfigureAwait(false);
            if (data.IsNullOrEmpty())
            {
                return;
            }

            long slow = 0;
            long normal = 0;
            long fast = 0;
            long total = 0;
            int found = 0;

            foreach (Match row in SpeedRowRegex.Matches(data))
            {
                if (!row.Success)
                {
                    continue;
                }

                string speed = StripTags(row.Groups[1].Value).Trim();
                string median = StripTags(row.Groups[2].Value).Trim();
                string average = StripTags(row.Groups[3].Value).Trim();
                string stddev = StripTags(row.Groups[4].Value).Trim();
                int votes = 0;
                int.TryParse(StripTags(row.Groups[5].Value).Trim(), out votes);
                long seconds = ParseDurationToSeconds(median);
                if (seconds <= 0)
                {
                    continue;
                }

                switch (speed.ToLowerInvariant())
                {
                    case "slow":
                        slow = seconds;
                        item.VndbSlowMedian = FormatVndbDuration(median);
                        item.VndbSlowAverage = FormatVndbDuration(average);
                        item.VndbSlowStddev = FormatVndbDuration(stddev);
                        item.VndbSlowVotes = votes;
                        found++;
                        break;
                    case "normal":
                        normal = seconds;
                        item.VndbNormalMedian = FormatVndbDuration(median);
                        item.VndbNormalAverage = FormatVndbDuration(average);
                        item.VndbNormalStddev = FormatVndbDuration(stddev);
                        item.VndbNormalVotes = votes;
                        found++;
                        break;
                    case "fast":
                        fast = seconds;
                        item.VndbFastMedian = FormatVndbDuration(median);
                        item.VndbFastAverage = FormatVndbDuration(average);
                        item.VndbFastStddev = FormatVndbDuration(stddev);
                        item.VndbFastVotes = votes;
                        found++;
                        break;
                    case "total":
                        total = seconds;
                        item.VndbTotalMedian = FormatVndbDuration(median);
                        item.VndbTotalAverage = FormatVndbDuration(average);
                        item.VndbTotalStddev = FormatVndbDuration(stddev);
                        item.VndbTotalVotes = votes;
                        found++;
                        break;
                }
            }

            if (found == 0)
            {
                return;
            }

            var hltbData = item.GameHltbData ?? new HltbData();
            hltbData.GameType = GameType.Game;

            // Map VNDB speed rows to existing 4 HLTB data columns for display + selection.
            hltbData.MainStoryClassic = slow;
            hltbData.MainStoryAverage = normal;
            hltbData.MainStoryMedian = fast;
            hltbData.MainStoryRushed = total;

            // Keep a fallback for old behavior if one of the values is missing.
            if (hltbData.MainStoryClassic == 0)
            {
                hltbData.MainStoryClassic = hltbData.MainStoryAverage != 0
                    ? hltbData.MainStoryAverage
                    : (item.GameHltbData?.MainStoryClassic ?? 0);
            }

            item.GameHltbData = hltbData;
        }

        private static async Task<string> GetString(string url)
        {
            try
            {
                using (var resp = await httpClient.GetAsync(url).ConfigureAwait(false))
                {
                    if (!resp.IsSuccessStatusCode)
                    {
                        return string.Empty;
                    }

                    return await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string StripTags(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return Regex.Replace(value, "<.*?>", string.Empty);
        }

        private static long ParseDurationToSeconds(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 0;
            }

            // Examples: "36h23m", "21h", "53m"
            var clean = value.Replace(" ", string.Empty).ToLowerInvariant();
            double hours = 0;
            double minutes = 0;

            int hPos = clean.IndexOf('h');
            if (hPos > -1 && double.TryParse(clean.Substring(0, hPos), NumberStyles.Any, CultureInfo.InvariantCulture, out double h))
            {
                hours = h;
                clean = clean.Substring(hPos + 1);
            }

            int mPos = clean.IndexOf('m');
            if (mPos > -1 && double.TryParse(clean.Substring(0, mPos), NumberStyles.Any, CultureInfo.InvariantCulture, out double m))
            {
                minutes = m;
            }

            var seconds = (long)(hours * 3600 + minutes * 60);
            return seconds < 0 ? 0 : seconds;
        }

        private static string FormatVndbDuration(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "--";
            }

            return value.Replace(" ", string.Empty).Trim();
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
