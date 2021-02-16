﻿using CommonPluginsShared;
using HowLongToBeat.Models;
using HowLongToBeat.Services;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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

        public GameHowLongToBeat gameHowLongToBeat;
        private Game _game;

        private List<HltbPlatform> items;


        public HowLongToBeatSelect(List<HltbData> data, Game game)
        {
            _game = game;

            InitializeComponent();

            SetPlatforms();

            SearchElement.Text = _game.Name;

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


        private void SetPlatforms()
        {
            PART_SelectPlatform.ItemsSource = HowLongToBeat.PluginDatabase.hltbPlatforms;
        }

        private void PART_SelectPlatform_KeyUp(object sender, KeyEventArgs e)
        {
            string SearchText = ((ComboBox)sender).Text;

            PART_SelectPlatform.ItemsSource = null;
            PART_SelectPlatform.ItemsSource = items.Where(x => x.Name.ToLower().Contains(SearchText)).Distinct().ToList();
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
            HltbDataUser Item = (HltbDataUser)lbSelectable.SelectedItem;

            gameHowLongToBeat = HowLongToBeat.PluginDatabase.GetDefault(_game);
            gameHowLongToBeat.Items = new List<HltbDataUser>() { Item };

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
            lbSelectable.ItemsSource = null;

            PART_DataLoadWishlist.Visibility = Visibility.Visible;
            SelectableContent.IsEnabled = false;

            string GameSearch = SearchElement.Text;
            string GamePlatform = ((HltbPlatform)PART_SelectPlatform.SelectedValue == null) ? string.Empty : ((HltbPlatform)PART_SelectPlatform.SelectedValue).Name;
            Task task = Task.Run(() =>
            {
                List<HltbDataUser> dataSearch = new List<HltbDataUser>();
                try
                {
                    dataSearch = HowLongToBeat.PluginDatabase.howLongToBeatClient.Search(GameSearch, GamePlatform);
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "HowLongToBeat", "Error on LoadData()");
                }

#if DEBUG
                logger.Debug($"HowLongToBeat [Ignored] - dataSearch: {JsonConvert.SerializeObject(dataSearch)}");
#endif
                this.Dispatcher.BeginInvoke((Action)delegate
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



    public class HltbPlatform
    {
        public string Name { get; set; }
        public string Category { get; set; }
    }
}
