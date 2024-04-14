using HowLongToBeat.Models;
using HowLongToBeat.Views;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using CommonPluginsShared;
using System.Threading;
using CommonPluginsShared.Extensions;
using System.Text;
using CommonPlayniteShared.Common;
using System.Security.Principal;

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
        private static ILogger Logger => LogManager.GetLogger();

        private static HowLongToBeatDatabase PluginDatabase => HowLongToBeat.PluginDatabase;


        internal string FileCookies { get; }


        #region Urls
        private const string UrlBase = "https://howlongtobeat.com/";

        private string UrlLogin { get; set; } = UrlBase + "login";
        private string UrlLogOut { get; set; } = UrlBase + "login?t=out";

        private string UrlUserStats { get; set; } = UrlBase + "user?n={0}&s=stats";
        private string UrlUserStatsMore { get; set; } = UrlBase + "user_stats_more";
        private string UrlUserStatsGamesList { get; set; } = UrlBase + "api/user/{0}/stats";
        private string UrlUserGamesList { get; set; } = UrlBase + "api/user/{0}/games/list";
        private string UrlUserStatsGameDetails { get; set; } = UrlBase + "user_games_detail";

        private string UrlPostData { get; set; } = UrlBase + "api/submit";
        private string UrlPostDataEdit { get; set; } = UrlBase + "submit/edit/{0}";
        private string UrlSearch { get; set; } = UrlBase + "api/search";

        private string UrlGameImg { get; set; } = UrlBase + "games/{0}";

        private string UrlGame { get; set; } = UrlBase + "game/{0}";

        private string UrlExportAll { get; set; } = UrlBase + "user_export?all=1";
        #endregion


        private bool? _IsConnected = null;
        public bool? IsConnected { get => _IsConnected; set => SetValue(ref _IsConnected, value); }


        public string UserLogin = string.Empty;
        public int UserId = 0;
        public HltbUserStats hltbUserStats = new HltbUserStats();

        private bool IsFirst = true;


        public HowLongToBeatClient()
        {
            UserLogin = PluginDatabase.PluginSettings.Settings.UserLogin;

            string PathData = PluginDatabase.Paths.PluginUserDataPath;
            FileCookies = Path.Combine(PathData, CommonPlayniteShared.Common.Paths.GetSafePathName($"HowLongToBeat.json"));
        }


        #region Search
        public List<HltbDataUser> Search(string Name, string Platform = "")
        {
            HltbSearchRoot data = GameSearch(Name, Platform).Result;
            List<HltbDataUser> dataParsed = SearchParser(data);
            return dataParsed;
        }

        public List<HltbDataUser> SearchTwoMethod(string Name, string Platform = "")
        {
            List<HltbDataUser> dataSearchNormalized = Search(PlayniteTools.NormalizeGameName(Name), Platform);
            List<HltbDataUser> dataSearch = HowLongToBeat.PluginDatabase.HowLongToBeatClient.Search(Name, Platform);

            List<HltbDataUser> dataSearchFinal = new List<HltbDataUser>();
            dataSearchFinal.AddRange(dataSearchNormalized);
            dataSearchFinal.AddRange(dataSearch);
            return dataSearchFinal.GroupBy(x => x.Id).Select(x => x.First()).ToList();
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

                string[] searchTerms = Name.Split(' ');
                requestMessage.Content = new StringContent("{\"searchType\":\"games\",\"searchTerms\":[" + String.Join(",", searchTerms.Select(x => "\"" + x + "\"")) + "],\"searchPage\":1,\"size\":20,\"searchOptions\":{\"games\":{\"userId\":0,\"platform\":\"" + Platform + "\",\"sortCategory\":\"popular\",\"rangeCategory\":\"main\",\"rangeTime\":{\"min\":0,\"max\":0},\"gameplay\":{\"perspective\":\"\",\"flow\":\"\",\"genre\":\"\"},\"modifier\":\"\"},\"users\":{\"sortCategory\":\"postcount\"},\"filter\":\"\",\"sort\":0,\"randomizer\":0}}", Encoding.UTF8, "application/json");

                HttpResponseMessage response = await httpClient.PostAsync(UrlSearch, requestMessage.Content);
                string json = await response.Content.ReadAsStringAsync();
                Serialization.TryFromJson(json, out HltbSearchRoot hltbSearchObj);

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

            if (API.Instance.ApplicationInfo.Mode == ApplicationMode.Desktop)
            {
                HowLongToBeatSelect ViewExtension = null;
                _ = Application.Current.Dispatcher.BeginInvoke((Action)delegate
                {
                    ViewExtension = new HowLongToBeatSelect(null, game);
                    Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(ResourceProvider.GetString("LOCSelection") + " - " + game.Name + " - " + (game.Source?.Name ?? "Playnite"), ViewExtension);
                    _ = windowExtension.ShowDialog();
                }).Wait();

                if (ViewExtension.GameHowLongToBeat?.Items.Count > 0)
                {
                    return ViewExtension.GameHowLongToBeat;
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
                try
                {
                    string Name = string.Empty;
                    int Id = 0;
                    string UrlImg = string.Empty;
                    string Url = string.Empty;
                    string Platform = string.Empty;

                    foreach (HltbSearchData entry in data.data)
                    {
                        Name = entry.game_name;
                        Id = entry.game_id;
                        UrlImg = string.Format(UrlGameImg, entry.game_image);
                        Url = string.Format(UrlGame, Id);
                        Url = string.Format(UrlGame, Id);
                        Platform = entry.profile_platform;

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
                            Platform = Platform,
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
                _ = SpinWait.SpinUntil(() => PluginDatabase.IsLoaded, -1);
                UserId = HowLongToBeat.PluginDatabase.Database.UserHltbData.UserId;
            }

            if (UserId == 0)
            {
                IsConnected = false;
                return false;
            }

            if (IsConnected == null)
            {
                IsConnected = GetUserId() != 0;
            }

            IsConnected = (bool)IsConnected;
            return (bool)IsConnected;
        }

        public void Login()
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate
            {
                Logger.Info("Login()");
                using (IWebView WebView = API.Instance.WebViews.CreateView(490, 670))
                {
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
                    _ = WebView.OpenDialog();
                }
            }).Completed += (s, e) =>
            {
                if ((bool)IsConnected)
                {
                    _ = Application.Current.Dispatcher?.BeginInvoke((Action)delegate
                    {
                        try
                        {
                            List<HttpCookie> Cookies = GetWebCookies();
                            _ = SetStoredCookies(Cookies);

                            PluginDatabase.Plugin.SavePluginSettings(PluginDatabase.PluginSettings.Settings);

                            _ = Task.Run(() =>
                            {
                                UserId = GetUserId();
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


        private int GetUserId()
        {
            try
            {
                List<HttpCookie> Cookies = GetStoredCookies();
                string response = Web.DownloadStringData("https://howlongtobeat.com/api/user", Cookies).GetAwaiter().GetResult();
                dynamic t = Serialization.FromJson<dynamic>(response);
                return t.data[0].user_id;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return 0;
            }
        }


        private HltbUserGamesList GetUserGamesList(bool WithDateUpdate = false)
        {
            try
            {
                List<HttpCookie> Cookies = GetStoredCookies();
                string payload = "{\"user_id\":" + UserId + ",\"lists\":[\"playing\",\"completed\",\"retired\"],\"set_playstyle\":\"comp_all\",\"name\":\"\",\"platform\":\"\",\"storefront\":\"\",\"sortBy\":\"\",\"sortFlip\":0,\"view\":\"\",\"limit\":10000,\"currentUserHome\":true}";
                string json = Web.PostStringDataPayload(string.Format(UrlUserGamesList, UserId), payload, Cookies).GetAwaiter().GetResult();
                _ = Serialization.TryFromJson(json, out HltbUserGamesList hltbUserGameList);
                return hltbUserGameList;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
                return new HltbUserGamesList();
            }
        }


        private TitleList GetTitleList(GamesList x)
        {
            try
            {
                _ = DateTime.TryParse(x.date_updated, out DateTime LastUpdate);
                _ = DateTime.TryParse(x.date_complete, out DateTime Completion);
                DateTime? CompletionFinal = null;
                if (Completion != default)
                {
                    CompletionFinal = Completion;
                }

                TitleList titleList = new TitleList
                {
                    UserGameId = x.id.ToString(),
                    GameName = x.custom_title,
                    Platform = x.platform,
                    Id = x.game_id,
                    CurrentTime = x.invested_pro,
                    IsReplay = x.play_count == 2,
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
            Logger.Info($"GetSubmitData({GameName}, {UserGameId})");
            try
            {
                List<HttpCookie> Cookies = GetStoredCookies();

                string response = Web.DownloadStringData(string.Format(UrlPostDataEdit, UserGameId), Cookies).GetAwaiter().GetResult();
                if (response.IsNullOrEmpty())
                {
                    Logger.Warn($"No SubmitData for {GameName} - {UserGameId}");
                    return null;
                }

                string jsonData = Tools.GetJsonInString(response, "<script id=\"__NEXT_DATA__\" type=\"application/json\">", "</script></body>");
                HltbPostData hltbPostData = new HltbPostData();

                if (Serialization.TryFromJson(jsonData, out NEXT_DATA data) && data?.props?.pageProps?.editData?.userId != null)
                {
                    hltbPostData.user_id = data.props.pageProps.editData.userId;
                    hltbPostData.edit_id = data.props.pageProps.editData.submissionId;
                    hltbPostData.game_id = data.props.pageProps.gameData.game_id;
                    hltbPostData.custom_title = data.props.pageProps.gameData.game_name;
                    hltbPostData.platform = data.props.pageProps.editData.platform;
                    hltbPostData.storefront = data.props.pageProps.editData.storefront;
                    hltbPostData.list_p = data.props.pageProps.editData.lists.playing ? "1" : "0";
                    hltbPostData.list_b = data.props.pageProps.editData.lists.backlog ? "1" : "0";
                    hltbPostData.list_r = data.props.pageProps.editData.lists.replay ? "1" : "0";
                    hltbPostData.list_c = data.props.pageProps.editData.lists.custom ? "1" : "0";
                    hltbPostData.list_cp = data.props.pageProps.editData.lists.completed ? "1" : "0";
                    hltbPostData.list_rt = data.props.pageProps.editData.lists.retired ? "1" : "0";                    
                    hltbPostData.protime_h = data.props.pageProps.editData.general.progress.hours?.ToString() ?? "0";
                    hltbPostData.protime_m = data.props.pageProps.editData.general.progress.minutes?.ToString() ?? "0";
                    hltbPostData.protime_s = data.props.pageProps.editData.general.progress.seconds?.ToString() ?? "0";
                    hltbPostData.rt_notes = data.props.pageProps.editData.general.retirementNotes;
                    hltbPostData.compyear = data.props.pageProps.editData.general.completionDate.year;
                    hltbPostData.compmonth = data.props.pageProps.editData.general.completionDate.month;
                    hltbPostData.compday = data.props.pageProps.editData.general.completionDate.day;
                    hltbPostData.playCount = data.props.pageProps.editData.singlePlayer.playCount ? "1" : "0";
                    hltbPostData.includesDLC = data.props.pageProps.editData.singlePlayer.includesDLC ? "1" : "0";
                    hltbPostData.c_main_h = data.props.pageProps.editData.singlePlayer.compMain.time.hours?.ToString() ?? "0";
                    hltbPostData.c_main_m = data.props.pageProps.editData.singlePlayer.compMain.time.minutes?.ToString() ?? "0";
                    hltbPostData.c_main_s = data.props.pageProps.editData.singlePlayer.compMain.time.seconds?.ToString() ?? "0";
                    hltbPostData.c_main_notes = data.props.pageProps.editData.singlePlayer.compMain.notes;
                    hltbPostData.c_plus_h = data.props.pageProps.editData.singlePlayer.compPlus.time.hours?.ToString() ?? "0";
                    hltbPostData.c_plus_m = data.props.pageProps.editData.singlePlayer.compPlus.time.minutes?.ToString() ?? "0";
                    hltbPostData.c_plus_s = data.props.pageProps.editData.singlePlayer.compPlus.time.seconds?.ToString() ?? "0";
                    hltbPostData.c_plus_notes = data.props.pageProps.editData.singlePlayer.compPlus.notes;
                    hltbPostData.c_100_h = data.props.pageProps.editData.singlePlayer.comp100.time.hours?.ToString() ?? "0";
                    hltbPostData.c_100_m = data.props.pageProps.editData.singlePlayer.comp100.time.minutes?.ToString() ?? "0";
                    hltbPostData.c_100_s = data.props.pageProps.editData.singlePlayer.comp100.time.seconds?.ToString() ?? "0";
                    hltbPostData.c_100_notes = data.props.pageProps.editData.singlePlayer.comp100.notes;
                    hltbPostData.c_speed_h = data.props.pageProps.editData.speedRuns.percAny.time.hours?.ToString() ?? "0";
                    hltbPostData.c_speed_m = data.props.pageProps.editData.speedRuns.percAny.time.minutes?.ToString() ?? "0";
                    hltbPostData.c_speed_s = data.props.pageProps.editData.speedRuns.percAny.time.seconds?.ToString() ?? "0";
                    hltbPostData.c_speed_notes = data.props.pageProps.editData.speedRuns.percAny.notes;
                    hltbPostData.c_speed100_h = data.props.pageProps.editData.speedRuns.perc100.time.hours?.ToString() ?? "0";
                    hltbPostData.c_speed100_m = data.props.pageProps.editData.speedRuns.perc100.time.minutes?.ToString() ?? "0";
                    hltbPostData.c_speed100_s = data.props.pageProps.editData.speedRuns.perc100.time.seconds?.ToString() ?? "0";
                    hltbPostData.c_speed100_notes = data.props.pageProps.editData.speedRuns.perc100.notes;
                    hltbPostData.cotime_h = data.props.pageProps.editData.multiPlayer.coOp.time.hours?.ToString() ?? "0";
                    hltbPostData.cotime_m = data.props.pageProps.editData.multiPlayer.coOp.time.minutes?.ToString() ?? "0";
                    hltbPostData.cotime_s = data.props.pageProps.editData.multiPlayer.coOp.time.seconds?.ToString() ?? "0";
                    hltbPostData.mptime_h = data.props.pageProps.editData.multiPlayer.vs.time.hours?.ToString() ?? "0";
                    hltbPostData.mptime_m = data.props.pageProps.editData.multiPlayer.vs.time.minutes?.ToString() ?? "0";
                    hltbPostData.mptime_s = data.props.pageProps.editData.multiPlayer.vs.time.seconds?.ToString() ?? "0";
                    hltbPostData.review_score = data.props.pageProps.editData.review.score;
                    hltbPostData.review_notes = data.props.pageProps.editData.review.notes;
                    hltbPostData.play_notes = data.props.pageProps.editData.additionals.notes;
                    hltbPostData.play_video = data.props.pageProps.editData.additionals.video;
                }
                else
                {
                    throw new Exception($"No submit data find for {GameName} - {UserGameId}");
                }

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
                    if (!Serialization.TryFromJsonFile(PathHltbUserStats, out hltbDataUser))
                    {
                        return new HltbUserStats();
                    }
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
                API.Instance.Notifications.Add(new NotificationMessage(
                    $"{PluginDatabase.PluginName}-Import-Error",
                    PluginDatabase.PluginName + Environment.NewLine + ResourceProvider.GetString("LOCCommonNotLoggedIn"),
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
                    return data?.TitlesList?.Find(x => x.Id == game_id);
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                }

                return null;
            }
            else
            {
                API.Instance.Notifications.Add(new NotificationMessage(
                    $"{PluginDatabase.PluginName}-Import-Error",
                    PluginDatabase.PluginName + Environment.NewLine + ResourceProvider.GetString("LOCCommonNotLoggedIn"),
                    NotificationType.Error,
                    () => PluginDatabase.Plugin.OpenSettingsView()
                ));
                return null;
            }
        }


        public bool EditIdExist(string UserGameId)
        {
            return GetUserGamesList()?.data?.gamesList?.Find(x => x.id.ToString().IsEqual(UserGameId))?.id != null;
        }

        public string FindIdExisting(string GameId)
        {
            return GetUserGamesList()?.data?.gamesList?.Find(x => x.game_id.ToString().IsEqual(GameId))?.id.ToString() ?? null;
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
                    List<HttpCookie> Cookies = GetStoredCookies();

                    string payload = "{\"submissionId\":" + hltbPostData.edit_id + ",\"userId\":" + hltbPostData.user_id + ",\"userName\":\"" + UserLogin
                        + "\",\"gameId\":" + hltbPostData.game_id + ",\"title\":\"" + hltbPostData.custom_title + "\",\"platform\":\"" + hltbPostData.platform
                        + "\",\"storefront\":\"" + hltbPostData.storefront + "\",\"lists\":{\"playing\":" + (hltbPostData.list_p == "1" ? "true" : "false")
                        + ",\"backlog\":" + (hltbPostData.list_b == "1" ? "true" : "false")
                        + ",\"replay\":" + (hltbPostData.list_r == "1" ? "true" : "false")
                        + ",\"custom\":" + (hltbPostData.list_c == "1" ? "true" : "false")
                        + ",\"custom2\":false,\"custom3\":false,\"completed\":" + (hltbPostData.list_cp == "1" ? "true" : "false")
                        + ",\"retired\":" + (hltbPostData.list_rt == "1" ? "true" : "false")
                        + "},\"general\":{\"progress\":{\"hours\":" + hltbPostData.protime_h + ",\"minutes\":" + hltbPostData.protime_m
                        + ",\"seconds\":" + hltbPostData.protime_s + "},\"retirementNotes\":\"" + hltbPostData.rt_notes
                        + "\",\"completionDate\":{\"year\":\"" + hltbPostData.compyear
                        + "\",\"month\":\"" + hltbPostData.compmonth + "\",\"day\":\"" + hltbPostData.compday
                        + "\"},"
                        + "\"progressBefore\":{\"hours\":0,\"minutes\":0,\"seconds\":0}"
                        + "},\"singlePlayer\":{\"playCount\":" + (hltbPostData.playCount == "0" ? "false" : "true")
                        + ",\"includesDLC\":" + (hltbPostData.includesDLC == "1" ? "true" : "false") + ",\"compMain\":{\"time\":{\"hours\":" + hltbPostData.c_main_h
                        + ",\"minutes\":" + hltbPostData.c_main_m + ",\"seconds\":" + hltbPostData.c_main_s + "},\"notes\":\"" + hltbPostData.c_main_notes
                        + "\"},\"compPlus\":{\"time\":{\"hours\":" + hltbPostData.c_plus_h + ",\"minutes\":" + hltbPostData.c_plus_m + ",\"seconds\":" + hltbPostData.c_plus_s
                        + "},\"notes\":\"" + hltbPostData.c_plus_notes + "\"},\"comp100\":{\"time\":{\"hours\":" + hltbPostData.c_100_h
                        + ",\"minutes\":" + hltbPostData.c_100_m + ",\"seconds\":" + hltbPostData.c_100_s + "},\"notes\":\"" + hltbPostData.c_100_notes
                        + "\"}},\"speedRuns\":{\"percAny\":{\"time\":{\"hours\":" + hltbPostData.c_speed_h + ",\"minutes\":" + hltbPostData.c_speed_m
                        + ",\"seconds\":" + hltbPostData.c_speed_s + "},\"notes\":\"" + hltbPostData.c_speed_notes + "\"},\"perc100\":{\"time\":{\"hours\":" + hltbPostData.c_speed100_h
                        + ",\"minutes\":" + hltbPostData.c_speed100_m + ",\"seconds\":" + hltbPostData.c_speed100_s + "},\"notes\":\"" + hltbPostData.c_speed100_notes
                        + "\"}},\"multiPlayer\":{\"coOp\":{\"time\":{\"hours\":" + hltbPostData.cotime_h + ",\"minutes\":" + hltbPostData.cotime_m
                        + ",\"seconds\":" + hltbPostData.cotime_s + "}},\"vs\":{\"time\":{\"hours\":" + hltbPostData.mptime_h + ",\"minutes\":" + hltbPostData.mptime_m
                        + ",\"seconds\":" + hltbPostData.mptime_s + "}}},\"review\":{\"score\":" + hltbPostData.review_score + ",\"notes\":\"" + hltbPostData.review_notes
                        + "\"},\"additionals\":{\"notes\":\"" + hltbPostData.play_notes + "\",\"video\":\"" + hltbPostData.play_video
                        + "\"},\"manualTimer\":{\"time\":{\"hours\":null,\"minutes\":null,\"seconds\":null}},\"adminId\":null}";

                    string response = await Web.PostStringDataPayload(UrlPostData, payload, Cookies);


                    // Check errors
                    // TODO Rewrite
                    if (response.Contains("error"))
                    {
                        Serialization.TryFromJson(response, out dynamic error);
                        API.Instance.Notifications.Add(new NotificationMessage(
                            $"{PluginDatabase.PluginName}-{game.Id}-Error",
                            PluginDatabase.PluginName + Environment.NewLine + game.Name + (error?["error"]?[0] != null ? System.Environment.NewLine + error["error"][0] : string.Empty),
                            NotificationType.Error
                        ));
                    }
                    else if (response.IsNullOrEmpty())
                    {
                        API.Instance.Notifications.Add(new NotificationMessage(
                              $"{PluginDatabase.PluginName}-{game.Id}-Error",
                              PluginDatabase.PluginName + Environment.NewLine + game.Name,
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
                API.Instance.Notifications.Add(new NotificationMessage(
                    $"{PluginDatabase.PluginName}-DataUpdate-Error",
                    PluginDatabase.PluginName + Environment.NewLine + ResourceProvider.GetString("LOCCommonNotLoggedIn"),
                    NotificationType.Error,
                    () => PluginDatabase.Plugin.OpenSettingsView()
                ));
                return false;
            }

            return false;
        }


        #region Cookies
        /// <summary>
        /// Read the last identified cookies stored.
        /// </summary>
        /// <returns></returns>
        internal List<HttpCookie> GetStoredCookies()
        {
            string InfoMessage = "No stored cookies";

            if (File.Exists(FileCookies))
            {
                try
                {
                    List<HttpCookie> StoredCookies = Serialization.FromJson<List<HttpCookie>>(
                        Encryption.DecryptFromFile(
                            FileCookies,
                            Encoding.UTF8,
                            WindowsIdentity.GetCurrent().User.Value));

                    bool isExpired = StoredCookies.Find(x => x.Name == "hltb_alive")?.Expires < DateTime.Now;
                    if (isExpired)
                    {
                        InfoMessage = "Expired cookies";
                    }
                    else
                    {
                        return StoredCookies;
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, "Failed to load saved cookies");
                }
            }

            Logger.Info(InfoMessage);
            List<HttpCookie> httpCookies = GetWebCookies();
            if (httpCookies?.Count > 0)
            {
                _ = SetStoredCookies(httpCookies);
                return httpCookies;
            }

            return null;
        }

        /// <summary>
        /// Save the last identified cookies stored.
        /// </summary>
        /// <param name="httpCookies"></param>
        internal bool SetStoredCookies(List<HttpCookie> httpCookies)
        {
            try
            {
                FileSystem.CreateDirectory(Path.GetDirectoryName(FileCookies));
                Encryption.EncryptToFile(
                    FileCookies,
                    Serialization.ToJson(httpCookies),
                    Encoding.UTF8,
                    WindowsIdentity.GetCurrent().User.Value);
                return true;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, "Failed to save cookies");
            }

            return false;
        }

        /// <summary>
        /// Get cookies in WebView or another method.
        /// </summary>
        /// <returns></returns>
        internal virtual List<HttpCookie> GetWebCookies(IWebView WebViewOffscreen = null)
        {
            List<HttpCookie> httpCookies = new List<HttpCookie>();

            try
            {
                if (WebViewOffscreen == null)
                {
                    using (WebViewOffscreen = API.Instance.WebViews.CreateOffscreenView())
                    {
                        httpCookies = WebViewOffscreen?.GetCookies()?
                            .Where(x => (x?.Domain?.Contains("howlongtobeat") ?? false) || (x?.Domain?.Contains("hltb") ?? false))?
                            .ToList() ?? new List<HttpCookie>();
                    }
                }
                else
                {
                    httpCookies = WebViewOffscreen.GetCookies()?
                        .Where(x => (x?.Domain?.Contains("howlongtobeat") ?? false) || (x?.Domain?.Contains("hltb") ?? false))?
                        .ToList() ?? new List<HttpCookie>();
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, "HowLongToBeat");
            }

            return httpCookies;
        }
        #endregion
    }
}
