using CommonPluginsShared.Converters;
using CommonPluginsShared.Plugins;
using HowLongToBeat.Models;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace HowLongToBeat.Services
{
    public class HowLongToBeatExport : PluginExportCsv<GameHowLongToBeat>
    {
        private static HowLongToBeatDatabase PluginDatabase => HowLongToBeat.PluginDatabase;

        private static readonly LocalDateConverter DateConverter = new LocalDateConverter();
        private static readonly PlayTimeToStringConverterWithZero PlayTimeConverter = new PlayTimeToStringConverterWithZero();

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
                { "LastRefresh", ResourceProvider.GetString("LOCCommonLastRefresh") },
                { "UserStartDate", ResourceProvider.GetString("LOCCommonStartDate") },
                { "UserLastUpdate", ResourceProvider.GetString("LOCCommonLastUpdate") },
                { "UserPlatform", ResourceProvider.GetString("LOCPlatformTitle") },
                { "UserStorefront", ResourceProvider.GetString("LOCHowLongToBeatStorefront") },
                { "UserCompletion", ResourceProvider.GetString("LOCHowLongToBeatCompleted") },
                { "UserCurrentTime", ResourceProvider.GetString("LOCTimePlayed") },
                { "UserTimeToBeat", ResourceProvider.GetString("LOCHowLongToBeatCsvUserTimeToBeat") },
                { "UserRemainingTime", ResourceProvider.GetString("LOCHltbRemainingTime") },
                { "UserMainStory", ResourceProvider.GetString("LOCHowLongToBeatCsvUserMainStory") },
                { "UserMainExtra", ResourceProvider.GetString("LOCHowLongToBeatCsvUserMainExtra") },
                { "UserCompletionist", ResourceProvider.GetString("LOCHowLongToBeatCsvUserCompletionist") },
                { "UserSolo", ResourceProvider.GetString("LOCHowLongToBeatCsvUserSolo") },
                { "UserCoOp", ResourceProvider.GetString("LOCHowLongToBeatCsvUserCoOp") },
                { "UserVs", ResourceProvider.GetString("LOCHowLongToBeatCsvUserVs") }
            };
        }

        protected override IEnumerable<Dictionary<string, string>> GetRows(GameHowLongToBeat item)
        {
            HltbDataUser data = item?.GetData();
            TitleList userData = ResolveUserData(item, data);

            List<Dictionary<string, string>> rows = new List<Dictionary<string, string>>();
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
                { "LastRefresh", FormatCsvUtcDateTime(item?.DateLastRefresh) },
                { "UserStartDate", FormatCsvLocalDate(userData?.StartDate) },
                { "UserLastUpdate", FormatCsvLocalDate(userData?.LastUpdate) },
                { "UserPlatform", userData?.Platform ?? string.Empty },
                { "UserStorefront", userData?.Storefront ?? string.Empty },
                { "UserCompletion", FormatCsvLocalDate(userData?.Completion) },
                { "UserCurrentTime", FormatCsvPlayTime(userData?.CurrentTime ?? 0) },
                { "UserTimeToBeat", FormatCsvPlayTime(userData?.TimeToBeat ?? 0) },
                { "UserRemainingTime", userData?.RemainingTimeFormat ?? string.Empty },
                { "UserMainStory", userData?.HltbUserData?.MainStoryFormat ?? "--" },
                { "UserMainExtra", userData?.HltbUserData?.MainExtraFormat ?? "--" },
                { "UserCompletionist", userData?.HltbUserData?.CompletionistFormat ?? "--" },
                { "UserSolo", userData?.HltbUserData?.SoloFormat ?? "--" },
                { "UserCoOp", userData?.HltbUserData?.CoOpFormat ?? "--" },
                { "UserVs", userData?.HltbUserData?.VsFormat ?? "--" }
            });

            return rows;
        }

        private static TitleList ResolveUserData(GameHowLongToBeat item, HltbDataUser data)
        {
            if (PluginDatabase == null || data == null || string.IsNullOrEmpty(data.Id))
            {
                return null;
            }

            return PluginDatabase.GetUserHltbDataCurrent(data.Id, item?.UserGameId);
        }

        private static string FormatCsvLocalDate(DateTime? value)
        {
            if (!value.HasValue || value.Value == default(DateTime))
            {
                return string.Empty;
            }

            return FormatCsvLocalDate(value.Value);
        }

        private static string FormatCsvLocalDate(DateTime value)
        {
            if (value == default(DateTime))
            {
                return string.Empty;
            }

            object converted = DateConverter.Convert(value, null, null, CultureInfo.CurrentCulture);
            return converted?.ToString() ?? string.Empty;
        }

        private static string FormatCsvPlayTime(long seconds)
        {
            if (seconds <= 0)
            {
                return string.Empty;
            }

            object converted = PlayTimeConverter.Convert(seconds, null, null, CultureInfo.CurrentCulture);
            return converted?.ToString() ?? string.Empty;
        }
    }
}
