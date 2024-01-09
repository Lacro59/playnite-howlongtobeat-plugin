using CommonPluginsShared.Converters;
using HowLongToBeat.Services;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using CommonPluginsShared.Extensions;

namespace HowLongToBeat.Models
{
    public class HltbUserStats
    {
        public string Login { get; set; }
        public int UserId { get; set; }

        public List<TitleList> TitlesList { get; set; } = new List<TitleList>();
    }


    public enum TitleListSort
    {
        GameName, Platform, Completion, CurrentTime, LastUpdate
    }


    public class TitleList
    {
        private HowLongToBeatDatabase PluginDatabase = HowLongToBeat.PluginDatabase;

        private LocalDateConverter converter = new LocalDateConverter();
        private PlayTimeToStringConverterWithZero playTimeToStringConverterWithZero = new PlayTimeToStringConverterWithZero();

        public int Id { get; set; }
        public string UserGameId { get; set; }
        public string GameName { get; set; }
        public string Platform { get; set; }
        public string Storefront { get; set; } = string.Empty;
        public long CurrentTime { get; set; }
        public long RemainingTime => (PluginDatabase.Get(GameId, true)?.GetData()?.GameHltbData?.TimeToBeat ?? 0) - CurrentTime > 0 ? PluginDatabase.Get(GameId, true).GetData().GameHltbData.TimeToBeat - CurrentTime : 0;
        public string RemainingTimeFormat => RemainingTime > 0 ? (string)playTimeToStringConverterWithZero.Convert(RemainingTime, null, null, CultureInfo.CurrentCulture) : string.Empty;

        public bool IsReplay { get; set; }
        public bool IsRetired { get; set; }

        public DateTime LastUpdate { get; set; } = default;

        public DateTime? Completion { get; set; }
        [DontSerialize]
        public string CompletionFormat => Completion == null ? string.Empty : (string)converter.Convert((DateTime)Completion, null, null, CultureInfo.CurrentCulture);

        public List<GameStatus> GameStatuses { get; set; } = new List<GameStatus>();

        public HltbData HltbUserData { get; set; }

        [DontSerialize]
        public Guid GameId
        {
            get
            {
                Guid? result = PluginDatabase.Database.Items.Where(x => x.Value.GetData()?.Id == Id
                                    && (x.Value.UserGameId.IsNullOrEmpty() ? true : x.Value.UserGameId.IsEqual(UserGameId)))?            
                    .FirstOrDefault().Key;

                return result == null || (Guid)result == default(Guid) ? new Guid() : (Guid)result;
            }
        }

        [DontSerialize]
        public RelayCommand<Guid> GoToGame => PluginDatabase.GoToGame;

        [DontSerialize]
        public bool GameExist => PluginDatabase.PlayniteApi.Database.Games.Get(GameId) != null;
    }


    public class GameStatus
    {
        public StatusType Status { get; set; }
        public long Time { get; set; }
    }
}
