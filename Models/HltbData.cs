using Newtonsoft.Json;
using System;


namespace HowLongToBeat.Models
{
    public class HltbData
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public string UrlImg { get; set; }
        public string Url { get; set; } = "";
        public long MainStory { get; set; }
        [JsonIgnore]
        public string MainStoryFormat
        {
            get
            {
                if (MainStory == 0)
                {
                    return "--";
                }
                return (int)TimeSpan.FromSeconds(MainStory).TotalHours + "h " + TimeSpan.FromSeconds(MainStory).ToString(@"mm") + "min";
            }
        }
        public long MainExtra { get; set; }
        [JsonIgnore]
        public string MainExtraFormat
        {
            get
            {
                if (MainExtra == 0)
                {
                    return "--";
                }
                return (int)TimeSpan.FromSeconds(MainExtra).TotalHours + "h " + TimeSpan.FromSeconds(MainExtra).ToString(@"mm") + "min";
            }
        }
        public long Completionist { get; set; }
        [JsonIgnore]
        public string CompletionistFormat
        {
            get
            {
                if (Completionist == 0)
                {
                    return "--";
                }
                return (int)TimeSpan.FromSeconds(Completionist).TotalHours + "h " + TimeSpan.FromSeconds(Completionist).ToString(@"mm") + "min";
            }
        }


        public long Solo { get; set; } = 0;
        [JsonIgnore]
        public string SoloFormat
        {
            get
            {
                if (Solo == 0)
                {
                    return "--";
                }
                return (int)TimeSpan.FromSeconds(Solo).TotalHours + "h " + TimeSpan.FromSeconds(Solo).ToString(@"mm") + "min";
            }
        }
        public long CoOp { get; set; } = 0;
        [JsonIgnore]
        public string CoOpFormat
        {
            get
            {
                if (CoOp == 0)
                {
                    return "--";
                }
                return (int)TimeSpan.FromSeconds(CoOp).TotalHours + "h " + TimeSpan.FromSeconds(CoOp).ToString(@"mm") + "min";
            }
        }
        public long Vs { get; set; } = 0;
        [JsonIgnore]
        public string VsFormat
        {
            get
            {
                if (Vs == 0)
                {
                    return "--";
                }
                return (int)TimeSpan.FromSeconds(Vs).TotalHours + "h " + TimeSpan.FromSeconds(Vs).ToString(@"mm") + "min";
            }
        }
    }
}
