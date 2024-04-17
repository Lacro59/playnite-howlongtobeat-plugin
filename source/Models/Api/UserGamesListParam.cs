using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HowLongToBeat.Models.Api
{
    public class UserGamesListParam
    {
        [SerializationPropertyName("user_id")]
        public int UserId { get; set; }

        [SerializationPropertyName("lists")]
        public List<string> Lists { get; set; } = new List<string> { "playing", "backlog", "replays", "custom", "custom2", "custom3", "completed", "retired" };

        [SerializationPropertyName("set_playstyle")]
        public string SetPlaystyle { get; set; } = "comp_all";

        [SerializationPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [SerializationPropertyName("platform")]
        public string Platform { get; set; } = string.Empty;

        [SerializationPropertyName("storefront")]
        public string Storefront { get; set; } = string.Empty;

        [SerializationPropertyName("sortBy")]
        public string SortBy { get; set; } = string.Empty;

        [SerializationPropertyName("sortFlip")]
        public int SortFlip { get; set; }

        [SerializationPropertyName("view")]
        public string View { get; set; } = string.Empty;

        [SerializationPropertyName("random")]
        public int Random { get; set; }

        [SerializationPropertyName("limit")]
        public int Limit { get; set; } = 5000;

        [SerializationPropertyName("currentUserHome")]
        public bool CurrentUserHome { get; set; } = true;
    }


}
