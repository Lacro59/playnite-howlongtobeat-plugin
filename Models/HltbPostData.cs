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
        public int edit_id { get; set; } = 0;

        public int game_id { get; set; }
        public string custom_title { get; set; }
        public string platform { get; set; }

        public string list_cp { get; set; }

        // Current Progress
        public int protime_h { get; set; }
        public int protime_m { get; set; }
        public int protime_s { get; set; }

        public string rt_notes { get; set; }

        // Completion Date
        public int compmonth { get; set; }
        public int compday { get; set; }
        public int compyear { get; set; }

        // First Playthrough?
        public int play_num { get; set; }

        // Main Story Only
        public int c_main_h { get; set; }
        public int c_main_m { get; set; }
        public int c_main_s { get; set; }
        public string c_main_notes { get; set; }

        // Main Story + Extra Quests/Unlockables
        public int c_plus_h { get; set; }
        public int c_plus_m { get; set; }
        public int c_plus_s { get; set; }
        public string c_plus_notes { get; set; }

        // 100% Complete
        public int c_100_h { get; set; }
        public int c_100_m { get; set; }
        public int c_100_s { get; set; }
        public string c_100_notes { get; set; }

        // Speedrun Any%
        public int c_speed_h { get; set; }
        public int c_speed_m { get; set; }
        public int c_speed_s { get; set; }
        public string c_speed_notes { get; set; }

        // Speedrun 100%
        public int c_speed100_h { get; set; }
        public int c_speed100_m { get; set; }
        public int c_speed100_s { get; set; }
        public string c_speed100_notes { get; set; }

        // Co-Operative
        public int cotime_h { get; set; }
        public int cotime_m { get; set; }
        public int cotime_s { get; set; }

        // Vs. Competitive
        public int mptime_h { get; set; }
        public int mptime_m { get; set; }
        public int mptime_s { get; set; }

        public int review_score { get; set; } = 0;
        public string review_notes { get; set; }
        public string play_notes { get; set; }
        public string play_video { get; set; }

        public string submitted { get; set; } = "Submit";
    }
}
