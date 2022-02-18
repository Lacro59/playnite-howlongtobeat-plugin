using CommonPluginsControls.LiveChartsCommon;
using CommonPluginsShared.Converters;
using HowLongToBeat.Models;
using HowLongToBeat.Services;
using LiveCharts;
using LiveCharts.Configurations;
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
using System.Windows.Threading;
using CommonPluginsShared.Extensions;
using System.Collections.ObjectModel;

namespace HowLongToBeat.Views
{
    /// <summary>
    /// Logique d'interaction pour HowLongToBeatUserView.xaml
    /// </summary>
    // TODO Optimize loading
    public partial class HowLongToBeatUserView : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        private bool DisplayFirst = true;

        private HowLongToBeatDatabase PluginDatabase = HowLongToBeat.PluginDatabase;
        private UserViewDataContext userViewDataContext = new UserViewDataContext();


        public HowLongToBeatUserView()
        {
            InitializeComponent();
            this.DataContext = userViewDataContext;

            if (PluginDatabase.Database.UserHltbData?.TitlesList?.Count != 0)
            {
                if (PluginDatabase.Database.UserHltbData?.TitlesList != null)
                {
                    userViewDataContext.ItemsSource = PluginDatabase.Database.UserHltbData.TitlesList.ToObservable();

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
                    };
                    ListViewGames.SortingDefaultDataName = SortingDefaultDataName;
                    ListViewGames.SortingSortDirection = (PluginDatabase.PluginSettings.Settings.IsAsc) ? ListSortDirection.Ascending : ListSortDirection.Descending;
                    ListViewGames.Sorting();


                    SetFilter();
                }

                //let create a mapper so LiveCharts know how to plot our CustomerViewModel class
                var customerVmMapper = Mappers.Xy<CustomerForSingle>()
                    .X((value, index) => index)
                    .Y(value => value.Values);

                //lets save the mapper globally
                Charting.For<CustomerForSingle>(customerVmMapper);

                SetChartDataStore();
                SetChartDataYear();
                SetChartData();
                SetStats();


                PART_PlayniteData.IsVisibleChanged += PART_PlayniteData_IsVisibleChanged;
            }
            else
            {
                DisplayPlayniteDataLoader();
                SetPlayniteData();
                PART_UserData.Visibility = Visibility.Collapsed;
                PART_TabControl.SelectedIndex = 1;
            }
        }


        private void PART_BtRefreshUserData_Click(object sender, RoutedEventArgs e)
        {
            PART_ChartUserDataYear.Series = null;
            PART_ChartUserDataYearLabelsX.Labels = null;
            PART_ChartUserData.Series = null;
            PART_ChartUserDataLabelsX.Labels = null;

            PART_CompletionsCount.Content = string.Empty;
            PART_TimeSinglePlayer.Content = string.Empty;
            PART_TimeCoOp.Content = string.Empty;
            PART_TimeVs.Content = string.Empty;


            PluginDatabase.RefreshUserData();

            PART_CbYear.SelectedIndex = 0;

            userViewDataContext.ItemsSource = PluginDatabase.Database.UserHltbData.TitlesList.ToObservable();
            ListViewGames.Sorting();


            SetChartDataStore();
            SetChartDataYear();
            SetChartData();
            SetStats();
            SetFilter();
        }


        private void SetChartDataYear()
        {
            if (PluginDatabase.Database.UserHltbData?.TitlesList != null)
            {
                // Default data
                string[] ChartDataLabels = new string[4];
                ChartValues<CustomerForSingle> ChartDataSeries = new ChartValues<CustomerForSingle>();

                for (int i = 3; i >= 0; i--)
                {
                    ChartDataLabels[(3 - i)] = DateTime.Now.AddYears(-i).ToString("yyyy");
                    ChartDataSeries.Add(new CustomerForSingle
                    {
                        Name = DateTime.Now.AddYears(-i).ToString("yyyy"),
                        Values = 0
                    });
                }


                // Set data
                foreach (TitleList titleList in PluginDatabase.Database.UserHltbData.TitlesList)
                {
                    if (titleList?.Completion != null)
                    {
                        string tempDateTime = ((DateTime)titleList.Completion).ToString("yyyy");
                        int index = Array.IndexOf(ChartDataLabels, tempDateTime);
                        if (index > 0)
                        {
                            ChartDataSeries[index].Values += 1;
                        }
                    }
                }


                // Create chart
                SeriesCollection ChartSeriesCollection = new SeriesCollection();
                ChartSeriesCollection.Add(new ColumnSeries
                {
                    Title = string.Empty,
                    Values = ChartDataSeries
                });


                PART_ChartUserDataYear.Series = ChartSeriesCollection;
                PART_ChartUserDataYearLabelsX.Labels = ChartDataLabels;
            }
        }

        private void SetChartDataStore()
        {
            if (PluginDatabase.Database.UserHltbData?.TitlesList != null)
            {
                var dataLabel = PluginDatabase.Database.UserHltbData.TitlesList
                    .Where(x => x.GameStatuses.Where(y => y.Status == StatusType.Completed).Count() > 0)
                    .GroupBy(x => x.Storefront)
                    .Select(x => new { Storefront = (x.Key.IsNullOrEmpty()) ? "Playnite" : x.Key, Count = x.Count() })
                    .OrderBy(x => x.Storefront)
                    .ToList();

                string[] ChartDataLabels = new string[dataLabel.Count];
                ChartValues<CustomerForSingle> ChartDataSeries = new ChartValues<CustomerForSingle>();

                for (int i = 0; i < dataLabel.Count; i++)
                {
                    ChartDataLabels[i] = dataLabel[i].Storefront;
                    ChartDataSeries.Add(new CustomerForSingle
                    {
                        Name = dataLabel[i].Storefront,
                        Values = dataLabel[i].Count
                    });
                }

                // Create chart
                SeriesCollection ChartSeriesCollection = new SeriesCollection();
                ChartSeriesCollection.Add(new ColumnSeries
                {
                    Title = string.Empty,
                    Values = ChartDataSeries
                });


                PART_ChartUserDataStore.Series = ChartSeriesCollection;
                PART_ChartUserDataStoreLabelsX.Labels = ChartDataLabels;
            }
        }

        private void SetChartData()
        {
            if (PluginDatabase.Database.UserHltbData?.TitlesList != null)
            {
                LocalDateYMConverter localDateYMConverter = new LocalDateYMConverter();


                // Default data
                string[] ChartDataLabels = new string[16];
                ChartValues<CustomerForSingle> ChartDataSeries = new ChartValues<CustomerForSingle>();


                for (int i = 15; i >= 0; i--)
                {
                    ChartDataLabels[(15 - i)] = (string)localDateYMConverter.Convert(DateTime.Now.AddMonths(-i), null, null, null);
                    ChartDataSeries.Add(new CustomerForSingle
                    {
                        Name = (string)localDateYMConverter.Convert(DateTime.Now.AddMonths(-i), null, null, null),
                        Values = 0
                    });
                }


                // Set data
                if (PluginDatabase.Database.UserHltbData?.TitlesList != null)
                {
                    foreach (TitleList titleList in PluginDatabase.Database.UserHltbData.TitlesList)
                    {
                        if (titleList?.Completion != null)
                        {
                            string tempDateTime = (string)localDateYMConverter.Convert((DateTime)titleList.Completion, null, null, null);
                            int index = Array.IndexOf(ChartDataLabels, tempDateTime);
                            if (index > 0)
                            {
                                ChartDataSeries[index].Values += 1;
                            }
                        }
                    }
                }


                // Create chart
                SeriesCollection ChartSeriesCollection = new SeriesCollection();
                ChartSeriesCollection.Add(new LineSeries
                {
                    Title = string.Empty,
                    Values = ChartDataSeries
                });


                PART_ChartUserData.Series = ChartSeriesCollection;
                PART_ChartUserDataLabelsX.Labels = ChartDataLabels;
            }
        }

        private void SetStats()
        {
            if (PluginDatabase.Database.UserHltbData?.TitlesList != null)
            {
                List<TitleList> titleLists = PluginDatabase.Database.UserHltbData.TitlesList;

                PART_CompletionsCount.Content = titleLists.Where(x => x.GameStatuses.Where(y => y.Status == StatusType.Completed).Count() > 0).Count();

                long TimeSinglePlayer = 0;
                long TimeCoOp = 0;
                long TimeVs = 0;

                foreach (TitleList titleList in titleLists)
                {
                    if (titleList.HltbUserData.Completionist != 0)
                    {
                        TimeSinglePlayer += titleList.HltbUserData.Completionist;
                    }
                    else if (titleList.HltbUserData.MainExtra != 0)
                    {
                        TimeSinglePlayer += titleList.HltbUserData.MainExtra;
                    }
                    else if (titleList.HltbUserData.MainStory != 0)
                    {
                        TimeSinglePlayer += titleList.HltbUserData.MainStory;
                    }

                    TimeCoOp += titleList.HltbUserData.CoOp;
                    TimeCoOp += titleList.HltbUserData.Vs;
                }

                PlayTimeToStringConverterWithZero converter = new PlayTimeToStringConverterWithZero();

                PART_TimeSinglePlayer.Content = (string)converter.Convert(TimeSinglePlayer, null, null, CultureInfo.CurrentCulture);
                PART_TimeCoOp.Content = (string)converter.Convert(TimeCoOp, null, null, CultureInfo.CurrentCulture);
                PART_TimeVs.Content = (string)converter.Convert(TimeVs, null, null, CultureInfo.CurrentCulture);


                PART_CountGameBeatenBeforeTime.Content = PluginDatabase.GetCountGameBeatenBeforeTime();
                PART_CountGameBeatenAfterTime.Content = PluginDatabase.GetCountGameBeatenAfterTime();
                PART_AvgGameByMonth.Content = string.Format("{0:0.0}", PluginDatabase.GetAvgGameByMonth());
                PART_AvgTimeByGame.Content = (string)converter.Convert(PluginDatabase.GetAvgTimeByGame(), null, null, CultureInfo.CurrentCulture);
            }
        }

        private void SetPlayniteData()
        {
            Task.Run(() =>
            {
                var PlayniteData = PluginDatabase.Database.Where(x => x.HasData && !x.HasDataEmpty)
                           .Select(x => new PlayniteData
                           {
                               GameContext = PluginDatabase.PlayniteApi.Database.Games.Get(x.Id)
                           }).ToList();

                this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
                {
                    ListViewDataGames.ItemsSource = PlayniteData;
                }));
            });
        }


        private void DisplayPlayniteDataLoader()
        {
            PART_DataLoad.Visibility = Visibility.Visible;
            PART_LvDataContener.Visibility = Visibility.Hidden;

            Task.Run(() =>
            {
                Thread.Sleep(10000);
                this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                {
                    PART_DataLoad.Visibility = Visibility.Collapsed;
                    PART_LvDataContener.Visibility = Visibility.Visible;
                }));
            });
        }


        private void PART_PlayniteData_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (((FrameworkElement)sender).Visibility == Visibility.Visible && DisplayFirst)
            {
                SetPlayniteData();
                DisplayPlayniteDataLoader();
                DisplayFirst = false;
            }
        }

        private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            DisplayPlayniteDataLoader();
        }


        #region Filter
        private void SetFilter()
        {
            // Filter
            var listYear = PluginDatabase.Database.UserHltbData.TitlesList.Select(x => x.Completion?.ToString("yyyy") ?? "----").Distinct().OrderBy(x => x).ToList();
            PART_CbYear.ItemsSource = null;
            PART_CbYear.ItemsSource = listYear;
            PART_CbYear.SelectedIndex = 0;

            var listStoreFront = PluginDatabase.Database.UserHltbData.TitlesList.Where(x => !x.Storefront.IsNullOrEmpty()).Select(y => y.Storefront).Distinct().ToList();
            listStoreFront.Add("----");
            listStoreFront = listStoreFront.OrderBy(x => x).ToList();
            PART_CbStorefront.ItemsSource = null;
            PART_CbStorefront.ItemsSource = listStoreFront;
            PART_CbStorefront.SelectedIndex = 0;

            var listPlatform = PluginDatabase.Database.UserHltbData.TitlesList.Where(x => !x.Platform.IsNullOrEmpty()).Select(y => y.Platform).Distinct().ToList();
            listPlatform.Add("----");
            listPlatform = listPlatform.OrderBy(x => x).ToList();
            PART_CbPlatform.ItemsSource = null;
            PART_CbPlatform.ItemsSource = listPlatform;
            PART_CbPlatform.SelectedIndex = 0;
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
                string Year = ((ComboBox)sender).SelectedValue.ToString();
                FilterData(PART_NameSearch.Text, Year, PART_CbStorefront.Text, PART_CbPlatform.Text);
            }
            catch { }
        }

        private void PART_CbStorefront_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                string StoreFront = ((ComboBox)sender).SelectedValue.ToString();
                FilterData(PART_NameSearch.Text, PART_CbYear.Text, StoreFront, PART_CbPlatform.Text);
            }
            catch { }
        }

        private void PART_CbPlatform_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                string Platform = ((ComboBox)sender).SelectedValue.ToString();
                FilterData(PART_NameSearch.Text, PART_CbYear.Text, PART_CbStorefront.Text, Platform);
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
                userViewDataContext.ItemsSource = PluginDatabase.Database.UserHltbData.TitlesList.ToObservable();
            }
            // StoreFront only
            else if ((Year.IsNullOrEmpty() || Year.IsEqual("----")) && (Platform.IsNullOrEmpty() || Platform.IsEqual("----")))
            {
                userViewDataContext.ItemsSource = PluginDatabase.Database.UserHltbData.TitlesList
                    .Where(x => x.Storefront != null && x.Storefront.IsEqual(StoreFront)).ToObservable();
            }
            // Year only
            else if ((StoreFront.IsNullOrEmpty() || StoreFront.IsEqual("----")) && (Platform.IsNullOrEmpty() || Platform.IsEqual("----")))
            {
                userViewDataContext.ItemsSource = PluginDatabase.Database.UserHltbData.TitlesList
                    .Where(x => x.Completion != null && ((DateTime)x.Completion).ToString("yyyy").IsEqual(Year)).ToObservable();
            }
            // Platform only
            else if ((Year.IsNullOrEmpty() || Year.IsEqual("----")) && (StoreFront.IsNullOrEmpty() || StoreFront.IsEqual("----")))
            {
                userViewDataContext.ItemsSource = PluginDatabase.Database.UserHltbData.TitlesList
                    .Where(x => x.Platform != null && x.Platform.IsEqual(Platform)).ToObservable();
            }
            // StoreFront missing
            else if (StoreFront.IsNullOrEmpty() || StoreFront.IsEqual("----"))
            {
                userViewDataContext.ItemsSource = PluginDatabase.Database.UserHltbData.TitlesList
                    .Where(x => x.Completion != null && ((DateTime)x.Completion).ToString("yyyy").IsEqual(Year) && x.Platform != null && x.Platform.IsEqual(Platform)).ToObservable();
            }
            // Year missing
            else if (Year.IsNullOrEmpty() || Year.IsEqual("----"))
            {
                userViewDataContext.ItemsSource = PluginDatabase.Database.UserHltbData.TitlesList
                    .Where(x => x.Storefront != null && x.Storefront.IsEqual(StoreFront) && x.Platform != null && x.Platform.IsEqual(Platform)).ToObservable();
            }
            // Platform missing
            else if (Platform.IsNullOrEmpty() || Platform.IsEqual("----"))
            {
                userViewDataContext.ItemsSource = PluginDatabase.Database.UserHltbData.TitlesList
                    .Where(x => x.Completion != null && ((DateTime)x.Completion).ToString("yyyy").IsEqual(Year) && x.Storefront != null && x.Storefront.IsEqual(StoreFront)).ToObservable();
            }
            else
            {
                userViewDataContext.ItemsSource = PluginDatabase.Database.UserHltbData.TitlesList
                    .Where(x => x.Completion != null && ((DateTime)x.Completion).ToString("yyyy").IsEqual(Year) && x.Storefront != null && x.Storefront.IsEqual(StoreFront) && x.Platform != null && x.Platform.IsEqual(Platform))
                    .ToObservable();
            }

            if (!Name.IsNullOrEmpty())
            {
                userViewDataContext.ItemsSource = userViewDataContext.ItemsSource.Where(x => x.GameName.Contains(Name, StringComparison.InvariantCultureIgnoreCase))                    .ToObservable();
            }

            if ((bool)PART_Replays.IsChecked)
            {
                userViewDataContext.ItemsSource = userViewDataContext.ItemsSource.Where(x => x.IsReplay).ToObservable();
            }

            ListViewGames.Sorting();
        }
        #endregion
    }


    public class PlayniteData
    {
        private HowLongToBeatDatabase PluginDatabase = HowLongToBeat.PluginDatabase;

        public Game GameContext { get; set; }

        public string GameName { get { return GameContext.Name; } }
        public string Icon { get { return GameContext.Icon; } }
        public Guid GameId { get { return GameContext.Id; } }
        public string Source { get { return GameContext.Source?.Name ?? string.Empty; } }
        public string CompletionStatus { get { return GameContext.CompletionStatus?.Name ?? string.Empty; } }
        public ulong Playtime { get { return GameContext.Playtime; } }
        public long TimeToBeat { get { return PluginDatabase.Get(GameId, true)?.GetData()?.GameHltbData?.TimeToBeat ?? 0; } }

        public RelayCommand<Guid> GoToGame
        {
            get
            {
                return PluginDatabase.GoToGame;
            }
        }

        public bool GameExist
        {
            get
            {
                return PluginDatabase.PlayniteApi.Database.Games.Get(GameId) != null;
            }
        }
    }


    public class UserViewDataContext : ObservableObject
    {
        private ObservableCollection<TitleList> _ItemsSource = new ObservableCollection<TitleList>();
        public ObservableCollection<TitleList> ItemsSource { get => _ItemsSource; set => SetValue(ref _ItemsSource, value); }
    }
}
