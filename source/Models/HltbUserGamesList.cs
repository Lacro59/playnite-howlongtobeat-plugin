using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HowLongToBeat.Models
{
    public class Data
    {
        public int count { get; set; }
        public List<GamesList> gamesList { get; set; }
        public int total { get; set; }
        public List<PlatformList> platformList { get; set; }
        public SummaryData summaryData { get; set; }
    }

    public class GamesList
    {
        public int id { get; set; }
        public string custom_title { get; set; }
        public string platform { get; set; }
        public string play_storefront { get; set; }
        public int list_playing { get; set; }
        public int list_backlog { get; set; }
        public int list_replay { get; set; }
        public int list_custom { get; set; }
        public int list_custom2 { get; set; }
        public int list_custom3 { get; set; }
        public int list_comp { get; set; }
        public int list_retired { get; set; }
        public int comp_main { get; set; }
        public int comp_plus { get; set; }
        public int comp_100 { get; set; }
        public int comp_speed { get; set; }
        public int comp_speed100 { get; set; }
        public string comp_main_notes { get; set; }
        public string comp_plus_notes { get; set; }
        public string comp_100_notes { get; set; }
        public string comp_speed_notes { get; set; }
        public string comp_speed100_notes { get; set; }
        public int invested_pro { get; set; }
        public int invested_sp { get; set; }
        public int invested_spd { get; set; }
        public int invested_co { get; set; }
        public int invested_mp { get; set; }
        public int play_count { get; set; }
        public int review_score { get; set; }
        public string review_notes { get; set; }
        public string retired_notes { get; set; }
        public string date_complete { get; set; }
        public string date_updated { get; set; }
        public string play_video { get; set; }
        public string play_notes { get; set; }
        public int game_id { get; set; }
        public string game_image { get; set; }
        public string game_type { get; set; }
        public string release_world { get; set; }
        public int comp_all { get; set; }
        public int comp_all_g { get; set; }
        public int review_score_g { get; set; }
    }

    public class PlatformList
    {
        public string platform { get; set; }
        public int count_total { get; set; }
    }

    public class HltbUserGamesList
    {
        public Data data { get; set; }
    }

    public class SummaryData
    {
        public int playCount { get; set; }
        public int dlcCount { get; set; }
        public int reviewTotal { get; set; }
        public int reviewCount { get; set; }
        public int totalPlayedSp { get; set; }
        public int totalPlayedMp { get; set; }
        public int toBeatListed { get; set; }
        public int uniqueGameCount { get; set; }
    }
}
