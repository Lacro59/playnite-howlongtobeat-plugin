using HowLongToBeat.Models;
using HowLongToBeat.Services;
using Newtonsoft.Json;
using Playnite.SDK;
using PluginCommon;
using PluginCommon.PlayniteResources;
using PluginCommon.PlayniteResources.API;
using PluginCommon.PlayniteResources.Common;
using PluginCommon.PlayniteResources.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HowLongToBeat.Views
{
    /// <summary>
    /// Logique d'interaction pour HowLongToBeatSelect.xaml
    /// </summary>
    public partial class HowLongToBeatSelect : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private string FileGameData;

        public HowLongToBeatSelect(List<HltbData> data, string FileGameData, string GameName)
        {
            this.FileGameData = FileGameData;

            InitializeComponent();

            SearchElement.Text = GameName;

            if (data == null)
            {
                SearchData();
            }
            else
            {
                lbSelectable.ItemsSource = data;
                lbSelectable.UpdateLayout();
                PART_DataLoadWishlist.Visibility = Visibility.Collapsed;
                SelectableContent.IsEnabled = true;
            }

            // Set Binding data
            DataContext = this;
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            ((Window)this.Parent).Close();
        }

        /// <summary>
        /// Valid the selection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonSelect_Click(object sender, RoutedEventArgs e)
        {
            HltbData Item = (HltbData)lbSelectable.SelectedItem;

            var SavData = new HltbDataUser
            {
                GameHltbData = Item
            };

            File.WriteAllText(FileGameData, JsonConvert.SerializeObject(SavData));
            ((Window)this.Parent).Close();
        }

        /// <summary>
        /// Deblock validation button after a selection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LbSelectable_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ButtonSelect.IsEnabled = true;
        }

        /// <summary>
        /// Search element by name.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchData();
        }

        private void SearchData()
        {
            PART_DataLoadWishlist.Visibility = Visibility.Visible;
            SelectableContent.IsEnabled = false;

            string GameSearch = SearchElement.Text;
            Task task = Task.Run(() =>
            {
                List<HltbData> dataSearch = new List<HltbData>();
                try
                {
                    HowLongToBeatClient howLongToBeatClient = new HowLongToBeatClient();
                    dataSearch = howLongToBeatClient.Search(GameSearch);
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "HowLongToBeat", "Error on LoadData()");
                }

#if DEBUG
                logger.Debug($"HowLongToBeat - dataSearch: {JsonConvert.SerializeObject(dataSearch)}");
#endif
                Application.Current.Dispatcher.BeginInvoke((Action)delegate
                {
                    lbSelectable.ItemsSource = dataSearch;
                    lbSelectable.UpdateLayout();

                    PART_DataLoadWishlist.Visibility = Visibility.Collapsed;
                    SelectableContent.IsEnabled = true;
                });
            });
        }

        /// <summary>
        /// Show or not the ToolTip.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBlock_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            string Text = ((TextBlock)sender).Text;
            TextBlock textBlock = (TextBlock)sender;

            Typeface typeface = new Typeface(
                textBlock.FontFamily,
                textBlock.FontStyle,
                textBlock.FontWeight,
                textBlock.FontStretch);

            FormattedText formattedText = new FormattedText(
                textBlock.Text,
                System.Threading.Thread.CurrentThread.CurrentCulture,
                textBlock.FlowDirection,
                typeface,
                textBlock.FontSize,
                textBlock.Foreground,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            if (formattedText.Width > textBlock.DesiredSize.Width)
            {
                ((ToolTip)((TextBlock)sender).ToolTip).Visibility = Visibility.Visible;
            }
            else
            {
                ((ToolTip)((TextBlock)sender).ToolTip).Visibility = Visibility.Hidden;
            }
        }

        /// <summary>
        /// Valid search by enter key.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SearchElement_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                ButtonSearch_Click(null, null);
            }
        }
    }
}
