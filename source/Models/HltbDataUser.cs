using HowLongToBeat.Models.Enumerations;
using Playnite.SDK;
using Playnite.SDK.Data;
using System.Collections.Generic;

namespace HowLongToBeat.Models
{
    public class HltbDataUser : ObservableObject
    {
        public string Name { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string UrlImg { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public GameType GameType { get; set; } = GameType.Game;

        public HltbData GameHltbData { get; set; }

        [DontSerialize]
        public bool NeedsDetails { get; set; } = false;

        [DontSerialize]
        public HltbData GameHltbDataByType => GameType == GameType.Multi
                    ? new HltbData
                    {
                        SoloClassic = GameHltbData.Solo,
                        CoOpClassic = GameHltbData.CoOp,
                        VsClassic = GameHltbData.Vs
                    }
                    : new HltbData
                    {
                        MainStoryClassic = GameHltbData.MainStory,
                        MainExtraClassic = GameHltbData.MainExtra,
                        CompletionistClassic = GameHltbData.Completionist
                    };

        [DontSerialize]
        public bool IsEmpty => GameHltbData == null || (GameHltbData.MainStory == 0 && GameHltbData.MainExtra == 0 && GameHltbData.Completionist == 0 && GameHltbData.Solo == 0 && GameHltbData.CoOp == 0 && GameHltbData.Vs == 0);


        public bool IsVndb { get; set; }

        /// <summary>
        /// When <see cref="IsVndb"/> is true, the reading speed row chosen by the user (maps to HLTB <see cref="DataType"/>:
        /// Classic=Slow, Average=Normal, Median=Fast, Rushed=Total). Stored explicitly because <see cref="HltbData.DataType"/>
        /// is derived from plugin toggles and non-zero time fields, while VNDB import collapses the selected time into MainStoryClassic.
        /// </summary>
        public DataType? VndbSpeedDataType { get; set; }

        public string VndbSlowMedian { get; set; } = "--";
        public string VndbSlowAverage { get; set; } = "--";
        public string VndbSlowStddev { get; set; } = "--";
        public int VndbSlowVotes { get; set; } = 0;

        public string VndbNormalMedian { get; set; } = "--";
        public string VndbNormalAverage { get; set; } = "--";
        public string VndbNormalStddev { get; set; } = "--";
        public int VndbNormalVotes { get; set; } = 0;

        public string VndbFastMedian { get; set; } = "--";
        public string VndbFastAverage { get; set; } = "--";
        public string VndbFastStddev { get; set; } = "--";
        public int VndbFastVotes { get; set; } = 0;

        public string VndbTotalMedian { get; set; } = "--";
        public string VndbTotalAverage { get; set; } = "--";
        public string VndbTotalStddev { get; set; } = "--";
        public int VndbTotalVotes { get; set; } = 0;

        /// <summary>
        /// When refreshing from the API, the collapsed story time in <paramref name="previousCollapsed"/> can be matched
        /// to one of the four VNDB vote rows on <paramref name="freshWithVoteRows"/> to restore speed before <see cref="ApplyVndbSpeedSelection"/>.
        /// </summary>
        public static DataType InferVndbSpeedAfterRefresh(HltbDataUser previousCollapsed, HltbDataUser freshWithVoteRows)
        {
            if (previousCollapsed?.VndbSpeedDataType != null)
            {
                return previousCollapsed.VndbSpeedDataType.Value;
            }

            long t = previousCollapsed?.GameHltbData?.MainStoryClassic ?? 0;
            var h = freshWithVoteRows?.GameHltbData;
            if (h == null || t <= 0)
            {
                return DataType.Average;
            }

            if (h.MainStoryClassic == t)
            {
                return DataType.Classic;
            }
            if (h.MainStoryAverage == t)
            {
                return DataType.Average;
            }
            if (h.MainStoryMedian == t)
            {
                return DataType.Median;
            }
            if (h.MainStoryRushed == t)
            {
                return DataType.Rushed;
            }

            return DataType.Average;
        }

        /// <summary>
        /// Copies the VNDB row matching <paramref name="selectedType"/> into <see cref="HltbData.MainStoryClassic"/> and clears
        /// other main-story variant fields so display stays independent of global HLTB data-type toggles.
        /// </summary>
        public void ApplyVndbSpeedSelection(DataType selectedType)
        {
            VndbSpeedDataType = selectedType;
            if (GameHltbData == null)
            {
                return;
            }

            long selectedValue;
            switch (selectedType)
            {
                case DataType.Average:
                    selectedValue = GameHltbData.MainStoryAverage != 0
                        ? GameHltbData.MainStoryAverage
                        : GameHltbData.MainStoryClassic;
                    break;
                case DataType.Median:
                    selectedValue = GameHltbData.MainStoryMedian != 0
                        ? GameHltbData.MainStoryMedian
                        : GameHltbData.MainStoryClassic;
                    break;
                case DataType.Rushed:
                    selectedValue = GameHltbData.MainStoryRushed != 0
                        ? GameHltbData.MainStoryRushed
                        : GameHltbData.MainStoryClassic;
                    break;
                case DataType.Classic:
                default:
                    selectedValue = GameHltbData.MainStoryClassic;
                    break;
            }

            GameHltbData.MainStoryClassic = selectedValue;
            GameHltbData.MainStoryAverage = 0;
            GameHltbData.MainStoryMedian = 0;
            GameHltbData.MainStoryRushed = 0;
            GameHltbData.MainStoryLeisure = 0;
        }
    }
}
