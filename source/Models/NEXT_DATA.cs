using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HowLongToBeat.Models
{
    public class Additionals
    {
        public string notes { get; set; }
        public string video { get; set; }
    }

    public class Comp100
    {
        public Time time { get; set; }
        public string notes { get; set; }
    }

    public class CompletionDate
    {
        public string year { get; set; }
        public string month { get; set; }
        public string day { get; set; }
    }

    public class CompMain
    {
        public Time time { get; set; }
        public string notes { get; set; }
    }

    public class CompPlus
    {
        public Time time { get; set; }
        public string notes { get; set; }
    }

    public class CoOp
    {
        public Time time { get; set; }
    }

    public class EditData
    {
        public int submissionId { get; set; }
        public string userIp { get; set; }
        public int userId { get; set; }
        public string userName { get; set; }
        public int gameId { get; set; }
        public string title { get; set; }
        public string platform { get; set; }
        public string storefront { get; set; }
        public Lists lists { get; set; }
        public General general { get; set; }
        public SinglePlayer singlePlayer { get; set; }
        public SpeedRuns speedRuns { get; set; }
        public MultiPlayer multiPlayer { get; set; }
        public Review review { get; set; }
        public Additionals additionals { get; set; }
    }

    public class GameData
    {
        public int count_discussion { get; set; }
        public int game_id { get; set; }
        public string game_name { get; set; }
        public int game_name_date { get; set; }
        public int count_playing { get; set; }
        public int count_backlog { get; set; }
        public int count_replay { get; set; }
        public int count_custom { get; set; }
        public int count_comp { get; set; }
        public int count_retired { get; set; }
        public int count_review { get; set; }
        public int review_score { get; set; }
        public string game_alias { get; set; }
        public string game_image { get; set; }
        public string game_type { get; set; }
        public int game_parent { get; set; }
        public string profile_summary { get; set; }
        public string profile_dev { get; set; }
        public string profile_pub { get; set; }
        public string profile_platform { get; set; }
        public string profile_genre { get; set; }
        public int profile_steam { get; set; }
        public int profile_steam_alt { get; set; }
        public int profile_itch { get; set; }
        public object profile_ign { get; set; }
        public string release_world { get; set; }
        public string release_na { get; set; }
        public string release_eu { get; set; }
        public string release_jp { get; set; }
        public string rating_esrb { get; set; }
        public string rating_pegi { get; set; }
        public string rating_cero { get; set; }
        public int comp_lvl_sp { get; set; }
        public int comp_lvl_spd { get; set; }
        public int comp_lvl_co { get; set; }
        public int comp_lvl_mp { get; set; }
        public int comp_lvl_combine { get; set; }
        public int comp_lvl_platform { get; set; }
        public int comp_all_count { get; set; }
        public int comp_all { get; set; }
        public int comp_all_l { get; set; }
        public int comp_all_h { get; set; }
        public int comp_all_avg { get; set; }
        public int comp_all_med { get; set; }
        public int comp_main_count { get; set; }
        public int comp_main { get; set; }
        public int comp_main_l { get; set; }
        public int comp_main_h { get; set; }
        public int comp_main_avg { get; set; }
        public int comp_main_med { get; set; }
        public int comp_plus_count { get; set; }
        public int comp_plus { get; set; }
        public int comp_plus_l { get; set; }
        public int comp_plus_h { get; set; }
        public int comp_plus_avg { get; set; }
        public int comp_plus_med { get; set; }
        public int comp_100_count { get; set; }
        public int comp_100 { get; set; }
        public int comp_100_l { get; set; }
        public int comp_100_h { get; set; }
        public int comp_100_avg { get; set; }
        public int comp_100_med { get; set; }
        public int comp_speed_count { get; set; }
        public int comp_speed { get; set; }
        public int comp_speed_min { get; set; }
        public int comp_speed_max { get; set; }
        public int comp_speed_avg { get; set; }
        public int comp_speed_med { get; set; }
        public int comp_speed100_count { get; set; }
        public int comp_speed100 { get; set; }
        public int comp_speed100_min { get; set; }
        public int comp_speed100_max { get; set; }
        public int comp_speed100_avg { get; set; }
        public int comp_speed100_med { get; set; }
        public int count_total { get; set; }
        public int invested_co_count { get; set; }
        public int invested_co { get; set; }
        public int invested_co_l { get; set; }
        public int invested_co_h { get; set; }
        public int invested_co_avg { get; set; }
        public int invested_co_med { get; set; }
        public int invested_mp_count { get; set; }
        public int invested_mp { get; set; }
        public int invested_mp_l { get; set; }
        public int invested_mp_h { get; set; }
        public int invested_mp_avg { get; set; }
        public int invested_mp_med { get; set; }
        public string added_stats { get; set; }
    }

    public class General
    {
        public Progress progress { get; set; }
        public string retirementNotes { get; set; }
        public CompletionDate completionDate { get; set; }
    }

    public class Lists
    {
        public bool playing { get; set; }
        public bool backlog { get; set; }
        public bool replay { get; set; }
        public bool custom { get; set; }
        public bool custom2 { get; set; }
        public bool custom3 { get; set; }
        public bool completed { get; set; }
        public bool retired { get; set; }
    }

    public class MultiPlayer
    {
        public CoOp coOp { get; set; }
        public Vs vs { get; set; }
    }

    public class PageMetadata
    {
        public bool noTopAd { get; set; }
        public string title { get; set; }
        public string image { get; set; }
        public string description { get; set; }
        public string canonical { get; set; }
        public string template { get; set; }
    }

    public class PageProps
    {
        public GameData gameData { get; set; }
        public EditData editData { get; set; }
        public PageMetadata pageMetadata { get; set; }
        public string _sentryTraceData { get; set; }
        public string _sentryBaggage { get; set; }
    }

    public class Perc100
    {
        public Time time { get; set; }
        public string notes { get; set; }
    }

    public class PercAny
    {
        public Time time { get; set; }
        public string notes { get; set; }
    }

    public class Progress
    {
        public int? hours { get; set; }
        public int? minutes { get; set; }
        public int? seconds { get; set; }
    }

    public class Props
    {
        public PageProps pageProps { get; set; }
        public bool __N_SSP { get; set; }
    }

    public class Query
    {
        public string subType { get; set; }
        public string subId { get; set; }
    }

    public class Review
    {
        public int score { get; set; }
        public string notes { get; set; }
    }

    public class NEXT_DATA
    {
        public Props props { get; set; }
        public string page { get; set; }
        public Query query { get; set; }
        public string buildId { get; set; }
        public bool isFallback { get; set; }
        public bool gssp { get; set; }
        public List<object> scriptLoader { get; set; }
    }

    public class SinglePlayer
    {
        public bool playCount { get; set; }
        public bool includesDLC { get; set; }
        public CompMain compMain { get; set; }
        public CompPlus compPlus { get; set; }
        public Comp100 comp100 { get; set; }
    }

    public class SpeedRuns
    {
        public PercAny percAny { get; set; }
        public Perc100 perc100 { get; set; }
    }

    public class Time
    {
        public int? hours { get; set; }
        public int? minutes { get; set; }
        public int? seconds { get; set; }
    }

    public class Vs
    {
        public Time time { get; set; }
    }
}
