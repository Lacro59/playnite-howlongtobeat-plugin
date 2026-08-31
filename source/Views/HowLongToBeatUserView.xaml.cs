using CommonPluginsControls.LiveChartsCommon;
using CommonPluginsShared.Converters;
using HowLongToBeat.Models;
using HowLongToBeat.Services;
using LiveCharts;
using LiveCharts.Wpf;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Playnite.SDK.Models;
using System.Threading.Tasks;
using System.Threading;
using CommonPluginsShared.Extensions;
using System.Collections.ObjectModel;
using CommonPluginsShared;
using System.Windows.Data;
using HowLongToBeat.Models.Enumerations;
using System.Windows.Media;
using CommonPluginsShared.Commands;

namespace HowLongToBeat.Views
{

    public partial class HowLongToBeatUserView : UserControl
    {
        private CancellationTokenSource _loadCts;
        private Task _loadTask;
        private HowLongToBeat Plugin { get; set; }
        private bool DisplayFirst { get; set; } = true;

        private static HowLongToBeatDatabase PluginDatabase => HowLongToBeat.PluginDatabase;
        private UserViewDataContext UserViewDataContext { get; set; } = new UserViewDataContext();

        private bool PlayniteDataFilter(object item)
        {
            return (!(bool)PART_FilteredGames.IsChecked || API.Instance.MainView.FilteredGames.Find(y => y.Id == (item as PlayniteData).GameContext.Id) != null)
                && (!(bool)PART_HidePlayedGames.IsChecked || (item as PlayniteData).Playtime == 0);
        }

        private void ApplyThemeResources()
        {
            try
            {
                Brush accent = ResourceProvider.GetResource("AccentColorBrush") as Brush;
                if (accent == null)
                {
                    accent = ResourceProvider.GetResource("NormalBrush") as Brush ?? new SolidColorBrush(Colors.DarkCyan);
                }

                this.Resources["ChartAccentBrush"] = accent;

                Brush controlFg = ResourceProvider.GetResource("ControlForegroundBrush") as Brush;
                if (controlFg == null)
                {
                    controlFg = ResourceProvider.GetResource("NormalForeground") as Brush ?? Brushes.White;
                }

                this.Resources["PrimaryButtonForegroundBrush"] = controlFg ?? Brushes.White;
            }
            catch (Exception ex)
            {
                try { Common.LogError(ex, false, false, PluginDatabase.PluginName); } catch { }
            }
        }

        public HowLongToBeatUserView(HowLongToBeat plugin)
        {
            Plugin = plugin;

            InitializeComponent();
            DataContext = UserViewDataContext;

            ApplyThemeResources();

            this.Unloaded += (s, e) =>
            {
                DisposeCts();
            };

            if (!PluginDatabase.PluginSettings.EnableProgressBarInDataView)
            {
                GridView lvView = (GridView)ListViewDataGames.View;
                lvView.Columns.RemoveAt(lvView.Columns.Count - 1);
                lvView.Columns.RemoveAt(lvView.Columns.Count - 1);
            }

            if (PluginDatabase.UserHltbData?.TitlesList?.Count != 0)
            {
                if (PluginDatabase.UserHltbData?.TitlesList != null)
                {
                    UserViewDataContext.ItemsSource = PluginDatabase.UserHltbData.TitlesList.ToObservable();
                    ApplyTitleListSortFromFilterSettings();

                    SetFilter();
                }


                PART_UserDataLoad.Visibility = Visibility.Visible;
                PART_Data.Visibility = Visibility.Collapsed;
                StartLoadUserData();
            }
            else
            {
                // Restore Playnite Data filters without waiting for LoadUserDataAsync / IsVisibleChanged
                // (those paths only run when TitlesList is non-empty).
                ApplyPlayniteDataFiltersFromSettings(PluginDatabase.PluginSettings.filterSettings);
                SetPlayniteData();
                PART_UserData.Visibility = Visibility.Collapsed;
                PART_TabControl.SelectedIndex = 1;
            }
        }


        private void PART_BtRefreshUserData_Click(object sender, RoutedEventArgs e)
        {
            PART_Data.Visibility = Visibility.Hidden;

            PluginDatabase.RefreshUserData();

            PART_CbYear.SelectedIndex = 0;

            UserViewDataContext.ItemsSource = PluginDatabase.UserHltbData.TitlesList.ToObservable();
            ListViewGames.Sorting();

            SetFilter();

            StartLoadUserData();
        }

        private void StartLoadUserData()
        {
            try
            {
                DisposeCts();
            }
            catch { }
            _loadCts = new CancellationTokenSource();
            _loadTask = LoadUserDataAsync(_loadCts.Token);
        }

        private async Task LoadUserDataAsync(CancellationToken cancellationToken)
        {
            try
            {
                var tasks = new List<Task>
                {
                    SetChartDataStore(cancellationToken),
                    SetChartDataYear(4, cancellationToken),
                    SetChartData(cancellationToken: cancellationToken),
                    SetChartDataHltbLists(cancellationToken),
                    SetStats(cancellationToken)
                };

                await Task.WhenAll(tasks).ConfigureAwait(false);

                if (cancellationToken.IsCancellationRequested) return;

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    try
                    {
                        PART_UserDataLoad.Visibility = Visibility.Collapsed;
                        PART_Data.Visibility = Visibility.Visible;
                        // Ensure the handler is not attached multiple times
                        try { PART_LvDataContener.IsVisibleChanged -= PART_PlayniteData_IsVisibleChanged; } catch { }
                        PART_LvDataContener.IsVisibleChanged += PART_PlayniteData_IsVisibleChanged;
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, false, PluginDatabase.PluginName);
                    }
                });
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Common.LogError(ex, false, false, PluginDatabase.PluginName);
            }
        }


        private Task SetChartDataYear(int axis = 4, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                if (cancellationToken.IsCancellationRequested) return;
                if (PluginDatabase.UserHltbData?.TitlesList == null)
                {
                    return;
                }

                try
                {
                    // Default data
                    string[] ChartDataLabels = new string[axis];
                    var seriesItems = new List<CustomerForSingle>(axis);

                    for (int i = axis - 1; i >= 0; i--)
                    {
                        if (cancellationToken.IsCancellationRequested) return;
                        ChartDataLabels[axis - 1 - i] = DateTime.Now.AddYears(-i).ToString("yyyy");
                        seriesItems.Add(new CustomerForSingle
                        {
                            Name = DateTime.Now.AddYears(-i).ToString("yyyy"),
                            Values = 0
                        });
                    }

                    var titles = PluginDatabase.UserHltbData.TitlesList;
                    for (int t = 0; t < titles.Count; t++)
                    {
                        if (cancellationToken.IsCancellationRequested) return;
                        var titleList = titles[t];
                        if (titleList?.Completion != null)
                        {
                            string tempDateTime = ((DateTime)titleList.Completion).ToString("yyyy");
                            int index = Array.IndexOf(ChartDataLabels, tempDateTime);
                            if (index >= 0)
                            {
                                seriesItems[index].Values += 1;
                            }
                        }
                    }

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        Application.Current.Dispatcher?.Invoke(() =>
                        {
                            if (cancellationToken.IsCancellationRequested) return;
                            try
                            {
                                // LiveCharts WPF series must be created on STA/UI thread.
                                var chartValues = new ChartValues<CustomerForSingle>();
                                for (int i = 0; i < seriesItems.Count; i++)
                                {
                                    chartValues.Add(seriesItems[i]);
                                }

                                var chartSeries = new SeriesCollection
                                {
                                    new ColumnSeries
                                    {
                                        Title = string.Empty,
                                        Values = chartValues
                                    }
                                };

                                UserViewDataContext.ChartUserDataYear_Series = chartSeries;
                                UserViewDataContext.ChartUserDataYearLabelsX_Labels = ChartDataLabels;
                            }
                            catch (Exception ex)
                            {
                                Common.LogError(ex, false, false, PluginDatabase.PluginName);
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, false, PluginDatabase.PluginName);
                }
            }, cancellationToken);
        }

        private Task SetChartDataStore(CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                if (cancellationToken.IsCancellationRequested) return;
                if (PluginDatabase.UserHltbData?.TitlesList == null)
                {
                    return;
                }

                try
                {
                    var dataLabel = PluginDatabase.UserHltbData.TitlesList
                        .Where(x => x.GameStatuses.Where(y => y.Status == StatusType.Completed).Count() > 0)
                        .GroupBy(x => x.Storefront)
                        .Select(x => new { Storefront = x.Key.IsNullOrEmpty() ? "Playnite" : x.Key, Count = x.Count() })
                        .OrderBy(x => x.Storefront)
                        .ToList();

                    string[] ChartDataLabels = new string[dataLabel.Count];
                    var seriesItems = new List<CustomerForSingle>(dataLabel.Count);

                    for (int i = 0; i < dataLabel.Count; i++)
                    {
                        if (cancellationToken.IsCancellationRequested) return;
                        ChartDataLabels[i] = dataLabel[i].Storefront;
                        seriesItems.Add(new CustomerForSingle
                        {
                            Name = dataLabel[i].Storefront,
                            Values = dataLabel[i].Count
                        });
                    }

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        Application.Current.Dispatcher?.Invoke(() =>
                        {
                            if (cancellationToken.IsCancellationRequested) return;
                            try
                            {
                                // LiveCharts WPF series must be created on STA/UI thread.
                                var chartValues = new ChartValues<CustomerForSingle>();
                                for (int i = 0; i < seriesItems.Count; i++)
                                {
                                    chartValues.Add(seriesItems[i]);
                                }

                                var chartSeries = new SeriesCollection();
                                chartSeries.Add(new ColumnSeries
                                {
                                    Title = string.Empty,
                                    Values = chartValues
                                });

                                UserViewDataContext.ChartUserDataStore_Series = chartSeries;
                                UserViewDataContext.ChartUserDataStoreLabelsX_Labels = ChartDataLabels;
                            }
                            catch (Exception ex)
                            {
                                Common.LogError(ex, false, false, PluginDatabase.PluginName);
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, false, PluginDatabase.PluginName);
                }
            }, cancellationToken);
        }

        private Task SetChartData(int axis = 16, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                if (cancellationToken.IsCancellationRequested) return;
                if (PluginDatabase.UserHltbData?.TitlesList == null)
                {
                    return;
                }

                try
                {
                    LocalDateYMConverter localDateYMConverter = new LocalDateYMConverter();

                    // Default data
                    string[] ChartDataLabels = new string[axis];
                    var seriesItems = new List<CustomerForSingle>(axis);

                    for (int i = axis - 1; i >= 0; i--)
                    {
                        if (cancellationToken.IsCancellationRequested) return;
                        ChartDataLabels[axis - 1 - i] = (string)localDateYMConverter.Convert(DateTime.Now.AddMonths(-i), null, null, null);
                        seriesItems.Add(new CustomerForSingle
                        {
                            Name = (string)localDateYMConverter.Convert(DateTime.Now.AddMonths(-i), null, null, null),
                            Values = 0
                        });
                    }

                    var titles = PluginDatabase.UserHltbData.TitlesList;
                    for (int t = 0; t < titles.Count; t++)
                    {
                        if (cancellationToken.IsCancellationRequested) return;
                        var titleList = titles[t];
                        if (titleList?.Completion != null)
                        {
                            string tempDateTime = (string)localDateYMConverter.Convert((DateTime)titleList.Completion, null, null, null);
                            int index = Array.IndexOf(ChartDataLabels, tempDateTime);
                            if (index >= 0)
                            {
                                seriesItems[index].Values += 1;
                            }
                        }
                    }

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        Application.Current.Dispatcher?.Invoke(() =>
                        {
                            if (cancellationToken.IsCancellationRequested) return;
                            try
                            {
                                // LiveCharts WPF series must be created on STA/UI thread.
                                var chartValues = new ChartValues<CustomerForSingle>();
                                for (int i = 0; i < seriesItems.Count; i++)
                                {
                                    chartValues.Add(seriesItems[i]);
                                }

                                var chartSeries = new SeriesCollection();
                                chartSeries.Add(new ColumnSeries
                                {
                                    Title = string.Empty,
                                    Values = chartValues
                                });

                                UserViewDataContext.ChartUserData_Series = chartSeries;
                                UserViewDataContext.ChartUserDataLabelsX_Labels = ChartDataLabels;
                            }
                            catch (Exception ex)
                            {
                                Common.LogError(ex, false, false, PluginDatabase.PluginName);
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, false, PluginDatabase.PluginName);
                }
            }, cancellationToken);
        }

        private static SeriesCollection BuildHltbListsPieSeries(IEnumerable<CustomerForSingle> items)
        {
            SeriesCollection series = new SeriesCollection();

            foreach (CustomerForSingle item in items)
            {
                if (item.Values <= 0)
                {
                    continue;
                }

                string sliceTitle = item.Name;
                series.Add(new PieSeries
                {
                    Title = sliceTitle,
                    Values = new ChartValues<double> { item.Values },
                    DataLabels = false,
                    LabelPoint = chartPoint => string.Format("{0}: {1}", sliceTitle, chartPoint.Y)
                });
            }

            return series;
        }

        private Task SetChartDataHltbLists(CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                if (PluginDatabase.UserHltbData?.TitlesList == null)
                {
                    return;
                }

                try
                {
                    int countBacklog = HowLongToBeatStats.GetCountByHltbListStatus(StatusType.Backlog);
                    int countPlaying = HowLongToBeatStats.GetCountByHltbListStatus(StatusType.Playing);
                    int countReplaysList = HowLongToBeatStats.GetCountByHltbListStatus(StatusType.Replays);
                    int countCompleted = HowLongToBeatStats.GetCountByHltbListStatus(StatusType.Completed);
                    int countRetired = HowLongToBeatStats.GetCountByHltbListStatus(StatusType.Retired);
                    int countCustom = HowLongToBeatStats.GetCountByHltbListStatus(StatusType.CustomTab);
                    int countMarkedReplay = HowLongToBeatStats.GetCountMarkedAsReplay();

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        Application.Current.Dispatcher?.Invoke(() =>
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                return;
                            }

                            try
                            {
                                string[] chartLabels = new string[]
                                {
                                    ResourceProvider.GetString("LOCHltbUserListBacklog"),
                                    ResourceProvider.GetString("LOCHltbUserListPlaying"),
                                    ResourceProvider.GetString("LOCHltbUserListReplays"),
                                    ResourceProvider.GetString("LOCHltbUserListCompleted"),
                                    ResourceProvider.GetString("LOCHltbUserListRetired"),
                                    ResourceProvider.GetString("LOCHltbUserListCustom"),
                                    ResourceProvider.GetString("LOCHltbStatsMarkedAsReplayShort")
                                };

                                CustomerForSingle[] chartItems = new CustomerForSingle[]
                                {
                                    new CustomerForSingle { Name = chartLabels[0], Values = countBacklog },
                                    new CustomerForSingle { Name = chartLabels[1], Values = countPlaying },
                                    new CustomerForSingle { Name = chartLabels[2], Values = countReplaysList },
                                    new CustomerForSingle { Name = chartLabels[3], Values = countCompleted },
                                    new CustomerForSingle { Name = chartLabels[4], Values = countRetired },
                                    new CustomerForSingle { Name = chartLabels[5], Values = countCustom },
                                    new CustomerForSingle { Name = chartLabels[6], Values = countMarkedReplay }
                                };

                                UserViewDataContext.ChartHltbLists_Series = BuildHltbListsPieSeries(chartItems);
                            }
                            catch (Exception ex)
                            {
                                Common.LogError(ex, false, false, PluginDatabase.PluginName);
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, false, PluginDatabase.PluginName);
                }
            }, cancellationToken);
        }

        private Task SetStats(CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                if (cancellationToken.IsCancellationRequested) return;
                if (PluginDatabase.UserHltbData?.TitlesList == null)
                {
                    return;
                }

                try
                {
                    List<TitleList> titleLists = PluginDatabase.UserHltbData.TitlesList;

                    if (cancellationToken.IsCancellationRequested) return;
                    var completionsCount = titleLists.Count(x => x.GameStatuses.Any(y => y.Status == StatusType.Completed)).ToString();

                    long timeSinglePlayer = 0;
                    long timeCoOp = 0;
                    long timeVs = 0;

                    foreach (var titleList in titleLists)
                    {
                        if (cancellationToken.IsCancellationRequested) return;
                        if (titleList.HltbUserData.Completionist != 0)
                        {
                            timeSinglePlayer += titleList.HltbUserData.Completionist;
                        }
                        else if (titleList.HltbUserData.MainExtra != 0)
                        {
                            timeSinglePlayer += titleList.HltbUserData.MainExtra;
                        }
                        else if (titleList.HltbUserData.MainStory != 0)
                        {
                            timeSinglePlayer += titleList.HltbUserData.MainStory;
                        }

                        timeCoOp += titleList.HltbUserData.CoOp;
                        timeVs += titleList.HltbUserData.Vs;
                    }

                    PlayTimeToStringConverterWithZero converter = new PlayTimeToStringConverterWithZero();

                    var timeSinglePlayerStr = (string)converter.Convert(timeSinglePlayer, null, null, CultureInfo.CurrentCulture);
                    var timeCoOpStr = (string)converter.Convert(timeCoOp, null, null, CultureInfo.CurrentCulture);
                    var timeVsStr = (string)converter.Convert(timeVs, null, null, CultureInfo.CurrentCulture);

                    var countBefore = HowLongToBeatStats.GetCountGameBeatenBeforeTime().ToString();
                    var countAfter = HowLongToBeatStats.GetCountGameBeatenAfterTime().ToString();
                    var avgGameByMonth = string.Format("{0:0.0}", HowLongToBeatStats.GetAvgGameByMonth()).ToString();
                    var avgTimeByGame = (string)converter.Convert(HowLongToBeatStats.GetAvgTimeByGame(), null, null, CultureInfo.CurrentCulture);
                    var countReplays = HowLongToBeatStats.GetCountMarkedAsReplay().ToString();
                    var countIncludesDlc = HowLongToBeatStats.GetCountIncludesDlc().ToString();
                    var countRetired = HowLongToBeatStats.GetCountGameRetired().ToString();

                    var countHltbListBacklog = HowLongToBeatStats.GetCountByHltbListStatus(StatusType.Backlog).ToString();
                    var countHltbListPlaying = HowLongToBeatStats.GetCountByHltbListStatus(StatusType.Playing).ToString();
                    var countHltbListReplays = HowLongToBeatStats.GetCountByHltbListStatus(StatusType.Replays).ToString();
                    var countHltbListCompleted = HowLongToBeatStats.GetCountByHltbListStatus(StatusType.Completed).ToString();
                    var countHltbListRetired = HowLongToBeatStats.GetCountByHltbListStatus(StatusType.Retired).ToString();
                    var countHltbListCustom = HowLongToBeatStats.GetCountByHltbListStatus(StatusType.CustomTab).ToString();

                    Application.Current.Dispatcher?.Invoke(() =>
                    {
                        if (cancellationToken.IsCancellationRequested) return;
                        try
                        {
                            UserViewDataContext.CompletionsCount = completionsCount;
                            UserViewDataContext.TimeSinglePlayer = timeSinglePlayerStr;
                            UserViewDataContext.TimeCoOp = timeCoOpStr;
                            UserViewDataContext.TimeVs = timeVsStr;

                            UserViewDataContext.CountGameBeatenBeforeTime = countBefore;
                            UserViewDataContext.CountGameBeatenAfterTime = countAfter;
                            UserViewDataContext.AvgGameByMonth = avgGameByMonth;
                            UserViewDataContext.AvgTimeByGame = avgTimeByGame;
                            UserViewDataContext.CountGameBeatenReplays = countReplays;
                            UserViewDataContext.CountIncludesDlc = countIncludesDlc;
                            UserViewDataContext.CountGameRetired = countRetired;

                            UserViewDataContext.CountHltbListBacklog = countHltbListBacklog;
                            UserViewDataContext.CountHltbListPlaying = countHltbListPlaying;
                            UserViewDataContext.CountHltbListReplays = countHltbListReplays;
                            UserViewDataContext.CountHltbListCompleted = countHltbListCompleted;
                            UserViewDataContext.CountHltbListRetiredList = countHltbListRetired;
                            UserViewDataContext.CountHltbListCustom = countHltbListCustom;
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false, false, PluginDatabase.PluginName);
                        }
                    });
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, false, PluginDatabase.PluginName);
                }
            }, cancellationToken);
        }

        private void SetPlayniteData()
        {
            try
            {
                //PART_DataLoad.Visibility = Visibility.Visible;
                //PART_LvDataContener.Visibility = Visibility.Collapsed;

                if (ListViewDataGames.ItemsSource == null)
                {
                    ListViewDataGames.ItemsSource = PluginDatabase.GetAllCache().Where(x => !x.HasDataEmpty && !x.Hidden)
                          .Select(x => new PlayniteData
                          {
                              GameContext = API.Instance.Database.Games.Get(x.Id),
                              ViewProgressBar = PluginDatabase.PluginSettings.EnableProgressBarInDataView
                          }).ToObservable();
                }

                CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(ListViewDataGames.ItemsSource);
                view.Filter = PlayniteDataFilter;

                CollectionViewSource.GetDefaultView(ListViewDataGames.ItemsSource).Refresh();
                ListViewDataGames.Sorting();
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, false, PluginDatabase.PluginName);
            }

            PART_DataLoad.Visibility = Visibility.Collapsed;
            PART_LvDataContener.Visibility = Visibility.Visible;
        }


        private void PART_PlayniteData_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (((FrameworkElement)sender).Visibility == Visibility.Visible && DisplayFirst)
            {
                PART_FilteredGames.IsChecked = PluginDatabase.PluginSettings.filterSettings.UsedFilteredGames;
                PART_HidePlayedGames.IsChecked = PluginDatabase.PluginSettings.filterSettings.OnlyNotPlayedGames;

                SetPlayniteData();
                DisplayFirst = false;
            }
        }


        #region Filter

        private void InitializeHltbListStatusCombo()
        {
            if (PART_CbHltbListStatus.Items.Count > 0)
            {
                return;
            }

            PART_CbHltbListStatus.Items.Add(CreateHltbListStatusFilterItem(FilterSettings.HltbListStatusAll, "LOCHltbFilterHltbListStatusAll"));
            PART_CbHltbListStatus.Items.Add(CreateHltbListStatusFilterItem(StatusType.Backlog.ToString(), "LOCHltbUserListBacklog"));
            PART_CbHltbListStatus.Items.Add(CreateHltbListStatusFilterItem(StatusType.Playing.ToString(), "LOCHltbUserListPlaying"));
            PART_CbHltbListStatus.Items.Add(CreateHltbListStatusFilterItem(StatusType.Replays.ToString(), "LOCHltbUserListReplays", "LOCHltbUserListReplaysTooltip"));
            PART_CbHltbListStatus.Items.Add(CreateHltbListStatusFilterItem(StatusType.Completed.ToString(), "LOCHltbUserListCompleted"));
            PART_CbHltbListStatus.Items.Add(CreateHltbListStatusFilterItem(StatusType.Retired.ToString(), "LOCHltbUserListRetired"));
            PART_CbHltbListStatus.Items.Add(CreateHltbListStatusFilterItem(StatusType.CustomTab.ToString(), "LOCHltbUserListCustom"));
        }

        private static ComboBoxItem CreateHltbListStatusFilterItem(string tag, string localizationKey, string tooltipLocalizationKey = null)
        {
            ComboBoxItem item = new ComboBoxItem
            {
                Content = ResourceProvider.GetString(localizationKey),
                Tag = tag
            };

            if (!tooltipLocalizationKey.IsNullOrEmpty())
            {
                item.ToolTip = ResourceProvider.GetString(tooltipLocalizationKey);
            }

            return item;
        }

        private void SelectHltbListStatusFilter(string savedToken)
        {
            string token = savedToken.IsNullOrEmpty() ? FilterSettings.HltbListStatusAll : savedToken;
            foreach (ComboBoxItem item in PART_CbHltbListStatus.Items)
            {
                if (item.Tag != null && item.Tag.ToString().IsEqual(token))
                {
                    PART_CbHltbListStatus.SelectedItem = item;
                    return;
                }
            }

            PART_CbHltbListStatus.SelectedIndex = 0;
        }

        private string GetSelectedHltbListStatusFilter()
        {
            if (PART_CbHltbListStatus.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                return item.Tag.ToString();
            }

            return FilterSettings.HltbListStatusAll;
        }

        private static bool MatchesHltbListStatusFilter(TitleList title, string listStatusFilter)
        {
            if (listStatusFilter.IsNullOrEmpty() || listStatusFilter.IsEqual(FilterSettings.HltbListStatusAll))
            {
                return true;
            }

            StatusType statusType;
            if (!Enum.TryParse(listStatusFilter, out statusType))
            {
                return true;
            }

            return title.HasHltbListStatus(statusType);
        }

        private void SetFilter()
        {
            InitializeHltbListStatusCombo();

            // Filter
            List<string> listYear = PluginDatabase.UserHltbData.TitlesList.Select(x => x.Completion?.ToString("yyyy") ?? "----").Distinct().OrderBy(x => x).ToList();
            PART_CbYear.ItemsSource = null;
            PART_CbYear.ItemsSource = listYear;
            PART_CbYear.SelectedIndex = 0;

            List<string> listStoreFront = PluginDatabase.UserHltbData.TitlesList.Where(x => !x.Storefront.IsNullOrEmpty()).Select(y => y.Storefront).Distinct().ToList();
            listStoreFront.AddMissing("----");
            listStoreFront = listStoreFront.OrderBy(x => x).ToList();
            PART_CbStorefront.ItemsSource = null;
            PART_CbStorefront.ItemsSource = listStoreFront;
            PART_CbStorefront.SelectedIndex = 0;

            List<string> listPlatform = PluginDatabase.UserHltbData.TitlesList.Where(x => !x.Platform.IsNullOrEmpty()).Select(y => y.Platform).Distinct().ToList();
            listPlatform.AddMissing("----");
            listPlatform = listPlatform.OrderBy(x => x).ToList();
            PART_CbPlatform.ItemsSource = null;
            PART_CbPlatform.ItemsSource = listPlatform;
            PART_CbPlatform.SelectedIndex = 0;

            ApplyFilterSettingsToUi(PluginDatabase.PluginSettings.filterSettings);
        }

        private void ApplyFilterSettingsToUi(FilterSettings filterSettings)
        {
            if (filterSettings == null)
            {
                return;
            }

            SelectComboBoxValue(PART_CbYear, filterSettings.Year);
            SelectComboBoxValue(PART_CbStorefront, filterSettings.Storefront);
            SelectComboBoxValue(PART_CbPlatform, filterSettings.Platform);

            PART_NameSearch.Text = filterSettings.NameSearch ?? string.Empty;
            PART_Replays.IsChecked = filterSettings.OnlyReplays;
            PART_IncludesDlc.IsChecked = filterSettings.OnlyIncludesDlc;
            PART_OnlyNotPlayed.IsChecked = filterSettings.OnlyNotPlayed;
            SelectHltbListStatusFilter(filterSettings.HltbListStatus);

            ApplyTitleListSort(filterSettings);
            FilterData(PART_NameSearch.Text, PART_CbYear.Text, PART_CbStorefront.Text, PART_CbPlatform.Text);

            ApplyPlayniteDataFiltersFromSettings(filterSettings);
        }

        private static void SelectComboBoxValue(ComboBox comboBox, string value)
        {
            if (comboBox?.ItemsSource == null)
            {
                return;
            }

            int index = 0;
            foreach (object item in comboBox.ItemsSource)
            {
                if (item != null && item.ToString().IsEqual(value))
                {
                    comboBox.SelectedIndex = index;
                    return;
                }

                index++;
            }

            comboBox.SelectedIndex = 0;
        }

        private void ApplyPlayniteDataFiltersFromSettings(FilterSettings filterSettings)
        {
            if (filterSettings == null || PART_FilteredGames == null || PART_HidePlayedGames == null)
            {
                return;
            }

            PART_FilteredGames.IsChecked = filterSettings.UsedFilteredGames;
            PART_HidePlayedGames.IsChecked = filterSettings.OnlyNotPlayedGames;

            if (ListViewDataGames?.ItemsSource == null)
            {
                return;
            }

            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(ListViewDataGames.ItemsSource);
            view?.Refresh();
            ListViewDataGames.Sorting();
        }

        private static string GetSortingDataName(TitleListSort titleListSort)
        {
            switch (titleListSort)
            {
                case TitleListSort.GameName:
                    return "GameName";
                case TitleListSort.Platform:
                    return "Platform";
                case TitleListSort.Completion:
                    return "Completion";
                case TitleListSort.LastUpdate:
                    return "LastUpdate";
                case TitleListSort.CurrentTime:
                    return "CurrentTime";
                default:
                    return "Completion";
            }
        }

        private static TitleListSort GetTitleListSortFromSortingDataName(string sortingDataName)
        {
            if (sortingDataName.IsEqual("GameName"))
            {
                return TitleListSort.GameName;
            }

            if (sortingDataName.IsEqual("Platform"))
            {
                return TitleListSort.Platform;
            }

            if (sortingDataName.IsEqual("Completion"))
            {
                return TitleListSort.Completion;
            }

            if (sortingDataName.IsEqual("LastUpdate"))
            {
                return TitleListSort.LastUpdate;
            }

            if (sortingDataName.IsEqual("CurrentTime"))
            {
                return TitleListSort.CurrentTime;
            }

            return TitleListSort.Completion;
        }

        private void ApplyTitleListSortFromFilterSettings()
        {
            ApplyTitleListSort(PluginDatabase.PluginSettings.filterSettings);
        }

        private void ApplyTitleListSort(FilterSettings filterSettings)
        {
            if (filterSettings == null || ListViewGames == null)
            {
                return;
            }

            ListViewGames.SortingDefaultDataName = GetSortingDataName(filterSettings.TitleListSort);
            ListViewGames.SortingSortDirection = filterSettings.IsAsc ? ListSortDirection.Ascending : ListSortDirection.Descending;
            ListViewGames.ApplyConfiguredSort();
        }

        private void GetCurrentTitleListSort(out TitleListSort sort, out bool isAsc)
        {
            ICollectionView view = ListViewGames.ItemsSource != null
                ? CollectionViewSource.GetDefaultView(ListViewGames.ItemsSource)
                : null;

            if (view != null && view.SortDescriptions.Count > 0)
            {
                SortDescription sortDescription = view.SortDescriptions[0];
                sort = GetTitleListSortFromSortingDataName(sortDescription.PropertyName);
                isAsc = sortDescription.Direction == ListSortDirection.Ascending;
                return;
            }

            sort = GetTitleListSortFromSortingDataName(ListViewGames.SortingDefaultDataName);
            isAsc = ListViewGames.SortingSortDirection == ListSortDirection.Ascending;
        }


        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                FilterData(PART_NameSearch.Text, PART_CbYear.Text, PART_CbStorefront.Text, PART_CbPlatform.Text);
            }
            catch { }
        }

        private void PART_CbYear_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (((ComboBox)sender).SelectedValue != null)
                {
                    string Year = ((ComboBox)sender).SelectedValue.ToString();
                    FilterData(PART_NameSearch.Text, Year, PART_CbStorefront.Text, PART_CbPlatform.Text);
                }
            }
            catch { }
        }

        private void PART_CbStorefront_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (((ComboBox)sender).SelectedValue != null)
                {
                    string StoreFront = ((ComboBox)sender).SelectedValue.ToString();
                    FilterData(PART_NameSearch.Text, PART_CbYear.Text, StoreFront, PART_CbPlatform.Text);
                }
            }
            catch { }
        }

        private void PART_CbPlatform_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (((ComboBox)sender).SelectedValue != null)
                {
                    string Platform = ((ComboBox)sender).SelectedValue.ToString();
                    FilterData(PART_NameSearch.Text, PART_CbYear.Text, PART_CbStorefront.Text, Platform);
                }
            }
            catch { }
        }

        private void PART_CbHltbListStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (PART_CbHltbListStatus.SelectedItem != null)
                {
                    FilterData(PART_NameSearch.Text, PART_CbYear.Text, PART_CbStorefront.Text, PART_CbPlatform.Text);
                }
            }
            catch { }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                FilterData(PART_NameSearch.Text, PART_CbYear.Text, PART_CbStorefront.Text, PART_CbPlatform.Text);
            }
            catch { }
        }


        private void FilterData(string Name, string Year, string StoreFront, string Platform)
        {
            // nothing
            if ((Year.IsNullOrEmpty() || Year.IsEqual("----")) && (StoreFront.IsNullOrEmpty() || StoreFront.IsEqual("----")) && (Platform.IsNullOrEmpty() || Platform.IsEqual("----")))
            {
                UserViewDataContext.ItemsSource = PluginDatabase.UserHltbData.TitlesList.ToObservable();
            }
            // StoreFront only
            else if ((Year.IsNullOrEmpty() || Year.IsEqual("----")) && (Platform.IsNullOrEmpty() || Platform.IsEqual("----")))
            {
                UserViewDataContext.ItemsSource = PluginDatabase.UserHltbData.TitlesList
                    .Where(x => x.Storefront != null && x.Storefront.IsEqual(StoreFront)).ToObservable();
            }
            // Year only
            else if ((StoreFront.IsNullOrEmpty() || StoreFront.IsEqual("----")) && (Platform.IsNullOrEmpty() || Platform.IsEqual("----")))
            {
                UserViewDataContext.ItemsSource = PluginDatabase.UserHltbData.TitlesList
                    .Where(x => x.Completion != null && ((DateTime)x.Completion).ToString("yyyy").IsEqual(Year)).ToObservable();
            }
            // Platform only
            else if ((Year.IsNullOrEmpty() || Year.IsEqual("----")) && (StoreFront.IsNullOrEmpty() || StoreFront.IsEqual("----")))
            {
                UserViewDataContext.ItemsSource = PluginDatabase.UserHltbData.TitlesList
                    .Where(x => x.Platform != null && x.Platform.IsEqual(Platform)).ToObservable();
            }
            // StoreFront missing
            else if (StoreFront.IsNullOrEmpty() || StoreFront.IsEqual("----"))
            {
                UserViewDataContext.ItemsSource = PluginDatabase.UserHltbData.TitlesList
                    .Where(x => x.Completion != null && ((DateTime)x.Completion).ToString("yyyy").IsEqual(Year) && x.Platform != null && x.Platform.IsEqual(Platform)).ToObservable();
            }
            // Year missing
            else if (Year.IsNullOrEmpty() || Year.IsEqual("----"))
            {
                UserViewDataContext.ItemsSource = PluginDatabase.UserHltbData.TitlesList
                    .Where(x => x.Storefront != null && x.Storefront.IsEqual(StoreFront) && x.Platform != null && x.Platform.IsEqual(Platform)).ToObservable();
            }
            // Platform missing
            else if (Platform.IsNullOrEmpty() || Platform.IsEqual("----"))
            {
                UserViewDataContext.ItemsSource = PluginDatabase.UserHltbData.TitlesList
                    .Where(x => x.Completion != null && ((DateTime)x.Completion).ToString("yyyy").IsEqual(Year) && x.Storefront != null && x.Storefront.IsEqual(StoreFront)).ToObservable();
            }
            else
            {
                UserViewDataContext.ItemsSource = PluginDatabase.UserHltbData.TitlesList
                    .Where(x => x.Completion != null && ((DateTime)x.Completion).ToString("yyyy").IsEqual(Year) && x.Storefront != null && x.Storefront.IsEqual(StoreFront) && x.Platform != null && x.Platform.IsEqual(Platform))
                    .ToObservable();
            }

            if (!Name.IsNullOrEmpty())
            {
                UserViewDataContext.ItemsSource = UserViewDataContext.ItemsSource.Where(x => x.GameName.Contains(Name, StringComparison.InvariantCultureIgnoreCase)).ToObservable();
            }

            if ((bool)PART_Replays.IsChecked)
            {
                UserViewDataContext.ItemsSource = UserViewDataContext.ItemsSource.Where(x => x.IsReplay).ToObservable();
            }

            if ((bool)PART_IncludesDlc.IsChecked)
            {
                UserViewDataContext.ItemsSource = UserViewDataContext.ItemsSource.Where(x => x.IsIncludesDlc).ToObservable();
            }

            if ((bool)PART_OnlyNotPlayed.IsChecked)
            {
                UserViewDataContext.ItemsSource = UserViewDataContext.ItemsSource.Where(x => x.CurrentTime == 0).ToObservable();
            }

            string hltbListStatus = GetSelectedHltbListStatusFilter();
            if (!hltbListStatus.IsEqual(FilterSettings.HltbListStatusAll))
            {
                UserViewDataContext.ItemsSource = UserViewDataContext.ItemsSource
                    .Where(x => MatchesHltbListStatusFilter(x, hltbListStatus))
                    .ToObservable();
            }

            ListViewGames.Sorting();
        }

        #endregion


        private void PART_ExpandChartYear_Click(object sender, RoutedEventArgs e)
        {
            Button bt = sender as Button;
            switch (bt.Tag.ToString())
            {
                case "0":
                    _ = SetChartDataYear(12);
                    PART_ChartUserData.Visibility = Visibility.Collapsed;
                    Grid.SetColumnSpan(PART_ChartUserDataYear, 3);
                    Grid.SetColumnSpan(PART_ExpandChartYear, 3);
                    bt.Content = "\ue9b0";
                    bt.Tag = "1";
                    break;

                case "1":
                    _ = SetChartDataYear(4);
                    PART_ChartUserData.Visibility = Visibility.Visible;
                    Grid.SetColumnSpan(PART_ChartUserDataYear, 1);
                    Grid.SetColumnSpan(PART_ExpandChartYear, 1);
                    bt.Content = "\ue9a8";
                    bt.Tag = "0";
                    break;

                default:
                    break;
            }
        }



        private void PART_FilteredGames_Click(object sender, RoutedEventArgs e)
        {
            SetPlayniteData();
        }

        private void PART_HidePlayedGames_Click(object sender, RoutedEventArgs e)
        {
            SetPlayniteData();
        }


        private void ClearFilter1_Click(object sender, RoutedEventArgs e)
        {
            FilterSettings filterSettings = PluginDatabase.PluginSettings.filterSettings;
            filterSettings.ResetToDefaults();
            ApplyFilterSettingsToUi(filterSettings);
        }
        private void SavedFilter1_Click(object sender, RoutedEventArgs e)
        {
            SaveFilterSettings();
        }

        private void SaveFilterSettings()
        {
            FilterSettings filterSettings = PluginDatabase.PluginSettings.filterSettings;

            filterSettings.NameSearch = PART_NameSearch.Text ?? string.Empty;
            filterSettings.Year = PART_CbYear.SelectedItem?.ToString() ?? "----";
            filterSettings.Storefront = PART_CbStorefront.SelectedItem?.ToString() ?? "----";
            filterSettings.Platform = PART_CbPlatform.SelectedItem?.ToString() ?? "----";
            filterSettings.HltbListStatus = GetSelectedHltbListStatusFilter();
            filterSettings.OnlyReplays = PART_Replays.IsChecked == true;
            filterSettings.OnlyIncludesDlc = PART_IncludesDlc.IsChecked == true;
            filterSettings.OnlyNotPlayed = PART_OnlyNotPlayed.IsChecked == true;

            TitleListSort sort;
            bool isAsc;
            GetCurrentTitleListSort(out sort, out isAsc);
            filterSettings.TitleListSort = sort;
            filterSettings.IsAsc = isAsc;

            filterSettings.UsedFilteredGames = PART_FilteredGames.IsChecked == true;
            filterSettings.OnlyNotPlayedGames = PART_HidePlayedGames.IsChecked == true;
            filterSettings.LegacySortMigrated = true;

            Plugin.SavePluginSettings(PluginDatabase.PluginSettings);
        }


        private void PART_TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Part_Found != null)
            {
                Part_Found.Visibility = PART_TabControl.SelectedIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void DisposeCts()
        {
            try { _loadCts?.Cancel(); } catch { }
            try { _loadCts?.Dispose(); } catch { }
            _loadCts = null;
            _loadTask = null;
        }
    }


    public class PlayniteData : ObservableObject
    {
        private HowLongToBeatDatabase PluginDatabase { get; set; } = HowLongToBeat.PluginDatabase;

        private PlayTimeToStringConverterWithZero PlayTimeToStringConverterWithZero { get; set; } = new PlayTimeToStringConverterWithZero();

        public Game GameContext { get; set; }
        public bool ViewProgressBar { get; set; }

        public string GameName => GameContext.Name;
        public string Icon => GameContext.Icon;
        public Guid GameId => GameContext.Id;
        public string Source => PlayniteTools.GetSourceName(GameContext);
        public string CompletionStatus => GameContext.CompletionStatus?.Name ?? string.Empty;
        public ulong Playtime => GameContext.Playtime;
        public long TimeToBeat => PluginDatabase.Get(GameId, true)?.GetData()?.GameHltbData?.TimeToBeat ?? 0;
        public long RemainingTime => (PluginDatabase.Get(GameId, true)?.GetData()?.GameHltbData?.TimeToBeat ?? 0) - (long)Playtime > 0 ? PluginDatabase.Get(GameId, true).GetData().GameHltbData.TimeToBeat - (long)Playtime : 0;
        public string RemainingTimeFormat => RemainingTime > 0 ? (string)PlayTimeToStringConverterWithZero.Convert(RemainingTime, null, null, CultureInfo.CurrentCulture) : string.Empty;

        public RelayCommand<Guid> GoToGame => CommandsNavigation.GoToGame;

        public bool GameExist => API.Instance.Database.Games.Get(GameId) != null;
    }


    public class UserViewDataContext : ObservableObject
    {
        private ObservableCollection<TitleList> itemsSource = new ObservableCollection<TitleList>();
        public ObservableCollection<TitleList> ItemsSource { get => itemsSource; set => SetValue(ref itemsSource, value); }


        private SeriesCollection chartUserDataStore_Series = new SeriesCollection();
        public SeriesCollection ChartUserDataStore_Series { get => chartUserDataStore_Series; set => SetValue(ref chartUserDataStore_Series, value); }

        private string[] chartUserDataStoreLabelsX_Labels = new string[0];
        public string[] ChartUserDataStoreLabelsX_Labels { get => chartUserDataStoreLabelsX_Labels; set => SetValue(ref chartUserDataStoreLabelsX_Labels, value); }


        private SeriesCollection chartUserDataYear_Series = new SeriesCollection();
        public SeriesCollection ChartUserDataYear_Series { get => chartUserDataYear_Series; set => SetValue(ref chartUserDataYear_Series, value); }

        private string[] chartUserDataYearLabelsX_Labels = new string[0];
        public string[] ChartUserDataYearLabelsX_Labels { get => chartUserDataYearLabelsX_Labels; set => SetValue(ref chartUserDataYearLabelsX_Labels, value); }


        private SeriesCollection chartUserData_Series = new SeriesCollection();
        public SeriesCollection ChartUserData_Series { get => chartUserData_Series; set => SetValue(ref chartUserData_Series, value); }

        private string[] chartUserDataLabelsX_Labels = new string[0];
        public string[] ChartUserDataLabelsX_Labels { get => chartUserDataLabelsX_Labels; set => SetValue(ref chartUserDataLabelsX_Labels, value); }


        private SeriesCollection chartHltbLists_Series = new SeriesCollection();
        public SeriesCollection ChartHltbLists_Series { get => chartHltbLists_Series; set => SetValue(ref chartHltbLists_Series, value); }

        private string[] chartHltbListsLabelsX_Labels = new string[0];
        public string[] ChartHltbListsLabelsX_Labels { get => chartHltbListsLabelsX_Labels; set => SetValue(ref chartHltbListsLabelsX_Labels, value); }


        private string completionsCount = "--";
        public string CompletionsCount { get => completionsCount; set => SetValue(ref completionsCount, value); }


        private string timeSinglePlayer = "--";
        public string TimeSinglePlayer { get => timeSinglePlayer; set => SetValue(ref timeSinglePlayer, value); }

        private string timeCoOp = "--";
        public string TimeCoOp { get => timeCoOp; set => SetValue(ref timeCoOp, value); }

        private string timeVs = "--";
        public string TimeVs { get => timeVs; set => SetValue(ref timeVs, value); }


        private string countGameBeatenBeforeTime = "--";
        public string CountGameBeatenBeforeTime { get => countGameBeatenBeforeTime; set => SetValue(ref countGameBeatenBeforeTime, value); }

        private string countGameBeatenAfterTime = "--";
        public string CountGameBeatenAfterTime { get => countGameBeatenAfterTime; set => SetValue(ref countGameBeatenAfterTime, value); }

        private string avgGameByMonth = "--";
        public string AvgGameByMonth { get => avgGameByMonth; set => SetValue(ref avgGameByMonth, value); }

        private string avgTimeByGame = "--";
        public string AvgTimeByGame { get => avgTimeByGame; set => SetValue(ref avgTimeByGame, value); }

        private string countGameBeatenReplays = "--";
        public string CountGameBeatenReplays { get => countGameBeatenReplays; set => SetValue(ref countGameBeatenReplays, value); }

        private string countIncludesDlc = "--";
        public string CountIncludesDlc { get => countIncludesDlc; set => SetValue(ref countIncludesDlc, value); }

        private string countGameRetired = "--";
        public string CountGameRetired { get => countGameRetired; set => SetValue(ref countGameRetired, value); }

        private string countHltbListBacklog = "--";
        public string CountHltbListBacklog { get => countHltbListBacklog; set => SetValue(ref countHltbListBacklog, value); }

        private string countHltbListPlaying = "--";
        public string CountHltbListPlaying { get => countHltbListPlaying; set => SetValue(ref countHltbListPlaying, value); }

        private string countHltbListReplays = "--";
        public string CountHltbListReplays { get => countHltbListReplays; set => SetValue(ref countHltbListReplays, value); }

        private string countHltbListCompleted = "--";
        public string CountHltbListCompleted { get => countHltbListCompleted; set => SetValue(ref countHltbListCompleted, value); }

        private string countHltbListRetiredList = "--";
        public string CountHltbListRetiredList { get => countHltbListRetiredList; set => SetValue(ref countHltbListRetiredList, value); }

        private string countHltbListCustom = "--";
        public string CountHltbListCustom { get => countHltbListCustom; set => SetValue(ref countHltbListCustom, value); }
    }
}
