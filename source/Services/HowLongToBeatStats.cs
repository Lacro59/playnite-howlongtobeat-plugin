using HowLongToBeat.Models;
using HowLongToBeat.Models.Enumerations;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HowLongToBeat.Services
{
    public class HowLongToBeatStats
    {
        private static ILogger Logger => LogManager.GetLogger();
        private static HowLongToBeatDatabase PluginDatabase => HowLongToBeat.PluginDatabase;


        public static double GetAvgGameByMonth()
        {
            double result = 0;

            Dictionary<string, int> DataByMonth = new Dictionary<string, int>();
            foreach (TitleList titleList in PluginDatabase.UserHltbData.TitlesList)
            {
                string Month = titleList.Completion?.ToString("yyyy-MM");
                if (!Month.IsNullOrEmpty())
                {
                    if (DataByMonth.TryGetValue(Month, out int value))
                    {
                        DataByMonth[Month]++;
                    }
                    else
                    {
                        DataByMonth.Add(Month, 1);
                    }
                }
            }

            if (DataByMonth.Count > 0)
            {
                foreach (KeyValuePair<string, int> data in DataByMonth)
                {
                    result += data.Value;
                }
                result /= DataByMonth.Count;
            }

            return result;
        }

        public static long GetAvgTimeByGame()
        {
            long result = 0;
            double count = 0;

            foreach (TitleList titleList in PluginDatabase.UserHltbData.TitlesList)
            {
                if (titleList.Completion != null && titleList.HltbUserData.TimeToBeat != 0)
                {
                    count++;
                    result += titleList.HltbUserData.TimeToBeat;
                }
            }

            if (count > 0)
            {
                result = (long)(result / count);
            }

            return result;
        }

        public static int GetCountGameBeatenBeforeTime()
        {
            return PluginDatabase.UserHltbData.TitlesList
                .Where(x => x.HltbUserData.TimeToBeat != 0 && x.Completion != null
                            && PluginDatabase.Get(x.GameId, true)?.GetData()?.GameHltbData?.TimeToBeat > x.HltbUserData?.TimeToBeat).Count();
        }

        public static int GetCountGameBeatenAfterTime()
        {
            return PluginDatabase.UserHltbData.TitlesList
                .Where(x => x.HltbUserData.TimeToBeat != 0 && x.Completion != null
                        && PluginDatabase.Get(x.GameId, true)?.GetData()?.GameHltbData?.TimeToBeat <= x.HltbUserData?.TimeToBeat).Count();
        }

        public static int GetCountGameBeatenReplays()
        {
            return GetCountMarkedAsReplay();
        }

        public static int GetCountGameRetired()
        {
            return PluginDatabase.UserHltbData.TitlesList.Where(x => x.IsRetired).Count();
        }

        /// <summary>
        /// Returns how many user titles belong to the given HowLongToBeat profile list.
        /// </summary>
        public static int GetCountByHltbListStatus(StatusType statusType)
        {
            if (PluginDatabase.UserHltbData?.TitlesList == null)
            {
                return 0;
            }

            return PluginDatabase.UserHltbData.TitlesList.Count(x => x.HasHltbListStatus(statusType));
        }

        /// <summary>
        /// Returns how many user titles have the "Mark as Replay" flag (played before).
        /// </summary>
        public static int GetCountMarkedAsReplay()
        {
            if (PluginDatabase.UserHltbData?.TitlesList == null)
            {
                return 0;
            }

            return PluginDatabase.UserHltbData.TitlesList.Count(x => x.IsReplay);
        }

        /// <summary>
        /// Returns how many user titles have the "DLC / Expansions Included" optional tag.
        /// </summary>
        public static int GetCountIncludesDlc()
        {
            if (PluginDatabase.UserHltbData?.TitlesList == null)
            {
                return 0;
            }

            return PluginDatabase.UserHltbData.TitlesList.Count(x => x.IsIncludesDlc);
        }
    }
}
