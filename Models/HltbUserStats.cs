using CommonPluginsShared;
using HowLongToBeat.Services;
using Newtonsoft.Json;
using Playnite.SDK;
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
        public int UserId { get; set; }

        public List<TitleList> TitlesList { get; set; }
    }


    public enum TitleListSort
    {
        GameName, Platform, Completion, CurrentTime
    }

    public class TitleList
    {
        private HowLongToBeatDatabase PluginDatabase = HowLongToBeat.PluginDatabase;

        private LocalDateConverter converter = new LocalDateConverter();

        public int Id { get; set; }
        public string UserGameId { get; set; }
        public string GameName { get; set; }
        public string Platform { get; set; }
        public long CurrentTime { get; set; }

        public bool IsReplay { get; set; }
        public bool IsRetired { get; set; }

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

        [JsonIgnore]
        public Guid GameId {
            get
            {
                foreach(var el in PluginDatabase.Database.Items)
                {
                    if (el.Value.GetData().Id == Id)
                    {
                        return el.Key;
                    }
                }

                return new Guid();
            }
        }

        [JsonIgnore]
        public RelayCommand<Guid> GoToGame
        {
            get
            {
                return PluginDatabase.GoToGame;
            }
        }

        [JsonIgnore]
        public bool GameExist
        {
            get
            {
                return PluginDatabase.PlayniteApi.Database.Games.Get(GameId) != null;
            }
        }
    }

    public class GameStatus
    {
        public StatusType Status { get; set; }
        public long Time { get; set; }
    }
}
