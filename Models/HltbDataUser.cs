using Newtonsoft.Json;
using System.Collections.Generic;

namespace HowLongToBeat.Models
{
    public class HltbDataUser : ObservableObject
    {
        public long UserMainStory { get; set; } = 0;
        public long UserMainExtra { get; set; } = 0;
        public long UserCompletionist { get; set; } = 0;

        public long UserSolo { get; set; } = 0;
        public long UserCoOp { get; set; } = 0;
        public long UserVs { get; set; } = 0;

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
