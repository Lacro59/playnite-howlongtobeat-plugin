using CommonPluginsControls.LiveChartsCommon;
using CommonPluginsPlaynite.Converters;
using CommonPluginsShared;
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
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HowLongToBeat.Views
{
    /// <summary>
    /// Logique d'interaction pour HowLongToBeatUserView.xaml
    /// </summary>
    public partial class HowLongToBeatUserView : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        private HowLongToBeatDatabase PluginDatabase = HowLongToBeat.PluginDatabase;

        private GridViewColumnHeader _lastHeaderClicked = null;
        private ListSortDirection _lastDirection = ListSortDirection.Ascending;


        public HowLongToBeatUserView()
        {
            InitializeComponent();

            PluginDatabase.Database.UserHltbData.TitlesList.Sort((x, y) => x.GameName.CompareTo(y.GameName));
            ListViewGames.ItemsSource = PluginDatabase.Database.UserHltbData.TitlesList;
            Sorting();


            //let create a mapper so LiveCharts know how to plot our CustomerViewModel class
            var customerVmMapper = Mappers.Xy<CustomerForSingle>()
                .X((value, index) => index)
                .Y(value => value.Values);

            //lets save the mapper globally
            Charting.For<CustomerForSingle>(customerVmMapper);


            SetChartData();
            SetStats();
        }


        #region Functions sorting ListviewGames.
        private void Sorting()
        {
            // Sorting
            try
            {
                var columnBinding = _lastHeaderClicked.Column.DisplayMemberBinding as Binding;
                var sortBy = columnBinding?.Path.Path ?? _lastHeaderClicked.Column.Header as string;
            }
            // If first view
            catch
            {
                CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(ListViewGames.ItemsSource);
                if (view != null)
                {
                    _lastHeaderClicked = lvName;
                    _lastHeaderClicked.Content += " ▲";
                    //view.SortDescriptions.Add(new SortDescription("lvName", _lastDirection));
                }
            }
        }

        private void ListviewGames_onHeaderClick(object sender, RoutedEventArgs e)
        {
            try
            {
                lvMainStoryValue.IsEnabled = true;
                lvMainExtraValue.IsEnabled = true;
                lvCompletionistValue.IsEnabled = true;
                lvSoloValue.IsEnabled = true;
                lvCoOpValue.IsEnabled = true;
                lvVsValue.IsEnabled = true;


                var headerClicked = e.OriginalSource as GridViewColumnHeader;
                ListSortDirection direction;

                if (headerClicked != null)
                {
                    if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                    {
                        if (headerClicked != _lastHeaderClicked)
                        {
                            direction = ListSortDirection.Ascending;
                        }
                        else
                        {
                            if (_lastDirection == ListSortDirection.Ascending)
                            {
                                direction = ListSortDirection.Descending;
                            }
                            else
                            {
                                direction = ListSortDirection.Ascending;
                            }
                        }

                        var columnBinding = headerClicked.Column.DisplayMemberBinding as Binding;
                        var sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;

                        // Specific sort with another column
                        if (headerClicked.Name == "lvMainStory")
                        {
                            columnBinding = lvMainStoryValue.Column.DisplayMemberBinding as Binding;
                            sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;
                        }
                        if (headerClicked.Name == "lvMainExtra")
                        {
                            columnBinding = lvMainExtraValue.Column.DisplayMemberBinding as Binding;
                            sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;
                        }
                        if (headerClicked.Name == "lvCompletionist")
                        {
                            columnBinding = lvCompletionistValue.Column.DisplayMemberBinding as Binding;
                            sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;
                        }
                        if (headerClicked.Name == "lvSolo")
                        {
                            columnBinding = lvSoloValue.Column.DisplayMemberBinding as Binding;
                            sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;
                        }
                        if (headerClicked.Name == "lvCoOp")
                        {
                            columnBinding = lvCoOpValue.Column.DisplayMemberBinding as Binding;
                            sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;
                        }
                        if (headerClicked.Name == "lvVs")
                        {
                            columnBinding = lvVsValue.Column.DisplayMemberBinding as Binding;
                            sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;
                        }

                        Sort(sortBy, direction);

                        if (_lastHeaderClicked != null)
                        {
                            _lastHeaderClicked.Content = ((string)_lastHeaderClicked.Content).Replace(" ▲", string.Empty);
                            _lastHeaderClicked.Content = ((string)_lastHeaderClicked.Content).Replace(" ▼", string.Empty);
                        }

                        if (direction == ListSortDirection.Ascending)
                        {
                            headerClicked.Content += " ▲";
                        }
                        else
                        {
                            headerClicked.Content += " ▼";
                        }

                        // Remove arrow from previously sorted header
                        if (_lastHeaderClicked != null && _lastHeaderClicked != headerClicked)
                        {
                            _lastHeaderClicked.Column.HeaderTemplate = null;
                        }

                        _lastHeaderClicked = headerClicked;
                        _lastDirection = direction;
                    }
                }

                lvMainStoryValue.IsEnabled = false;
                lvMainExtraValue.IsEnabled = false;
                lvCompletionistValue.IsEnabled = false;
                lvSoloValue.IsEnabled = false;
                lvCoOpValue.IsEnabled = false;
                lvVsValue.IsEnabled = false;
            }
            catch
            {

            }
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            ICollectionView dataView = CollectionViewSource.GetDefaultView(ListViewGames.ItemsSource);

            dataView.SortDescriptions.Clear();
            SortDescription sd = new SortDescription(sortBy, direction);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();
        }
        #endregion


        private void PART_BtRefreshUserData_Click(object sender, RoutedEventArgs e)
        {
            ListViewGames.ItemsSource = null;

            PART_ChartUserData.Series = null;
            PART_ChartUserDataLabelsX.Labels = null;

            PART_CompletionsCount.Content = string.Empty;
            PART_TimeSinglePlayer.Content = string.Empty;
            PART_TimeCoOp.Content = string.Empty;
            PART_TimeVs.Content = string.Empty;


            PluginDatabase.RefreshUserData();


            ListViewGames.ItemsSource = PluginDatabase.Database.UserHltbData.TitlesList;
            Sorting();

            SetChartData();
            SetStats();
        }


        private void SetChartData()
        {
            LocalDateYMConverter localDateYMConverter = new LocalDateYMConverter();

            
            // Dafault data
            string[] ChartDataLabels = new string[25];           
            ChartValues<CustomerForSingle> ChartDataSeries = new ChartValues<CustomerForSingle>();

            for (int i = 24; i >= 0; i--)
            {
                ChartDataLabels[(24 - i)] = (string)localDateYMConverter.Convert(DateTime.Now.AddMonths(-i), null, null, null);
                ChartDataSeries.Add(new CustomerForSingle
                {
                    Name = (string)localDateYMConverter.Convert(DateTime.Now.AddMonths(-i), null, null, null),
                    Values = 0
                });
            }


            // Set data
            foreach (TitleList titleList in PluginDatabase.Database.UserHltbData.TitlesList)
            {
                if (titleList.Completion != null)
                {
                    string tempDateTime = (string)localDateYMConverter.Convert((DateTime)titleList.Completion, null, null, null);

                    int index = Array.IndexOf(ChartDataLabels, tempDateTime);

                    if (index > 0)
                    {
                        ChartDataSeries[index].Values += 1;
                    }
                }
            }


            // Create chart
            SeriesCollection StatsGraphicAchievementsSeries = new SeriesCollection();
            StatsGraphicAchievementsSeries.Add(new LineSeries
            {
                Title = string.Empty,
                Values = ChartDataSeries
            });


            PART_ChartUserData.Series = StatsGraphicAchievementsSeries;
            PART_ChartUserDataLabelsX.Labels = ChartDataLabels;
        }

        private void SetStats()
        {
            List<TitleList> titleLists = PluginDatabase.Database.UserHltbData.TitlesList;


            PART_CompletionsCount.Content = titleLists.FindAll(x => x.Completion != null).Count;


            long TimeSinglePlayer = 0;
            long TimeCoOp = 0;
            long TimeVs = 0;

            foreach (TitleList titleList  in titleLists)
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

            LongToTimePlayedConverter converter = new LongToTimePlayedConverter();

            PART_TimeSinglePlayer.Content = (string)converter.Convert((long)TimeSinglePlayer, null, null, CultureInfo.CurrentCulture);
            PART_TimeCoOp.Content = (string)converter.Convert((long)TimeCoOp, null, null, CultureInfo.CurrentCulture); 
            PART_TimeVs.Content = (string)converter.Convert((long)TimeVs, null, null, CultureInfo.CurrentCulture); 
        }
    }
}
