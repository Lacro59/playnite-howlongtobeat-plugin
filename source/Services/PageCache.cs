using CommonPluginsShared;
using Playnite.SDK.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HowLongToBeat.Services
{
    internal class PageCache
    {
        private static readonly object PersistLock = new object();
        private class CacheEntry
        {
            public long TimestampUtcMs { get; set; }
            public string Json { get; set; }
        }

        private readonly string filePath;
        private readonly TimeSpan ttl;
        private readonly ConcurrentDictionary<string, CacheEntry> cache = new ConcurrentDictionary<string, CacheEntry>();

        public PageCache(string pluginUserDataPath, TimeSpan? ttlOverride = null)
        {
            filePath = Path.Combine(pluginUserDataPath ?? string.Empty, "HowLongToBeatPageCache.json");
            ttl = ttlOverride ?? TimeSpan.FromDays(7);
            Load();
        }

        private void Load()
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return;
                }

                string json = File.ReadAllText(filePath);
                if (json.IsNullOrEmpty())
                {
                    return;
                }

                var dict = Serialization.FromJson<Dictionary<string, CacheEntry>>(json);
                if (dict == null)
                {
                    return;
                }

                foreach (var kv in dict)
                {
                    cache.TryAdd(kv.Key, kv.Value);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }
        }

        private void PersistAsync()
        {
            try
            {
                var snapshot = cache.ToDictionary(kv => kv.Key, kv => kv.Value);
                Task.Run(() =>
                {
                    string json = null;
                    try
                    {
                        json = Serialization.ToJson(snapshot);
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false);
                        return;
                    }
                    
                    const int maxAttempts = 5;
                    int attempt = 0;
                    int delayMs = 100;
                    while (true)
                    {
                        attempt++;
                        try
                        {
                            lock (PersistLock)
                            {
                                var dir = Path.GetDirectoryName(filePath);
                                if (string.IsNullOrEmpty(dir)) dir = Directory.GetCurrentDirectory();
                                var tempPath = Path.Combine(dir, Path.GetFileName(filePath) + ".tmp");

                                using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                                using (var sw = new StreamWriter(fs, System.Text.Encoding.UTF8))
                                {
                                    sw.Write(json);
                                    sw.Flush();
                                    fs.Flush(true);
                                }

                                if (File.Exists(filePath))
                                {
                                    try
                                    {
                                        File.Replace(tempPath, filePath, null);
                                    }
                                    catch (PlatformNotSupportedException)
                                    {
                                        File.Delete(filePath);
                                        File.Move(tempPath, filePath);
                                    }
                                    catch (IOException)
                                    {
                                        if (File.Exists(filePath))
                                        {
                                            File.Delete(filePath);
                                        }
                                        File.Move(tempPath, filePath);
                                    }
                                }
                                else
                                {
                                    File.Move(tempPath, filePath);
                                }
                            }

                            break;
                        }
                        catch (IOException ioex)
                        {
                            Common.LogDebug(true, $"PageCache persist attempt {attempt} failed with IOException, retrying: {ioex.Message}");
                            if (attempt >= maxAttempts)
                            {
                                Common.LogError(ioex, false);
                                break;
                            }

                            var jitter = new Random().Next(0, Math.Min(100, delayMs));
                            Thread.Sleep(delayMs + jitter);
                            delayMs = Math.Min(2000, delayMs * 2);
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false);
                            break;
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }
        }

        public bool TryGetJson(string id, out string json)
        {
            json = null;
            try
            {
                if (cache.TryGetValue(id, out CacheEntry entry))
                {
                    var age = DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeMilliseconds(entry.TimestampUtcMs);
                    if (age <= ttl && !entry.Json.IsNullOrEmpty())
                    {
                        json = entry.Json;
                        return true;
                    }
                    else
                    {
                        cache.TryRemove(id, out _);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            return false;
        }

        public void Set(string id, string json)
        {
            try
            {
                if (id.IsNullOrEmpty() || json.IsNullOrEmpty())
                {
                    return;
                }

                var entry = new CacheEntry { TimestampUtcMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), Json = json };
                cache.AddOrUpdate(id, entry, (k, v) => entry);
                PersistAsync();
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }
        }
    }
}
