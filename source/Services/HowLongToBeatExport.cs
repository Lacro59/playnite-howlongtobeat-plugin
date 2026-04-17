using CommonPluginsShared.Plugins;
using HowLongToBeat.Models;
using Playnite.SDK;
using System.Collections.Generic;

namespace HowLongToBeat.Services
{
    public class HowLongToBeatExport : PluginExportCsv<GameHowLongToBeat>
    {
        protected override Dictionary<string, string> GetHeader()
        {
            return new Dictionary<string, string>
            {
                { "GameName", ResourceProvider.GetString("LOCGameNameTitle") },
                { "Source", ResourceProvider.GetString("LOCSourceLabel") },
                { "HltbName", ResourceProvider.GetString("LOCHowLongToBeatTitle") },
                { "HltbId", ResourceProvider.GetString("LOCHowLongToBeatId") },
                { "MainStory", ResourceProvider.GetString("LOCHowLongToBeatMainStory") },
                { "MainExtra", ResourceProvider.GetString("LOCHowLongToBeatMainExtra") },
                { "Completionist", ResourceProvider.GetString("LOCHowLongToBeatCompletionist") },
                { "Solo", ResourceProvider.GetString("LOCHowLongToBeatSolo") },
                { "CoOp", ResourceProvider.GetString("LOCHowLongToBeatCoOp") },
                { "Vs", ResourceProvider.GetString("LOCHowLongToBeatVs") },
                { "TimeToBeat", ResourceProvider.GetString("LOCHowLongToBeatTimeToBeat") },
                { "LastRefresh", ResourceProvider.GetString("LOCCommonLastRefresh") }
            };
        }

        protected override IEnumerable<Dictionary<string, string>> GetRows(GameHowLongToBeat item)
        {
            List<Dictionary<string, string>> rows = new List<Dictionary<string, string>>();
            HltbDataUser data = item?.GetData();

            rows.Add(new Dictionary<string, string>
            {
                { "GameName", item?.Game?.Name ?? string.Empty },
                { "Source", item?.Game?.Source?.Name ?? string.Empty },
                { "HltbName", data?.Name ?? string.Empty },
                { "HltbId", data?.Id ?? string.Empty },
                { "MainStory", data?.GameHltbData?.MainStoryFormat ?? "--" },
                { "MainExtra", data?.GameHltbData?.MainExtraFormat ?? "--" },
                { "Completionist", data?.GameHltbData?.CompletionistFormat ?? "--" },
                { "Solo", data?.GameHltbData?.SoloFormat ?? "--" },
                { "CoOp", data?.GameHltbData?.CoOpFormat ?? "--" },
                { "Vs", data?.GameHltbData?.VsFormat ?? "--" },
                { "TimeToBeat", data?.GameHltbData?.TimeToBeatFormat ?? "--" },
                { "LastRefresh", FormatCsvUtcDateTime(item?.DateLastRefresh) }
            });

            return rows;
        }
    }
}
