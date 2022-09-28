using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using HowLongToBeat.Models;
using HowLongToBeat.Views;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Text.RegularExpressions;
using System.IO;
using CommonPluginsShared;
using System.Threading;
using System.Reflection;
using AngleSharp.Dom;
using CommonPluginsShared.Converters;
using CommonPluginsShared.Extensions;
using System.Text;
using Newtonsoft;
using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace HowLongToBeat.Services
{
    public enum StatusType
    {
        Playing,
        Backlog,
        Replays,
        CustomTab,
        Completed,
        Retired
    }

    public enum TimeType
    {
        MainStory,
        MainStoryExtra,
        Completionist,
        solo,
        CoOp,
        Versus
    }


    public class HowLongToBeatClient : ObservableObject
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        private static HowLongToBeatDatabase PluginDatabase = HowLongToBeat.PluginDatabase;

        protected static IWebView _WebViewOffscreen;
        internal static IWebView WebViewOffscreen
        {
            get
            {
                if (_WebViewOffscreen == null)
                {
                    _WebViewOffscreen = PluginDatabase.PlayniteApi.WebViews.CreateOffscreenView();
                }
                return _WebViewOffscreen;
            }

            set
            {
                _WebViewOffscreen = value;
            }
        }


        private const string UrlBase = "https://howlongtobeat.com/";

        private const string UrlLogin = UrlBase + "login";
        private const string UrlLogOut = UrlBase + "login?t=out";

        private const string UrlUserStats = UrlBase + "user?n={0}&s=stats";
        private const string UrlUserStatsMore = UrlBase + "user_stats_more";
        private const string UrlUserStatsGamesList = UrlBase + "api/user/{0}/stats";
        private const string UrlUserGamesList = UrlBase + "api/user/{0}/games/list";
        private const string UrlUserStatsGameDetails = UrlBase + "user_games_detail";

        private const string UrlPostData = UrlBase + "submit";
        private const string UrlPostDataEdit = UrlBase + "submit?s=add&eid={0}";
        private const string UrlSearch = UrlBase + "api/search";

        private const string UrlGameImg = UrlBase + "games/{0}";

        private const string UrlGame = UrlBase + "game/{0}";

        private const string UrlExportAll = UrlBase + "user_export?all=1";


        private bool? _IsConnected = null;
        public bool? IsConnected
        {
            get
            {
                return _IsConnected;
            }
            set
            {
                _IsConnected = value;
                OnPropertyChanged();
            }
        }

        public string UserLogin = string.Empty;
        public int UserId = 0;
        public HltbUserStats hltbUserStats = new HltbUserStats();

        private bool IsFirst = true;


        public HowLongToBeatClient()
        {
            UserLogin = PluginDatabase.PluginSettings.Settings.UserLogin;
        }


        /// <summary>
        /// Convert Time string from hltb to long seconds.
        /// </summary>
        /// <param name="Time"></param>
        /// <returns></returns>
        private long ConvertStringToLong(string Time)
        {
            if (Time.IndexOf("Hours") > -1)
            {
                Time = Time.Replace("Hours", string.Empty);
                Time = Time.Replace("&#189;", ".5");
                Time = Time.Replace("½", ".5");
                Time = Time.Trim();

                return (long)(Convert.ToDouble(Time, new NumberFormatInfo { NumberGroupSeparator = "." }) * 3600);
            }

            if (Time.IndexOf("Mins") > -1)
            {
                Time = Time.Replace("Mins", string.Empty);
                Time = Time.Replace("&#189;", ".5");
                Time = Time.Replace("½", ".5");
                Time = Time.Trim();

                return (long)(Convert.ToDouble(Time, new NumberFormatInfo { NumberGroupSeparator = "." }) * 60);
            }

            return 0;
        }

        private long ConvertStringToLongUser(string Time)
        {
            long.TryParse(Regex.Match(Time, @"\d+h").Value.Replace("h", string.Empty).Trim(), out long hours);
            long.TryParse(Regex.Match(Time, @"\d+m").Value.Replace("m", string.Empty).Trim(), out long minutes);
            long.TryParse(Regex.Match(Time, @"\d+s").Value.Replace("s", string.Empty).Trim(), out long secondes);

            long TimeConverted = hours * 3600 + minutes * 60 + secondes;

            Common.LogDebug(true, $"ConvertStringToLongUser: {Time.Trim()} - {TimeConverted}");

            return TimeConverted;
        }


        #region Search
        public List<HltbDataUser> Search(string Name, string Platform = "")
        {
            HltbSearchRoot data = GameSearch(Name, Platform).Result;
            List<HltbDataUser> dataParsed = SearchParser(data);
            return dataParsed;
        }

        /// <summary>
        /// Download search data.
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        private async Task<HltbSearchRoot> GameSearch(string Name, string Platform = "")
        {
            try
            {

                HttpClient httpClient = new HttpClient();

                httpClient.DefaultRequestHeaders.Add("User-Agent", Web.UserAgent);
                httpClient.DefaultRequestHeaders.Add("origin", "https://howlongtobeat.com");
                httpClient.DefaultRequestHeaders.Add("referer", "https://howlongtobeat.com");

                HttpRequestMessage requestMessage = new HttpRequestMessage();

                var searchTerms = Name.Split(' ');
                requestMessage.Content = new StringContent("{\"searchType\":\"games\",\"searchTerms\":[" + String.Join(",", searchTerms.Select(x => "\"" + x + "\"")) + "],\"searchPage\":1,\"size\":20,\"searchOptions\":{\"games\":{\"userId\":0,\"platform\":\"" + Platform + "\",\"sortCategory\":\"popular\",\"rangeCategory\":\"main\",\"rangeTime\":{\"min\":0,\"max\":0},\"gameplay\":{\"perspective\":\"\",\"flow\":\"\",\"genre\":\"\"},\"modifier\":\"\"},\"users\":{\"sortCategory\":\"postcount\"},\"filter\":\"\",\"sort\":0,\"randomizer\":0}}", Encoding.UTF8, "application/json");

                HttpResponseMessage response = await httpClient.PostAsync(UrlSearch, requestMessage.Content);
                var json = await response.Content.ReadAsStringAsync();

                HltbSearchRoot hltbSearchObj = new HltbSearchRoot();
                hltbSearchObj = JsonConvert.DeserializeObject<HltbSearchRoot>(json);
                return hltbSearchObj;
            }

            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
                return new HltbSearchRoot();
            }
        }

        public GameHowLongToBeat SearchData(Game game)
        {
            Common.LogDebug(true, $"Search data for {game.Name}");

            if (PluginDatabase.PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Desktop)
            {
                HowLongToBeatSelect ViewExtension = null;
                Application.Current.Dispatcher.BeginInvoke((Action)delegate
                {
                    ViewExtension = new HowLongToBeatSelect(null, game);
                    Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PluginDatabase.PlayniteApi, resources.GetString("LOCSelection"), ViewExtension);
                    windowExtension.ShowDialog();
                }).Wait();

                if (ViewExtension.gameHowLongToBeat?.Items.Count > 0)
                {
                    return ViewExtension.gameHowLongToBeat;
                }
            }
            return null;
        }
        

        /// <summary>
        /// Parse html search result.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private List<HltbDataUser> SearchParser(HltbSearchRoot data)
        {
            List<HltbDataUser> ReturnData = new List<HltbDataUser>();

            if (data != null)
            {
                try {
                    string Name = string.Empty;
                    int Id = 0;
                    string UrlImg = string.Empty;
                    string Url = string.Empty;

                    foreach (HltbSearchData entry in data.data)
                    {
                        Name = entry.game_name;
                        Id = entry.game_id;
                        UrlImg = string.Format(UrlGameImg, entry.game_image);
                        Url = string.Format(UrlGame, Id);

                        long MainStory = entry.comp_lvl_combine == 0 ? entry.comp_main : 0;
                        long MainExtra = entry.comp_lvl_combine == 0 ? entry.comp_plus : 0;
                        long Completionist = entry.comp_lvl_combine == 0 ? entry.comp_100 : 0;
                        long Solo = (entry.comp_lvl_combine == 1 && entry.comp_lvl_sp == 1) ? entry.comp_all : 0;
                        long CoOp = (entry.comp_lvl_combine == 1 && entry.comp_lvl_co == 1) ? entry.invested_co : 0;
                        long Vs = (entry.comp_lvl_combine == 1 && entry.comp_lvl_mp == 1) ? entry.invested_mp : 0;
                        
                        ReturnData.Add(new HltbDataUser
                        {
                            Name = Name,
                            Id = Id,
                            UrlImg = UrlImg,
                            Url = Url,
                            GameHltbData = new HltbData
                            {
                                MainStory = MainStory,
                                MainExtra = MainExtra,
                                Completionist = Completionist,
                                Solo = Solo,
                                CoOp = CoOp,
                                Vs = Vs
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                }
            }

            return ReturnData;
        }
        #endregion


        #region user account
        public bool GetIsUserLoggedIn()
        {
            if (UserId == 0)
            {
                UserId = HowLongToBeat.PluginDatabase.Database.UserHltbData.UserId;
            }

            if (UserId == 0)
            {
                IsConnected = false;
                return false;
            }

            if (IsConnected == null)
            {
                WebViewOffscreen.NavigateAndWait(UrlBase);
                IsConnected = WebViewOffscreen.GetPageSource().ToLower().IndexOf("log in") == -1;
            }

            IsConnected = (bool)IsConnected;
            return !!(bool)IsConnected;
        }

        public void Login()
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate
            {
                logger.Info("Login()");
                IWebView WebView = PluginDatabase.PlayniteApi.WebViews.CreateView(490, 670);
                WebView.LoadingChanged += (s, e) =>
                {
                    Common.LogDebug(true, $"NavigationChanged - {WebView.GetCurrentAddress()}");

                    if (WebView.GetCurrentAddress().StartsWith("https://howlongtobeat.com/user/"))
                    {
                        UserLogin = WebUtility.HtmlDecode(WebView.GetCurrentAddress().Replace("https://howlongtobeat.com/user/", string.Empty));
                        IsConnected = true;

                        PluginDatabase.PluginSettings.Settings.UserLogin = UserLogin;

                        Thread.Sleep(1500);
                        WebView.Close();
                    }
                };

                IsConnected = false;
                WebView.Navigate(UrlLogOut);
                WebView.Navigate(UrlLogin);
                WebView.OpenDialog();
            }).Completed += (s, e) => 
            {
                if ((bool)IsConnected)
                {
                    Application.Current.Dispatcher?.BeginInvoke((Action)delegate
                    {
                        try
                        {
                            PluginDatabase.Plugin.SavePluginSettings(PluginDatabase.PluginSettings.Settings);

                            Task.Run(() => {
                                string url = @"https://howlongtobeat.com/submit";
                                WebViewOffscreen.NavigateAndWait(url);

                                HtmlParser parser = new HtmlParser();
                                IHtmlDocument htmlDocument = parser.Parse(WebViewOffscreen.GetPageSource());

                                var el = htmlDocument.QuerySelector("input[name=user_id]");
                                if (el != null)
                                {
                                    string stringUserId = el.GetAttribute("value");
                                    int.TryParse(stringUserId, out UserId);
                                }

                                HowLongToBeat.PluginDatabase.RefreshUserData();
                            });
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false, true, PluginDatabase.PluginName);
                        }
                    });
                }
            };
        }


        private Dictionary<string, DateTime> GetListGameWithDateUpdate()
        {
            //string webData = GetUserGamesList(true);
            string webData = "";
            HtmlParser parser = new HtmlParser();
            IHtmlDocument htmlDocument = parser.Parse(webData);

            Dictionary<string, DateTime> data = new Dictionary<string, DateTime>();
            foreach (IElement ListGame in htmlDocument.QuerySelectorAll("table.user_game_list tbody"))
            {
                IHtmlCollection<IElement> tr = ListGame.QuerySelectorAll("tr");
                IHtmlCollection<IElement> td = tr[0].QuerySelectorAll("td");

                string UserGameId = ListGame.GetAttribute("id").Replace("user_sel_", string.Empty).Trim();
                string sDateTime = td[1].InnerHtml;
                DateTime.TryParseExact(sDateTime, "MMM dd, yyyy", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime dateTime);

                if (dateTime == default(DateTime))
                {
                    DateTime.TryParseExact(sDateTime, "MMMM dd, yyyy", new CultureInfo("en-US"), DateTimeStyles.None, out dateTime);
                }

                data.Add(UserGameId, dateTime);
            }

            return data;
        }

        private HltbUserGamesList GetUserGamesList(bool WithDateUpdate = false)
        {
            try
            {
                List<HttpCookie> Cookies = WebViewOffscreen.GetCookies();
                Cookies = Cookies.Where(x => x != null && x.Domain != null && (x.Domain.Contains("howlongtobeat", StringComparison.InvariantCultureIgnoreCase) || x.Domain.Contains("hltb", StringComparison.InvariantCultureIgnoreCase))).ToList();

                string payload = "{\"user_id\":" + UserId + ",\"lists\":[\"playing\",\"completed\",\"retired\"],\"set_playstyle\":\"comp_all\",\"name\":\"\",\"platform\":\"\",\"storefront\":\"\",\"sortBy\":\"\",\"sortFlip\":0,\"view\":\"\",\"limit\":1000,\"currentUserHome\":true}";
                string json = Web.PostStringDataPayload(string.Format(UrlUserGamesList, UserId), payload, Cookies).GetAwaiter().GetResult();

                HltbUserGamesList hltbUserGameList = new HltbUserGamesList();
                hltbUserGameList = JsonConvert.DeserializeObject<HltbUserGamesList>(json);

                return hltbUserGameList;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
                return new HltbUserGamesList();
            }
        }

        private string GetUserGamesDetail(string UserGameId)
        {
            try
            {
                List<HttpCookie> Cookies = WebViewOffscreen.GetCookies();
                Cookies = Cookies.Where(x => x != null && x.Domain != null && (x.Domain.Contains("howlongtobeat", StringComparison.InvariantCultureIgnoreCase) || x.Domain.Contains("hltb", StringComparison.InvariantCultureIgnoreCase))).ToList();

                FormUrlEncodedContent formContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("option", UserGameId),
                    new KeyValuePair<string, string>("option_b", "comp_all")
                });

                string response = Web.PostStringDataCookies(UrlUserStatsGameDetails, formContent, Cookies).GetAwaiter().GetResult();
                return response;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
                return string.Empty;
            }
        }

        private TitleList GetTitleList(GamesList x)
        {
            try
            {
                DateTime.TryParse(x.date_updated, out DateTime LastUpdate);
                DateTime.TryParse(x.date_complete, out DateTime Completion);
                DateTime? CompletionFinal = null;
                if (Completion != default) 
                {
                    CompletionFinal = Completion;
                }

                TitleList titleList = new TitleList
                {
                    UserGameId = x.game_id.ToString(),
                    GameName = x.custom_title,
                    Platform = x.platform,
                    Id = x.id,
                    CurrentTime = x.invested_pro,
                    IsReplay = x.list_replay == 1,
                    IsRetired = x.list_retired == 1,
                    Storefront = x.play_storefront,
                    LastUpdate = LastUpdate,
                    Completion = CompletionFinal,
                    HltbUserData = new HltbData
                    {
                        MainStory = x.comp_main,
                        MainExtra = x.comp_plus,
                        Completionist = x.comp_100,
                        Solo = 0,
                        CoOp = x.invested_co,
                        Vs = x.invested_mp
                    },
                    GameStatuses = new List<GameStatus>()
                };

                if (x.list_backlog == 1)
                {
                    titleList.GameStatuses.Add(new GameStatus { Status = StatusType.Backlog });
                }

                if (x.list_comp == 1)
                {
                    titleList.GameStatuses.Add(new GameStatus { Status = StatusType.Completed });
                }

                if (x.list_custom == 1)
                {
                    titleList.GameStatuses.Add(new GameStatus { Status = StatusType.CustomTab });
                }

                if (x.list_playing == 1)
                {
                    titleList.GameStatuses.Add(new GameStatus { Status = StatusType.Playing });
                }

                if (x.list_replay == 1)
                {
                    titleList.GameStatuses.Add(new GameStatus { Status = StatusType.Replays });
                }

                if (x.list_retired == 1)
                {
                    titleList.GameStatuses.Add(new GameStatus { Status = StatusType.Retired });
                }

                Common.LogDebug(true, $"titleList: {Serialization.ToJson(titleList)}");
                return titleList;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
                return null;
            }
        }

        public HltbPostData GetSubmitData(string GameName, string UserGameId)
        {
            logger.Info($"GetSubmitData({GameName}, {UserGameId})");
            try
            {
                List<HttpCookie> Cookies = WebViewOffscreen.GetCookies();
                Cookies = Cookies.Where(x => x != null && x.Domain != null && (x.Domain.Contains("howlongtobeat", StringComparison.InvariantCultureIgnoreCase) || x.Domain.Contains("hltb", StringComparison.InvariantCultureIgnoreCase))).ToList();

                string response = Web.DownloadStringData(string.Format(UrlPostDataEdit, UserGameId), Cookies).GetAwaiter().GetResult();
                if (response.IsNullOrEmpty())
                {
                    logger.Warn($"No SubmitData for {GameName} - {UserGameId}");
                    return null;
                }

                HtmlParser parser = new HtmlParser();
                IHtmlDocument htmlDocument = parser.Parse(response);

                HltbPostData hltbPostData = new HltbPostData();

                IElement user_id = htmlDocument.QuerySelector("input[name=user_id]");
                int.TryParse(user_id.GetAttribute("value"), out int user_id_value);
                hltbPostData.user_id = user_id_value;

                IElement edit_id = htmlDocument.QuerySelector("input[name=edit_id]");
                int.TryParse(edit_id.GetAttribute("value"), out int edit_id_value);
                hltbPostData.edit_id = edit_id_value;

                IElement game_id = htmlDocument.QuerySelector("input[name=game_id]");
                int.TryParse(game_id.GetAttribute("value"), out int game_id_value);
                hltbPostData.game_id = game_id_value;


                if (hltbPostData.user_id == 0)
                {
                    throw new Exception($"No user_id for {GameName} - {UserGameId}");
                }
                if (hltbPostData.edit_id == 0)
                {
                    throw new Exception($"No edit_id for {GameName} - {UserGameId}");
                }
                if (hltbPostData.game_id == 0)
                {
                    throw new Exception($"No game_id for {GameName} - {UserGameId}");
                }


                IElement CustomTitle = htmlDocument.QuerySelector("input[name=custom_title]");
                hltbPostData.custom_title = CustomTitle.GetAttribute("value");

                // TODO No selected....
                IHtmlCollection<IElement> SelectPlatform = htmlDocument.QuerySelectorAll("select[name=platform]");
                foreach(IElement option in SelectPlatform[0].QuerySelectorAll("option"))
                {
                    if (option.GetAttribute("selected") == "selected")
                    {
                        hltbPostData.platform = option.InnerHtml;
                    }
                }
                if (hltbPostData.platform.IsNullOrEmpty())
                {
                    if (SelectPlatform.Count() > 1)
                    {
                        foreach (IElement option in SelectPlatform[1].QuerySelectorAll("option"))
                        {
                            if (option.GetAttribute("selected") == "selected")
                            {
                                hltbPostData.platform = option.InnerHtml;
                            }
                        }
                    }
                }


                IElement cbList = htmlDocument.QuerySelector("#list_p");
                if ((bool)cbList?.OuterHtml?.ToLower()?.Contains(" checked"))
                {
                    hltbPostData.list_p = "1";
                }

                cbList = htmlDocument.QuerySelector("#list_b");
                if (cbList != null && (bool)cbList?.OuterHtml?.ToLower()?.Contains(" checked"))
                {
                    hltbPostData.list_b = "1";
                }

                cbList = htmlDocument.QuerySelector("#list_r");
                if (cbList != null && (bool)cbList?.OuterHtml?.ToLower()?.Contains(" checked"))
                {
                    hltbPostData.list_r = "1";
                }

                cbList = htmlDocument.QuerySelector("#list_c");
                if (cbList != null && (bool)cbList?.OuterHtml?.ToLower()?.Contains(" checked"))
                {
                    hltbPostData.list_c = "1";
                }

                cbList = htmlDocument.QuerySelector("#list_cp");
                if (cbList != null && (bool)cbList?.OuterHtml?.ToLower()?.Contains(" checked"))
                {
                    hltbPostData.list_cp = "1";
                }

                cbList = htmlDocument.QuerySelector("#list_rt");
                if (cbList != null && (bool)cbList?.OuterHtml?.ToLower()?.Contains(" checked"))
                {
                    hltbPostData.list_rt = "1";
                }

                IElement cp_pull_h = htmlDocument.QuerySelector("#cp_pull_h");
                hltbPostData.protime_h = cp_pull_h.GetAttribute("value");

                IElement cp_pull_m = htmlDocument.QuerySelector("#cp_pull_m");
                hltbPostData.protime_m = cp_pull_m.GetAttribute("value");

                IElement cp_pull_s = htmlDocument.QuerySelector("#cp_pull_s");
                hltbPostData.protime_s = cp_pull_s.GetAttribute("value");


                IElement rt_notes = htmlDocument.QuerySelector("input[name=rt_notes]");
                hltbPostData.rt_notes = rt_notes.GetAttribute("value");


                IElement compmonth = htmlDocument.QuerySelector("#compmonth");
                foreach (IElement option in compmonth.QuerySelectorAll("option"))
                {
                    if (option.GetAttribute("selected") == "selected")
                    {
                        hltbPostData.compmonth = option.GetAttribute("value");
                    }
                }

                IElement compday = htmlDocument.QuerySelector("#compday");
                foreach (IElement option in compday.QuerySelectorAll("option"))
                {
                    if (option.GetAttribute("selected") == "selected")
                    {
                        hltbPostData.compday = option.GetAttribute("value");
                    }
                }

                IElement compyear = htmlDocument.QuerySelector("#compyear");
                foreach (IElement option in compyear.QuerySelectorAll("option"))
                {
                    if (option.GetAttribute("selected") == "selected")
                    {
                        hltbPostData.compyear = option.GetAttribute("value");
                    }
                }


                IElement play_num = htmlDocument.QuerySelector("#play_num");
                foreach (IElement option in play_num.QuerySelectorAll("option"))
                {
                    if (option.GetAttribute("selected") == "selected")
                    {
                        int.TryParse(option.GetAttribute("value"), out int play_num_value);
                        hltbPostData.play_num = play_num_value;
                    }
                }


                IElement c_main_h = htmlDocument.QuerySelector("#c_main_h");
                hltbPostData.c_main_h = c_main_h?.GetAttribute("value");

                IElement c_main_m = htmlDocument.QuerySelector("#c_main_m");
                hltbPostData.c_main_m = c_main_m?.GetAttribute("value");

                IElement c_main_s = htmlDocument.QuerySelector("#c_main_s");
                hltbPostData.c_main_s = c_main_s?.GetAttribute("value");

                IElement c_main_notes = htmlDocument.QuerySelector("input[name=c_main_notes]");
                hltbPostData.c_main_notes = c_main_notes?.GetAttribute("value");


                IElement c_plus_h = htmlDocument.QuerySelector("#c_plus_h");
                hltbPostData.c_plus_h = c_plus_h?.GetAttribute("value");

                IElement c_plus_m = htmlDocument.QuerySelector("#c_plus_m");
                hltbPostData.c_plus_m = c_plus_m?.GetAttribute("value");

                IElement c_plus_s = htmlDocument.QuerySelector("#c_plus_s");
                hltbPostData.c_plus_s = c_plus_s?.GetAttribute("value");

                IElement c_plus_notes = htmlDocument.QuerySelector("input[name=c_plus_notes]");
                hltbPostData.c_plus_notes = c_plus_notes?.GetAttribute("value");


                IElement c_100_h = htmlDocument.QuerySelector("#c_100_h");
                hltbPostData.c_100_h = c_100_h?.GetAttribute("value");

                IElement c_100_m = htmlDocument.QuerySelector("#c_100_m");
                hltbPostData.c_100_m = c_100_m?.GetAttribute("value");

                IElement c_100_s = htmlDocument.QuerySelector("#c_100_s");
                hltbPostData.c_100_s = c_100_s?.GetAttribute("value");

                IElement c_100_notes = htmlDocument.QuerySelector("input[name=c_100_notes]");
                hltbPostData.c_100_notes = c_100_notes?.GetAttribute("value");


                IElement c_speed_h = htmlDocument.QuerySelector("#c_speed_h");
                hltbPostData.c_speed_h = c_speed_h?.GetAttribute("value");

                IElement c_speed_m = htmlDocument.QuerySelector("#c_speed_m");
                hltbPostData.c_speed_m = c_speed_m?.GetAttribute("value");

                IElement c_speed_s = htmlDocument.QuerySelector("#c_speed_s");
                hltbPostData.c_speed_s = c_speed_s?.GetAttribute("value");

                IElement c_speed_notes = htmlDocument.QuerySelector("input[name=c_speed_notes]");
                hltbPostData.c_speed_notes = c_speed_notes?.GetAttribute("value");


                IElement cotime_h = htmlDocument.QuerySelector("#cotime_h");
                hltbPostData.cotime_h = cotime_h?.GetAttribute("value");

                IElement cotime_m = htmlDocument.QuerySelector("#cotime_m");
                hltbPostData.cotime_m = cotime_m?.GetAttribute("value");

                IElement cotime_s = htmlDocument.QuerySelector("#cotime_s");
                hltbPostData.cotime_s = cotime_s?.GetAttribute("value");


                IElement mptime_h = htmlDocument.QuerySelector("#mptime_h");
                hltbPostData.mptime_h = mptime_h?.GetAttribute("value");

                IElement mptime_m = htmlDocument.QuerySelector("#mptime_m");
                hltbPostData.mptime_m = mptime_m?.GetAttribute("value");

                IElement mptime_s = htmlDocument.QuerySelector("#mptime_s");
                hltbPostData.mptime_s = mptime_s?.GetAttribute("value");

                IElement mptime_notes = htmlDocument.QuerySelector("#mptime_notes");


                IElement review_score = htmlDocument.QuerySelector("select[name=review_score]");
                foreach (IElement option in review_score.QuerySelectorAll("option"))
                {
                    if (option.GetAttribute("selected") == "selected")
                    {
                        int.TryParse(option.GetAttribute("value"), out int review_score_value);
                        hltbPostData.review_score = review_score_value;
                    }
                }


                IElement review_notes = htmlDocument.QuerySelector("textarea[name=review_notes]");
                hltbPostData.review_notes = review_notes?.InnerHtml;

                IElement play_notes = htmlDocument.QuerySelector("textarea[name=play_notes]");
                hltbPostData.play_notes = play_notes?.InnerHtml;

                IElement play_video = htmlDocument.QuerySelector("input[name=play_video]");
                hltbPostData.play_video = play_video?.GetAttribute("value");


                return hltbPostData;
            }
            catch (Exception ex)
            {
                if (IsFirst)
                {
                    IsFirst = false;
                    return GetSubmitData(GameName, UserGameId);
                }
                else
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                    return null;
                }
            }
        }


        public HltbUserStats LoadUserData()
        {
            string PathHltbUserStats = Path.Combine(PluginDatabase.Plugin.GetPluginUserDataPath(), "HltbUserStats.json");
            HltbUserStats hltbDataUser = new HltbUserStats();

            if (File.Exists(PathHltbUserStats))
            {
                try
                {
                    hltbDataUser = Serialization.FromJsonFile<HltbUserStats>(PathHltbUserStats);
                    hltbDataUser.TitlesList = hltbDataUser.TitlesList.Where(x => x != null).ToList();
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                }
            }

            return hltbDataUser;
        }


        public HltbUserStats GetUserData()
        {
            if (GetIsUserLoggedIn())
            {
                hltbUserStats = new HltbUserStats();
                hltbUserStats.Login = UserLogin.IsNullOrEmpty() ? HowLongToBeat.PluginDatabase.Database.UserHltbData.Login : UserLogin;
                hltbUserStats.UserId = (UserId == 0) ? HowLongToBeat.PluginDatabase.Database.UserHltbData.UserId : UserId;
                hltbUserStats.TitlesList = new List<TitleList>();

                HltbUserGamesList response = GetUserGamesList();
                if (response == null)
                {
                    return null;
                }

                try
                {
                    response.data.gamesList.ForEach(x =>
                    {
                        TitleList titleList = GetTitleList(x);
                        hltbUserStats.TitlesList.Add(titleList);
                    });
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                    return null;
                }

                return hltbUserStats;
            }
            else
            {
                PluginDatabase.PlayniteApi.Notifications.Add(new NotificationMessage(
                    $"{PluginDatabase.PluginName}-Import-Error",
                    PluginDatabase.PluginName + System.Environment.NewLine + resources.GetString("LOCCommonNotLoggedIn"),
                    NotificationType.Error,
                    () => PluginDatabase.Plugin.OpenSettingsView()
                ));
                return null;
            }
        }

        public TitleList GetUserData(int game_id)
        {
            if (GetIsUserLoggedIn())
            {
                try
                {
                    HltbUserStats data = GetUserData();
                    return data?.TitlesList?.Find(x => x.UserGameId.IsEqual(game_id.ToString()));
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                }

                return null;
            }
            else
            {
                PluginDatabase.PlayniteApi.Notifications.Add(new NotificationMessage(
                    $"{PluginDatabase.PluginName}-Import-Error",
                    PluginDatabase.PluginName + System.Environment.NewLine + resources.GetString("LOCCommonNotLoggedIn"),
                    NotificationType.Error,
                    () => PluginDatabase.Plugin.OpenSettingsView()
                ));
                return null;
            }
        }


        public bool EditIdExist(string UserGameId)
        {
            return GetUserGamesList().data.gamesList.FirstOrDefault().game_id.Equals(UserGameId);
        }

        public string FindIdExisting(string GameId)
        {
            return GetUserGamesList()?.data?.gamesList?.Find(x => x.game_id.Equals(GameId))?.id.ToString() ?? null;
        }
        #endregion


        /// <summary>
        /// Post current data in HowLongToBeat website.
        /// </summary>
        /// <param name="hltbPostData"></param>
        /// <returns></returns>
        public async Task<bool> PostData(Game game, HltbPostData hltbPostData)
        {
            if (GetIsUserLoggedIn() && hltbPostData.user_id != 0 && hltbPostData.game_id != 0)
            {
                try
                {
                    Type type = typeof(HltbPostData);
                    PropertyInfo[] properties = type.GetProperties();
                    Dictionary<string, string> data = new Dictionary<string, string>();


                    // Get existing data
                    if (hltbPostData.edit_id != 0)
                    {
                        logger.Info($"Edit {game.Name} - {hltbPostData.edit_id}");
                        data.Add("edited", "Save Edit");
                    }
                    else
                    {
                        logger.Info($"Submit {game.Name}");
                        data.Add("submitted", "Submit");
                    }


                    foreach (PropertyInfo property in properties)
                    {
                        switch (property.Name)
                        {
                            case "list_p":
                                if (property.GetValue(hltbPostData, null).ToString() != string.Empty)
                                {
                                    data.Add(property.Name, property.GetValue(hltbPostData, null).ToString());
                                }
                                break;
                            case "list_b":
                                if (property.GetValue(hltbPostData, null).ToString() != string.Empty)
                                {
                                    data.Add(property.Name, property.GetValue(hltbPostData, null).ToString());
                                }
                                break;
                            case "list_r":
                                if (property.GetValue(hltbPostData, null).ToString() != string.Empty)
                                {
                                    data.Add(property.Name, property.GetValue(hltbPostData, null).ToString());
                                }
                                break;
                            case "list_c":
                                if (property.GetValue(hltbPostData, null).ToString() != string.Empty)
                                {
                                    data.Add(property.Name, property.GetValue(hltbPostData, null).ToString());
                                }
                                break;
                            case "list_cp":
                                if (property.GetValue(hltbPostData, null).ToString() != string.Empty)
                                {
                                    data.Add(property.Name, property.GetValue(hltbPostData, null).ToString());
                                }
                                break;
                            case "list_rt":
                                if (property.GetValue(hltbPostData, null).ToString() != string.Empty)
                                {
                                    data.Add(property.Name, property.GetValue(hltbPostData, null).ToString());
                                }
                                break;


                            case "compmonth":
                                if (property.GetValue(hltbPostData, null).ToString() == string.Empty)
                                {
                                    data.Add(property.Name, DateTime.Now.ToString("MM"));
                                }
                                else
                                {
                                    data.Add(property.Name, property.GetValue(hltbPostData, null).ToString());
                                }
                                break;
                            case "compday":
                                if (property.GetValue(hltbPostData, null).ToString() == string.Empty)
                                {
                                    data.Add(property.Name, DateTime.Now.ToString("dd"));
                                }
                                else
                                {
                                    data.Add(property.Name, property.GetValue(hltbPostData, null).ToString());
                                }
                                break;
                            case "compyear":
                                if (property.GetValue(hltbPostData, null).ToString() == string.Empty)
                                {
                                    data.Add(property.Name, DateTime.Now.ToString("yyyy"));
                                }
                                else
                                {
                                    data.Add(property.Name, property.GetValue(hltbPostData, null).ToString());
                                }
                                break;

                            default:
                                data.Add(property.Name, property.GetValue(hltbPostData, null).ToString());
                                break;
                        }
                    }
                    

                    List<HttpCookie> Cookies = WebViewOffscreen.GetCookies();
                    Cookies = Cookies.Where(x => x != null && x.Domain != null && (x.Domain.Contains("howlongtobeat", StringComparison.InvariantCultureIgnoreCase) || x.Domain.Contains("hltb", StringComparison.InvariantCultureIgnoreCase))).ToList();

                    FormUrlEncodedContent formContent = new FormUrlEncodedContent(data);
                    string response = await Web.PostStringDataCookies(UrlPostData, formContent, Cookies);


                    // Check errors
                    HtmlParser parser = new HtmlParser();
                    IHtmlDocument htmlDocument = parser.Parse(response);

                    string errorMessage = string.Empty;
                    foreach (IElement el in htmlDocument.QuerySelectorAll("div.in.back_red.shadow_box li"))
                    {
                        if (errorMessage.IsNullOrEmpty())
                        {
                            errorMessage += el.InnerHtml;
                        }
                        else
                        {
                            errorMessage += System.Environment.NewLine + el.InnerHtml;
                        }
                    }


                    if (!errorMessage.IsNullOrEmpty())
                    {
                        PluginDatabase.PlayniteApi.Notifications.Add(new NotificationMessage(
                            $"{PluginDatabase.PluginName}-{game.Id}-Error",
                            PluginDatabase.PluginName + System.Environment.NewLine + game.Name + System.Environment.NewLine + errorMessage,
                            NotificationType.Error
                        ));
                    }
                    else
                    {
                        PluginDatabase.RefreshUserData(hltbPostData.game_id);
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                    return false;
                }
            }
            else
            {
                PluginDatabase.PlayniteApi.Notifications.Add(new NotificationMessage(
                    $"{PluginDatabase.PluginName}-DataUpdate-Error",
                    PluginDatabase.PluginName + System.Environment.NewLine + resources.GetString("LOCCommonNotLoggedIn"),
                    NotificationType.Error,
                    () => PluginDatabase.Plugin.OpenSettingsView()
                ));
                return false;
            }

            return false;
        }
    }
}
