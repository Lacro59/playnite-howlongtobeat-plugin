using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using HowLongToBeat.Models;
using HowLongToBeat.Views;
using Newtonsoft.Json;
using Playnite.SDK;
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
using CommonShared;

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

        private IPlayniteAPI _PlayniteApi;
        private IWebView webViews;

        HowLongToBeat _plugin;

        private readonly string UrlBase = "https://howlongtobeat.com/";

        private string UrlLogin { get; set; }
        private string UrlLogOut { get; set; }

        private string UrlUserStats { get; set; }
        private string UrlUserStatsMore { get; set; }
        private string UrlUserStatsGameList { get; set; }
        private string UrlUserStatsGameDetails { get; set; }

        private string UrlPostData { get; set; }
        private string UrlSearch { get; set; }

        private string UrlGame { get; set; }

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
        public HltbUserStats hltbUserStats = new HltbUserStats();


        public HowLongToBeatClient(HowLongToBeat plugin, IPlayniteAPI PlayniteApi)
        {
            _plugin = plugin;
            _PlayniteApi = PlayniteApi;

            webViews = PlayniteApi.WebViews.CreateOffscreenView();

            UrlPostData = UrlBase + "submit?s=add&gid={0}"; 

            UrlLogin = UrlBase + "login";
            UrlLogOut = UrlBase + "login?t=out";

            UrlUserStats = UrlBase + "user?n={0}&s=stats";
            UrlUserStatsMore = UrlBase + "user_stats_more";
            UrlUserStatsGameList = UrlBase + "user_games_list";
            UrlUserStatsGameDetails = UrlBase + "user_games_detail";

            UrlSearch = UrlBase + "search_results.php";

            UrlGame = UrlBase + "game.php?id={0}";
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
#if DEBUG
            logger.Debug($"HowLongToBeat - ConvertStringToLongUser: {Time} - {TimeConverted}");
#endif

            return TimeConverted;
        }


        #region Search
        public List<HltbDataUser> Search(string Name, string Platform = "")
        {
            string data = GameSearch(Name, Platform).GetAwaiter().GetResult();
            return SearchParser(data);
        }

        /// <summary>
        /// Download search data.
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        private async Task<string> GameSearch(string Name, string Platform = "")
        { 
            try
            {
                HttpClient client = new HttpClient();

                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("queryString", Name),
                    new KeyValuePair<string, string>("t", "games"),
                    new KeyValuePair<string, string>("sorthead", "popular"),
                    new KeyValuePair<string, string>("sortd", "Normal Order"),
                    new KeyValuePair<string, string>("plat", Platform),
                    new KeyValuePair<string, string>("length_type", "main"),
                    new KeyValuePair<string, string>("length_min", string.Empty),
                    new KeyValuePair<string, string>("length_max", string.Empty),
                    new KeyValuePair<string, string>("detail", "0")
                });
                
                var response = client.PostAsync(UrlSearch, content).Result;
                string responseBody = response.Content.ReadAsStringAsync().Result;

                return responseBody;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "HowLongToBeat", $"Error on GameSearch()");

                return string.Empty;
            }
        }

        public GameHowLongToBeat SearchData(Game game)
        {
#if DEBUG
            logger.Debug($"HowLongToBeat - Search data for {game.Name}");
#endif

            HowLongToBeatSelect ViewExtension = null;
            Application.Current.Dispatcher.BeginInvoke((Action)delegate
            {
                ViewExtension = new HowLongToBeatSelect(null, game);
                Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(_PlayniteApi, resources.GetString("LOCSelection"), ViewExtension);
                windowExtension.ShowDialog();
            }).Wait();

            if (ViewExtension.gameHowLongToBeat != null && ViewExtension.gameHowLongToBeat.Items.Count > 0)
            {
                return ViewExtension.gameHowLongToBeat;
            }

            return null;
        }
        

        /// <summary>
        /// Parse html search result.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private List<HltbDataUser> SearchParser(string data)
        {
            List<HltbDataUser> ReturnData = new List<HltbDataUser>();

            if (data != string.Empty)
            {
                try {
                    HtmlParser parser = new HtmlParser();
                    IHtmlDocument htmlDocument = parser.Parse(data);

                    string Name = string.Empty;
                    int Id = 0;
                    string UrlImg = string.Empty;
                    string Url = string.Empty;

                    foreach (var SearchElement in htmlDocument.QuerySelectorAll("li.back_darkish"))
                    {
                        var ElementA = SearchElement.QuerySelector(".search_list_image a");
                        var ElementImg = SearchElement.QuerySelector(".search_list_image a img");
                        Name = ElementA.GetAttribute("title");
                        Id = int.Parse(ElementA.GetAttribute("href").Replace("game?id=", string.Empty));
                        UrlImg = ElementImg.GetAttribute("src");
                        Url = UrlBase + ElementA.GetAttribute("href");

                        var ElementDetails = SearchElement.QuerySelector(".search_list_details_block");
                        var Details = ElementDetails.QuerySelectorAll(".search_list_tidbit");
                        if (Details.Length == 0)
                        {
                            Details = ElementDetails.QuerySelectorAll("div");
                        }

                        long MainStory = 0;
                        long MainExtra = 0;
                        long Completionist = 0;
                        long Solo = 0;
                        long CoOp = 0;
                        long Vs = 0;

                        bool IsMainStory = true;
                        bool IsMainExtra = true;
                        bool IsCompletionist = true;
                        bool IsCoOp = true;
                        bool IsVs = true;
                        bool IsSolo = true;

                        int iElement = 0;
                        foreach (var El in Details)
                        {
                            if (iElement % 2 == 0)
                            {
                                IsMainStory = (El.InnerHtml == "Main Story");
                                IsMainExtra = (El.InnerHtml == "Main + Extra");
                                IsCompletionist = (El.InnerHtml == "Completionist");
                                IsCoOp = (El.InnerHtml == "Co-Op");
                                IsVs = (El.InnerHtml == "Vs.");
                                IsSolo = (El.InnerHtml == "Solo");
                            }
                            else
                            {
                                if (IsMainStory)
                                {
                                    MainStory = ConvertStringToLong(El.InnerHtml);
                                }
                                if (IsMainExtra)
                                {
                                    MainExtra = ConvertStringToLong(El.InnerHtml);
                                }
                                if (IsCompletionist)
                                {
                                    Completionist = ConvertStringToLong(El.InnerHtml);
                                }
                                if (IsCoOp)
                                {
                                    CoOp = ConvertStringToLong(El.InnerHtml);
                                }
                                if (IsVs)
                                {
                                    Vs = ConvertStringToLong(El.InnerHtml);
                                }
                                if (IsSolo)
                                {
                                    Solo = ConvertStringToLong(El.InnerHtml);
                                }
                            }

                            iElement += 1;
                        }

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
                    Common.LogError(ex, "HowLongToBeat", $"Error on SearchParser()");
                }
            }

            return ReturnData;
        }
        #endregion


        #region user account
        public bool GetIsUserLoggedIn()
        {
            if (IsConnected == null)
            {
                webViews.NavigateAndWait(UrlBase);
                IsConnected = webViews.GetPageSource().ToLower().IndexOf("log in") == -1;

                if ((bool)IsConnected)
                {
                    HtmlParser parser = new HtmlParser();
                    IHtmlDocument htmlDocument = parser.Parse(webViews.GetPageSource());

                    UserLogin = htmlDocument.QuerySelector("ul.login div.label").InnerHtml;
                }
            }

            return (bool)IsConnected;
        }

        public void Login()
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate
            {
                IWebView webView = _PlayniteApi.WebViews.CreateView(490, 670);

                logger.Info("HowLongToBeat - Login()");

                webView.LoadingChanged += (s, e) =>
                {
#if DEBUG
                    logger.Debug($"HowLongToBeat - NavigationChanged - {webView.GetCurrentAddress()}");
#endif

                    if (webView.GetCurrentAddress().IndexOf("https://howlongtobeat.com/user?n=") > -1)
                    {
#if DEBUG
                        logger.Debug($"HowLongToBeat - webView.Close();");
#endif
                        UserLogin = WebUtility.HtmlDecode(webView.GetCurrentAddress().Replace("https://howlongtobeat.com/user?n=", string.Empty));
                        IsConnected = true;
                        webView.Close();
                    }
                };

                IsConnected = false;
                webView.Navigate(UrlLogOut);
                webView.OpenDialog();
            });
        }


        public HltbUserStats LoadUserData()
        {
            string PathHltbUserStats = Path.Combine(_plugin.GetPluginUserDataPath(), "HltbUserStats.json");
            HltbUserStats hltbDataUser = new HltbUserStats();

            if (File.Exists(PathHltbUserStats))
            {
                try
                {
                    var JsonStringData = File.ReadAllText(PathHltbUserStats);
                    hltbDataUser = JsonConvert.DeserializeObject<HltbUserStats>(JsonStringData);
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "HowLongToBeat");
                }
            }

            return hltbDataUser;
        }

        public HltbUserStats GetUserData()
        {
            if (GetIsUserLoggedIn())
            {
                hltbUserStats = new HltbUserStats();
                hltbUserStats.Login = UserLogin;
                hltbUserStats.TitlesList = new List<TitleList>();

                try
                {
                    List<HttpCookie> Cookies = webViews.GetCookies();
                    Cookies = Cookies.Where(x => x.Domain.Contains("howlongtobeat")).ToList();
#if DEBUG
                    logger.Debug($"HowLongToBeat - Cookies: {JsonConvert.SerializeObject(Cookies)}");
#endif

                    // Get list games
                    var formContent = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("n", UserLogin),
                        new KeyValuePair<string, string>("c", "user_beat"),
                        new KeyValuePair<string, string>("p", string.Empty),
                        new KeyValuePair<string, string>("y", string.Empty)
                    });

                    string response = Web.PostStringDataCookies(UrlUserStatsGameList, formContent, Cookies).GetAwaiter().GetResult();
#if DEBUG
                    //logger.Debug($"HowLongToBeat - webViews: {response.Replace(Environment.NewLine, string.Empty)}");
#endif

                    HtmlParser parser = new HtmlParser();
                    IHtmlDocument htmlDocument = parser.Parse(response);

                    foreach (var ListGame in htmlDocument.QuerySelectorAll("table.user_game_list tbody"))
                    {
                        TitleList titleList = new TitleList();

                        var tr = ListGame.QuerySelectorAll("tr");
                        var td = tr[0].QuerySelectorAll("td");

                        titleList.UserGameId = ListGame.GetAttribute("id").Replace("user_sel_", string.Empty).Trim();
                        titleList.GameName = td[0].QuerySelector("a").InnerHtml.Trim();
                        titleList.Platform = td[0].QuerySelector("span").InnerHtml.Trim();
                        titleList.Id = int.Parse(td[0].QuerySelector("a").GetAttribute("href").Replace("game?id=", string.Empty));


                        // Get game details
                        formContent = new FormUrlEncodedContent(new[]
                        {
                            new KeyValuePair<string, string>("option", titleList.UserGameId),
                            new KeyValuePair<string, string>("option_b", "comp_all")
                        });

                        response = Web.PostStringDataCookies(UrlUserStatsGameDetails, formContent, Cookies).GetAwaiter().GetResult();
#if DEBUG
                        //logger.Debug($"HowLongToBeat - webViews: {response.Replace(Environment.NewLine, string.Empty)}");
#endif

                        parser = new HtmlParser();
                        htmlDocument = parser.Parse(response);

                        var GameDetails = htmlDocument.QuerySelectorAll("div.user_game_detail > div");

                        // Game status type
                        titleList.GameStatuses = new List<GameStatus>();
                        foreach (var GameStatus in GameDetails[0].QuerySelectorAll("span"))
                        {
                            switch(GameStatus.InnerHtml.ToLower())
                            {
                                case "playing":
                                    titleList.GameStatuses.Add(new GameStatus { Status = StatusType.Playing });
                                    break;
                                case "backlog":
                                    titleList.GameStatuses.Add(new GameStatus { Status = StatusType.Backlog });
                                    break;
                                case "replays":
                                    titleList.GameStatuses.Add(new GameStatus { Status = StatusType.Replays });
                                    break;
                                case "custom tab":
                                    titleList.GameStatuses.Add(new GameStatus { Status = StatusType.CustomTab });
                                    break;
                                case "completed":
                                    titleList.GameStatuses.Add(new GameStatus { Status = StatusType.Completed });
                                    break;
                                case "retired":
                                    titleList.GameStatuses.Add(new GameStatus { Status = StatusType.Retired });
                                    break;
                            }
                        }

                        // Game status time
                        int iPosUserData = 1;
                        if (GameDetails[1].InnerHtml.ToLower().Contains("<h5>progress</h5>"))
                        {
                            List<string> ListTime = GameDetails[1].QuerySelector("span").InnerHtml
                                .Replace("<strong>", string.Empty).Replace("</strong>", string.Empty)
                                .Split('/').ToList();

                            for (int i = 0; i < ListTime.Count; i++)
                            {
                                titleList.GameStatuses[i].Time = ConvertStringToLongUser(ListTime[i]);
                            }

                            iPosUserData = 2;
                        }

                        // User data
                        titleList.HltbUserData = new HltbData();
                        for (int i = 0; i < GameDetails[iPosUserData].Children.Count(); i++)
                        {
                            string tempTime = string.Empty;

                            // Completion date
                            if (GameDetails[iPosUserData].Children[i].InnerHtml.ToLower().Contains("completion"))
                            {
                                if (GameDetails[iPosUserData].Children[i].QuerySelectorAll("p").FirstOrDefault() != null)
                                {
                                    tempTime = GameDetails[iPosUserData].Children[i].QuerySelectorAll("p").FirstOrDefault().InnerHtml;
                                    titleList.Completion = Convert.ToDateTime(tempTime);
                                }
                            }


                            if (GameDetails[iPosUserData].Children[i].InnerHtml.ToLower().Contains("main story"))
                            {
                                i++;
                                tempTime = GameDetails[iPosUserData].Children[i].QuerySelector("span").InnerHtml;
                                titleList.HltbUserData.MainStory = ConvertStringToLongUser(tempTime);
                            }

                            if (GameDetails[iPosUserData].Children[i].InnerHtml.ToLower().Contains("main + extras"))
                            {
                                i++;
                                tempTime = GameDetails[iPosUserData].Children[i].QuerySelector("span").InnerHtml;
                                titleList.HltbUserData.MainExtra = ConvertStringToLongUser(tempTime);
                            }

                            if (GameDetails[iPosUserData].Children[i].InnerHtml.ToLower().Contains("completionist"))
                            {
                                i++;
                                tempTime = GameDetails[iPosUserData].Children[i].QuerySelector("span").InnerHtml;
                                titleList.HltbUserData.Completionist = ConvertStringToLongUser(tempTime);
                            }


                            if (GameDetails[iPosUserData].Children[i].InnerHtml.ToLower().Contains("solo"))
                            {
                                i++;
                                tempTime = GameDetails[iPosUserData].Children[i].QuerySelector("span").InnerHtml;
                                titleList.HltbUserData.Solo = ConvertStringToLongUser(tempTime);
                            }

                            if (GameDetails[iPosUserData].Children[i].InnerHtml.ToLower().Contains("co-op"))
                            {
                                i++;
                                tempTime = GameDetails[iPosUserData].Children[i].QuerySelector("span").InnerHtml;
                                titleList.HltbUserData.CoOp = ConvertStringToLongUser(tempTime);
                            }

                            if (GameDetails[iPosUserData].Children[i].InnerHtml.ToLower().Contains("vs"))
                            {
                                i++;
                                tempTime = GameDetails[iPosUserData].Children[i].QuerySelector("span").InnerHtml;
                                titleList.HltbUserData.Vs = ConvertStringToLongUser(tempTime);
                            }
                        }
#if DEBUG
                        logger.Debug($"HowLongToBeat - titleList: {JsonConvert.SerializeObject(titleList)}");
#endif

                        hltbUserStats.TitlesList.Add(titleList);
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "HowLongToBeat");
                    return null;
                }

                return hltbUserStats;
            }
            else
            {
                _PlayniteApi.Notifications.Add(new NotificationMessage(
                    "HowLongToBeat-Import-Error",
                    "HowLongToBeat" + System.Environment.NewLine +
                    resources.GetString("LOCNotLoggedIn"),
                    NotificationType.Error,
                    () => _plugin.OpenSettingsView()));

                return null;
            }
        }
        #endregion


        public async Task<bool> PostData(HltbPostData hltbPostData)
        {






            return false;
        }
    }
}
