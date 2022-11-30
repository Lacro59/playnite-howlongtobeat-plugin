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
    public class HowLongToBeatSearch : SearchContext
    {
        private static IResourceProvider resources = new ResourceProvider();
        private readonly HowLongToBeatDatabase PluginDatabase = HowLongToBeat.PluginDatabase;


        public HowLongToBeatSearch()
        {
            Description = resources.GetString("LOCHowLongToBeatSearchDescription");
            Label = PluginDatabase.PluginName;
            Hint = resources.GetString("LOCHowLongToBeatSearchHint");
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

                args.SearchTerm.Split(' ').ForEach(x =>
                {
                    if (x.Contains("-time=", StringComparison.InvariantCultureIgnoreCase))
                    {
                        hasTime = true;
                        paramsTime = x.Replace("-time=", string.Empty);
                    }

                    if (!hasNp) hasNp = x.IsEqual("-np");
                    if (!hasFav) hasFav = x.IsEqual("-fav");

                    if (x.Contains("-stores=", StringComparison.InvariantCultureIgnoreCase))
                    {
                        stores = x.Replace("-stores=", string.Empty, StringComparison.InvariantCultureIgnoreCase).Split(',').ToList();
                    }

                    if (x.Contains("-status=", StringComparison.InvariantCultureIgnoreCase))
                    {
                        status = x.Replace("-status=", string.Empty, StringComparison.InvariantCultureIgnoreCase).Split(',').ToList();
                    }
                });

                string SearchTerm = Regex.Replace(args.SearchTerm, @"-stores=(\w*,)*\w*", string.Empty, RegexOptions.IgnoreCase).Trim();
                SearchTerm = Regex.Replace(SearchTerm, @"-status=(\w*,)*\w*", string.Empty, RegexOptions.IgnoreCase).Trim();
                SearchTerm = Regex.Replace(SearchTerm, @"-percent=(<|>|\w*<>)*\w*", string.Empty, RegexOptions.IgnoreCase).Trim();
                SearchTerm = Regex.Replace(SearchTerm, @"-time=(<|>|\w*<>)*\w*", string.Empty, RegexOptions.IgnoreCase).Trim();
                SearchTerm = Regex.Replace(SearchTerm, @"-\w*", string.Empty, RegexOptions.IgnoreCase).Trim();


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
                            searchItems.Add(new GameSearchItem(x.Game, resources.GetString("LOCGameSearchItemActionSwitchTo"), () => API.Instance.MainView.SelectGame(x.Game.Id)));
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
                if (data.Count() == 2)
                {
                    double timeMin = Tools.GetElapsedSeconde(data[0]);
                    double timeMax = Tools.GetElapsedSeconde(data[1]);

                    if (timeMin > -1 && timeMax > -1)
                    {
                        return x.Items[0]?.GameHltbData?.TimeToBeat == 0 ? false : x.Items[0]?.GameHltbData?.TimeToBeat >= timeMin && x.Items[0]?.GameHltbData?.TimeToBeat <= timeMax;
                    }
                }
            }
            else if (query.Contains("<"))
            {
                double time = Tools.GetElapsedSeconde(query.Replace("<", string.Empty));

                if (time > -1)
                {
                    return x.Items[0]?.GameHltbData?.TimeToBeat == 0 ? false : x.Items[0]?.GameHltbData?.TimeToBeat <= time;
                }
            }
            else if (query.Contains(">"))
            {
                double time = Tools.GetElapsedSeconde(query.Replace(">", string.Empty));

                if (time > -1)
                {
                    return x.Items[0]?.GameHltbData?.TimeToBeat == 0 ? false : x.Items[0]?.GameHltbData?.TimeToBeat >= time;
                }
            }
            else
            {
                double time = Tools.GetElapsedSeconde(query);

                if (time > -1)
                {
                    return x.Items[0]?.GameHltbData?.TimeToBeat == 0 ? false : x.Items[0]?.GameHltbData?.TimeToBeat == time;
                }
            }

            return false;
        }
    }
}
