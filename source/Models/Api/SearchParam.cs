using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HowLongToBeat.Models.Api
{
    public class Gameplay
    {
        [SerializationPropertyName("perspective")]
        public string Perspective { get; set; } = string.Empty;

        [SerializationPropertyName("flow")]
        public string Flow { get; set; } = string.Empty;

        [SerializationPropertyName("genre")]
        public string Genre { get; set; } = string.Empty;

        [SerializationPropertyName("difficulty")]
        public string Difficulty { get; set; } = string.Empty;
    }

    public class Games
    {
        [SerializationPropertyName("userId")]
        public int UserId { get; set; } = 0;

        [SerializationPropertyName("platform")]
        public string Platform { get; set; } = string.Empty;

        [SerializationPropertyName("sortCategory")]
        public string SortCategory { get; set; } = "popular";

        [SerializationPropertyName("rangeCategory")]
        public string RangeCategory { get; set; } = "main";

        [SerializationPropertyName("rangeTime")]
        public RangeTime RangeTime { get; set; } = new RangeTime();

        [SerializationPropertyName("gameplay")]
        public Gameplay Gameplay { get; set; } = new Gameplay();

        [SerializationPropertyName("modifier")]
        public string Modifier { get; set; } = string.Empty;
    }

    public class RangeTime
    {
        [SerializationPropertyName("min")]
        public int Min { get; set; }

        [SerializationPropertyName("max")]
        public int Max { get; set; }
    }

    public class SearchParam
    {
        [SerializationPropertyName("searchType")]
        public string SearchType { get; set; } = "games";

        [SerializationPropertyName("searchTerms")]
        public List<string> SearchTerms { get; set; } = new List<string>();

        [SerializationPropertyName("searchPage")]
        public int SearchPage { get; set; } = 1;

        [SerializationPropertyName("size")]
        public int Size { get; set; } = 20;

        [SerializationPropertyName("searchOptions")]
        public SearchOptions SearchOptions { get; set; } = new SearchOptions();
    }

    public class SearchOptions
    {
        [SerializationPropertyName("games")]
        public Games Games { get; set; } = new Games();

        [SerializationPropertyName("users")]
        public Users Users { get; set; } = new Users();

        [SerializationPropertyName("filter")]
        public string Filter { get; set; } = string.Empty;

        [SerializationPropertyName("sort")]
        public int Sort { get; set; } = 0;

        [SerializationPropertyName("randomizer")]
        public int Randomizer { get; set; } = 0;
    }

    public class Users
    {
        [SerializationPropertyName("sortCategory")]
        public string SortCategory { get; set; } = "postcount";
    }
}
