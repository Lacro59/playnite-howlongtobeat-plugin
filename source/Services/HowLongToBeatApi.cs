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
using HowLongToBeat.Models.Api;
using System.Text.RegularExpressions;
using FuzzySharp;
using HowLongToBeat.Models.Enumerables;

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


    public class HowLongToBeatApi : ObservableObject
    {
        private static ILogger Logger => LogManager.GetLogger();

        private static HowLongToBeatDatabase PluginDatabase => HowLongToBeat.PluginDatabase;


        internal string FileCookies { get; }

        private static string SearchId { get; set; } = null;

        private static DateTime CookiesLastAccess { get; set; } = default;
        private static List<HttpCookie> CookiesStored { get; set; } = null;


        #region Urls
        private static string UrlBase => "https://howlongtobeat.com";

        private static string UrlLogin => UrlBase + "/login";
        private static string UrlLogOut => UrlBase + "/login?t=out";
        private static string UrlSearchWeb => UrlBase + "/?q={0}";

        private static string UrlUser => UrlBase + "/api/user";
        private static string UrlUserStats => UrlUser + "?n={0}&s=stats";
        private static string UrlUserStatsMore => UrlBase + "/user_stats_more";
        private static string UrlUserStatsGamesList => UrlUser + "/{0}/stats";
        private static string UrlUserGamesList => UrlUser + "/{0}/games/list";
        private static string UrlUserStatsGameDetails => UrlBase + "/user_games_detail";

        private static string UrlPostData => UrlBase + "/api/submit";
        private static string UrlPostDataEdit => UrlBase + "/submit/edit/{0}";
        private static string UrlSearch => UrlBase + "/api/seek";

        private static string UrlGameImg => UrlBase + "/games/{0}";

        private static string UrlGame => UrlBase + "/game?id={0}";

        private static string UrlExportAll => UrlBase + "/user_export?all=1";
        #endregion


        private bool? isConnected = null;
        public bool? IsConnected { get => isConnected; set => SetValue(ref isConnected, value); }


        public string UserLogin { get; set; } = string.Empty;
        public int UserId { get; set; } = 0;

        private bool IsFirst = true;


        public HowLongToBeatApi()
        {
            UserLogin = PluginDatabase.PluginSettings.Settings.UserLogin;

            string pathData = PluginDatabase.Paths.PluginUserDataPath;

            // TEMP
            FileCookies = Path.Combine(pathData, CommonPlayniteShared.Common.Paths.GetSafePathName($"HowLongToBeat.json"));
            FileSystem.DeleteFileSafe(FileCookies);

            FileCookies = Path.Combine(pathData, CommonPlayniteShared.Common.Paths.GetSafePathName($"HowLongToBeat.dat"));
        }


        private async Task<GameData> GetGameData(string id)
        {
            try
            {
                string response = await Web.DownloadStringData(string.Format(UrlGame, id));
                string jsonData = Tools.GetJsonInString(response, @"<script[ ]?id=""__NEXT_DATA__""[ ]?type=""application/json"">");
                _ = Serialization.TryFromJson(jsonData, out NEXT_DATA next_data, out Exception ex);
                if (ex != null)
                {
                    Common.LogError(ex, false, false, PluginDatabase.PluginName);
                }

                return next_data?.Props?.PageProps?.Game?.Data?.Game != null
                    ? next_data.Props.PageProps.Game.Data.Game.FirstOrDefault()
                    : null;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            return null;
        }


        #region Search
        private async Task<List<HltbDataUser>> Search(string name, string platform = "")
        {
            try
            {
                SearchResult searchResult = await ApiSearch(name, platform);

                List<HltbDataUser> search = searchResult?.Data?.Select(x =>
                    new HltbDataUser
                    {
                        Name = x.GameName,
                        Id = x.GameId.ToString(),
                        UrlImg = string.Format(UrlGameImg, x.GameImage),
                        Url = string.Format(UrlGame, x.GameId),
                        Platform = x.ProfilePlatform,
                        GameType = x.GameType.IsEqual("game") ? GameType.Game : x.GameType.IsEqual("multi") ? GameType.Multi : GameType.Compil,
                        GameHltbData = new HltbData
                        {
                            GameType = x.GameType.IsEqual("game") ? GameType.Game : x.GameType.IsEqual("multi") ? GameType.Multi : GameType.Compil,
                            MainStoryClassic = x.CompMain,
                            MainExtraClassic = x.CompPlus,
                            CompletionistClassic = x.Comp100,
                            SoloClassic = x.CompAll,
                            CoOpClassic = x.InvestedCo,
                            VsClassic = x.InvestedMp
                        }
                    }
                )?.ToList() ?? new List<HltbDataUser>();

                search.ForEach(x =>
                {
                    GameData gameData = GetGameData(x.Id).GetAwaiter().GetResult();
                    if (gameData != null)
                    {
                        x.GameHltbData.MainStoryMedian = gameData.CompMainMed;
                        x.GameHltbData.MainExtraMedian = gameData.CompPlusMed;
                        x.GameHltbData.CompletionistMedian = gameData.Comp100Med;
                        x.GameHltbData.SoloMedian = gameData.CompAllMed;
                        x.GameHltbData.CoOpMedian = gameData.InvestedCoMed;
                        x.GameHltbData.VsMedian = gameData.InvestedMpMed;

                        x.GameHltbData.MainStoryAverage = gameData.CompMainAvg;
                        x.GameHltbData.MainExtraAverage = gameData.CompPlusAvg;
                        x.GameHltbData.CompletionistAverage = gameData.Comp100Avg;
                        x.GameHltbData.SoloAverage = gameData.CompAllAvg;
                        x.GameHltbData.CoOpAverage = gameData.InvestedCoAvg;
                        x.GameHltbData.VsAverage = gameData.InvestedMpAvg;

                        x.GameHltbData.MainStoryRushed = gameData.CompMainL;
                        x.GameHltbData.MainExtraRushed = gameData.CompPlusL;
                        x.GameHltbData.CompletionistRushed = gameData.Comp100L;
                        x.GameHltbData.SoloRushed = gameData.CompAllL;
                        x.GameHltbData.CoOpRushed = gameData.InvestedCoL;
                        x.GameHltbData.VsRushed = gameData.InvestedMpL;

                        x.GameHltbData.MainStoryLeisure = gameData.CompMainH;
                        x.GameHltbData.MainExtraLeisure = gameData.CompPlusH;
                        x.GameHltbData.CompletionistLeisure = gameData.Comp100H;
                        x.GameHltbData.SoloLeisure = gameData.CompAllH;
                        x.GameHltbData.CoOpLeisure = gameData.InvestedCoH;
                        x.GameHltbData.VsLeisure = gameData.InvestedMpH;
                    }
                    else
                    {
                        Logger.Warn($"No GameData find for {x.Name} with {x.Id}");
                    }
                });

                return search;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
                return new List<HltbDataUser>();
            }
        }

        private async Task<string> GetSearchId()
        {
            if (!SearchId.IsNullOrEmpty())
            {
                return SearchId;
            }

            try
            {
                string url = UrlBase;
                string response = await Web.DownloadStringData(url);
                string js = Regex.Match(response, @"_app-\w*.js").Value;
                if (!js.IsNullOrEmpty())
                {
                    url += $"/_next/static/chunks/pages/{js}";
                    response = await Web.DownloadStringData(url);
                    Match matches = Regex.Match(response, "\"/api/seek/\".concat[(]\"(\\w*)\"[)].concat[(]\"(\\w*)\"[)]");
                    SearchId = matches.Groups[1].Value + matches.Groups[2].Value;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            return SearchId;
        }

        public async Task<List<HltbSearch>> SearchTwoMethod(string name, string platform = "")
        {
            List<HltbDataUser> dataSearchNormalized = await Search(PlayniteTools.NormalizeGameName(name), platform);
            List<HltbDataUser> dataSearch = await Search(name, platform);

            List<HltbDataUser> dataSearchFinal = new List<HltbDataUser>();
            dataSearchFinal.AddRange(dataSearchNormalized);
            dataSearchFinal.AddRange(dataSearch);

            dataSearchFinal = dataSearchFinal.GroupBy(x => x.Id).Select(x => x.First()).ToList();

            return dataSearchFinal.Select(x => new HltbSearch { MatchPercent = Fuzz.Ratio(name.ToLower(), x.Name.ToLower()), Data = x })
                .OrderByDescending(x => x.MatchPercent)
                .ToList();
        }

        private async Task<SearchResult> ApiSearch(string name, string platform = "")
        {
            try
            {
                List<HttpHeader> httpHeaders = new List<HttpHeader>
                {
                    new HttpHeader { Key = "User-Agent", Value = Web.UserAgent },
                    new HttpHeader { Key = "Origin", Value = UrlBase },
                    new HttpHeader { Key = "Referer", Value = UrlBase }
                };

                SearchParam searchParam = new SearchParam
                {
                    SearchTerms = name.Split(' ').ToList(),
                    SearchOptions = new SearchOptions { Games = new Games { Platform = platform } }
                };

                SearchResult searchResult;
                using (HttpClient httpClient = new HttpClient())
                {
                    httpHeaders.ForEach(x =>
                    {
                        httpClient.DefaultRequestHeaders.Add(x.Key, x.Value);
                    });

                    string serializedBody = Serialization.ToJson(searchParam);

                    HttpRequestMessage requestMessage = new HttpRequestMessage
                    {
                        Content = new StringContent(serializedBody, Encoding.UTF8, "application/json")
                    };

                    string searchId = await GetSearchId();
                    Thread.Sleep(2000);
                    HttpResponseMessage response = await httpClient.PostAsync(UrlSearch + "/" + searchId, requestMessage.Content);
                    string json = await response.Content.ReadAsStringAsync();

                    _ = Serialization.TryFromJson(json, out searchResult);
                }

                return searchResult;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
                return null;
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
                    ViewExtension = new HowLongToBeatSelect(game, null);
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
        #endregion


        #region user account
        public bool GetIsUserLoggedIn()
        {
            if (UserId == 0)
            {
                _ = SpinWait.SpinUntil(() => PluginDatabase.IsLoaded, -1);
                UserId = PluginDatabase.Database.UserHltbData.UserId;
            }

            if (UserId == 0)
            {
                IsConnected = false;
                return false;
            }

            if (IsConnected == null)
            {
                IsConnected = GetUserId().GetAwaiter().GetResult() != 0;
            }

            IsConnected = (bool)IsConnected;
            return (bool)IsConnected;
        }

        public void Login()
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate
            {
                Logger.Info("Login()");
                using (IWebView webView = API.Instance.WebViews.CreateView(490, 670))
                {
                    webView.LoadingChanged += (s, e) =>
                    {
                        Common.LogDebug(true, $"NavigationChanged - {webView.GetCurrentAddress()}");

                        if (webView.GetCurrentAddress().StartsWith(UrlBase + "/user/"))
                        {
                            UserLogin = WebUtility.HtmlDecode(webView.GetCurrentAddress().Replace(UrlBase + "/user/", string.Empty));
                            IsConnected = true;

                            PluginDatabase.PluginSettings.Settings.UserLogin = UserLogin;

                            Thread.Sleep(1500);
                            webView.Close();
                        }
                    };

                    IsConnected = false;
                    webView.Navigate(UrlLogOut);
                    _ = webView.OpenDialog();
                }
            }).Completed += (s, e) =>
            {
                if ((bool)IsConnected)
                {
                    _ = Application.Current.Dispatcher?.BeginInvoke((Action)delegate
                    {
                        try
                        {
                            List<HttpCookie> cookies = GetWebCookies();
                            _ = SetStoredCookies(cookies);

                            PluginDatabase.Plugin.SavePluginSettings(PluginDatabase.PluginSettings.Settings);

                            _ = Task.Run(() =>
                            {
                                UserId = GetUserId().GetAwaiter().GetResult();
                                PluginDatabase.RefreshUserData();
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


        private async Task<int> GetUserId()
        {
            try
            {
                List<HttpCookie> cookies = GetStoredCookies();
                string response = await Web.DownloadStringData(UrlUser, cookies);
                dynamic t = Serialization.FromJson<dynamic>(response);
                return response == "{}" ? 0 : t?.data[0]?.user_id ?? 0;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return 0;
            }
        }


        private async Task<UserGamesList> GetUserGamesList()
        {
            try
            {
                List<HttpCookie> cookies = GetStoredCookies();

                UserGamesListParam userGamesListParam = new UserGamesListParam { UserId = UserId };
                string payload = Serialization.ToJson(userGamesListParam);

                string json = await Web.PostStringDataPayload(string.Format(UrlUserGamesList, UserId), payload, cookies);
                _ = Serialization.TryFromJson(json, out UserGamesList userGamesList);

                return userGamesList;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
                return null;
            }
        }


        private TitleList GetTitleList(GamesList gamesList)
        {
            try
            {
                _ = DateTime.TryParse(gamesList.DateUpdated, out DateTime lastUpdate);
                _ = DateTime.TryParse(gamesList.DateComplete, out DateTime completion);
                _ = DateTime.TryParse(gamesList.DateStart, out DateTime dateStart);
                DateTime? completionFinal = null;
                if (completion != default)
                {
                    completionFinal = completion;
                }

                TitleList titleList = new TitleList
                {
                    UserGameId = gamesList.Id.ToString(),
                    GameName = gamesList.CustomTitle,
                    Platform = gamesList.Platform,
                    Id = gamesList.GameId.ToString(),
                    CurrentTime = gamesList.InvestedPro,
                    IsReplay = gamesList.PlayCount == 2,
                    IsRetired = gamesList.ListRetired == 1,
                    Storefront = gamesList.PlayStorefront,
                    StartDate = dateStart,
                    LastUpdate = lastUpdate,
                    Completion = completionFinal,
                    HltbUserData = new HltbData
                    {
                        GameType = gamesList.GameType.IsEqual("game") ? GameType.Game : gamesList.GameType.IsEqual("multi") ? GameType.Multi : GameType.Compil,
                        MainStoryClassic = gamesList.CompMain,
                        MainExtraClassic = gamesList.CompPlus,
                        CompletionistClassic = gamesList.Comp100,
                        SoloClassic = 0,
                        CoOpClassic = gamesList.InvestedCo,
                        VsClassic = gamesList.InvestedMp
                    },
                    GameStatuses = new List<GameStatus>()
                };

                if (gamesList.ListBacklog == 1)
                {
                    titleList.GameStatuses.Add(new GameStatus { Status = StatusType.Backlog });
                }

                if (gamesList.ListComp == 1)
                {
                    titleList.GameStatuses.Add(new GameStatus { Status = StatusType.Completed });
                }

                if (gamesList.ListCustom == 1)
                {
                    titleList.GameStatuses.Add(new GameStatus { Status = StatusType.CustomTab });
                }

                if (gamesList.ListPlaying == 1)
                {
                    titleList.GameStatuses.Add(new GameStatus { Status = StatusType.Playing });
                }

                if (gamesList.ListReplay == 1)
                {
                    titleList.GameStatuses.Add(new GameStatus { Status = StatusType.Replays });
                }

                if (gamesList.ListRetired == 1)
                {
                    titleList.GameStatuses.Add(new GameStatus { Status = StatusType.Retired });
                }

                return titleList;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
                return null;
            }
        }

        public async Task<EditData> GetEditData(string gameName, string userGameId)
        {
            Logger.Info($"GetEditData({gameName}, {userGameId})");
            try
            {
                List<HttpCookie> cookies = GetStoredCookies();

                string response = await Web.DownloadStringData(string.Format(UrlPostDataEdit, userGameId), cookies);
                if (response.IsNullOrEmpty() && !response.Contains("__NEXT_DATA__"))
                {
                    Logger.Warn($"No EditData for {gameName} - {userGameId}");
                    return null;
                }

                string jsonData = Tools.GetJsonInString(response, @"<script[ ]?id=""__NEXT_DATA__""[ ]?type=""application/json"">");
                _ = Serialization.TryFromJson(jsonData, out NEXT_DATA next_data, out Exception ex);
                if (ex != null)
                {
                    Common.LogError(ex, false, false, PluginDatabase.PluginName);
                }

                return next_data?.Props?.PageProps?.EditData?.UserId != null
                    ? next_data.Props.PageProps.EditData
                    : throw new Exception($"No EditData find for {gameName} - {userGameId}");
            }
            catch (Exception ex)
            {
                if (IsFirst)
                {
                    IsFirst = false;
                    return await GetEditData(gameName, userGameId);
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
            string pathHltbUserStats = Path.Combine(PluginDatabase.Plugin.GetPluginUserDataPath(), "HltbUserStats.json");
            HltbUserStats hltbDataUser = new HltbUserStats();

            if (File.Exists(pathHltbUserStats))
            {
                try
                {
                    if (!Serialization.TryFromJsonFile(pathHltbUserStats, out hltbDataUser))
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
                HltbUserStats hltbUserStats = new HltbUserStats
                {
                    Login = UserLogin.IsNullOrEmpty() ? PluginDatabase.Database.UserHltbData.Login : UserLogin,
                    UserId = (UserId == 0) ? PluginDatabase.Database.UserHltbData.UserId : UserId,
                    TitlesList = new List<TitleList>()
                };

                UserGamesList userGamesList = GetUserGamesList().GetAwaiter().GetResult();
                if (userGamesList == null)
                {
                    return null;
                }

                try
                {
                    userGamesList.Data.GamesList.ForEach(x =>
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

        public TitleList GetUserData(string gameId)
        {
            if (GetIsUserLoggedIn())
            {
                try
                {
                    HltbUserStats data = GetUserData();
                    return data?.TitlesList?.Find(x => x.Id == gameId);
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


        public bool EditIdExist(string userGameId)
        {
            return GetUserGamesList()?.GetAwaiter().GetResult()?.Data?.GamesList?.Find(x => x.Id.ToString().IsEqual(userGameId))?.Id != null;
        }

        public string FindIdExisting(string gameId)
        {
            return GetUserGamesList()?.GetAwaiter().GetResult().Data?.GamesList?.Find(x => x.GameId.ToString().IsEqual(gameId))?.Id.ToString() ?? null;
        }
        #endregion


        /// <summary>
        /// Post current data in HowLongToBeat website.
        /// </summary>
        /// <param name="editData"></param>
        /// <returns></returns>
        public async Task<bool> ApiSubmitData(Game game, EditData editData)
        {
            if (GetIsUserLoggedIn() && editData.UserId != 0 && editData.GameId != 0)
            {
                try
                {
                    List<HttpCookie> Cookies = GetStoredCookies();
                    string payload = Serialization.ToJson(editData);
                    string response = await Web.PostStringDataPayload(UrlPostData, payload, Cookies);

                    // Check errors
                    // TODO Rewrite
                    if (response.Contains("error"))
                    {
                        _ = Serialization.TryFromJson(response, out dynamic error);
                        API.Instance.Notifications.Add(new NotificationMessage(
                            $"{PluginDatabase.PluginName}-{game.Id}-Error",
                            PluginDatabase.PluginName + Environment.NewLine + game.Name + (error?["error"]?[0] != null ? Environment.NewLine + error["error"][0] : string.Empty),
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
                        PluginDatabase.RefreshUserData(editData.GameId.ToString());
                        return true;
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
            string infoMessage = "No stored cookies";

            if (CookiesLastAccess > DateTime.Now.AddMinutes(-2))
            {
                return CookiesStored;
            }

            if (File.Exists(FileCookies))
            {
                try
                {
                    List<HttpCookie> storedCookies = Serialization.FromJson<List<HttpCookie>>(
                        Encryption.DecryptFromFile(
                            FileCookies,
                            Encoding.UTF8,
                            WindowsIdentity.GetCurrent().User.Value));

                    bool isExpired = storedCookies.Find(x => x.Name == "hltb_alive")?.Expires < DateTime.Now;
                    if (isExpired)
                    {
                        infoMessage = "Expired cookies";
                        Logger.Info(infoMessage);
                    }

                    CookiesStored = storedCookies;
                    CookiesLastAccess = DateTime.Now;
                    return CookiesStored;
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, "Failed to load saved cookies");
                    return null;
                }
            }

            Logger.Info(infoMessage);
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
        internal virtual List<HttpCookie> GetWebCookies(IWebView webView = null, bool deleteCookies = true)
        {
            List<HttpCookie> httpCookies = new List<HttpCookie>();

            try
            {
                if (webView == null)
                {
                    using (webView = API.Instance.WebViews.CreateOffscreenView())
                    {
                        httpCookies = webView?.GetCookies()?
                            .Where(x => x?.Domain?.Contains("howlongtobeat") ?? false)?
                            .ToList() ?? new List<HttpCookie>();
                    }
                }
                else
                {
                    httpCookies = webView.GetCookies()?
                        .Where(x => x?.Domain?.Contains("howlongtobeat") ?? false)?
                        .ToList() ?? new List<HttpCookie>();
                }

                if (deleteCookies)
                {
                    webView.DeleteDomainCookies("howlongtobeat.com");
                    webView.DeleteDomainCookies(".howlongtobeat.com");
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, "HowLongToBeat");
            }

            return httpCookies;
        }

        public void UpdatedCookies()
        {
            _ = Task.Run(() =>
            {
                try
                {
                    if (PluginDatabase.Database.UserHltbData.UserId != 0)
                    {
                        using (IWebView webView = API.Instance.WebViews.CreateOffscreenView())
                        {
                            List<HttpCookie> oldCookies = GetStoredCookies();
                            oldCookies?.ForEach(x =>
                            {
                                string domain = x.Domain.StartsWith(".") ? x.Domain.Substring(1) : x.Domain;
                                webView.SetCookies("https://" + domain, x);
                            });

                            webView.NavigateAndWait(UrlBase);

                            List<HttpCookie> cookies = GetWebCookies(webView);
                            _ = SetStoredCookies(cookies);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                }
            });
        }
        #endregion
    }
}
