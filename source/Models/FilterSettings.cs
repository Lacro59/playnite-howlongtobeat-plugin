using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HowLongToBeat.Models
{
    public class FilterSettings
    {
        /// <summary>
        /// Sentinel value for the HLTB list filter meaning no list filter is applied.
        /// </summary>
        public const string HltbListStatusAll = "----";

        public string Year { get; set; } = "----";
        public string Storefront { get; set; } = "----";
        public string Platform { get; set; } = "----";

        /// <summary>
        /// Selected HowLongToBeat profile list filter token (<see cref="HltbListStatusAll"/> or a <see cref="Enumerations.StatusType"/> name).
        /// </summary>
        public string HltbListStatus { get; set; } = HltbListStatusAll;

        public bool OnlyReplays { get; set; } = false;
        public bool OnlyNotPlayed { get; set; } = false;

        public bool UsedFilteredGames { get; set; } = true;
        public bool OnlyNotPlayedGames { get; set; } = false;
    }
}
