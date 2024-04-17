using HowLongToBeat.Models.Api;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HowLongToBeat.Models
{
    public class GameData
    {
        [SerializationPropertyName("count_discussion")]
        public int CountDiscussion { get; set; }

        [SerializationPropertyName("game_id")]
        public int GameId { get; set; }

        [SerializationPropertyName("game_name")]
        public string GameName { get; set; }

        [SerializationPropertyName("game_name_date")]
        public int GameNameDate { get; set; }

        [SerializationPropertyName("count_playing")]
        public int CountPlaying { get; set; }

        [SerializationPropertyName("count_backlog")]
        public int CountBacklog { get; set; }

        [SerializationPropertyName("count_replay")]
        public int CountReplay { get; set; }

        [SerializationPropertyName("count_custom")]
        public int CountCustom { get; set; }

        [SerializationPropertyName("count_comp")]
        public int CountComp { get; set; }

        [SerializationPropertyName("count_retired")]
        public int CountRetired { get; set; }

        [SerializationPropertyName("count_review")]
        public int CountReview { get; set; }

        [SerializationPropertyName("review_score")]
        public int ReviewScore { get; set; }

        [SerializationPropertyName("game_alias")]
        public string GameAlias { get; set; }

        [SerializationPropertyName("game_image")]
        public string GameImage { get; set; }

        [SerializationPropertyName("game_type")]
        public string GameType { get; set; }

        [SerializationPropertyName("game_parent")]
        public int GameParent { get; set; }

        [SerializationPropertyName("profile_summary")]
        public string ProfileSummary { get; set; }

        [SerializationPropertyName("profile_dev")]
        public string ProfileDev { get; set; }

        [SerializationPropertyName("profile_pub")]
        public string ProfilePub { get; set; }

        [SerializationPropertyName("profile_platform")]
        public string ProfilePlatform { get; set; }

        [SerializationPropertyName("profile_genre")]
        public string ProfileGenre { get; set; }

        [SerializationPropertyName("profile_steam")]
        public int ProfileSteam { get; set; }

        [SerializationPropertyName("profile_steam_alt")]
        public int ProfileSteamAlt { get; set; }

        [SerializationPropertyName("profile_itch")]
        public int ProfileItch { get; set; }

        [SerializationPropertyName("profile_ign")]
        public object ProfileIgn { get; set; }

        [SerializationPropertyName("release_world")]
        public string ReleaseWorld { get; set; }

        [SerializationPropertyName("release_na")]
        public string ReleaseNa { get; set; }

        [SerializationPropertyName("release_eu")]
        public string ReleaseEu { get; set; }

        [SerializationPropertyName("release_jp")]
        public string ReleaseJp { get; set; }

        [SerializationPropertyName("rating_esrb")]
        public string RatingEsrb { get; set; }

        [SerializationPropertyName("rating_pegi")]
        public string RatingPegi { get; set; }

        [SerializationPropertyName("rating_cero")]
        public string RatingCero { get; set; }

        [SerializationPropertyName("comp_lvl_sp")]
        public int CompLvlSp { get; set; }

        [SerializationPropertyName("comp_lvl_spd")]
        public int CompLvlSpd { get; set; }

        [SerializationPropertyName("comp_lvl_co")]
        public int CompLvlCo { get; set; }

        [SerializationPropertyName("comp_lvl_mp")]
        public int CompLvlMp { get; set; }

        [SerializationPropertyName("comp_lvl_combine")]
        public int CompLvlCombine { get; set; }

        [SerializationPropertyName("comp_lvl_platform")]
        public int CompLvlPlatform { get; set; }

        [SerializationPropertyName("comp_all_count")]
        public int CompAllCount { get; set; }

        [SerializationPropertyName("comp_all")]
        public int CompAll { get; set; }

        [SerializationPropertyName("comp_all_l")]
        public int CompAllL { get; set; }

        [SerializationPropertyName("comp_all_h")]
        public int CompAllH { get; set; }

        [SerializationPropertyName("comp_all_avg")]
        public int CompAllAvg { get; set; }

        [SerializationPropertyName("comp_all_med")]
        public int CompAllMed { get; set; }

        [SerializationPropertyName("comp_main_count")]
        public int CompMainCount { get; set; }

        [SerializationPropertyName("comp_main")]
        public int CompMain { get; set; }

        [SerializationPropertyName("comp_main_l")]
        public int CompMainL { get; set; }

        [SerializationPropertyName("comp_main_h")]
        public int CompMainH { get; set; }

        [SerializationPropertyName("comp_main_avg")]
        public int CompMainAvg { get; set; }

        [SerializationPropertyName("comp_main_med")]
        public int CompMainMed { get; set; }

        [SerializationPropertyName("comp_plus_count")]
        public int CompPlusCount { get; set; }

        [SerializationPropertyName("comp_plus")]
        public int CompPlus { get; set; }

        [SerializationPropertyName("comp_plus_l")]
        public int CompPlusL { get; set; }

        [SerializationPropertyName("comp_plus_h")]
        public int CompPlusH { get; set; }

        [SerializationPropertyName("comp_plus_avg")]
        public int CompPlusAvg { get; set; }

        [SerializationPropertyName("comp_plus_med")]
        public int CompPlusMed { get; set; }

        [SerializationPropertyName("comp_100_count")]
        public int Comp100Count { get; set; }

        [SerializationPropertyName("comp_100")]
        public int Comp100 { get; set; }

        [SerializationPropertyName("comp_100_l")]
        public int Comp100L { get; set; }

        [SerializationPropertyName("comp_100_h")]
        public int Comp100H { get; set; }

        [SerializationPropertyName("comp_100_avg")]
        public int Comp100Avg { get; set; }

        [SerializationPropertyName("comp_100_med")]
        public int Comp100Med { get; set; }

        [SerializationPropertyName("comp_speed_count")]
        public int CompSpeedCount { get; set; }

        [SerializationPropertyName("comp_speed")]
        public int CompSpeed { get; set; }

        [SerializationPropertyName("comp_speed_min")]
        public int CompSpeedMin { get; set; }

        [SerializationPropertyName("comp_speed_max")]
        public int CompSpeedMax { get; set; }

        [SerializationPropertyName("comp_speed_avg")]
        public int CompSpeedAvg { get; set; }

        [SerializationPropertyName("comp_speed_med")]
        public int CompSpeedMed { get; set; }

        [SerializationPropertyName("comp_speed100_count")]
        public int CompSpeed100Count { get; set; }

        [SerializationPropertyName("comp_speed100")]
        public int CompSpeed100 { get; set; }

        [SerializationPropertyName("comp_speed100_min")]
        public int CompSpeed100Min { get; set; }

        [SerializationPropertyName("comp_speed100_max")]
        public int CompSpeed100Max { get; set; }

        [SerializationPropertyName("comp_speed100_avg")]
        public int CompSpeed100Avg { get; set; }

        [SerializationPropertyName("comp_speed100_med")]
        public int CompSpeed100Med { get; set; }

        [SerializationPropertyName("count_total")]
        public int CountTotal { get; set; }

        [SerializationPropertyName("invested_co_count")]
        public int InvestedCoCount { get; set; }

        [SerializationPropertyName("invested_co")]
        public int InvestedCo { get; set; }

        [SerializationPropertyName("invested_co_l")]
        public int InvestedCoL { get; set; }

        [SerializationPropertyName("invested_co_h")]
        public int InvestedCoH { get; set; }

        [SerializationPropertyName("invested_co_avg")]
        public int InvestedCoAvg { get; set; }

        [SerializationPropertyName("invested_co_med")]
        public int InvestedCoMed { get; set; }

        [SerializationPropertyName("invested_mp_count")]
        public int InvestedMpCount { get; set; }

        [SerializationPropertyName("invested_mp")]
        public int InvestedMp { get; set; }

        [SerializationPropertyName("invested_mp_l")]
        public int InvestedMpL { get; set; }

        [SerializationPropertyName("invested_mp_h")]
        public int InvestedMpH { get; set; }

        [SerializationPropertyName("invested_mp_avg")]
        public int InvestedMpAvg { get; set; }

        [SerializationPropertyName("invested_mp_med")]
        public int InvestedMpMed { get; set; }

        [SerializationPropertyName("added_stats")]
        public string AddedStats { get; set; }
    }

    public class PageMetadata
    {
        [SerializationPropertyName("noTopAd")]
        public bool NoTopAd { get; set; }

        [SerializationPropertyName("title")]
        public string Title { get; set; }

        [SerializationPropertyName("image")]
        public string Image { get; set; }

        [SerializationPropertyName("description")]
        public string Description { get; set; }

        [SerializationPropertyName("canonical")]
        public string Canonical { get; set; }

        [SerializationPropertyName("template")]
        public string Template { get; set; }
    }

    public class PageProps
    {
        [SerializationPropertyName("gameData")]
        public GameData GameData { get; set; }

        [SerializationPropertyName("editData")]
        public EditData EditData { get; set; }

        [SerializationPropertyName("pageMetadata")]
        public PageMetadata PageMetadata { get; set; }
    }

    public class Props
    {
        [SerializationPropertyName("pageProps")]
        public PageProps PageProps { get; set; }

        [SerializationPropertyName("__N_SSP")]
        public bool NSSP { get; set; }
    }

    public class Query
    {
        [SerializationPropertyName("subType")]
        public string SubType { get; set; }

        [SerializationPropertyName("subId")]
        public string SubId { get; set; }
    }

    public class NEXT_DATA
    {
        [SerializationPropertyName("props")]
        public Props Props { get; set; }

        [SerializationPropertyName("page")]
        public string Page { get; set; }

        [SerializationPropertyName("query")]
        public Query Query { get; set; }

        [SerializationPropertyName("buildId")]
        public string BuildId { get; set; }

        [SerializationPropertyName("isFallback")]
        public bool IsFallback { get; set; }

        [SerializationPropertyName("isExperimentalCompile")]
        public bool IsExperimentalCompile { get; set; }

        [SerializationPropertyName("gssp")]
        public bool Gssp { get; set; }

        [SerializationPropertyName("scriptLoader")]
        public List<object> ScriptLoader { get; set; }
    }
}
