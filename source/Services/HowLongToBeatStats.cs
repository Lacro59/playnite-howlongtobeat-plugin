using HowLongToBeat.Models;
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
            foreach (TitleList titleList in PluginDatabase.Database.UserHltbData.TitlesList)
            {
                string Month = titleList.Completion?.ToString("yyyy-MM");
                if (!Month.IsNullOrEmpty())
                {
                    if (DataByMonth.ContainsKey(Month))
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

            foreach (TitleList titleList in PluginDatabase.Database.UserHltbData.TitlesList)
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
            return PluginDatabase.Database.UserHltbData.TitlesList
                .Where(x => x.HltbUserData.TimeToBeat != 0 && x.Completion != null
                            && PluginDatabase.Get(x.GameId, true)?.GetData()?.GameHltbData?.TimeToBeat > x.HltbUserData?.TimeToBeat).Count();
        }

        public static int GetCountGameBeatenAfterTime()
        {
            return PluginDatabase.Database.UserHltbData.TitlesList
                .Where(x => x.HltbUserData.TimeToBeat != 0 && x.Completion != null
                        && PluginDatabase.Get(x.GameId, true)?.GetData()?.GameHltbData?.TimeToBeat <= x.HltbUserData?.TimeToBeat).Count();
        }

        public static int GetCountGameBeatenReplays()
        {
            return PluginDatabase.Database.UserHltbData.TitlesList.Where(x => x.IsReplay).Count();
        }

        public static int GetCountGameRetired()
        {
            return PluginDatabase.Database.UserHltbData.TitlesList.Where(x => x.IsRetired).Count();
        }
    }
}
