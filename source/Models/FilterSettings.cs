using HowLongToBeat.Models.Enumerations;

namespace HowLongToBeat.Models
{
    /// <summary>
    /// Persisted filter state for the HowLongToBeat user view (User Data and Playnite data tabs).
    /// </summary>
    public class FilterSettings
    {
        /// <summary>
        /// Sentinel value for combo filters meaning no filter is applied.
        /// </summary>
        public const string HltbListStatusAll = "----";

        /// <summary>
        /// Filters for the User Data (HowLongToBeat profile) tab.
        /// </summary>
        public UserDataFilterSettings UserData { get; set; } = new UserDataFilterSettings();

        /// <summary>
        /// Filters for the Playnite data tab.
        /// </summary>
        public PlayniteDataFilterSettings PlayniteData { get; set; } = new PlayniteDataFilterSettings();

        /// <summary>
        /// True after root-level <c>TitleListSort</c> / <c>IsAsc</c> settings were copied into <see cref="UserData"/>.
        /// </summary>
        public bool LegacySortMigrated { get; set; } = false;

        /// <summary>
        /// True after legacy flat <c>filterSettings</c> JSON was migrated into <see cref="UserData"/> / <see cref="PlayniteData"/>.
        /// </summary>
        public bool NestedFiltersMigrated { get; set; } = false;

        /// <summary>
        /// Ensures nested filter objects are non-null after deserialization.
        /// </summary>
        public void EnsureNestedFilters()
        {
            if (UserData == null)
            {
                UserData = new UserDataFilterSettings();
            }

            if (PlayniteData == null)
            {
                PlayniteData = new PlayniteDataFilterSettings();
            }
        }

        /// <summary>
        /// Resets all user-facing nested filter fields to their factory defaults.
        /// </summary>
        public void ResetToDefaults()
        {
            bool legacySortMigrated = LegacySortMigrated;
            bool nestedFiltersMigrated = NestedFiltersMigrated;

            EnsureNestedFilters();
            UserData.ResetToDefaults();
            PlayniteData.ResetToDefaults();

            LegacySortMigrated = legacySortMigrated;
            NestedFiltersMigrated = nestedFiltersMigrated;
        }
    }

    /// <summary>
    /// Filter fields for the User Data (HowLongToBeat profile list) tab.
    /// </summary>
    public class UserDataFilterSettings
    {
        public string NameSearch { get; set; } = string.Empty;

        public string Year { get; set; } = FilterSettings.HltbListStatusAll;
        public string Storefront { get; set; } = FilterSettings.HltbListStatusAll;

        /// <summary>
        /// HowLongToBeat profile platform string filter.
        /// </summary>
        public string Platform { get; set; } = FilterSettings.HltbListStatusAll;

        /// <summary>
        /// Selected HowLongToBeat profile list filter token (<see cref="FilterSettings.HltbListStatusAll"/> or a <see cref="StatusType"/> name).
        /// </summary>
        public string HltbListStatus { get; set; } = FilterSettings.HltbListStatusAll;

        public bool OnlyReplays { get; set; } = false;
        public bool OnlyIncludesDlc { get; set; } = false;
        public bool OnlyNotPlayed { get; set; } = false;

        /// <summary>
        /// When true, keeps only titles linked to an installed Playnite game.
        /// Titles without a resolved Playnite game are excluded.
        /// </summary>
        public bool OnlyInstalled { get; set; } = false;

        public TitleListSort TitleListSort { get; set; } = TitleListSort.LastUpdate;
        public bool IsAsc { get; set; } = false;

        /// <summary>
        /// Resets User Data filter fields to factory defaults.
        /// </summary>
        public void ResetToDefaults()
        {
            NameSearch = string.Empty;
            Year = FilterSettings.HltbListStatusAll;
            Storefront = FilterSettings.HltbListStatusAll;
            Platform = FilterSettings.HltbListStatusAll;
            HltbListStatus = FilterSettings.HltbListStatusAll;
            OnlyReplays = false;
            OnlyIncludesDlc = false;
            OnlyNotPlayed = false;
            OnlyInstalled = false;
            TitleListSort = TitleListSort.LastUpdate;
            IsAsc = false;
        }
    }

    /// <summary>
    /// Filter fields for the Playnite data tab.
    /// </summary>
    public class PlayniteDataFilterSettings
    {
        public bool UsedFilteredGames { get; set; } = true;
        public bool OnlyNotPlayed { get; set; } = false;

        /// <summary>
        /// When true, keeps only games with <c>Game.IsInstalled</c>.
        /// </summary>
        public bool OnlyInstalled { get; set; } = false;

        /// <summary>
        /// Playnite platform name filter, or <see cref="FilterSettings.HltbListStatusAll"/> when unset.
        /// Matched against <c>Game.Platforms</c> display names.
        /// </summary>
        public string Platform { get; set; } = FilterSettings.HltbListStatusAll;

        /// <summary>
        /// Resets Playnite data filter fields to factory defaults.
        /// </summary>
        public void ResetToDefaults()
        {
            UsedFilteredGames = true;
            OnlyNotPlayed = false;
            OnlyInstalled = false;
            Platform = FilterSettings.HltbListStatusAll;
        }
    }
}
