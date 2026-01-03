using CommonPluginsShared;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using CommonPluginsShared.Controls;
using System.Linq;
using System.Web;

namespace HowLongToBeat.Services
{
    public class HowLongToBeatAuth
    {
        private static ILogger Logger => LogManager.GetLogger();
        private readonly HowLongToBeatClient _client;
        
        // Circular dependency potential here if Database uses Auth and Auth uses Database.
        // We will pass the Settings directly or a minimal interface if possible.
        // For now, accessing PluginDatabase via static HowLongToBeat.PluginDatabase is the existing pattern, 
        // but we should try to be cleaner.
        // We need: PluginName, UserLogin, SavePluginSettings, RefreshUserData.
        
        // Actually, we can just fire events or callbacks?
        // Let's stick to the pattern used in Api for now to minimize changes.
        private static HowLongToBeatDatabase PluginDatabase => HowLongToBeat.PluginDatabase;

        private bool? _isConnected = null;
        public bool? IsConnected { get => _isConnected; set => _isConnected = value; } // Simplified for now, assuming no binding needed directly
        
        public string UserLogin { get; set; } = string.Empty;
        public int UserId { get; set; } = 0;

        private DateTime lastLoginCheckUtc = DateTime.MinValue;
        private bool? lastLoginCheckResult = null;

        public HowLongToBeatAuth(HowLongToBeatClient client)
        {
            _client = client;
            if (PluginDatabase?.PluginSettings?.Settings != null)
            {
                UserLogin = PluginDatabase.PluginSettings.Settings.UserLogin;
            }
        }

        public async Task<bool> GetIsUserLoggedInAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                if (lastLoginCheckResult != null)
                {
                    var ttl = lastLoginCheckResult == true ? TimeSpan.FromMinutes(10) : TimeSpan.FromMinutes(1);
                    if (now - lastLoginCheckUtc < ttl)
                    {
                        return lastLoginCheckResult.Value;
                    }
                }

                int userId = await GetUserId().ConfigureAwait(false);
                UserId = userId;
                IsConnected = userId != 0;

                lastLoginCheckUtc = now;
                lastLoginCheckResult = userId != 0;

                return userId != 0;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, "HowLongToBeat");
                UserId = 0;
                IsConnected = false;
                lastLoginCheckUtc = DateTime.UtcNow;
                lastLoginCheckResult = false;
                return false;
            }
        }

        public bool GetIsUserLoggedIn()
        {
            try
            {
                return Task.Run(() => GetIsUserLoggedInAsync()).Result;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, false, "HowLongToBeat");
                return false;
            }
        }

        public async Task<int> GetUserId()
        {
            try
            {
                List<HttpCookie> cookies = _client.CookiesTools.GetStoredCookies();
                
                // UrlUser constant from Api... we should probably move constants or duplicate
                string UrlUser = "https://howlongtobeat.com/api/user";
                
                string response = await Web.DownloadPageText(UrlUser, cookies);
                dynamic t = Serialization.FromJson<dynamic>(response);

                int userId = response == "{}" ? 0 : t?.data[0]?.user_id ?? 0;
                return userId;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return 0;
            }
        }

        public void Login()
        {
            System.Windows.Threading.DispatcherOperation op = null;
            try
            {
                op = Application.Current.Dispatcher.BeginInvoke((Action)delegate
                {
                    Logger.Info("Login()");

                    bool cookiesCaptured = false;
                    string UrlBase = "https://howlongtobeat.com";
                    string UrlLogOut = UrlBase + "/login?t=out";

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
                            try
                            {
                                if (!cookiesCaptured && webView.GetCurrentAddress().StartsWith(UrlBase + "/user/"))
                                {
                                    cookiesCaptured = true;
                                    UserLogin = WebUtility.HtmlDecode(webView.GetCurrentAddress().Replace(UrlBase + "/user/", string.Empty));
                                    IsConnected = true;

                                    Task.Run(async () =>
                                    {
                                        try
                                        {
                                            await Task.Delay(1000).ConfigureAwait(false);
                                            await Application.Current.Dispatcher.InvokeAsync(() =>
                                            {
                                                try
                                                {
                                                    List<HttpCookie> cookies = _client.CookiesTools.GetWebCookies(deleteCookies: false, webView: webView);
                                                    _client.CookiesTools.SetStoredCookies(cookies);
                                                    lastLoginCheckResult = null;
                                                }
                                                catch (Exception ex)
                                                {
                                                    Common.LogError(ex, false, true, "HowLongToBeat");
                                                }
                                                finally
                                                {
                                                    try { webView.Close(); } catch { }
                                                }
                                            });
                                        }
                                        catch (Exception ex)
                                        {
                                            Common.LogError(ex, false, true, "HowLongToBeat");
                                        }
                                    });
                                    return;
                                }
                            }
                            catch (Exception ex)
                            {
                                Common.LogError(ex, false, true, "HowLongToBeat");
                            }
                        };

                        IsConnected = false;
                        try
                        {
                            webView.DeleteDomainCookiesRegex("howlongtobeat\\.com$");
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn(ex, "Failed to delete regex cookies"); 
                        {
                            try { webView.DeleteDomainCookies(".howlongtobeat.com"); } catch (Exception e) { Logger.Warn(e, "Failed to delete cookie .howlongtobeat.com"); }
                            try { webView.DeleteDomainCookies("howlongtobeat.com"); } catch (Exception e) { Logger.Warn(e, "Failed to delete cookie howlongtobeat.com"); }
                        }
                        webView.Navigate(UrlLogOut);
                        webView.OpenDialog();
                    }
                });
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, "HowLongToBeat");
            }

            try
            {
                if (op != null)
                {
                    op.Completed += (s, e) =>
                    {
                        try
                        {
                            if (IsConnected == true)
                            {
                                Application.Current.Dispatcher?.BeginInvoke((Action)delegate
                                {
                                    try
                                    {
                                        PluginDatabase.PluginSettings.Settings.UserLogin = UserLogin;
                                        PluginDatabase.Plugin.SavePluginSettings(PluginDatabase.PluginSettings.Settings);
                                        PluginDatabase.RefreshUserData();
                                    }
                                    catch (Exception ex)
                                    {
                                        Common.LogError(ex, false, true, "HowLongToBeat");
                                    }
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false, true, "HowLongToBeat");
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                 Common.LogError(ex, false, true, "HowLongToBeat");
            }
        }
    }
}
