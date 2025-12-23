using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.IO;

namespace HowLongToBeat.Services
{
    internal static class GameNameAliases
    {
        private static ILogger Logger => LogManager.GetLogger();

        internal const string AliasFileName = "aliases.json";

        internal static Dictionary<string, string> GetDefaultPokemonAliases()
        {
            // Keys and values here are raw titles; matching is done on normalized strings.
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // Gen 1
                { "Pokemon Red Version", "Pokemon Red and Blue" },
                { "Pokemon Blue Version", "Pokemon Red and Blue" },

                // Gen 2
                { "Pokemon Gold Version", "Pokemon Gold and Silver" },
                { "Pokemon Silver Version", "Pokemon Gold and Silver" },

                // Gen 3
                { "Pokemon Ruby Version", "Pokemon Ruby and Sapphire" },
                { "Pokemon Sapphire Version", "Pokemon Ruby and Sapphire" },
                { "Pokemon FireRed Version", "Pokemon FireRed and LeafGreen" },
                { "Pokemon LeafGreen Version", "Pokemon FireRed and LeafGreen" },

                // Gen 4
                { "Pokemon Diamond Version", "Pokemon Diamond and Pearl" },
                { "Pokemon Pearl Version", "Pokemon Diamond and Pearl" },
                { "Pokemon HeartGold Version", "Pokemon HeartGold and SoulSilver" },
                { "Pokemon SoulSilver Version", "Pokemon HeartGold and SoulSilver" },

                // Gen 5
                { "Pokemon Black Version", "Pokemon Black and White" },
                { "Pokemon White Version", "Pokemon Black and White" },
                { "Pokemon Black Version 2", "Pokemon Black 2 and White 2" },
                { "Pokemon White Version 2", "Pokemon Black 2 and White 2" },

                // Gen 6
                { "Pokemon X", "Pokemon X and Y" },
                { "Pokemon Y", "Pokemon X and Y" },
                { "Pokemon Omega Ruby", "Pokemon Omega Ruby and Alpha Sapphire" },
                { "Pokemon Alpha Sapphire", "Pokemon Omega Ruby and Alpha Sapphire" },

                // Gen 7
                { "Pokemon Sun", "Pokemon Sun and Moon" },
                { "Pokemon Moon", "Pokemon Sun and Moon" },
                { "Pokemon Ultra Sun", "Pokemon Ultra Sun and Ultra Moon" },
                { "Pokemon Ultra Moon", "Pokemon Ultra Sun and Ultra Moon" },

                // Gen 8
                { "Pokemon Sword", "Pokemon Sword and Shield" },
                { "Pokemon Shield", "Pokemon Sword and Shield" },
                { "Pokemon Brilliant Diamond", "Pokemon Brilliant Diamond and Shining Pearl" },
                { "Pokemon Shining Pearl", "Pokemon Brilliant Diamond and Shining Pearl" },

                // Gen 9
                { "Pokemon Scarlet", "Pokemon Scarlet and Violet" },
                { "Pokemon Violet", "Pokemon Scarlet and Violet" }
            };
        }

        internal static Dictionary<string, string> ReadAliasFile(string pluginUserDataPath)
        {
            try
            {
                if (string.IsNullOrEmpty(pluginUserDataPath))
                {
                    return null;
                }

                var filePath = Path.Combine(pluginUserDataPath, AliasFileName);
                if (!File.Exists(filePath))
                {
                    return null;
                }

                if (!Serialization.TryFromJsonFile(filePath, out Dictionary<string, string> data, out Exception ex))
                {
                    if (ex != null)
                    {
                        try { Logger.Warn(ex, $"HLTB aliases: failed to parse {filePath}"); } catch { }
                    }
                    return null;
                }

                return data;
            }
            catch (Exception ex)
            {
                try { Logger.Warn(ex, "HLTB aliases: ReadAliasFile failed"); } catch { }
                return null;
            }
        }

        internal static string ApplyAlias(string inputName, HowLongToBeatSettings settings, string pluginUserDataPath)
        {
            try
            {
                if (string.IsNullOrEmpty(inputName))
                {
                    return inputName;
                }

                // Exact match on normalized title to avoid accidental remaps.
                var normInput = PlayniteTools.NormalizeGameName(inputName, true, true);
                if (string.IsNullOrEmpty(normInput))
                {
                    return inputName;
                }

                // Load & normalize alias candidates.
                // Precedence: user settings aliases > aliases.json > built-in Pokemon defaults.
                var candidates = new List<Dictionary<string, string>>();

                if (settings?.GameNameAliases != null && settings.GameNameAliases.Count > 0)
                {
                    candidates.Add(settings.GameNameAliases);
                }

                var fileAliases = ReadAliasFile(pluginUserDataPath);
                if (fileAliases != null && fileAliases.Count > 0)
                {
                    candidates.Add(fileAliases);
                }

                candidates.Add(GetDefaultPokemonAliases());

                foreach (var map in candidates)
                {
                    if (map == null || map.Count == 0) continue;

                    foreach (var kvp in map)
                    {
                        var key = kvp.Key;
                        var value = kvp.Value;

                        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value)) continue;

                        var normKey = PlayniteTools.NormalizeGameName(key, true, true);
                        if (!string.IsNullOrEmpty(normKey) && normKey.IsEqual(normInput))
                        {
                            return value;
                        }
                    }
                }
            }
            catch { }

            return inputName;
        }

        internal static void EnsureAliasFileExists(string pluginUserDataPath)
        {
            try
            {
                if (string.IsNullOrEmpty(pluginUserDataPath))
                {
                    return;
                }

                var filePath = Path.Combine(pluginUserDataPath, AliasFileName);
                if (File.Exists(filePath))
                {
                    return;
                }

                Directory.CreateDirectory(pluginUserDataPath);
                var defaults = GetDefaultPokemonAliases();
                File.WriteAllText(filePath, Serialization.ToJson(defaults, true));
            }
            catch (Exception ex)
            {
                try { Logger.Warn(ex, "HLTB aliases: failed to create default aliases.json"); } catch { }
            }
        }

        internal static string GetAliasFilePath(string pluginUserDataPath)
        {
            try
            {
                if (string.IsNullOrEmpty(pluginUserDataPath))
                {
                    return null;
                }

                return Path.Combine(pluginUserDataPath, AliasFileName);
            }
            catch
            {
                return null;
            }
        }

        internal static bool TryExportAliasesToFile(string pluginUserDataPath, Dictionary<string, string> aliases, out string filePath, out Exception error)
        {
            filePath = null;
            error = null;

            try
            {
                filePath = GetAliasFilePath(pluginUserDataPath);
                if (filePath == null)
                {
                    return false;
                }

                Directory.CreateDirectory(pluginUserDataPath);

                var data = aliases ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                File.WriteAllText(filePath, Serialization.ToJson(data, true));
                return true;
            }
            catch (Exception ex)
            {
                error = ex;
                return false;
            }
        }

        internal static bool TryImportAliasesFromFile(string pluginUserDataPath, out Dictionary<string, string> aliases, out string filePath, out Exception error)
        {
            aliases = null;
            filePath = null;
            error = null;

            try
            {
                filePath = GetAliasFilePath(pluginUserDataPath);
                if (filePath == null)
                {
                    return false;
                }

                if (!File.Exists(filePath))
                {
                    aliases = null;
                    return false;
                }

                if (!Serialization.TryFromJsonFile(filePath, out Dictionary<string, string> data, out Exception ex))
                {
                    error = ex;
                    aliases = null;
                    return false;
                }

                aliases = data;
                return aliases != null;
            }
            catch (Exception ex)
            {
                error = ex;
                aliases = null;
                return false;
            }
        }
    }
}
