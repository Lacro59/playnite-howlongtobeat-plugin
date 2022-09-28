using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HowLongToBeat.Models
{
    class HltbUserStatsGamesList
    {
        public int game_id { get; set; }
        public string custom_title { get; set; }
        public string platform { get; set; }
        public int list_playing { get; set; }
        public int list_backlog { get; set; }
        public int list_replay { get; set; }
        public int list_custom { get; set; }
        public int list_custom2 { get; set; }
        public int list_custom3 { get; set; }
        public int list_comp { get; set; }
        public int list_retired { get; set; }
        public int invested_sp { get; set; }
        public int invested_spd { get; set; }
        public int invested_co { get; set; }
        public int invested_mp { get; set; }
        public string date_complete { get; set; }
        public string date_added { get; set; }
        public int? release_year { get; set; }
        public string release_world { get; set; }
    }
}
