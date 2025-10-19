﻿using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using FuzzySharp;
using HowLongToBeat.Models;
using HowLongToBeat.Models.Api;
using HowLongToBeat.Models.Enumerations;
using HowLongToBeat.Views;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace HowLongToBeat.Services
{
    public class HowLongToBeatApi : ObservableObject
    {
        private static ILogger Logger => LogManager.GetLogger();

        private static HowLongToBeatDatabase PluginDatabase => HowLongToBeat.PluginDatabase;


        /// <summary>
        /// Tool for managing cookies for HowLongToBeat sessions.
        /// </summary>
        protected CookiesTools CookiesTools { get; }
        /// <summary>
        /// List of domains for which cookies are managed.
        /// </summary>
        protected List<string> CookiesDomains { get; }
        /// <summary>
        /// Path to the file where cookies are stored.
        /// </summary>
        internal string FileCookies { get; }

        private static string SearchId { get; set; } = null;


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

        private static string SearchEndPoint => "/api/locate";
        private static string UrlSearch => UrlBase + SearchEndPoint;

        private static string UrlGameImg => UrlBase + "/games/{0}";

        private static string UrlGame => UrlBase + "/game?id={0}";

        private static string UrlExportAll => UrlBase + "/user_export?all=1";

        #endregion


        private bool? _isConnected = null;
        /// <summary>
        /// Indicates if the user is currently connected (logged in).
        /// </summary>
        public bool? IsConnected { get => _isConnected; set => SetValue(ref _isConnected, value); }

        /// <summary>
        /// The username of the currently logged-in user.
        /// </summary>
        public string UserLogin { get; set; } = string.Empty;
        /// <summary>
        /// The user ID of the currently logged-in user.
        /// </summary>
        public int UserId { get; set; } = 0;

        private bool IsFirst = true;


        /// <summary>
        /// Initializes a new instance of the <see cref="HowLongToBeatApi"/> class.
        /// </summary>
        public HowLongToBeatApi()
        {
            UserLogin = PluginDatabase.PluginSettings.Settings.UserLogin;

            CookiesDomains = new List<string> { ".howlongtobeat.com", "howlongtobeat.com" };
            string pathData = PluginDatabase.Paths.PluginUserDataPath;
            FileCookies = Path.Combine(pathData, CommonPlayniteShared.Common.Paths.GetSafePathName($"HowLongToBeat.dat"));
            CookiesTools = new CookiesTools(
                PluginDatabase.PluginName,
                "HowLongToBeat",
                FileCookies,
                CookiesDomains
            );
        }


        /// <summary>
        /// Retrieves game data from HowLongToBeat by game ID.
        /// </summary>
        /// <param name="id">The game ID.</param>
        /// <returns>Returns <see cref="HltbData"/> with game times, or null if not found.</returns>
        private async Task<HltbData> GetGameData(string id)
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

                GameData gameData = next_data?.Props?.PageProps?.Game?.Data?.Game != null
                    ? next_data.Props.PageProps.Game.Data.Game.FirstOrDefault()
                    : null;

                if (gameData != null)
                {
                    HltbData hltbData = new HltbData
                    {
                        MainStoryClassic = gameData.CompMain,
                        MainExtraClassic = gameData.CompPlus,
                        CompletionistClassic = gameData.Comp100,
                        SoloClassic = gameData.CompAll,
                        CoOpClassic = gameData.InvestedCo,
                        VsClassic = gameData.InvestedMp,

                        MainStoryMedian = gameData.CompMainMed,
                        MainExtraMedian = gameData.CompPlusMed,
                        CompletionistMedian = gameData.Comp100Med,
                        SoloMedian = gameData.CompAllMed,
                        CoOpMedian = gameData.InvestedCoMed,
                        VsMedian = gameData.InvestedMpMed,

                        MainStoryAverage = gameData.CompMainAvg,
                        MainExtraAverage = gameData.CompPlusAvg,
                        CompletionistAverage = gameData.Comp100Avg,
                        SoloAverage = gameData.CompAllAvg,
                        CoOpAverage = gameData.InvestedCoAvg,
                        VsAverage = gameData.InvestedMpAvg,

                        MainStoryRushed = gameData.CompMainL,
                        MainExtraRushed = gameData.CompPlusL,
                        CompletionistRushed = gameData.Comp100L,
                        SoloRushed = gameData.CompAllL,
                        CoOpRushed = gameData.InvestedCoL,
                        VsRushed = gameData.InvestedMpL,

                        MainStoryLeisure = gameData.CompMainH,
                        MainExtraLeisure = gameData.CompPlusH,
                        CompletionistLeisure = gameData.Comp100H,
                        SoloLeisure = gameData.CompAllH,
                        CoOpLeisure = gameData.InvestedCoH,
                        VsLeisure = gameData.InvestedMpH
                    };
                    return hltbData;
                }
                else
                {
                    Logger.Warn($"No GameData find with {id}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            return null;
        }

        /// <summary>
        /// Updates the HLTB data for a user game entry.
        /// </summary>
        /// <param name="hltbDataUser">The user game data to update.</param>
        /// <returns>Returns the updated <see cref="HltbDataUser"/>.</returns>
        public async Task<HltbDataUser> UpdateGameData(HltbDataUser hltbDataUser)
        {
            try
            {
                HltbData hltbData = await GetGameData(hltbDataUser.Id);
                hltbDataUser.GameHltbData = hltbData ?? hltbDataUser.GameHltbData;
                return hltbDataUser;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
                return null;
            }
        }


        #region Search

        /// <summary>
        /// Retrieves the search ID required for API search requests.
        /// </summary>
        /// <returns>Returns the search ID as a string.</returns>
        private async Task<string> GetSearchId()
        {
            if (!SearchId.IsNullOrEmpty())
            {
                return SearchId;
            }

            try
            {
                string url = UrlBase;
                var data = await Web.DownloadSourceDataWebView(url);
                string response = data.Item1;
                string js = Regex.Match(response, @"submit-\w*.js").Value;
                if (!js.IsNullOrEmpty())
                {
                    url += $"/_next/static/chunks/pages/{js}";
                    response = await Web.DownloadStringData(url);
                    Match matches = Regex.Match(response, $"\"{SearchEndPoint}/\".concat[(]\"(\\w*)\"[)].concat[(]\"(\\w*)\"[)]");
                    SearchId = matches.Groups[1].Value + matches.Groups[2].Value;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            return SearchId;
        }


        /// <summary>
        /// Searches for games on HowLongToBeat by name and platform.
        /// </summary>
        /// <param name="name">Game name to search for.</param>
        /// <param name="platform">Optional platform filter.</param>
        /// <returns>Returns a list of <see cref="HltbDataUser"/> matching the search.</returns>
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
                    x.GameHltbData = GetGameData(x.Id).GetAwaiter().GetResult();
                });
                return search;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
                return new List<HltbDataUser>();
            }
        }

        /// <summary>
        /// Performs two search methods (normalized and original name) and merges results.
        /// </summary>
        /// <param name="name">Game name to search for.</param>
        /// <param name="platform">Optional platform filter.</param>
        /// <returns>Returns a list of <see cref="HltbSearch"/> with match percentages.</returns>
        public async Task<List<HltbSearch>> SearchTwoMethod(string name, string platform = "")
        {
            List<HltbDataUser> dataSearchNormalized = await Search(PlayniteTools.NormalizeGameName(name, true, true), platform);
            List<HltbDataUser> dataSearch = await Search(name, platform);

            List<HltbDataUser> dataSearchFinal = new List<HltbDataUser>();
            dataSearchFinal.AddRange(dataSearchNormalized);
            dataSearchFinal.AddRange(dataSearch);

            dataSearchFinal = dataSearchFinal.GroupBy(x => x.Id).Select(x => x.First()).ToList();

            return dataSearchFinal.Select(x => new HltbSearch { MatchPercent = Fuzz.Ratio(name.ToLower(), x.Name.ToLower()), Data = x })
                .OrderByDescending(x => x.MatchPercent)
                .ToList();
        }

        /// <summary>
        /// Performs an API search for games.
        /// </summary>
        /// <param name="name">Game name to search for.</param>
        /// <param name="platform">Optional platform filter.</param>
        /// <returns>Returns a <see cref="SearchResult"/> object.</returns>
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


        /// <summary>
        /// Opens a selection window for the user to choose the correct game data.
        /// </summary>
        /// <param name="game">The Playnite game object.</param>
        /// <param name="data">Optional list of search results.</param>
        /// <returns>Returns a <see cref="GameHowLongToBeat"/> object if a selection is made, otherwise null.</returns>
        public GameHowLongToBeat SearchData(Game game, List<HltbDataUser> data = null)
        {
            Common.LogDebug(true, $"Search data for {game.Name}");
            if (API.Instance.ApplicationInfo.Mode == ApplicationMode.Desktop)
            {
                HowLongToBeatSelect ViewExtension = null;
                _ = Application.Current.Dispatcher.BeginInvoke((Action)delegate
                {
                    ViewExtension = new HowLongToBeatSelect(game, data);
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

        /// <summary>
        /// Checks if the user is currently logged in to HowLongToBeat.
        /// </summary>
        /// <returns>True if logged in, otherwise false.</returns>
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

        /// <summary>
        /// Initiates the login process for HowLongToBeat.
        /// </summary>
        public void Login()
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate
            {
                Logger.Info("Login()");

                WebViewSettings settings = new WebViewSettings
                {
                    JavaScriptEnabled = true,
                    WindowHeight = 670,
                    WindowWidth = 490,
                    UserAgent = Web.UserAgent
                };

                using (IWebView webView = API.Instance.WebViews.CreateView(settings))
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
                            List<HttpCookie> cookies = CookiesTools.GetWebCookies();
                            _ = CookiesTools.SetStoredCookies(cookies);

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

        /// <summary>
        /// Retrieves the user ID of the currently logged-in user.
        /// </summary>
        /// <returns>User ID as integer, or 0 if not logged in.</returns>
        private async Task<int> GetUserId()
        {
            try
            {
                List<HttpCookie> cookies = CookiesTools.GetStoredCookies();
                string response = await Web.DownloadPageText(UrlUser, cookies);
                dynamic t = Serialization.FromJson<dynamic>(response);
                return response == "{}" ? 0 : t?.data[0]?.user_id ?? 0;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return 0;
            }
        }

        /// <summary>
        /// Retrieves the list of games for the current user.
        /// </summary>
        /// <returns>Returns a <see cref="UserGamesList"/> object.</returns>
        private async Task<UserGamesList> GetUserGamesList()
        {
            try
            {
                List<HttpCookie> cookies = CookiesTools.GetStoredCookies();

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

        /// <summary>
        /// Converts a <see cref="GamesList"/> entry to a <see cref="TitleList"/>.
        /// </summary>
        /// <param name="gamesList">The games list entry.</param>
        /// <returns>Returns a <see cref="TitleList"/> object.</returns>
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

        /// <summary>
        /// Retrieves the edit data for a specific user game entry.
        /// </summary>
        /// <param name="gameName">The name of the game.</param>
        /// <param name="userGameId">The user game ID.</param>
        /// <returns>Returns an <see cref="EditData"/> object.</returns>
        public async Task<EditData> GetEditData(string gameName, string userGameId)
        {
            Logger.Info($"GetEditData({gameName}, {userGameId})");
            try
            {
                List<HttpCookie> cookies = CookiesTools.GetStoredCookies();

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

        /// <summary>
        /// Loads user stats from the local file.
        /// </summary>
        /// <returns>Returns a <see cref="HltbUserStats"/> object.</returns>
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

        /// <summary>
        /// Retrieves the user data from HowLongToBeat.
        /// </summary>
        /// <returns>Returns a <see cref="HltbUserStats"/> object, or null if not logged in.</returns>
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

        /// <summary>
        /// Retrieves user data for a specific game by game ID.
        /// </summary>
        /// <param name="gameId">The game ID.</param>
        /// <returns>Returns a <see cref="TitleList"/> object, or null if not found.</returns>
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

        /// <summary>
        /// Checks if a user game ID exists in the user's games list.
        /// </summary>
        /// <param name="userGameId">The user game ID.</param>
        /// <returns>True if the ID exists, otherwise false.</returns>
        public bool EditIdExist(string userGameId)
        {
            return GetUserGamesList()?.GetAwaiter().GetResult()?.Data?.GamesList?.Find(x => x.Id.ToString().IsEqual(userGameId))?.Id != null;
        }

        /// <summary>
        /// Finds the existing user game ID for a given game ID.
        /// </summary>
        /// <param name="gameId">The game ID.</param>
        /// <returns>Returns the user game ID as a string, or null if not found.</returns>
        public string FindIdExisting(string gameId)
        {
            return GetUserGamesList()?.GetAwaiter().GetResult().Data?.GamesList?.Find(x => x.GameId.ToString().IsEqual(gameId))?.Id.ToString() ?? null;
        }

        #endregion


        /// <summary>
        /// Submits the current game data to the HowLongToBeat website.
        /// </summary>
        /// <param name="game">The Playnite game object.</param>
        /// <param name="editData">The data to submit.</param>
        /// <returns>True if submission is successful, otherwise false.</returns>
        public async Task<bool> ApiSubmitData(Game game, EditData editData)
        {
            if (GetIsUserLoggedIn() && editData.UserId != 0 && editData.GameId != 0)
            {
                try
                {
                    List<HttpCookie> cookies = CookiesTools.GetStoredCookies();
                    string payload = Serialization.ToJson(editData);
                    List<KeyValuePair<string, string>> moreHeaders = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36")
                    };
                    string response = await Web.PostStringDataPayload(UrlPostData, payload, cookies);

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

        /// <summary>
        /// Updates the stored cookies for the current user session.
        /// </summary>
        public void UpdatedCookies()
        {
            _ = Task.Run(() =>
            {
                try
                {
                    // Wait extension database are loaded
                    _ = SpinWait.SpinUntil(() => PluginDatabase.IsLoaded, -1);

                    if (PluginDatabase.Database.UserHltbData?.UserId != null && PluginDatabase.Database.UserHltbData.UserId != 0)
                    {
                        Logger.Info($"Refresh HowLongToBeat user cookies");
                        List<HttpCookie> cookies = CookiesTools.GetNewWebCookies(new List<string> { UrlBase, UrlUser }, true);
                        _ = CookiesTools.SetStoredCookies(cookies);
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                }
            });
        }
    }
}