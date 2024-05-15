using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HowLongToBeat.Models.Vndb
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Image
    {
        [SerializationPropertyName("url")]
        public string Url { get; set; }
    }

    public class Result
    {
        [SerializationPropertyName("title")]
        public string Title { get; set; }

        [SerializationPropertyName("id")]
        public string Id { get; set; }

        [SerializationPropertyName("alttitle")]
        public string Alttitle { get; set; }

        [SerializationPropertyName("length")]
        public int? Length { get; set; }

        [SerializationPropertyName("image")]
        public Image Image { get; set; }

        [SerializationPropertyName("length_minutes")]
        public int? LengthMinutes { get; set; }
    }

    public class VndbSearch
    {
        [SerializationPropertyName("results")]
        public List<Result> Results { get; set; }

        [SerializationPropertyName("more")]
        public bool More { get; set; }
    }
}
