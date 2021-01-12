using CommonPluginsShared;
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
            items = new List<HltbPlatform>();
            //items.Add(new HltbPlatform() { Name = "", Category = "" });

            //items.Add(new HltbPlatform() { Name = "Emulated", Category = "Popular Platforms" });
            //items.Add(new HltbPlatform() { Name = "Nintendo 3DS", Category = "Popular Platforms" });
            //items.Add(new HltbPlatform() { Name = "Nintendo Switch", Category = "Popular Platforms" });
            //items.Add(new HltbPlatform() { Name = "PC", Category = "Popular Platforms" });
            //items.Add(new HltbPlatform() { Name = "PlayStation 3", Category = "Popular Platforms" });
            //items.Add(new HltbPlatform() { Name = "PlayStation 4", Category = "Popular Platforms" });
            //items.Add(new HltbPlatform() { Name = "PlayStation Now", Category = "Popular Platforms" });
            //items.Add(new HltbPlatform() { Name = "Wii U", Category = "Popular Platforms" });
            //items.Add(new HltbPlatform() { Name = "Xbox 360", Category = "Popular Platforms" });
            //items.Add(new HltbPlatform() { Name = "Xbox One", Category = "Popular Platforms" });

            items.Add(new HltbPlatform() { Name = "3DO", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Amiga", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Amstrad CPC", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Android", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Apple II", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Arcade", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Atari 2600", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Atari 5200", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Atari 7800", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Atari 8-bit Family", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Atari Jaguar", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Atari Jaguar CD", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Atari Lynx", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Atari ST", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "BBC Micro", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Browser", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "ColecoVision", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Commodore 64", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Dreamcast", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Emulated", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "FM Towns", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Game & Watch", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Game Boy", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Game Boy Advance", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Game Boy Color", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Gear VR", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Google Stadia", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Intellivision", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Interactive Movie", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "iOS", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Linux", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Mac", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Mobile", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "MSX", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "N-Gage", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "NEC PC-8800", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "NEC PC-9801/21", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "NEC PC-FX", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Neo Geo", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Neo Geo CD", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Neo Geo Pocket", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "NES", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Nintendo 3DS", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Nintendo 64", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Nintendo DS", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Nintendo GameCube", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Nintendo Switch", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Oculus Go", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Oculus Quest", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "OnLive", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Ouya", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "PC", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "PC VR", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Philips CD-i", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Philips Videopac G7000", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "PlayStation", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "PlayStation 2", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "PlayStation 3", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "PlayStation 4", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "PlayStation 5", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "PlayStation Mobile", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "PlayStation Now", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "PlayStation Portable", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "PlayStation Vita", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "PlayStation VR", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Plug & Play", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Sega 32X", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Sega CD", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Sega Game Gear", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Sega Master System", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Sega Mega Drive/Genesis", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Sega Saturn", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "SG-1000", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Sharp X68000", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Super Nintendo", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Tiger Handheld", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "TurboGrafx-16", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "TurboGrafx-CD", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Virtual Boy", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Wii", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Wii U", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Windows Phone", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "WonderSwan", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Xbox", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Xbox 360", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Xbox One", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "Xbox Series X/S", Category = "All Platforms" });
            items.Add(new HltbPlatform() { Name = "ZX Spectrum", Category = "All Platforms" });

            PART_SelectPlatform.ItemsSource = items;
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
