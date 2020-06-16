using Newtonsoft.Json;
using System;

namespace HowLongToBeat.Models
{
    public class HltbData
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public string UrlImg { get; set; }
        public long MainStory { get; set; }
        [JsonIgnore]
        public string MainStoryFormat
        {
            get
            {
                return (int)TimeSpan.FromSeconds(MainStory).TotalHours + "h " + TimeSpan.FromSeconds(MainStory).ToString(@"mm") + "min";
            }
        }
        public long MaintExtra { get; set; }
        [JsonIgnore]
        public string MaintExtraFormat
        {
            get
            {
                return (int)TimeSpan.FromSeconds(MaintExtra).TotalHours + "h " + TimeSpan.FromSeconds(MaintExtra).ToString(@"mm") + "min";
            }
        }
        public long Completionist { get; set; }
        [JsonIgnore]
        public string CompletionistFormat
        {
            get
            {
                return (int)TimeSpan.FromSeconds(Completionist).TotalHours + "h " + TimeSpan.FromSeconds(Completionist).ToString(@"mm") + "min";
            }
        }
    }
}
