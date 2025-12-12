using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using HowLongToBeat.Models;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HowLongToBeat.Services
{
    public partial class HowLongToBeatSearch : SearchContext
    {
        private HowLongToBeatDatabase PluginDatabase { get; set; } = HowLongToBeat.PluginDatabase;


        public HowLongToBeatSearch()
        {
            Description = ResourceProvider.GetString("LOCHowLongToBeatSearchDescription");
            Label = PluginDatabase.PluginName;
            Hint = ResourceProvider.GetString("LOCHowLongToBeatSearchHint");
            Delay = 500;
        }

        public override IEnumerable<SearchItem> GetSearchResults(GetSearchResultsArgs args)
        {
            List<SearchItem> searchItems = new List<SearchItem>();

            try
            {
                // Parameters
                bool hasNp = false;
                bool hasFav = false;
                bool hasTime = false;
                string paramsTime = string.Empty;
                List<string> stores = new List<string>();
                List<string> status = new List<string>();

                foreach (var x in args.SearchTerm.Split(' '))
                {
                    if (x.Contains("-time=", StringComparison.InvariantCultureIgnoreCase))
                    {
                        hasTime = true;
                        paramsTime = x.Replace("-time=", string.Empty);
                    }

                    if (!hasNp)
                    {
                        hasNp = x.IsEqual("-np");
                    }

                    if (!hasFav)
                    {
                        hasFav = x.IsEqual("-fav");
                    }

                    if (x.Contains("-stores=", StringComparison.InvariantCultureIgnoreCase))
                    {
                        stores = x.Replace("-stores=", string.Empty, StringComparison.InvariantCultureIgnoreCase).Split(',').ToList();
                    }

                    if (x.Contains("-status=", StringComparison.InvariantCultureIgnoreCase))
                    {
                        status = x.Replace("-status=", string.Empty, StringComparison.InvariantCultureIgnoreCase).Split(',').ToList();
                    }
                }

                string SearchTerm = Regex.Replace(args.SearchTerm, @"-stores=(\w*,)*\w*", string.Empty, RegexOptions.IgnoreCase).Trim();
                SearchTerm = _regexStatus.Replace(SearchTerm, string.Empty).Trim();
                SearchTerm = _regexPercent.Replace(SearchTerm, string.Empty).Trim();
                SearchTerm = _regexTime.Replace(SearchTerm, string.Empty).Trim();
                SearchTerm = _regexGeneric.Replace(SearchTerm, string.Empty).Trim();


                // Search
                PluginDatabase.Database.Where(x => x.Name.Contains(SearchTerm, StringComparison.InvariantCultureIgnoreCase)
                                && !x.IsDeleted
                                && (args.GameFilterSettings.Uninstalled || x.IsInstalled)
                                && (args.GameFilterSettings.Hidden || !x.Hidden)
                                && (!hasNp || x.Playtime == 0)
                                && (!hasFav || x.Favorite)
                                && (!hasTime || SearchByTime(x, paramsTime))
                                && (stores.Count == 0 || stores.Any(y => x.Source?.Name?.Contains(y, StringComparison.InvariantCultureIgnoreCase) ?? false))
                                && (status.Count == 0 || status.Any(y => x.Game?.CompletionStatus?.Name?.Contains(y, StringComparison.InvariantCultureIgnoreCase) ?? false))
                                )
                    .ForEach(x =>
                    {
                        bool isOK = true;

                        if (isOK)
                        {
                            searchItems.Add(new GameSearchItem(x.Game, ResourceProvider.GetString("LOCGameSearchItemActionSwitchTo"), () => API.Instance.MainView.SelectGame(x.Game.Id)));
                        }
                    });

                if (args.CancelToken.IsCancellationRequested)
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return null;
            }

            return searchItems;
        }


        public bool SearchByTime(GameHowLongToBeat x, string query)
        {
            if (query.Contains("<>"))
            {
                string[] data = query.Replace("<>", "#").Split('#');
                if (data.Length == 2)
                {
                    double timeMin = Tools.GetElapsedSeconde(data[0]);
                    double timeMax = Tools.GetElapsedSeconde(data[1]);

                    if (timeMin > -1 && timeMax > -1)
                    {
                        return (x.Items[0]?.GameHltbData?.TimeToBeat) != 0 && x.Items[0]?.GameHltbData?.TimeToBeat >= timeMin && x.Items[0]?.GameHltbData?.TimeToBeat <= timeMax;
                    }
                }
            }
            else if (query.Contains('<'))
            {
                double time = Tools.GetElapsedSeconde(query.Replace("<", string.Empty));

                if (time > -1)
                {
                    return (x.Items[0]?.GameHltbData?.TimeToBeat) != 0 && x.Items[0]?.GameHltbData?.TimeToBeat <= time;
                }
            }
            else if (query.Contains('>'))
            {
                double time = Tools.GetElapsedSeconde(query.Replace(">", string.Empty));

                if (time > -1)
                {
                    return (x.Items[0]?.GameHltbData?.TimeToBeat) != 0 && x.Items[0]?.GameHltbData?.TimeToBeat >= time;
                }
            }
            else
            {
                double time = Tools.GetElapsedSeconde(query);

                if (time > -1)
                {
                    return (x.Items[0]?.GameHltbData?.TimeToBeat) != 0 && x.Items[0]?.GameHltbData?.TimeToBeat == time;
                }
            }

            return false;
        }

        private static readonly Regex _regexStatus = new Regex(@"-status=(\w*,)*\w*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex _regexPercent = new Regex(@"-percent=(<|>|\w*<>)*\w*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex _regexTime = new Regex(@"-time=(<|>|\w*<>)*\w*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex _regexGeneric = new Regex(@"-\w*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}
