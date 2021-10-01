using CommonPluginsControls.LiveChartsCommon;
using CommonPluginsPlaynite.Converters;
using CommonPluginsShared;
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


        public HowLongToBeatUserView()
        {
            InitializeComponent();

            if (PluginDatabase.Database.UserHltbData?.TitlesList != null)
            {
                ListViewGames.ItemsSource = PluginDatabase.Database.UserHltbData.TitlesList;

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
                    case TitleListSort.CurrentTime:
                        SortingDefaultDataName = "CurrentTime";
                        break;
                };
                ListViewGames.SortingDefaultDataName = SortingDefaultDataName;
                ListViewGames.SortingSortDirection = (PluginDatabase.PluginSettings.Settings.IsAsc) ? ListSortDirection.Ascending : ListSortDirection.Descending;
                ListViewGames.Sorting();
            }

            //let create a mapper so LiveCharts know how to plot our CustomerViewModel class
            var customerVmMapper = Mappers.Xy<CustomerForSingle>()
                .X((value, index) => index)
                .Y(value => value.Values);

            //lets save the mapper globally
            Charting.For<CustomerForSingle>(customerVmMapper);


            SetChartDataYear();
            SetChartDataStore();
            SetChartData();
            SetStats();
        }


        private void PART_BtRefreshUserData_Click(object sender, RoutedEventArgs e)
        {
            ListViewGames.ItemsSource = null;

            PART_ChartUserDataYear.Series = null;
            PART_ChartUserDataYearLabelsX.Labels = null;
            PART_ChartUserData.Series = null;
            PART_ChartUserDataLabelsX.Labels = null;

            PART_CompletionsCount.Content = string.Empty;
            PART_TimeSinglePlayer.Content = string.Empty;
            PART_TimeCoOp.Content = string.Empty;
            PART_TimeVs.Content = string.Empty;


            PluginDatabase.RefreshUserData();

            ListViewGames.ItemsSource = PluginDatabase.Database.UserHltbData.TitlesList;
            ListViewGames.Sorting();

            SetChartDataYear();
            SetChartDataStore();
            SetChartData();
            SetStats();
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
                    if (titleList.Completion != null)
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


                PART_CompletionsCount.Content = titleLists.FindAll(x => x.Completion != null).Count;


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

                PlayTimeToStringConverter converter = new PlayTimeToStringConverter();

                PART_TimeSinglePlayer.Content = (string)converter.Convert(TimeSinglePlayer, null, null, CultureInfo.CurrentCulture);
                PART_TimeCoOp.Content = (string)converter.Convert(TimeCoOp, null, null, CultureInfo.CurrentCulture);
                PART_TimeVs.Content = (string)converter.Convert(TimeVs, null, null, CultureInfo.CurrentCulture);


                PART_AvgGameByMonth.Content = string.Format("{0:0.0}", PluginDatabase.GetAvgGameByMonth());
                PART_AvgTimeByGame.Content = (string)converter.Convert(PluginDatabase.GetAvgTimeByGame(), null, null, CultureInfo.CurrentCulture);
            }
        }
    }
}
