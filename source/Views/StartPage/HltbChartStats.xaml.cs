using CommonPluginsControls.LiveChartsCommon;
using CommonPluginsShared;
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
using System.Windows.Threading;

namespace HowLongToBeat.Views.StartPage
{
    public partial class HltbChartStats : UserControl
    {
        private HowLongToBeatDatabase PluginDatabase => HowLongToBeat.PluginDatabase;

        private HltbChartStatsDataContext ControlDataContext { get; set; } = new HltbChartStatsDataContext();


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


        private async void Update()
        {
            int maxWaitMs = 30000;
            int elapsed = 0;
            try
            {
                while (!PluginDatabase.IsLoaded && elapsed < maxWaitMs)
                {
                    await Task.Delay(100).ConfigureAwait(false);
                    elapsed += 100;
                }

                if (!PluginDatabase.IsLoaded)
                {
                    try { Common.LogDebug(true, "HltbChartStats: Database not loaded after timeout"); } catch { }
                    return;
                }
            }
            catch (Exception ex)
            {
                try { Common.LogError(ex, false, true, PluginDatabase.PluginName); } catch { }
                return;
            }

            ControlDataContext.Margin = PluginDatabase.PluginSettings.Settings.HltbChartStatsOptions.Margin;
            ControlDataContext.ChartTitle = PluginDatabase.PluginSettings.Settings.HltbChartStatsOptions.ChartTitle;
            ControlDataContext.ChartLabels = PluginDatabase.PluginSettings.Settings.HltbChartStatsOptions.ChartLabels;
            ControlDataContext.ChartLabelsOrdinates = PluginDatabase.PluginSettings.Settings.HltbChartStatsOptions.ChartLabelsOrdinates;

            _ = Application.Current.Dispatcher?.BeginInvoke(DispatcherPriority.Render, (Action)delegate
            {
                switch (PluginDatabase.PluginSettings.Settings.HltbChartStatsOptions.StatsType)
                {
                    case ChartStatsType.year:
                        SetChartDataYear(Convert.ToInt32(PluginDatabase.PluginSettings.Settings.HltbChartStatsOptions.DataNumber));
                        break;

                    case ChartStatsType.month:
                        SetChartDataMonth(Convert.ToInt32(PluginDatabase.PluginSettings.Settings.HltbChartStatsOptions.DataNumber));
                        break;

                    default:
                        break;
                }
            });
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
                            if (index >= 0)
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


                ControlDataContext.SeriesViews = ChartSeriesCollection;
                ControlDataContext.Labels = ChartDataLabels;
            }
        }

        private void SetChartDataYear(int axis = 4)
        {
            if (PluginDatabase.Database.UserHltbData?.TitlesList != null)
            {
                // Default data
                string[] ChartDataLabels = new string[axis];
                ChartValues<CustomerForSingle> ChartDataSeries = new ChartValues<CustomerForSingle>();

                for (int i = axis - 1; i >= 0; i--)
                {
                    ChartDataLabels[axis - 1 - i] = DateTime.Now.AddYears(-i).ToString("yyyy");
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
                        if (index >= 0)
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


                ControlDataContext.SeriesViews = ChartSeriesCollection;
                ControlDataContext.Labels = ChartDataLabels;
            }
        }
    }


    public class HltbChartStatsDataContext : ObservableObject
    {
        private double margin = 10;
        public double Margin { get => margin; set => SetValue(ref margin, value); }


        private SeriesCollection seriesViews = null;
        public SeriesCollection SeriesViews { get => seriesViews; set => SetValue(ref seriesViews, value); }

        private IList<string> labels = null;
        public IList<string> Labels { get => labels; set => SetValue(ref labels, value); }


        private bool chartTitle = false;
        public bool ChartTitle { get => chartTitle; set => SetValue(ref chartTitle, value); }

        private bool chartLabels = false;
        public bool ChartLabels { get => chartLabels; set => SetValue(ref chartLabels, value); }

        private bool chartLabelsOrdinates = false;
        public bool ChartLabelsOrdinates { get => chartLabelsOrdinates; set => SetValue(ref chartLabelsOrdinates, value); }
    }
}
