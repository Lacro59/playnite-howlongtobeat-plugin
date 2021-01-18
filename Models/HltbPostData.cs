using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HowLongToBeat.Models
{
    public class HltbPostData
    {
        public int user_id { get; set; }

        // If editing
        public int edit_id { get; set; }

        public int game_id { get; set; }
        public string custom_title { get; set; } = string.Empty;
        public string platform { get; set; } = string.Empty;

        // Add to List
        public string list_p { get; set; } = string.Empty;
        public string list_b { get; set; } = string.Empty;
        public string list_r { get; set; } = string.Empty;
        public string list_c { get; set; } = string.Empty;
        public string list_cp { get; set; } = string.Empty;
        public string list_rt { get; set; } = string.Empty;

        // Current Progress
        public string protime_h { get; set; } = "0";
        public string protime_m { get; set; } = "0";
        public string protime_s { get; set; } = "0";

        public string rt_notes { get; set; } = string.Empty;

        // Completion Date
        public string compmonth { get; set; } = string.Empty;
        public string compday { get; set; } = string.Empty;
        public string compyear { get; set; } = string.Empty;

        // First Playthrough?
        public int play_num { get; set; }

        // Main Story Only
        public string c_main_h { get; set; } = "0";
        public string c_main_m { get; set; } = "0";
        public string c_main_s { get; set; } = "0";
        public string c_main_notes { get; set; } = string.Empty;

        // Main Story + Extra Quests/Unlockables
        public string c_plus_h { get; set; } = "0";
        public string c_plus_m { get; set; } = "0";
        public string c_plus_s { get; set; } = "0";
        public string c_plus_notes { get; set; } = string.Empty;

        // 100% Complete
        public string c_100_h { get; set; } = "0";
        public string c_100_m { get; set; } = "0";
        public string c_100_s { get; set; } = "0";
        public string c_100_notes { get; set; } = string.Empty;

        // Speedrun Any%
        public string c_speed_h { get; set; } = "0";
        public string c_speed_m { get; set; } = "0";
        public string c_speed_s { get; set; } = "0";
        public string c_speed_notes { get; set; } = string.Empty;

        // Speedrun 100%
        public string c_speed100_h { get; set; } = "0";
        public string c_speed100_m { get; set; } = "0";
        public string c_speed100_s { get; set; } = "0";
        public string c_speed100_notes { get; set; } = string.Empty;

        // Co-Operative
        public string cotime_h { get; set; } = "0";
        public string cotime_m { get; set; } = "0";
        public string cotime_s { get; set; } = "0";

        // Vs. Competitive
        public string mptime_h { get; set; } = "0";
        public string mptime_m { get; set; } = "0";
        public string mptime_s { get; set; } = "0";

        public int review_score { get; set; }
        public string review_notes { get; set; } = string.Empty;
        public string play_notes { get; set; } = string.Empty;
        public string play_video { get; set; } = string.Empty;

        public string submitted { get; set; } = "Submit";
    }
}
