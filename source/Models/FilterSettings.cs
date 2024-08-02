using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HowLongToBeat.Models
{
    public class FilterSettings
    {
        public string Year { get; set; } = "----";
        public string Storefront { get; set; } = "----";
        public string Platform { get; set; } = "----";
        public bool OnlyReplays { get; set; } = false;
        public bool OnlyNotPlayed { get; set; } = false;

        public bool UsedFilteredGames { get; set; } = true;
        public bool OnlyNotPlayedGames { get; set; } = false;
    }
}
