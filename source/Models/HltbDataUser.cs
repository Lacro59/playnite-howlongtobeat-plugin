using Playnite.SDK.Data;
using System.Collections.Generic;

namespace HowLongToBeat.Models
{
    public class HltbDataUser : ObservableObject
    {
        public string Name { get; set; } = string.Empty;
        public int Id { get; set; } = 0;
        public string UrlImg { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;

        public HltbData GameHltbData { get; set; }

        [DontSerialize]
        public bool IsEmpty => GameHltbData == null || (GameHltbData.MainStory == 0 && GameHltbData.MainExtra == 0 && GameHltbData.Completionist == 0 && GameHltbData.Solo == 0 && GameHltbData.CoOp == 0 && GameHltbData.Vs == 0);
    }
}
