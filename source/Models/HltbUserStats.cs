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
using HowLongToBeat.Models.Enumerations;
using CommonPluginsShared.Commands;

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
        private static readonly StatusType[] HltbListDisplayOrder =
        {
            StatusType.Playing,
            StatusType.Backlog,
            StatusType.Replays,
            StatusType.Completed,
            StatusType.Retired,
            StatusType.CustomTab
        };

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

        /// <summary>
        /// Comma-separated HowLongToBeat profile list names (localized), in a stable display order.
        /// </summary>
        [DontSerialize]
        public string HltbListsFormat
        {
            get
            {
                if (GameStatuses == null || GameStatuses.Count == 0)
                {
                    return string.Empty;
                }

                var labels = new List<string>();
                foreach (StatusType status in HltbListDisplayOrder)
                {
                    if (HasHltbListStatus(status))
                    {
                        labels.Add(GetHltbListStatusLabel(status));
                    }
                }

                return string.Join(", ", labels);
            }
        }

        [DontSerialize]
        public double ProgressPercent
        {
            get
            {
                if (TimeToBeat <= 0)
                {
                    return 0;
                }

                return Math.Min(100, (double)CurrentTime * 100 / TimeToBeat);
            }
        }

        [DontSerialize]
        public string ProgressPercentFormat => TimeToBeat > 0
            ? ((int)ProgressPercent).ToString(CultureInfo.CurrentCulture) + "%"
            : string.Empty;

        [DontSerialize]
        public string ProgressToolTip
        {
            get
            {
                if (TimeToBeat <= 0)
                {
                    return string.Empty;
                }

                string played = (string)PlayTimeToStringConverterWithZero.Convert(CurrentTime, null, null, CultureInfo.CurrentCulture);
                string goal = (string)PlayTimeToStringConverterWithZero.Convert(TimeToBeat, null, null, CultureInfo.CurrentCulture);
                return ResourceProvider.GetString("LOCTimePlayed") + ": " + played
                    + Environment.NewLine
                    + ResourceProvider.GetString("LOCHowLongToBeatTimeToBeat") + ": " + goal
                    + Environment.NewLine
                    + ProgressPercentFormat;
            }
        }

        /// <summary>
        /// True when HowLongToBeat has "Mark as Replay — I have played this before" enabled (API <c>play_count == 2</c>).
        /// Not the same as belonging to the profile <see cref="StatusType.Replays"/> list.
        /// </summary>
        public bool IsReplay { get; set; }

        /// <summary>
        /// True when HowLongToBeat has "DLC / Expansions Included" enabled (API <c>play_dlc == 1</c>).
        /// </summary>
        public bool IsIncludesDlc { get; set; }

        public bool IsRetired { get; set; }

        public DateTime StartDate { get; set; } = default;
        public DateTime LastUpdate { get; set; } = default;

        public DateTime? Completion { get; set; }
        [DontSerialize]
        public string CompletionFormat => Completion == null ? string.Empty : (string)Converter.Convert((DateTime)Completion, null, null, CultureInfo.CurrentCulture);

        public List<GameStatus> GameStatuses { get; set; } = new List<GameStatus>();

        public HltbData HltbUserData { get; set; }

        [DontSerialize]
        public Guid GameId => PluginDatabase?.ResolveGameIdFromUserTitle(Id, UserGameId) ?? default;

        // TODO
        [DontSerialize]
        public List<Guid> GameIds => PluginDatabase?.ResolveGameIdsFromUserTitle(Id, UserGameId) ?? new List<Guid>();

        [DontSerialize]
        public RelayCommand<Guid> GoToGame => CommandsNavigation.GoToGame;

        [DontSerialize]
        public bool GameExist => API.Instance.Database.Games.Get(GameId) != null;

        /// <summary>
        /// Returns whether this user title belongs to the given HowLongToBeat profile list.
        /// </summary>
        public bool HasHltbListStatus(StatusType statusType)
        {
            return GameStatuses != null && GameStatuses.Any(s => s.Status == statusType);
        }

        private static string GetHltbListStatusLabel(StatusType statusType)
        {
            switch (statusType)
            {
                case StatusType.Backlog:
                    return ResourceProvider.GetString("LOCHltbUserListBacklog");
                case StatusType.Playing:
                    return ResourceProvider.GetString("LOCHltbUserListPlaying");
                case StatusType.Replays:
                    return ResourceProvider.GetString("LOCHltbUserListReplays");
                case StatusType.Completed:
                    return ResourceProvider.GetString("LOCHltbUserListCompleted");
                case StatusType.Retired:
                    return ResourceProvider.GetString("LOCHltbUserListRetired");
                case StatusType.CustomTab:
                    return ResourceProvider.GetString("LOCHltbUserListCustom");
                default:
                    return statusType.ToString();
            }
        }
    }


    public class GameStatus
    {
        public StatusType Status { get; set; }
        public long Time { get; set; }
    }
}
