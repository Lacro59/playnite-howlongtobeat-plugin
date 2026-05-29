using HowLongToBeat.Models.Enumerations;

namespace HowLongToBeat.Models
{
    public class FilterSettings
    {
        /// <summary>
        /// Sentinel value for the HLTB list filter meaning no list filter is applied.
        /// </summary>
        public const string HltbListStatusAll = "----";

        public string NameSearch { get; set; } = string.Empty;

        public string Year { get; set; } = "----";
        public string Storefront { get; set; } = "----";
        public string Platform { get; set; } = "----";

        /// <summary>
        /// Selected HowLongToBeat profile list filter token (<see cref="HltbListStatusAll"/> or a <see cref="Enumerations.StatusType"/> name).
        /// </summary>
        public string HltbListStatus { get; set; } = HltbListStatusAll;

        public bool OnlyReplays { get; set; } = false;
        public bool OnlyNotPlayed { get; set; } = false;

        public TitleListSort TitleListSort { get; set; } = TitleListSort.LastUpdate;
        public bool IsAsc { get; set; } = false;

        public bool UsedFilteredGames { get; set; } = true;
        public bool OnlyNotPlayedGames { get; set; } = false;

        /// <summary>
        /// True after root-level <c>TitleListSort</c> / <c>IsAsc</c> settings were copied into this object.
        /// </summary>
        public bool LegacySortMigrated { get; set; } = false;

        /// <summary>
        /// Resets all user-facing filter fields to their factory defaults.
        /// </summary>
        public void ResetToDefaults()
        {
            bool legacySortMigrated = LegacySortMigrated;

            NameSearch = string.Empty;
            Year = HltbListStatusAll;
            Storefront = HltbListStatusAll;
            Platform = HltbListStatusAll;
            HltbListStatus = HltbListStatusAll;
            OnlyReplays = false;
            OnlyNotPlayed = false;
            TitleListSort = TitleListSort.LastUpdate;
            IsAsc = false;
            UsedFilteredGames = true;
            OnlyNotPlayedGames = false;
            LegacySortMigrated = legacySortMigrated;
        }
    }
}
