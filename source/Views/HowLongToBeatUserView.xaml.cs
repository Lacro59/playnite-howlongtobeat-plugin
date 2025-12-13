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

            if (!PluginDatabase.PluginSettings.Settings.EnableProgressBarInDataView)
            {
                GridView lvView = (GridView)ListViewDataGames.View;
                lvView.Columns.RemoveAt(lvView.Columns.Count - 1);
                lvView.Columns.RemoveAt(lvView.Columns.Count - 1);
            }

            if (PluginDatabase.Database.UserHltbData?.TitlesList?.Count != 0)
            {
                if (PluginDatabase.Database.UserHltbData?.TitlesList != null)
                {
                    UserViewDataContext.ItemsSource = PluginDatabase.Database.UserHltbData.TitlesList.ToObservable();

                    string SortingDefaultDataName = string.Empty;
                    switch (PluginDatabase.PluginSettings.Settings.TitleListSort)
                    {
                        case TitleListSort.GameName:
                            SortingDefaultDataName = "GameName";
                            break;
                        case TitleListSort.Platform:
                            SortingDefaultDataName = "Platform";
                            break;
                        case TitleListSort.Completion:
                            SortingDefaultDataName = "Completion";
                            break;
                        case TitleListSort.LastUpdate:
                            SortingDefaultDataName = "LastUpdate";
                            break;
                        case TitleListSort.CurrentTime:
                            SortingDefaultDataName = "CurrentTime";
                            break;
                        default:
                            break;
                    }
                    ListViewGames.SortingDefaultDataName = SortingDefaultDataName;
                    ListViewGames.SortingSortDirection = PluginDatabase.PluginSettings.Settings.IsAsc ? ListSortDirection.Ascending : ListSortDirection.Descending;
                    ListViewGames.Sorting();

                    SetFilter();
                }


                PART_UserDataLoad.Visibility = Visibility.Visible;
                PART_Data.Visibility = Visibility.Collapsed;
                StartLoadUserData();
            }
            else
            {
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

            UserViewDataContext.ItemsSource = PluginDatabase.Database.UserHltbData.TitlesList.ToObservable();
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
                if (PluginDatabase.Database.UserHltbData?.TitlesList == null)
                {
                    return;
                }

                try
                {
                    // Default data
                    string[] ChartDataLabels = new string[axis];
                    ChartValues<CustomerForSingle> ChartDataSeries = new ChartValues<CustomerForSingle>();

                    for (int i = axis - 1; i >= 0; i--)
                    {
                        if (cancellationToken.IsCancellationRequested) return;
                        ChartDataLabels[axis - 1 - i] = DateTime.Now.AddYears(-i).ToString("yyyy");
                        ChartDataSeries.Add(new CustomerForSingle
                        {
                            Name = DateTime.Now.AddYears(-i).ToString("yyyy"),
                            Values = 0
                        });
                    }

                    var titles = PluginDatabase.Database.UserHltbData.TitlesList;
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
                                ChartDataSeries[index].Values += 1;
                            }
                        }
                    }

                    var chartSeries = new SeriesCollection
                    {
                        new ColumnSeries
                        {
                            Title = string.Empty,
                            Values = ChartDataSeries
                        }
                    };

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        Application.Current.Dispatcher?.Invoke(() =>
                        {
                            if (cancellationToken.IsCancellationRequested) return;
                            try
                            {
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
                if (PluginDatabase.Database.UserHltbData?.TitlesList == null)
                {
                    return;
                }

                try
                {
                    var dataLabel = PluginDatabase.Database.UserHltbData.TitlesList
                        .Where(x => x.GameStatuses.Where(y => y.Status == StatusType.Completed).Count() > 0)
                        .GroupBy(x => x.Storefront)
                        .Select(x => new { Storefront = x.Key.IsNullOrEmpty() ? "Playnite" : x.Key, Count = x.Count() })
                        .OrderBy(x => x.Storefront)
                        .ToList();

                    string[] ChartDataLabels = new string[dataLabel.Count];
                    ChartValues<CustomerForSingle> ChartDataSeries = new ChartValues<CustomerForSingle>();

                    for (int i = 0; i < dataLabel.Count; i++)
                    {
                        if (cancellationToken.IsCancellationRequested) return;
                        ChartDataLabels[i] = dataLabel[i].Storefront;
                        ChartDataSeries.Add(new CustomerForSingle
                        {
                            Name = dataLabel[i].Storefront,
                            Values = dataLabel[i].Count
                        });
                    }

                    var chartSeries = new SeriesCollection();
                    chartSeries.Add(new ColumnSeries
                    {
                        Title = string.Empty,
                        Values = ChartDataSeries
                    });

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        Application.Current.Dispatcher?.Invoke(() =>
                        {
                            if (cancellationToken.IsCancellationRequested) return;
                            try
                            {
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
                if (PluginDatabase.Database.UserHltbData?.TitlesList == null)
                {
                    return;
                }

                try
                {
                    LocalDateYMConverter localDateYMConverter = new LocalDateYMConverter();

                    // Default data
                    string[] ChartDataLabels = new string[axis];
                    ChartValues<CustomerForSingle> ChartDataSeries = new ChartValues<CustomerForSingle>();

                    for (int i = axis - 1; i >= 0; i--)
                    {
                        if (cancellationToken.IsCancellationRequested) return;
                        ChartDataLabels[axis - 1 - i] = (string)localDateYMConverter.Convert(DateTime.Now.AddMonths(-i), null, null, null);
                        ChartDataSeries.Add(new CustomerForSingle
                        {
                            Name = (string)localDateYMConverter.Convert(DateTime.Now.AddMonths(-i), null, null, null),
                            Values = 0
                        });
                    }

                    var titles = PluginDatabase.Database.UserHltbData.TitlesList;
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
                                ChartDataSeries[index].Values += 1;
                            }
                        }
                    }

                    var chartSeries = new SeriesCollection();
                    chartSeries.Add(new ColumnSeries
                    {
                        Title = string.Empty,
                        Values = ChartDataSeries
                    });

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        Application.Current.Dispatcher?.Invoke(() =>
                        {
                            if (cancellationToken.IsCancellationRequested) return;
                            try
                            {
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

        private Task SetStats(CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                if (cancellationToken.IsCancellationRequested) return;
                if (PluginDatabase.Database.UserHltbData?.TitlesList == null)
                {
                    return;
                }

                try
                {
                    List<TitleList> titleLists = PluginDatabase.Database.UserHltbData.TitlesList;

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
                    var countReplays = HowLongToBeatStats.GetCountGameBeatenReplays().ToString();
                    var countRetired = HowLongToBeatStats.GetCountGameRetired().ToString();

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
                            UserViewDataContext.CountGameRetired = countRetired;
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
                    ListViewDataGames.ItemsSource = PluginDatabase.Database.Where(x => !x.HasDataEmpty && !x.Hidden)
                          .Select(x => new PlayniteData
                          {
                              GameContext = API.Instance.Database.Games.Get(x.Id),
                              ViewProgressBar = PluginDatabase.PluginSettings.Settings.EnableProgressBarInDataView
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
                PART_FilteredGames.IsChecked = PluginDatabase.PluginSettings.Settings.filterSettings.UsedFilteredGames;
                PART_HidePlayedGames.IsChecked = PluginDatabase.PluginSettings.Settings.filterSettings.OnlyNotPlayedGames;

                SetPlayniteData();
                DisplayFirst = false;
            }
        }


        #region Filter
        private void SetFilter()
        {
            // Filter
            List<string> listYear = PluginDatabase.Database.UserHltbData.TitlesList.Select(x => x.Completion?.ToString("yyyy") ?? "----").Distinct().OrderBy(x => x).ToList();
            PART_CbYear.ItemsSource = null;
            PART_CbYear.ItemsSource = listYear;
            PART_CbYear.SelectedIndex = 0;

            List<string> listStoreFront = PluginDatabase.Database.UserHltbData.TitlesList.Where(x => !x.Storefront.IsNullOrEmpty()).Select(y => y.Storefront).Distinct().ToList();
            listStoreFront.AddMissing("----");
            listStoreFront = listStoreFront.OrderBy(x => x).ToList();
            PART_CbStorefront.ItemsSource = null;
            PART_CbStorefront.ItemsSource = listStoreFront;
            PART_CbStorefront.SelectedIndex = 0;

            List<string> listPlatform = PluginDatabase.Database.UserHltbData.TitlesList.Where(x => !x.Platform.IsNullOrEmpty()).Select(y => y.Platform).Distinct().ToList();
            listPlatform.AddMissing("----");
            listPlatform = listPlatform.OrderBy(x => x).ToList();
            PART_CbPlatform.ItemsSource = null;
            PART_CbPlatform.ItemsSource = listPlatform;
            PART_CbPlatform.SelectedIndex = 0;

            // Saved settings
            int index = listYear.FindIndex(x => x == PluginDatabase.PluginSettings.Settings.filterSettings.Year);
            PART_CbYear.SelectedIndex = index == -1 ? 0 : index;

            index = listStoreFront.FindIndex(x => x == PluginDatabase.PluginSettings.Settings.filterSettings.Storefront);
            PART_CbStorefront.SelectedIndex = index == -1 ? 0 : index;

            index = listPlatform.FindIndex(x => x == PluginDatabase.PluginSettings.Settings.filterSettings.Platform);
            PART_CbPlatform.SelectedIndex = index == -1 ? 0 : index;

            PART_Replays.IsChecked = PluginDatabase.PluginSettings.Settings.filterSettings.OnlyReplays;
            PART_OnlyNotPlayed.IsChecked = PluginDatabase.PluginSettings.Settings.filterSettings.OnlyNotPlayed;
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
                UserViewDataContext.ItemsSource = PluginDatabase.Database.UserHltbData.TitlesList.ToObservable();
            }
            // StoreFront only
            else if ((Year.IsNullOrEmpty() || Year.IsEqual("----")) && (Platform.IsNullOrEmpty() || Platform.IsEqual("----")))
            {
                UserViewDataContext.ItemsSource = PluginDatabase.Database.UserHltbData.TitlesList
                    .Where(x => x.Storefront != null && x.Storefront.IsEqual(StoreFront)).ToObservable();
            }
            // Year only
            else if ((StoreFront.IsNullOrEmpty() || StoreFront.IsEqual("----")) && (Platform.IsNullOrEmpty() || Platform.IsEqual("----")))
            {
                UserViewDataContext.ItemsSource = PluginDatabase.Database.UserHltbData.TitlesList
                    .Where(x => x.Completion != null && ((DateTime)x.Completion).ToString("yyyy").IsEqual(Year)).ToObservable();
            }
            // Platform only
            else if ((Year.IsNullOrEmpty() || Year.IsEqual("----")) && (StoreFront.IsNullOrEmpty() || StoreFront.IsEqual("----")))
            {
                UserViewDataContext.ItemsSource = PluginDatabase.Database.UserHltbData.TitlesList
                    .Where(x => x.Platform != null && x.Platform.IsEqual(Platform)).ToObservable();
            }
            // StoreFront missing
            else if (StoreFront.IsNullOrEmpty() || StoreFront.IsEqual("----"))
            {
                UserViewDataContext.ItemsSource = PluginDatabase.Database.UserHltbData.TitlesList
                    .Where(x => x.Completion != null && ((DateTime)x.Completion).ToString("yyyy").IsEqual(Year) && x.Platform != null && x.Platform.IsEqual(Platform)).ToObservable();
            }
            // Year missing
            else if (Year.IsNullOrEmpty() || Year.IsEqual("----"))
            {
                UserViewDataContext.ItemsSource = PluginDatabase.Database.UserHltbData.TitlesList
                    .Where(x => x.Storefront != null && x.Storefront.IsEqual(StoreFront) && x.Platform != null && x.Platform.IsEqual(Platform)).ToObservable();
            }
            // Platform missing
            else if (Platform.IsNullOrEmpty() || Platform.IsEqual("----"))
            {
                UserViewDataContext.ItemsSource = PluginDatabase.Database.UserHltbData.TitlesList
                    .Where(x => x.Completion != null && ((DateTime)x.Completion).ToString("yyyy").IsEqual(Year) && x.Storefront != null && x.Storefront.IsEqual(StoreFront)).ToObservable();
            }
            else
            {
                UserViewDataContext.ItemsSource = PluginDatabase.Database.UserHltbData.TitlesList
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

            if ((bool)PART_OnlyNotPlayed.IsChecked)
            {
                UserViewDataContext.ItemsSource = UserViewDataContext.ItemsSource.Where(x => x.CurrentTime == 0).ToObservable();
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
            PART_NameSearch.Text = string.Empty;
            PART_CbYear.SelectedIndex = 0;
            PART_CbStorefront.SelectedIndex = 0;
            PART_CbPlatform.SelectedIndex = 0;
            PART_Replays.IsChecked = false;
            PART_OnlyNotPlayed.IsChecked = false;
        }
        private void SavedFilter1_Click(object sender, RoutedEventArgs e)
        {
            PluginDatabase.PluginSettings.Settings.filterSettings.Year = PART_CbYear.SelectedItem.ToString();
            PluginDatabase.PluginSettings.Settings.filterSettings.Storefront = PART_CbStorefront.SelectedItem.ToString();
            PluginDatabase.PluginSettings.Settings.filterSettings.Platform = PART_CbPlatform.SelectedItem.ToString();
            PluginDatabase.PluginSettings.Settings.filterSettings.OnlyReplays = (bool)PART_Replays.IsChecked;
            PluginDatabase.PluginSettings.Settings.filterSettings.OnlyNotPlayed = (bool)PART_OnlyNotPlayed.IsChecked;

            Plugin.SavePluginSettings(PluginDatabase.PluginSettings.Settings);
        }

        private void SavedFilter2_Click(object sender, RoutedEventArgs e)
        {
            PluginDatabase.PluginSettings.Settings.filterSettings.UsedFilteredGames = (bool)PART_FilteredGames.IsChecked;
            PluginDatabase.PluginSettings.Settings.filterSettings.OnlyNotPlayedGames = (bool)PART_HidePlayedGames.IsChecked;

            Plugin.SavePluginSettings(PluginDatabase.PluginSettings.Settings);
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

        public RelayCommand<Guid> GoToGame => Commands.GoToGame;

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

        private string countGameRetired = "--";
        public string CountGameRetired { get => countGameRetired; set => SetValue(ref countGameRetired, value); }
    }
}
