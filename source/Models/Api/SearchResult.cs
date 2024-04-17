using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HowLongToBeat.Models.Api
{
    public class Datum
    {
        [SerializationPropertyName("game_id")]
        public int GameId { get; set; }

        [SerializationPropertyName("game_name")]
        public string GameName { get; set; }

        [SerializationPropertyName("game_name_date")]
        public int GameNameDate { get; set; }

        [SerializationPropertyName("game_alias")]
        public string GameAlias { get; set; }

        [SerializationPropertyName("game_type")]
        public string GameType { get; set; }

        [SerializationPropertyName("game_image")]
        public string GameImage { get; set; }

        [SerializationPropertyName("comp_lvl_combine")]
        public int CompLvlCombine { get; set; }

        [SerializationPropertyName("comp_lvl_sp")]
        public int CompLvlSp { get; set; }

        [SerializationPropertyName("comp_lvl_co")]
        public int CompLvlCo { get; set; }

        [SerializationPropertyName("comp_lvl_mp")]
        public int CompLvlMp { get; set; }

        [SerializationPropertyName("comp_lvl_spd")]
        public int CompLvlSpd { get; set; }

        [SerializationPropertyName("comp_main")]
        public int CompMain { get; set; }

        [SerializationPropertyName("comp_plus")]
        public int CompPlus { get; set; }

        [SerializationPropertyName("comp_100")]
        public int Comp100 { get; set; }

        [SerializationPropertyName("comp_all")]
        public int CompAll { get; set; }

        [SerializationPropertyName("comp_main_count")]
        public int CompMainCount { get; set; }

        [SerializationPropertyName("comp_plus_count")]
        public int CompPlusCount { get; set; }

        [SerializationPropertyName("comp_100_count")]
        public int Comp100Count { get; set; }

        [SerializationPropertyName("comp_all_count")]
        public int CompAllCount { get; set; }

        [SerializationPropertyName("invested_co")]
        public int InvestedCo { get; set; }

        [SerializationPropertyName("invested_mp")]
        public int InvestedMp { get; set; }

        [SerializationPropertyName("invested_co_count")]
        public int InvestedCoCount { get; set; }

        [SerializationPropertyName("invested_mp_count")]
        public int InvestedMpCount { get; set; }

        [SerializationPropertyName("count_comp")]
        public int CountComp { get; set; }

        [SerializationPropertyName("count_speedrun")]
        public int CountSpeedrun { get; set; }

        [SerializationPropertyName("count_backlog")]
        public int CountBacklog { get; set; }

        [SerializationPropertyName("count_review")]
        public int CountReview { get; set; }

        [SerializationPropertyName("review_score")]
        public int ReviewScore { get; set; }

        [SerializationPropertyName("count_playing")]
        public int CountPlaying { get; set; }

        [SerializationPropertyName("count_retired")]
        public int CountRetired { get; set; }

        [SerializationPropertyName("profile_dev")]
        public string ProfileDev { get; set; }

        [SerializationPropertyName("profile_popular")]
        public int ProfilePopular { get; set; }

        [SerializationPropertyName("profile_steam")]
        public int ProfileSteam { get; set; }

        [SerializationPropertyName("profile_platform")]
        public string ProfilePlatform { get; set; }

        [SerializationPropertyName("release_world")]
        public int ReleaseWorld { get; set; }
    }

    public class SearchResult
    {
        [SerializationPropertyName("color")]
        public string Color { get; set; }

        [SerializationPropertyName("title")]
        public string Title { get; set; }

        [SerializationPropertyName("category")]
        public string Category { get; set; }

        [SerializationPropertyName("count")]
        public int Count { get; set; }

        [SerializationPropertyName("pageCurrent")]
        public int PageCurrent { get; set; }

        [SerializationPropertyName("pageTotal")]
        public int PageTotal { get; set; }

        [SerializationPropertyName("pageSize")]
        public int PageSize { get; set; }

        [SerializationPropertyName("data")]
        public List<Datum> Data { get; set; }

        [SerializationPropertyName("userData")]
        public List<object> UserData { get; set; }

        [SerializationPropertyName("displayModifier")]
        public object DisplayModifier { get; set; }
    }
}
