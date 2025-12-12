using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using CommonPluginsShared;
using HowLongToBeat.Models;
using HowLongToBeat.Models.Enumerations;
using HowLongToBeat.Services;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;

namespace HowLongToBeat.Views
{
    /// <summary>
    /// Logique d'interaction pour HowLongToBeatSelect.xaml
    /// </summary>
    public partial class HowLongToBeatSelect : UserControl
    {
        private static HowLongToBeatDatabase PluginDatabase => HowLongToBeat.PluginDatabase;

        public GameHowLongToBeat GameHowLongToBeat { get; set; }
        private Game GameContext { get; set; }


        public HowLongToBeatSelect(Game game, List<HltbDataUser> data)
        {
            InitializeComponent();

            ApplyThemeResources();

            GameContext = game;
            SearchElement.Text = GameContext.Name;

            if (PluginDatabase.PluginSettings.Settings.UseHtltbClassic)
            {
                lbSelectable.Tag = DataType.Classic;
            }
            if (PluginDatabase.PluginSettings.Settings.UseHtltbAverage)
            {
                lbSelectable.Tag = DataType.Average;
            }
            if (PluginDatabase.PluginSettings.Settings.UseHtltbMedian)
            {
                lbSelectable.Tag = DataType.Median;
            }
            if (PluginDatabase.PluginSettings.Settings.UseHtltbRushed)
            {
                lbSelectable.Tag = DataType.Rushed;
            }
            if (PluginDatabase.PluginSettings.Settings.UseHtltbLeisure)
            {
                lbSelectable.Tag = DataType.Leisure;
            }

            if (data == null)
            {
                _ = Task.Run(() =>
                {
                    Thread.Sleep(300);
                    Application.Current?.Dispatcher?.Invoke(new Action(() =>
                    {
                        SearchData();
                    }));
                });
            }
            else
            {
                lbSelectable.ItemsSource = data;
                lbSelectable.UpdateLayout();
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
            HltbDataUser Item = (HltbDataUser)lbSelectable.SelectedItem;

            GameHowLongToBeat = HowLongToBeat.PluginDatabase.GetDefault(GameContext);
            GameHowLongToBeat.Items = new List<HltbDataUser>() { Item };

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

            string gameSearch = SearchElement.Text;
            string gamePlatform = (PART_SelectPlatform.SelectedValue == null)
                  ? string.Empty
                  : ((HltbPlatform)PART_SelectPlatform.SelectedValue).GetDescription();

            bool isVndb = (bool)PART_Vndb.IsChecked;

            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions($"{ResourceProvider.GetString("LOCDownloadingLabel")}")
            {
                Cancelable = false,
                IsIndeterminate = true
            };

            _ = API.Instance.Dialogs.ActivateGlobalProgress(async (activateGlobalProgress) =>
            {
                List<HltbDataUser> dataSearch = new List<HltbDataUser>();
                try
                {
                    if (isVndb)
                    {
                        var vndbResults = await VndbApi.SearchByNameAsync(gameSearch);
                        dataSearch = vndbResults?.Select(x => x.Data).ToList() ?? new List<HltbDataUser>();
                    }
                    else
                    {
                        var hlResults = await PluginDatabase.HowLongToBeatApi.SearchTwoMethod(gameSearch, gamePlatform);
                        dataSearch = hlResults?.Select(x => x.Data).ToList() ?? new List<HltbDataUser>();
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                }

                Common.LogDebug(true, $"dataSearch: {Serialization.ToJson(dataSearch)}");
                Application.Current.Dispatcher?.Invoke(new Action(() =>
                {
                    lbSelectable.ItemsSource = dataSearch;
                    lbSelectable.UpdateLayout();
                }));
            }, globalProgressOptions);
        }

        /// <summary>
        /// Show or not the ToolTip.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBlock_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var textBlock = sender as TextBlock;
            if (textBlock == null)
            {
                return;
            }

            var toolTip = textBlock.ToolTip as ToolTip;
            if (toolTip == null)
            {
                return;
            }

            try
            {
                var text = textBlock.Text ?? string.Empty;
                if (string.IsNullOrEmpty(text))
                {
                    toolTip.Visibility = Visibility.Hidden;
                    return;
                }

                var typeface = new Typeface(
                    textBlock.FontFamily,
                    textBlock.FontStyle,
                    textBlock.FontWeight,
                    textBlock.FontStretch);

                // Use the FormattedText overload with PixelsPerDip to avoid obsolete warning
                double pixelsPerDip = 1.0;
                try
                {
                    pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
                }
                catch { }

                var formattedText = new FormattedText(
                    text,
                    System.Threading.Thread.CurrentThread.CurrentCulture,
                    textBlock.FlowDirection,
                    typeface,
                    textBlock.FontSize,
                    textBlock.Foreground,
                    pixelsPerDip);

                // Compare measured width with actual available width. If larger, show tooltip.
                toolTip.Visibility = formattedText.Width > (textBlock.ActualWidth - 1.0)
                    ? Visibility.Visible
                    : Visibility.Hidden;
            }
            catch
            {
                // On any error, hide tooltip rather than crash
                try { toolTip.Visibility = Visibility.Hidden; } catch { }
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

        private void ListBoxItem_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var lbi = sender as ListBoxItem;
            if (lbi == null) return;

            if (!lbi.IsSelected)
            {
                lbi.IsSelected = true;
            }
            e.Handled = false;
        }

        private void ApplyThemeResources()
        {
            try
            {
                // Try to get common theme brushes from the host via ResourceProvider
                var normal = ResourceProvider.GetResource("NormalBrush") as Brush;
                var controlBg = ResourceProvider.GetResource("ControlBackgroundBrush") as Brush ?? normal;
                var controlFg = ResourceProvider.GetResource("ControlForegroundBrush") as Brush ?? Brushes.White;
                var controlBorder = ResourceProvider.GetResource("ControlBorderBrush") as Brush ?? Brushes.Gray;
                var accent = ResourceProvider.GetResource("AccentColorBrush") as Brush ?? normal;

                // Override the local resource keys defined in XAML so DynamicResource bindings pick them
                this.Resources["CardBackgroundBrush"] = controlBg ?? new SolidColorBrush(Color.FromRgb(0x2E, 0x2F, 0x33));
                this.Resources["CardForegroundBrush"] = controlFg ?? Brushes.White;
                this.Resources["CardBorderBrush"] = controlBorder ?? new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44));

                this.Resources["PrimaryButtonBackgroundBrush"] = accent ?? new SolidColorBrush(Color.FromRgb(0x5A, 0x9B, 0xD5));
                this.Resources["PrimaryButtonHoverBrush"] = accent ?? new SolidColorBrush(Color.FromRgb(0x4B, 0x89, 0xC6));
                this.Resources["PrimaryButtonForegroundBrush"] = controlFg ?? Brushes.White;
            }
            catch { }
        }
    }
}
