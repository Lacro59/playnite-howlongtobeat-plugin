using CommonPluginsShared;
using CommonPluginsShared.Converters;
using CommonPluginsShared.Extensions;
using HowLongToBeat.Services;
using Playnite.SDK;
using QuickSearch.SearchItems;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HowLongToBeat.Models
{
    public class QuickSearchItemSource : ISearchSubItemSource<string>
    {
        private static HowLongToBeatDatabase PluginDatabase => HowLongToBeat.PluginDatabase;


        public string Prefix => PluginDatabase.PluginName;

        public bool DisplayAllIfQueryIsEmpty => true;

        public string Icon => Path.Combine(PluginDatabase.Paths.PluginPath, "Resources", "command-line.png");


        public IEnumerable<ISearchItem<string>> GetItems()
        {
            return null;
        }

        public IEnumerable<ISearchItem<string>> GetItems(string query)
        {
            if (query.IsEqual("ttb"))
            {
                return new List<ISearchItem<string>>
                {
                    new CommandItem("<", new List<CommandAction>(), "example: ttb < 30 s", Icon),
                    new CommandItem("<>", new List<CommandAction>(), "example: ttb 30 min <> 1 h", Icon),
                    new CommandItem(">", new List<CommandAction>(), "example: ttb > 2 h", Icon),

                    new CommandItem("-np (not played) (optional)", new List<CommandAction>(), "example: ttb > 2 h -np", Icon),
                }.AsEnumerable();
            }

            return new List<ISearchItem<string>>
            {
                new CommandItem("ttb", new List<CommandAction>(), ResourceProvider.GetString("LOCHltbQuickSearchByTTB"), Icon)
            }.AsEnumerable();
        }

        public Task<IEnumerable<ISearchItem<string>>> GetItemsTask(string query, IReadOnlyList<Candidate> addedItems)
        {
            List<string> parameters = GetParameters(query);
            if (parameters.Count > 0)
            {
                switch (parameters[0].ToLower())
                {
                    case "ttb":
                        return SearchByTtb(query);

                    default:
                        break;
                }
            }
            return null;
        }


        private List<string> GetParameters(string query)
        {
            List<string> parameters = query.Split(' ').ToList();
            if (parameters.Count > 1 && parameters[0].IsNullOrEmpty())
            {
                parameters.RemoveAt(0);
            }
            return parameters;
        }

        private CommandItem GetCommandItem(GameHowLongToBeat data, string query)
        {
            DefaultIconConverter defaultIconConverter = new DefaultIconConverter();
            LocalDateTimeConverter localDateTimeConverter = new LocalDateTimeConverter();

            string title = data.Name;
            string TimeToBeat = data.Items[0].GameHltbData.TimeToBeatFormat;
            string icon = defaultIconConverter.Convert(data.Icon, null, null, null).ToString();
            string dateSession = localDateTimeConverter.Convert(data.LastActivity, null, null, CultureInfo.CurrentCulture).ToString();
            string LastSession = data.LastActivity == null ? string.Empty : ResourceProvider.GetString("LOCLastPlayedLabel") + " " + dateSession;

            CommandItem item = new CommandItem(title, () => API.Instance.MainView.SelectGame(data.Id), "", null, icon)
            {
                IconChar = null,
                BottomLeft = PlayniteTools.GetSourceName(data.Id),
                BottomCenter = null,
                BottomRight = ResourceProvider.GetString("LOCHowLongToBeatTimeToBeat") + " " + TimeToBeat,
                TopLeft = title,
                TopRight = LastSession,
                Keys = new List<ISearchKey<string>>() { new CommandItemKey() { Key = query, Weight = 1 } }
            };

            return item;
        }

        private double GetElapsedSeconde(string value, string type)
        {
            switch (type.ToLower())
            {
                case "h":
                    double h = double.Parse(value);
                    return h * 3600;

                case "min":
                    double m = double.Parse(value);
                    return m * 60;

                case "s":
                    return double.Parse(value);

                default:
                    return 0;
            }
        }

        private List<KeyValuePair<Guid, GameHowLongToBeat>> GetDb(ConcurrentDictionary<Guid, GameHowLongToBeat> db)
        {
            return db.Where(x => API.Instance.Database.Games.Get(x.Key) != null && !x.Value.HasDataEmpty).ToList();
        }


        private Task<IEnumerable<ISearchItem<string>>> SearchByTtb(string query)
        {
            bool OnlyNp = query.Contains("-np", StringComparison.OrdinalIgnoreCase);
            query = query.Replace("-np", string.Empty).Trim();

            List<string> parameters = GetParameters(query);
            List<KeyValuePair<Guid, GameHowLongToBeat>> db = GetDb(PluginDatabase.Database.Items).Where(x => x.Value.Items[0].GameHltbData.TimeToBeat != 0).ToList();

            if (OnlyNp)
            {
                db = db.Where(x => x.Value.LastActivity == null).ToList();
            }

            if (parameters.Count == 4)
            {
                return Task.Run(() =>
                {
                    List<ISearchItem<string>> search = new List<ISearchItem<string>>();
                    switch (parameters[1])
                    {
                        case ">":
                            try
                            {
                                double s = GetElapsedSeconde(parameters[2], parameters[3]);
                                foreach (KeyValuePair<Guid, GameHowLongToBeat> data in db)
                                {
                                    if (data.Value.Items[0].GameHltbData.TimeToBeat >= s)
                                    {
                                        search.Add(GetCommandItem(data.Value, query));
                                    }
                                }
                            }
                            catch { }
                            break;

                        case "<":
                            try
                            {
                                double s = GetElapsedSeconde(parameters[2], parameters[3]);
                                foreach (KeyValuePair<Guid, GameHowLongToBeat> data in db)
                                {
                                    if (data.Value.Items[0].GameHltbData.TimeToBeat <= s)
                                    {
                                        search.Add(GetCommandItem(data.Value, query));
                                    }
                                }
                            }
                            catch { }
                            break;

                        default:
                            break;
                    }

                    return search.AsEnumerable();
                });
            }

            if (parameters.Count == 6)
            {
                return Task.Run(() =>
                {
                    List<ISearchItem<string>> search = new List<ISearchItem<string>>();
                    switch (parameters[3])
                    {
                        case "<>":
                            try
                            {
                                double sMin = GetElapsedSeconde(parameters[1], parameters[2]);
                                double sMax = GetElapsedSeconde(parameters[4], parameters[5]);
                                foreach (KeyValuePair<Guid, GameHowLongToBeat> data in db)
                                {
                                    if (data.Value.Items[0].GameHltbData.TimeToBeat >= sMin && data.Value.Items[0].GameHltbData.TimeToBeat <= sMax)
                                    {
                                        search.Add(GetCommandItem(data.Value, query));
                                    }
                                }
                            }
                            catch { }
                            break;

                        default:
                            break;
                    }

                    return search.AsEnumerable();
                });
            }

            return null;
        }
    }
}
