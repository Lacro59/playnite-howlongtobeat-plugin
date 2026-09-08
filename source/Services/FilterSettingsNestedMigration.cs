using System;
using System.IO;
using CommonPluginsShared;
using HowLongToBeat.Models;
using HowLongToBeat.Models.Enumerations;
using Playnite.SDK.Data;

namespace HowLongToBeat.Services
{
    /// <summary>
    /// TMP: one-shot migration of legacy flat <c>filterSettings</c> JSON into nested UserData / PlayniteData.
    /// Delete this class once nested filters are the only persisted shape in the wild.
    /// </summary>
    internal static class FilterSettingsNestedMigration
    {
        private const string ConfigFileName = "config.json";

        /// <summary>
        /// Reads the on-disk plugin <c>config.json</c> and copies flat filter fields into nested settings when needed.
        /// </summary>
        /// <param name="settings">In-memory plugin settings (already loaded).</param>
        /// <param name="pluginUserDataPath">Plugin user data folder containing <c>config.json</c>.</param>
        /// <returns>True when nested filters were updated and should be saved.</returns>
        public static bool TryMigrateFromLegacyFlatConfig(HowLongToBeatSettings settings, string pluginUserDataPath)
        {
            if (settings == null)
            {
                return false;
            }

            if (settings.filterSettings == null)
            {
                settings.filterSettings = new FilterSettings();
            }

            settings.filterSettings.EnsureNestedFilters();

            if (settings.filterSettings.NestedFiltersMigrated)
            {
                return false;
            }

            try
            {
                if (string.IsNullOrEmpty(pluginUserDataPath))
                {
                    settings.filterSettings.NestedFiltersMigrated = true;
                    return true;
                }

                string configPath = Path.Combine(pluginUserDataPath, ConfigFileName);
                if (!File.Exists(configPath))
                {
                    settings.filterSettings.NestedFiltersMigrated = true;
                    return true;
                }

                LegacyConfigRoot root;
                Exception ex;
                if (!Serialization.TryFromJsonFile(configPath, out root, out ex) || root?.filterSettings == null)
                {
                    if (ex != null)
                    {
                        Common.LogError(ex, false, false, "HowLongToBeat");
                    }

                    settings.filterSettings.NestedFiltersMigrated = true;
                    return true;
                }

                LegacyFlatFilterSettingsBlob blob = root.filterSettings;
                FilterSettings target = settings.filterSettings;

                if (blob.NestedFiltersMigrated || blob.UserData != null || blob.PlayniteData != null)
                {
                    ApplyNestedBlob(blob, target);
                }
                else
                {
                    ApplyFlatBlob(blob, target);
                }

                target.NestedFiltersMigrated = true;
                return true;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, "HowLongToBeat");
                settings.filterSettings.NestedFiltersMigrated = true;
                return true;
            }
        }

        private static void ApplyNestedBlob(LegacyFlatFilterSettingsBlob blob, FilterSettings target)
        {
            if (blob.UserData != null)
            {
                target.UserData = blob.UserData;
            }

            if (blob.PlayniteData != null)
            {
                target.PlayniteData = blob.PlayniteData;
            }

            target.EnsureNestedFilters();

            if (blob.LegacySortMigrated)
            {
                target.LegacySortMigrated = true;
            }
        }

        private static void ApplyFlatBlob(LegacyFlatFilterSettingsBlob blob, FilterSettings target)
        {
            target.EnsureNestedFilters();

            UserDataFilterSettings userData = target.UserData;
            userData.NameSearch = blob.NameSearch ?? string.Empty;
            userData.Year = string.IsNullOrEmpty(blob.Year) ? FilterSettings.HltbListStatusAll : blob.Year;
            userData.Storefront = string.IsNullOrEmpty(blob.Storefront) ? FilterSettings.HltbListStatusAll : blob.Storefront;
            userData.Platform = string.IsNullOrEmpty(blob.Platform) ? FilterSettings.HltbListStatusAll : blob.Platform;
            userData.HltbListStatus = string.IsNullOrEmpty(blob.HltbListStatus) ? FilterSettings.HltbListStatusAll : blob.HltbListStatus;
            userData.OnlyReplays = blob.OnlyReplays;
            userData.OnlyIncludesDlc = blob.OnlyIncludesDlc;
            userData.OnlyNotPlayed = blob.OnlyNotPlayed;
            userData.OnlyInstalled = blob.OnlyInstalled;
            userData.TitleListSort = blob.TitleListSort;
            userData.IsAsc = blob.IsAsc;

            PlayniteDataFilterSettings playniteData = target.PlayniteData;
            playniteData.UsedFilteredGames = blob.UsedFilteredGames;
            playniteData.OnlyNotPlayed = blob.OnlyNotPlayedGames;
            playniteData.OnlyInstalled = blob.OnlyInstalledGames;
            playniteData.Platform = string.IsNullOrEmpty(blob.PlaynitePlatform)
                ? FilterSettings.HltbListStatusAll
                : blob.PlaynitePlatform;

            if (blob.LegacySortMigrated)
            {
                target.LegacySortMigrated = true;
            }
        }

        /// <summary>
        /// TMP DTO matching the former flat <c>filterSettings</c> JSON (and optional already-nested shape).
        /// </summary>
        private sealed class LegacyConfigRoot
        {
            public LegacyFlatFilterSettingsBlob filterSettings { get; set; }
        }

        /// <summary>
        /// TMP DTO for deserializing legacy flat filter fields from <c>config.json</c>.
        /// </summary>
        private sealed class LegacyFlatFilterSettingsBlob
        {
            public UserDataFilterSettings UserData { get; set; }
            public PlayniteDataFilterSettings PlayniteData { get; set; }

            public bool LegacySortMigrated { get; set; }
            public bool NestedFiltersMigrated { get; set; }

            public string NameSearch { get; set; } = string.Empty;
            public string Year { get; set; } = FilterSettings.HltbListStatusAll;
            public string Storefront { get; set; } = FilterSettings.HltbListStatusAll;
            public string Platform { get; set; } = FilterSettings.HltbListStatusAll;
            public string HltbListStatus { get; set; } = FilterSettings.HltbListStatusAll;
            public bool OnlyReplays { get; set; }
            public bool OnlyIncludesDlc { get; set; }
            public bool OnlyNotPlayed { get; set; }
            public bool OnlyInstalled { get; set; }
            public TitleListSort TitleListSort { get; set; } = TitleListSort.LastUpdate;
            public bool IsAsc { get; set; }

            public bool UsedFilteredGames { get; set; } = true;
            public bool OnlyNotPlayedGames { get; set; }
            public bool OnlyInstalledGames { get; set; }
            public string PlaynitePlatform { get; set; } = FilterSettings.HltbListStatusAll;
        }
    }
}
