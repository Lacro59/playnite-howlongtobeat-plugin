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
using CommonPluginsShared;
using System.Windows.Threading;
using System.Threading;
using System.Reflection;
using System.Diagnostics;

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
        HowLongToBeatSettings _settings;

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
        public int UserId = 0;
        public HltbUserStats hltbUserStats = new HltbUserStats();


        public HowLongToBeatClient(HowLongToBeat plugin, IPlayniteAPI PlayniteApi, HowLongToBeatSettings settings)
        {
            _plugin = plugin;
            _PlayniteApi = PlayniteApi;
            _settings = settings;

            webViews = PlayniteApi.WebViews.CreateOffscreenView();

            UrlPostData = UrlBase + "submit"; 

            UrlLogin = UrlBase + "login";
            UrlLogOut = UrlBase + "login?t=out";

            UrlUserStats = UrlBase + "user?n={0}&s=stats";
            UrlUserStatsMore = UrlBase + "user_stats_more";
            UrlUserStatsGameList = UrlBase + "user_games_list";
            UrlUserStatsGameDetails = UrlBase + "user_games_detail";

            UrlSearch = UrlBase + "search_results.php";

            UrlGame = UrlBase + "game.php?id={0}";

            UserLogin = _settings.UserLogin;
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
            logger.Debug($"HowLongToBeat [Ignored] - ConvertStringToLongUser: {Time} - {TimeConverted}");
#endif

            return TimeConverted;
        }


        #region Search
        public List<HltbDataUser> Search(string Name, string Platform = "")
        {
#if DEBUG
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
#endif

            string data = GameSearch(Name, Platform).GetAwaiter().GetResult();
            var dataParsed = SearchParser(data);

#if DEBUG
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            logger.Debug($"HowLongToBeat [Ignored] - Task Search() - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)}");
#endif

            return dataParsed;
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

                return await Web.PostStringDataCookies(UrlSearch, content);
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
            logger.Debug($"HowLongToBeat [Ignored] - Search data for {game.Name}");
#endif
            if (_PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Desktop)
            {
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
            }

            IsConnected = (bool)IsConnected;
            return !!(bool)IsConnected;
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
                    logger.Debug($"HowLongToBeat [Ignored] - NavigationChanged - {webView.GetCurrentAddress()}");
#endif

                    if (webView.GetCurrentAddress().IndexOf("https://howlongtobeat.com/user?n=") > -1)
                    {
#if DEBUG
                        logger.Debug($"HowLongToBeat [Ignored] - webView.Close();");
#endif
                        UserLogin = WebUtility.HtmlDecode(webView.GetCurrentAddress().Replace("https://howlongtobeat.com/user?n=", string.Empty));
                        IsConnected = true;

                        _settings.UserLogin = UserLogin;
                        _plugin.SavePluginSettings(_settings);

                        Thread.Sleep(1500);
                        webView.Close();
                    }
                };

                IsConnected = false;
                webView.Navigate(UrlLogOut);
                webView.Navigate(UrlLogin);
                webView.OpenDialog();
            }).Completed += (s, e) => 
            {
                if ((bool)IsConnected)
                {
                    Application.Current.Dispatcher.BeginInvoke((Action)delegate
                    {
                        try
                        {
                            Task.Run(() => {
                                string url = @"https://howlongtobeat.com/submit?s=add";
                                webViews.NavigateAndWait(url);

                                HtmlParser parser = new HtmlParser();
                                IHtmlDocument htmlDocument = parser.Parse(webViews.GetPageSource());

                                foreach (var el in htmlDocument.QuerySelectorAll("input[type=\"hidden\"]"))
                                {
                                    if (el.GetAttribute("name") == "user_id")
                                    {
                                        string stringUserId = el.GetAttribute("value");
                                        int.TryParse(stringUserId, out UserId);
                                        break;
                                    }
                                }

                                HowLongToBeat.PluginDatabase.RefreshUserData();
                            });
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, "HowLongToBeat");
                        }
                    });
                }
            };
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
                hltbUserStats.Login = (UserLogin.IsNullOrEmpty()) ? HowLongToBeat.PluginDatabase.Database.UserHltbData.Login : UserLogin;
                hltbUserStats.UserId = (UserId == 0) ? HowLongToBeat.PluginDatabase.Database.UserHltbData.UserId : UserId;
                hltbUserStats.TitlesList = new List<TitleList>();

                try
                {
                    webViews.NavigateAndWait(UrlUserStatsGameList);

                    List<HttpCookie> Cookies = webViews.GetCookies();
                    Cookies = Cookies.Where(x => x.Domain.Contains("howlongtobeat")).ToList();
#if DEBUG
                    logger.Debug($"HowLongToBeat [Ignored] - Cookies: {JsonConvert.SerializeObject(Cookies)}");
#endif

                    // Get list games
                    var formContent = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("n", hltbUserStats.Login),
                        new KeyValuePair<string, string>("c", "user_beat"),
                        new KeyValuePair<string, string>("p", string.Empty),
                        new KeyValuePair<string, string>("y", string.Empty)
                    });

                    string response = Web.PostStringDataCookies(UrlUserStatsGameList, formContent, Cookies).GetAwaiter().GetResult();

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

                        string sCurrentTime = td[1].InnerHtml;
                        titleList.CurrentTime = ConvertStringToLongUser(sCurrentTime);

                        // Get game details
                        formContent = new FormUrlEncodedContent(new[]
                        {
                            new KeyValuePair<string, string>("option", titleList.UserGameId),
                            new KeyValuePair<string, string>("option_b", "comp_all")
                        });

                        response = Web.PostStringDataCookies(UrlUserStatsGameDetails, formContent, Cookies).GetAwaiter().GetResult();

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

                            for (int i = 0; i < titleList.GameStatuses.Count; i++)
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

                            if (GameDetails[iPosUserData].Children[i].InnerHtml.ToLower().Contains("main+extras"))
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
                        logger.Debug($"HowLongToBeat [Ignored] - titleList: {JsonConvert.SerializeObject(titleList)}");
#endif

                        hltbUserStats.TitlesList.Add(titleList);
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "HowLongToBeat");

                    _PlayniteApi.Notifications.Add(new NotificationMessage(
                        "HowLongToBeat-Import-Error",
                        "HowLongToBeat" + System.Environment.NewLine +
                        ex.Message,
                        NotificationType.Error,
                        () => _plugin.OpenSettingsView()));

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
            if (GetIsUserLoggedIn() && hltbPostData.user_id != 0 && hltbPostData.game_id != 0)
            {
                try
                {
                    Type type = typeof(HltbPostData);
                    PropertyInfo[] properties = type.GetProperties();
                    var data = new Dictionary<string, string>();
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
                                data.Add(property.Name, WebUtility.UrlEncode(property.GetValue(hltbPostData, null).ToString()));
                                break;
                        }
                    }

                    
                    if (hltbPostData.edit_id != 0)
                    {
                        string url = string.Format(UrlPostData + "?s=add&gid={0}", hltbPostData.game_id);
                        webViews.NavigateAndWait(url);
                    }
                    else
                    {

                    }


                    List<HttpCookie> Cookies = webViews.GetCookies();
                    Cookies = Cookies.Where(x => x.Domain.Contains("howlongtobeat")).ToList();
#if DEBUG
                    logger.Debug($"HowLongToBeat [Ignored] - Cookies: {JsonConvert.SerializeObject(Cookies)}");
#endif

                    var formContent = new FormUrlEncodedContent(data);
                    string response = Web.PostStringDataCookies(UrlPostData, formContent, Cookies).GetAwaiter().GetResult();

                    HtmlParser parser = new HtmlParser();
                    IHtmlDocument htmlDocument = parser.Parse(response);


                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "HowLongToBeat");

                    _PlayniteApi.Notifications.Add(new NotificationMessage(
                        "HowLongToBeat-DataUpdate-Error",
                        "HowLongToBeat" + System.Environment.NewLine +
                        ex.Message,
                        NotificationType.Error,
                        () => _plugin.OpenSettingsView()));

                    return false;
                }
            }
            else
            {
                _PlayniteApi.Notifications.Add(new NotificationMessage(
                    "HowLongToBeat-DataUpdate-Error",
                    "HowLongToBeat" + System.Environment.NewLine +
                    resources.GetString("LOCNotLoggedIn"),
                    NotificationType.Error,
                    () => _plugin.OpenSettingsView()));

                return false;
            }

            return false;
        }
    }
}
