using HowLongToBeat.Models;
using HowLongToBeat.Services;
using Newtonsoft.Json;
using Playnite.Controls;
using Playnite.SDK;
using PluginCommon;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace HowLongToBeat.Views
{
    /// <summary>
    /// Logique d'interaction pour HowLongToBeatSelect.xaml
    /// </summary>
    public partial class HowLongToBeatSelect : WindowBase
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private string FileGameData;

        public HowLongToBeatSelect(List<HltbData> data, string FileGameData)
        {
            this.FileGameData = FileGameData;

            InitializeComponent();

            lbSelectable.ItemsSource = data;

            // Set Binding data
            DataContext = this;
        }

        private void LbSelectable_Loaded(object sender, RoutedEventArgs e)
        {
            Tools.DesactivePlayniteWindowControl(this);
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ButtonSelect_Click(object sender, RoutedEventArgs e)
        {
            HltbData Item = (HltbData)lbSelectable.SelectedItem;

            var SavData = new HltbDataUser
            {
                GameHltbData = Item
            };

            File.WriteAllText(FileGameData, JsonConvert.SerializeObject(SavData));

            Close();
        }

        private void LbSelectable_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ButtonSelect.IsEnabled = true;
        }

        private void ButtonSearch_Click(object sender, RoutedEventArgs e)
        {
            List<HltbData> dataSearch = new HowLongToBeatClient().Search(SearchElement.Text);
            lbSelectable.ItemsSource = dataSearch;
            lbSelectable.UpdateLayout();
        }
    }
}
