using CommonPluginsShared;
using HowLongToBeat.Models;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HowLongToBeat.Services
{
    /// <summary>
    /// Exports HLTB library times from the settings view (CSV / JSON).
    /// </summary>
    public static class HltbSettingsLibraryExport
    {
        private static HowLongToBeatDatabase PluginDatabase => HowLongToBeat.PluginDatabase;

        /// <summary>
        /// Writes a CSV export for all games that have HLTB data into <paramref name="folder"/>.
        /// </summary>
        /// <param name="folder">Destination folder path.</param>
        /// <param name="delimiter">CSV field delimiter.</param>
        /// <returns><c>true</c> when a file was written; otherwise <c>false</c>.</returns>
        public static bool TryExportCsv(string folder, char delimiter)
        {
            try
            {
                if (!ValidateFolder(folder))
                {
                    return false;
                }

                string path = Path.Combine(folder, $"HLTB_Export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
                List<string> lines = new List<string>
                {
                    string.Join(delimiter.ToString(), new[]
                    {
                        "GameId", "Name", "Platform", "Type",
                        "Main (formatted)", "Main+Extra (formatted)", "Completionist (formatted)",
                        "Solo (formatted)", "Co-Op (formatted)", "Vs (formatted)",
                        "Developers", "Publishers", "Date added", "Last activity"
                    })
                };

                int exportedCount = 0;
                int failedCount = 0;
                foreach (Game game in API.Instance.Database.Games)
                {
                    try
                    {
                        GameHowLongToBeat entry = PluginDatabase.Get(game.Id, true);
                        HltbData data = entry?.GetData()?.GameHltbData;
                        if (entry == null || data == null)
                        {
                            continue;
                        }

                        string name = entry.GetData()?.Name ?? game.Name;
                        string platform = entry.GetData()?.Platform ?? string.Empty;
                        string type = data.GameType.ToString();
                        List<string> developers = game.Developers?.Select(d => d.Name)?.ToList() ?? new List<string>();
                        List<string> publishers = game.Publishers?.Select(p => p.Name)?.ToList() ?? new List<string>();

                        lines.Add(string.Join(delimiter.ToString(), new[]
                        {
                            game.Id.ToString(),
                            EscapeCsvWithDelimiter(name, delimiter),
                            EscapeCsvWithDelimiter(platform, delimiter),
                            EscapeCsvWithDelimiter(type, delimiter),
                            EscapeCsvWithDelimiter(data.MainStoryFormat, delimiter),
                            EscapeCsvWithDelimiter(data.MainExtraFormat, delimiter),
                            EscapeCsvWithDelimiter(data.CompletionistFormat, delimiter),
                            EscapeCsvWithDelimiter(data.SoloFormat, delimiter),
                            EscapeCsvWithDelimiter(data.CoOpFormat, delimiter),
                            EscapeCsvWithDelimiter(data.VsFormat, delimiter),
                            EscapeCsvWithDelimiter(string.Join(", ", developers), delimiter),
                            EscapeCsvWithDelimiter(string.Join(", ", publishers), delimiter),
                            EscapeCsvWithDelimiter(game.Added?.ToString("yyyy-MM-ddTHH:mm:ss"), delimiter),
                            EscapeCsvWithDelimiter(game.LastActivity?.ToString("yyyy-MM-ddTHH:mm:ss"), delimiter)
                        }));
                        exportedCount++;
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        Common.LogError(ex, false, false, PluginDatabase.PluginName);
                    }
                }

                File.WriteAllLines(path, lines, new System.Text.UTF8Encoding(true));
                string msg = string.Format(ResourceProvider.GetString("LOCExportedCsvMessage"), exportedCount, path, delimiter);
                if (failedCount > 0)
                {
                    msg += "\n" + string.Format(ResourceProvider.GetString("LOCExportFailedCount"), failedCount);
                }

                API.Instance.Dialogs.ShowMessage(msg);
                return true;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
                return false;
            }
        }

        /// <summary>
        /// Writes a JSON export for all games that have HLTB data into <paramref name="folder"/>.
        /// </summary>
        /// <param name="folder">Destination folder path.</param>
        /// <returns><c>true</c> when a file was written; otherwise <c>false</c>.</returns>
        public static bool TryExportJson(string folder)
        {
            try
            {
                if (!ValidateFolder(folder))
                {
                    return false;
                }

                string path = Path.Combine(folder, $"HLTB_Export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json");
                List<object> items = new List<object>();
                int exportedCount = 0;
                int failedCount = 0;

                foreach (Game game in API.Instance.Database.Games)
                {
                    try
                    {
                        GameHowLongToBeat entry = PluginDatabase.Get(game.Id, true);
                        HltbData data = entry?.GetData()?.GameHltbData;
                        if (entry == null || data == null)
                        {
                            continue;
                        }

                        List<string> developers = game.Developers?.Select(d => d.Name)?.ToList() ?? new List<string>();
                        List<string> publishers = game.Publishers?.Select(p => p.Name)?.ToList() ?? new List<string>();

                        items.Add(new
                        {
                            GameId = game.Id,
                            Name = entry.GetData()?.Name ?? game.Name,
                            Platform = entry.GetData()?.Platform ?? string.Empty,
                            Type = data.GameType.ToString(),
                            Main = data.MainStoryClassic,
                            MainExtra = data.MainExtraClassic,
                            Completionist = data.CompletionistClassic,
                            Solo = data.SoloClassic,
                            CoOp = data.CoOpClassic,
                            Vs = data.VsClassic,
                            MainFormatted = data.MainStoryFormat,
                            MainExtraFormatted = data.MainExtraFormat,
                            CompletionistFormatted = data.CompletionistFormat,
                            SoloFormatted = data.SoloFormat,
                            CoOpFormatted = data.CoOpFormat,
                            VsFormatted = data.VsFormat,
                            Developers = developers,
                            Publishers = publishers,
                            DateAdded = game.Added,
                            LastActivity = game.LastActivity
                        });
                        exportedCount++;
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        Common.LogError(ex, false, false, PluginDatabase.PluginName);
                    }
                }

                File.WriteAllText(path, Serialization.ToJson(items, true));
                string msgJson = string.Format(ResourceProvider.GetString("LOCExportedJsonMessage"), exportedCount, path);
                if (failedCount > 0)
                {
                    msgJson += "\n" + string.Format(ResourceProvider.GetString("LOCExportFailedCount"), failedCount);
                }

                API.Instance.Dialogs.ShowMessage(msgJson);
                return true;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
                return false;
            }
        }

        private static bool ValidateFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
            {
                API.Instance.Dialogs.ShowMessage(ResourceProvider.GetString("LOCExportSelectFolderFirst"));
                return false;
            }

            if (!Directory.Exists(folder))
            {
                API.Instance.Dialogs.ShowMessage(ResourceProvider.GetString("LOCExportFolderNotExist"));
                return false;
            }

            return true;
        }

        private static string EscapeCsvWithDelimiter(string input, char delimiter)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            bool needsQuotes = input.IndexOf(delimiter) >= 0
                || input.IndexOf('"') >= 0
                || input.IndexOf('\n') >= 0
                || input.IndexOf('\r') >= 0;
            string escaped = input.Replace("\"", "\"\"");
            return needsQuotes ? "\"" + escaped + "\"" : escaped;
        }
    }
}
