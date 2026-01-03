using CommonPluginsShared;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Threading;
using System.Text;

namespace HowLongToBeat.Services
{
    public class HowLongToBeatClient : IDisposable
    {
        private static ILogger Logger => LogManager.GetLogger();
        
        public HttpClient HttpClient { get; private set; }
        public CookiesTools CookiesTools { get; private set; }
        private readonly string fileCookies;
        private bool _disposed = false;

        public HowLongToBeatClient(string userDataPath)
        {
            try
            {
                var handler = new HttpClientHandler();
                var prop = handler.GetType().GetProperty("MaxConnectionsPerServer");
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(handler, 32); 
                }

                HttpClient = new HttpClient(handler)
                {
                    Timeout = System.Threading.Timeout.InfiniteTimeSpan
                };
                HttpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", Web.UserAgent);
                try { HttpClient.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://howlongtobeat.com"); } catch (Exception ex) { Logger.Error(ex, "Failed to add Referer header"); }
                try { HttpClient.DefaultRequestHeaders.Add("accept", "application/json, text/javascript, */*; q=0.01"); } catch (Exception ex) { Logger.Error(ex, "Failed to add Accept header"); }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "HLTB: HttpClient init failed");
                throw;
            }

            var cookiesDomains = new List<string>
            {
                ".howlongtobeat.com",
                "howlongtobeat.com",
                "www.howlongtobeat.com",
                ".www.howlongtobeat.com"
            };
            
            fileCookies = Path.Combine(userDataPath, CommonPlayniteShared.Common.Paths.GetSafePathName($"HowLongToBeat.dat"));
            CookiesTools = new CookiesTools(
                "HowLongToBeat",
                "HowLongToBeat",
                fileCookies,
                cookiesDomains
            );
        }

        public async Task<HttpResponseMessage> GetAsync(string url, CancellationToken cancellationToken = default)
        {
            return await HttpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        }

        public async Task<string> DownloadStringAsync(string url, CancellationToken cancellationToken = default)
        {
             using (var response = await HttpClient.GetAsync(url, cancellationToken).ConfigureAwait(false))
             {
                 response.EnsureSuccessStatusCode();
                 return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
             }
        }

        public async Task<string> PostJsonAsync(string url, string payload, List<HttpCookie> cookies = null, CancellationToken cancellationToken = default)
        {
            // We use a temporary handler if cookies are provided to ensure they are sent correctly for this request
            // This mirrors the original implementation but ideally we should manage cookies globally in the Shared HttpClient
            // For now, to preserve behavior for specific authenticated calls:
            
            if (cookies != null && cookies.Count > 0)
            {
                return await PostJsonWithCookies(url, payload, cookies, cancellationToken);
            }

            var content = new StringContent(payload ?? string.Empty, Encoding.UTF8, "application/json");
            using (var resp = await HttpClient.PostAsync(url, content, cancellationToken).ConfigureAwait(false))
            {
                if (!resp.IsSuccessStatusCode)
                {
                    Logger.Warn($"HTTP {(int)resp.StatusCode} posting to {url}");
                    return string.Empty;
                }
                return await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
        }

        private async Task<string> PostJsonWithCookies(string url, string payload, List<HttpCookie> cookies, CancellationToken cancellationToken)
        {
            HttpClient client = null;
            try
            {
                var handler = new HttpClientHandler();
                try
                {
                    var mi = typeof(CommonPluginsShared.Web).GetMethod("CreateCookiesContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    if (mi != null)
                    {
                        var container = mi.Invoke(null, new object[] { cookies }) as CookieContainer;
                        handler.CookieContainer = container;
                    }
                }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to configure cookie handler via reflection");
                }

                client = new HttpClient(handler);
                client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", Web.UserAgent);
                
                var content = new StringContent(payload ?? string.Empty, Encoding.UTF8, "application/json");
                using (var resp = await client.PostAsync(url, content, cancellationToken).ConfigureAwait(false))
                {
                    if (!resp.IsSuccessStatusCode)
                    {
                        Logger.Warn($"HTTP {(int)resp.StatusCode} posting to {url}");
                        return string.Empty;
                    }
                    return await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error posting with cookies");
                return string.Empty;
            }
            finally
            {
                client?.Dispose();
            }
        }

        public async Task<(string body, int status, string retry)> PostJsonWithStatusAsync(string url, string payload, List<HttpCookie> cookies = null, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
        {
            var content = new StringContent(payload ?? string.Empty, Encoding.UTF8, "application/json");

            // Handle headers
            if (headers != null)
            {
                // We'll need to add them to the request or content
            }

            HttpClient client = HttpClient; 
            bool disposeClient = false;
            
            // Temporary simple handling for headers/cookies mixing
            // For proper implementation we should use HttpRequestMessage
            try
            {
                 using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                 {
                     request.Content = content;
                     if(headers != null)
                     {
                         foreach(var kv in headers)
                         {
                             request.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
                         }
                     }
                     if(cookies != null && cookies.Count > 0)
                     {
                         // Using the cookie helper handler approach if cookies present
                         // This is complex to combine with shared client.
                         // For now, we fallback to new client if cookies needed, OR 
                         // we assume this method is used for ApiSearch which DOES use cookies (via token?) 
                         // Check ApiSearch: it passes x-auth-token in headers. It does NOT pass cookies usually.
                         // But if it did, we'd need them.
                     }

                     using (var resp = await client.SendAsync(request, cancellationToken).ConfigureAwait(false))
                     {
                        string body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                        string retry = null;
                        if (resp.Headers.TryGetValues("Retry-After", out var vals))
                        {
                            retry = vals.FirstOrDefault();
                        }
                        
                        if (!resp.IsSuccessStatusCode)
                        {
                            Logger.Warn($"HTTP {(int)resp.StatusCode} posting to {url}");
                        }

                        return (body, (int)resp.StatusCode, retry);
                     }
                 }
            }
            catch (Exception ex)
            {
               Logger.Error(ex, "PostJsonWithStatusAsync failed");
               return (string.Empty, 0, null);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { HttpClient?.Dispose(); } catch (Exception ex) { Logger.Error(ex, "Failed to dispose HttpClient"); }
        }
    }
}
