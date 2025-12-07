using HowLongToBeat.Models.StartPage;
using HowLongToBeat.Services;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace HowLongToBeat.Views.StartPage
{
    /// <summary>
    /// Logique d'interaction pour HltbChartStatsSettings.xaml
    /// </summary>
    public partial class HltbChartStatsSettings : UserControl
    {
        private HowLongToBeat plugin { get; }
        private HowLongToBeatDatabase PluginDatabase { get; set; } = HowLongToBeat.PluginDatabase;


        public HltbChartStatsSettings(HowLongToBeat plugin)
        {
            InitializeComponent();

            this.plugin = plugin;
            this.DataContext = PluginDatabase.PluginSettings;

            PART_CbType.ItemsSource = new List<CbType>
            {
                new CbType { Id = ChartStatsType.month, Name= ResourceProvider.GetString("LOCCommonMonth") },
                new CbType { Id = ChartStatsType.year, Name= ResourceProvider.GetString("LOCCommonYear") }
            };
        }

        private void Grid_Unloaded(object sender, RoutedEventArgs e)
        {
            plugin.SavePluginSettings(PluginDatabase.PluginSettings.Settings);
            PluginDatabase.PluginSettings.OnPropertyChanged();
        }
    }


    public class CbType
    {
        public ChartStatsType Id { get; set; }
        public string Name { get; set; }
    }
}
