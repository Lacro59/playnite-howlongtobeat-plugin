using CommonPluginsControls.LiveChartsCommon;
using CommonPluginsShared.Converters;
using HowLongToBeat.Models;
using HowLongToBeat.Models.StartPage;
using HowLongToBeat.Services;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace HowLongToBeat.Views.StartPage
{
    /// <summary>
    /// Logique d'interaction pour HltbChartStats.xaml
    /// </summary>
    public partial class HltbChartStats : UserControl
    {
        private HowLongToBeatDatabase PluginDatabase { get; set; } = HowLongToBeat.PluginDatabase;

        private HltbChartStatsDataContext ControlDataContext = new HltbChartStatsDataContext();


        public HltbChartStats()
        {
            PluginDatabase.PluginSettings.Settings.PropertyChanged += Settings_PropertyChanged;
            PluginDatabase.PluginSettings.PropertyChanged += SettingsViewModel_PropertyChanged;


            InitializeComponent();
            this.DataContext = ControlDataContext;


            Update();
        }


        private void SettingsViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Update();
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Update();
        }


        private void Update()
        {
            System.Threading.SpinWait.SpinUntil(() => PluginDatabase.IsLoaded, -1);

            ControlDataContext.Margin = PluginDatabase.PluginSettings.Settings.hltbChartStatsOptions.Margin;
            ControlDataContext.chartTitle = PluginDatabase.PluginSettings.Settings.hltbChartStatsOptions.ChartTitle;
            ControlDataContext.chartLabels = PluginDatabase.PluginSettings.Settings.hltbChartStatsOptions.ChartLabels;
            ControlDataContext.chartLabelsOrdinates = PluginDatabase.PluginSettings.Settings.hltbChartStatsOptions.ChartLabelsOrdinates;            

            switch (PluginDatabase.PluginSettings.Settings.hltbChartStatsOptions.StatsType)
            {
                case ChartStatsType.year:
                    SetChartDataYear(Convert.ToInt32(PluginDatabase.PluginSettings.Settings.hltbChartStatsOptions.DataNumber));
                    break;

                case ChartStatsType.month:
                    SetChartDataMonth(Convert.ToInt32(PluginDatabase.PluginSettings.Settings.hltbChartStatsOptions.DataNumber));
                    break;
            }
        }


        private void SetChartDataMonth(int axis = 15)
        {
            if (PluginDatabase.Database?.UserHltbData?.TitlesList != null)
            {
                LocalDateYMConverter localDateYMConverter = new LocalDateYMConverter();


                // Default data
                string[] ChartDataLabels = new string[axis + 1];
                ChartValues<CustomerForSingle> ChartDataSeries = new ChartValues<CustomerForSingle>();


                for (int i = axis; i >= 0; i--)
                {
                    ChartDataLabels[axis - i] = (string)localDateYMConverter.Convert(DateTime.Now.AddMonths(-i), null, null, null);
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


                ControlDataContext.seriesViews = ChartSeriesCollection;
                ControlDataContext.labels = ChartDataLabels;
            }
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


                ControlDataContext.seriesViews = ChartSeriesCollection;
                ControlDataContext.labels = ChartDataLabels;
            }
        }
    }


    public class HltbChartStatsDataContext : ObservableObject
    {
        private double _Margin = 10;
        public double Margin { get => _Margin; set => SetValue(ref _Margin, value); }


        private SeriesCollection _seriesViews = null;
        public SeriesCollection seriesViews { get => _seriesViews; set => SetValue(ref _seriesViews, value); }
    
        private IList<string> _labels = null;
        public IList<string> labels { get => _labels; set => SetValue(ref _labels, value); }


        private bool _chartTitle = false;
        public bool chartTitle { get => _chartTitle; set => SetValue(ref _chartTitle, value); }

        private bool _chartLabels = false;
        public bool chartLabels { get => _chartLabels; set => SetValue(ref _chartLabels, value); }
    
        private bool _chartLabelsOrdinates = false;
        public bool chartLabelsOrdinates { get => _chartLabelsOrdinates; set => SetValue(ref _chartLabelsOrdinates, value); }
    }
}
