using CommonPluginsShared;
using HowLongToBeat.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HowLongToBeat.Models
{
    public class HltbUserStats
    {
        public string Login { get; set; }

        public List<TitleList> TitlesList { get; set; }
    }


    public class TitleList
    {
        private LocalDateConverter converter = new LocalDateConverter();

        public int Id { get; set; }
        public string UserGameId { get; set; }
        public string GameName { get; set; }
        public string Platform { get; set; }
        public DateTime? Completion { get; set; }
        [JsonIgnore]
        public string CompletionFormat
        {
            get
            {
                if (Completion == null)
                {
                    return string.Empty;
                }
                return (string)converter.Convert((DateTime)Completion, null, null, CultureInfo.CurrentCulture);
            }
        }

        public List<GameStatus> GameStatuses { get; set; }

        public HltbData HltbUserData { get; set; }
    }

    public class GameStatus
    {
        public StatusType Status { get; set; }
        public long Time { get; set; }
    }
}
