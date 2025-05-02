using CommonPluginsShared.Converters;
using HowLongToBeat.Services;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using CommonPluginsShared.Extensions;
using CommonPluginsShared;

namespace HowLongToBeat.Models
{
    public class HltbUserStats
    {
        public string Login { get; set; }
        public int UserId { get; set; }

        public List<TitleList> TitlesList { get; set; } = new List<TitleList>();
    }


    public class TitleList
    {
        private static HowLongToBeatDatabase PluginDatabase => HowLongToBeat.PluginDatabase;

        private LocalDateConverter Converter => new LocalDateConverter();
        private PlayTimeToStringConverterWithZero PlayTimeToStringConverterWithZero => new PlayTimeToStringConverterWithZero();

        public string Id { get; set; }
        public string UserGameId { get; set; }
        public string GameName { get; set; }
        public string Platform { get; set; }
        public string Storefront { get; set; } = string.Empty;
        public long CurrentTime { get; set; }
        [DontSerialize]
        public long TimeToBeat => PluginDatabase.Get(GameId, true)?.GetData()?.GameHltbData?.TimeToBeat ?? 0;
        [DontSerialize]
        public long RemainingTime => TimeToBeat - CurrentTime > 0 ? TimeToBeat - CurrentTime : 0;
        [DontSerialize]
        public string RemainingTimeFormat => RemainingTime > 0 ? (string)PlayTimeToStringConverterWithZero.Convert(RemainingTime, null, null, CultureInfo.CurrentCulture) : string.Empty;

        public bool IsReplay { get; set; }
        public bool IsRetired { get; set; }

        public DateTime StartDate { get; set; } = default;
        public DateTime LastUpdate { get; set; } = default;

        public DateTime? Completion { get; set; }
        [DontSerialize]
        public string CompletionFormat => Completion == null ? string.Empty : (string)Converter.Convert((DateTime)Completion, null, null, CultureInfo.CurrentCulture);

        public List<GameStatus> GameStatuses { get; set; } = new List<GameStatus>();

        public HltbData HltbUserData { get; set; }

        [DontSerialize]
        public Guid GameId
        {
            get
            {
                Guid? result = PluginDatabase.Database.Items
                    .Where(x => !x.Value.Game.Hidden && x.Value.GetData()?.Id == Id && (x.Value.UserGameId.IsNullOrEmpty() || x.Value.UserGameId.IsEqual(UserGameId)))
                    ?.FirstOrDefault().Key;

                return result == null ? default : (Guid)result;
            }
        }

        // TODO
        [DontSerialize]
        public List<Guid> GameIds
        {
            get
            {
                Guid? result = PluginDatabase.Database.Items
                    .Where(x => x.Value.GetData()?.Id == Id && (x.Value.UserGameId.IsNullOrEmpty() || x.Value.UserGameId.IsEqual(UserGameId)))
                    ?.FirstOrDefault().Key;

                List<Guid> results = PluginDatabase.Database.Items
                    .Where(x => x.Value.GetData()?.Id == Id && (x.Value.UserGameId.IsNullOrEmpty() || x.Value.UserGameId.IsEqual(UserGameId)))
                    ?.Select(x => x.Key)
                    ?.ToList() ?? new List<Guid>();

                return results;
            }
        }

        [DontSerialize]
        public RelayCommand<Guid> GoToGame => Commands.GoToGame;

        [DontSerialize]
        public bool GameExist => API.Instance.Database.Games.Get(GameId) != null;
    }


    public class GameStatus
    {
        public StatusType Status { get; set; }
        public long Time { get; set; }
    }
}
