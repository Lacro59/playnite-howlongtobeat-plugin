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
using CommonPluginsShared;
using System.Windows.Data;

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

        private readonly HowLongToBeat Plugin;

        private bool DisplayFirst = true;

        private HowLongToBeatDatabase PluginDatabase = HowLongToBeat.PluginDatabase;
        private UserViewDataContext userViewDataContext = new UserViewDataContext();

        private bool PlayniteDataFilter(object item)
        {
            return ((bool)PART_FilteredGames.IsChecked ? API.Instance.MainView.FilteredGames.Find(y => y.Id == (item as PlayniteData).GameContext.Id) != null : true)
                && ((bool)PART_HidePlayedGames.IsChecked ? (item as PlayniteData).Playtime == 0 : true);
        }


        public HowLongToBeatUserView(HowLongToBeat plugin)
        {
            Plugin = plugin;

            InitializeComponent();
            this.DataContext = userViewDataContext;


            if (!PluginDatabase.PluginSettings.Settings.EnableProgressBarInDataView)
            {
                GridView lvView = (GridView)ListViewDataGames.View;
                lvView.Columns.RemoveAt(lvView.Columns.Count - 1);
                lvView.Columns.RemoveAt(lvView.Columns.Count - 1);
            }


            ListViewDataGames.ItemsSource = PluginDatabase.Database.Where(x => x.HasData && !x.HasDataEmpty && !x.Hidden)
              .Select(x => new PlayniteData
              {
                  GameContext = PluginDatabase.PlayniteApi.Database.Games.Get(x.Id),
                  ViewProgressBar = PluginDatabase.PluginSettings.Settings.EnableProgressBarInDataView
              }).ToObservable();

            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(ListViewDataGames.ItemsSource);
            view.Filter = PlayniteDataFilter;


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
                        default:
                            break;
                    };
                    ListViewGames.SortingDefaultDataName = SortingDefaultDataName;
                    ListViewGames.SortingSortDirection = (PluginDatabase.PluginSettings.Settings.IsAsc) ? ListSortDirection.Ascending : ListSortDirection.Descending;
                    ListViewGames.Sorting();

                    SetFilter();
                }


                SetChartDataStore();
                SetChartDataYear();
                SetChartData();
                SetStats();


                PART_PlayniteData.IsVisibleChanged += PART_PlayniteData_IsVisibleChanged;
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


        private void SetChartDataYear(int axis = 4)
        {
            if (PluginDatabase.Database.UserHltbData?.TitlesList != null)
            {
                // Default data
                string[] ChartDataLabels = new string[axis];
                ChartValues<CustomerForSingle> ChartDataSeries = new ChartValues<CustomerForSingle>();

                for (int i = (axis - 1); i >= 0; i--)
                {
                    ChartDataLabels[((axis - 1) - i)] = DateTime.Now.AddYears(-i).ToString("yyyy");
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
                PART_CountGameBeatenReplays.Content = PluginDatabase.GetCountGameBeatenReplays();
                PART_CountGameRetired.Content = PluginDatabase.GetCountGameRetired();
            }
        }

        private void SetPlayniteData()
        {
            CollectionViewSource.GetDefaultView(ListViewDataGames.ItemsSource).Refresh();

            //ListViewDataGames.ItemsSource = null;
            //ObservableCollection<PlayniteData> PlayniteData = null;
            //
            //PlayniteData = PluginDatabase.Database.Where(x => x.HasData && !x.HasDataEmpty && !x.Hidden
            //                            && ((bool)PART_FilteredGames.IsChecked ? API.Instance.MainView.FilteredGames.Find(y => y.Id == x.Id) != null : true)
            //                            && ((bool)PART_HidePlayedGames.IsChecked ? x.Playtime == 0 : true))
            //      .Select(x => new PlayniteData
            //      {
            //          GameContext = PluginDatabase.PlayniteApi.Database.Games.Get(x.Id),
            //          ViewProgressBar = PluginDatabase.PluginSettings.Settings.EnableProgressBarInDataView
            //      }).ToObservable();
            //
            //ListViewDataGames.ItemsSource = PlayniteData;
            ListViewDataGames.Sorting();
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
            listStoreFront.Add("----");
            listStoreFront = listStoreFront.OrderBy(x => x).ToList();
            PART_CbStorefront.ItemsSource = null;
            PART_CbStorefront.ItemsSource = listStoreFront;
            PART_CbStorefront.SelectedIndex = 0;

            List<string> listPlatform = PluginDatabase.Database.UserHltbData.TitlesList.Where(x => !x.Platform.IsNullOrEmpty()).Select(y => y.Platform).Distinct().ToList();
            listPlatform.Add("----");
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

            if ((bool)PART_OnlyNotPlayed.IsChecked)
            {
                userViewDataContext.ItemsSource = userViewDataContext.ItemsSource.Where(x => x.Completion == null).ToObservable();
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
                    SetChartDataYear(12);
                    PART_ChartUserData.Visibility = Visibility.Collapsed;
                    Grid.SetColumnSpan(PART_ChartUserDataYear, 3);
                    Grid.SetColumnSpan(PART_ExpandChartYear, 3);
                    bt.Content = "\ue9b0";
                    bt.Tag = "1";
                    break;

                case "1":
                    SetChartDataYear(4);
                    PART_ChartUserData.Visibility = Visibility.Visible;
                    Grid.SetColumnSpan(PART_ChartUserDataYear, 1);
                    Grid.SetColumnSpan(PART_ExpandChartYear, 1);
                    bt.Content = "\ue9a8";
                    bt.Tag = "0";
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
            try
            {
                Part_Found.Visibility = PART_TabControl.SelectedIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch { }
        }
    }


    public class PlayniteData : ObservableObject
    {
        private HowLongToBeatDatabase PluginDatabase = HowLongToBeat.PluginDatabase;

        private PlayTimeToStringConverterWithZero playTimeToStringConverterWithZero = new PlayTimeToStringConverterWithZero();

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
        public string RemainingTimeFormat => RemainingTime > 0 ? (string)playTimeToStringConverterWithZero.Convert(RemainingTime, null, null, CultureInfo.CurrentCulture) : string.Empty;

        public RelayCommand<Guid> GoToGame => PluginDatabase.GoToGame;

        public bool GameExist => PluginDatabase.PlayniteApi.Database.Games.Get(GameId) != null;
    }


    public class UserViewDataContext : ObservableObject
    {
        private ObservableCollection<TitleList> _ItemsSource = new ObservableCollection<TitleList>();
        public ObservableCollection<TitleList> ItemsSource { get => _ItemsSource; set => SetValue(ref _ItemsSource, value); }
    }
}
