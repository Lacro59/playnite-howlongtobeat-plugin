using Newtonsoft.Json;
using System.Collections.Generic;

namespace HowLongToBeat.Models
{
    public class HltbDataUser : ObservableObject
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public string UrlImg { get; set; }
        public string Url { get; set; } = string.Empty;

        public HltbData GameHltbData { get; set; }

        [JsonIgnore]
        public bool IsEmpty {
            get
            {
                if (GameHltbData == null)
                {
                    return true;
                }

                if (GameHltbData.MainStory != 0 || GameHltbData.MainExtra != 0 || GameHltbData.Completionist != 0 ||
                    GameHltbData.Solo != 0 || GameHltbData.CoOp != 0 || GameHltbData.Vs != 0)
                {
                    return true;
                }

                return false;
            }
        }
    }
}
