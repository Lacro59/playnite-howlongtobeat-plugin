using Newtonsoft.Json;
using Playnite.Converters;
using System.Globalization;

namespace HowLongToBeat.Models
{
    public class HltbData
    {
        private LongToTimePlayedConverter converter = new LongToTimePlayedConverter();

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
                return (string)converter.Convert((long)MainStory, null, null, CultureInfo.CurrentCulture);
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
                return (string)converter.Convert((long)MainExtra, null, null, CultureInfo.CurrentCulture);
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
                return (string)converter.Convert((long)Completionist, null, null, CultureInfo.CurrentCulture);
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
                return (string)converter.Convert((long)Solo, null, null, CultureInfo.CurrentCulture);
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
                return (string)converter.Convert((long)CoOp, null, null, CultureInfo.CurrentCulture);
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
                return (string)converter.Convert((long)Vs, null, null, CultureInfo.CurrentCulture);
            }
        }
    }
}
