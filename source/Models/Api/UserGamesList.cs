using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HowLongToBeat.Models.Api
{
    public class Data
    {
        [SerializationPropertyName("count")]
        public int Count { get; set; }

        [SerializationPropertyName("gamesList")]
        public List<GamesList> GamesList { get; set; }

        [SerializationPropertyName("total")]
        public int Total { get; set; }

        [SerializationPropertyName("platformList")]
        public List<PlatformList> PlatformList { get; set; }

        [SerializationPropertyName("summaryData")]
        public SummaryData SummaryData { get; set; }
    }

    public class GamesList
    {
        [SerializationPropertyName("id")]
        public int Id { get; set; }

        [SerializationPropertyName("custom_title")]
        public string CustomTitle { get; set; }

        [SerializationPropertyName("platform")]
        public string Platform { get; set; }

        [SerializationPropertyName("play_storefront")]
        public string PlayStorefront { get; set; }

        [SerializationPropertyName("list_playing")]
        public int ListPlaying { get; set; }

        [SerializationPropertyName("list_backlog")]
        public int ListBacklog { get; set; }

        [SerializationPropertyName("list_replay")]
        public int ListReplay { get; set; }

        [SerializationPropertyName("list_custom")]
        public int ListCustom { get; set; }

        [SerializationPropertyName("list_custom2")]
        public int ListCustom2 { get; set; }

        [SerializationPropertyName("list_custom3")]
        public int ListCustom3 { get; set; }

        [SerializationPropertyName("list_comp")]
        public int ListComp { get; set; }

        [SerializationPropertyName("list_retired")]
        public int ListRetired { get; set; }

        [SerializationPropertyName("comp_main")]
        public int CompMain { get; set; }

        [SerializationPropertyName("comp_plus")]
        public int CompPlus { get; set; }

        [SerializationPropertyName("comp_100")]
        public int Comp100 { get; set; }

        [SerializationPropertyName("comp_speed")]
        public int CompSpeed { get; set; }

        [SerializationPropertyName("comp_speed100")]
        public int CompSpeed100 { get; set; }

        [SerializationPropertyName("comp_main_notes")]
        public string CompMainNotes { get; set; }

        [SerializationPropertyName("comp_plus_notes")]
        public string CompPlusNotes { get; set; }

        [SerializationPropertyName("comp_100_notes")]
        public string Comp100Notes { get; set; }

        [SerializationPropertyName("comp_speed_notes")]
        public string CompSpeedNotes { get; set; }

        [SerializationPropertyName("comp_speed100_notes")]
        public string CompSpeed100Notes { get; set; }

        [SerializationPropertyName("invested_pro")]
        public int InvestedPro { get; set; }

        [SerializationPropertyName("invested_sp")]
        public int InvestedSp { get; set; }

        [SerializationPropertyName("invested_spd")]
        public int InvestedSpd { get; set; }

        [SerializationPropertyName("invested_co")]
        public int InvestedCo { get; set; }

        [SerializationPropertyName("invested_mp")]
        public int InvestedMp { get; set; }

        [SerializationPropertyName("play_count")]
        public int PlayCount { get; set; }

        [SerializationPropertyName("play_dlc")]
        public int PlayDlc { get; set; }

        [SerializationPropertyName("review_score")]
        public int ReviewScore { get; set; }

        [SerializationPropertyName("review_notes")]
        public string ReviewNotes { get; set; }

        [SerializationPropertyName("retired_notes")]
        public string RetiredNotes { get; set; }

        [SerializationPropertyName("date_complete")]
        public string DateComplete { get; set; }

        [SerializationPropertyName("date_updated")]
        public string DateUpdated { get; set; }

        [SerializationPropertyName("play_video")]
        public string PlayVideo { get; set; }

        [SerializationPropertyName("play_notes")]
        public string PlayNotes { get; set; }

        [SerializationPropertyName("game_id")]
        public int GameId { get; set; }

        [SerializationPropertyName("game_image")]
        public string GameImage { get; set; }

        [SerializationPropertyName("game_type")]
        public string GameType { get; set; }

        [SerializationPropertyName("release_world")]
        public string ReleaseWorld { get; set; }

        [SerializationPropertyName("comp_all")]
        public int CompAll { get; set; }

        [SerializationPropertyName("comp_all_g")]
        public int CompAllG { get; set; }

        [SerializationPropertyName("review_score_g")]
        public int ReviewScoreG { get; set; }
    }

    public class PlatformList
    {
        [SerializationPropertyName("platform")]
        public string Platform { get; set; }

        [SerializationPropertyName("count_total")]
        public int CountTotal { get; set; }
    }

    public class UserGamesList
    {
        [SerializationPropertyName("data")]
        public Data Data { get; set; }
    }

    public class SummaryData
    {
        [SerializationPropertyName("playCount")]
        public int PlayCount { get; set; }

        [SerializationPropertyName("dlcCount")]
        public int DlcCount { get; set; }

        [SerializationPropertyName("reviewTotal")]
        public int ReviewTotal { get; set; }

        [SerializationPropertyName("reviewCount")]
        public int ReviewCount { get; set; }

        [SerializationPropertyName("totalPlayedSp")]
        public int TotalPlayedSp { get; set; }

        [SerializationPropertyName("totalPlayedMp")]
        public int TotalPlayedMp { get; set; }

        [SerializationPropertyName("toBeatListed")]
        public int ToBeatListed { get; set; }

        [SerializationPropertyName("uniqueGameCount")]
        public int UniqueGameCount { get; set; }
    }
}
