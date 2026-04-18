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
    public partial class HowLongToBeatSelect : UserControl
    {
        private static HowLongToBeatDatabase PluginDatabase => HowLongToBeat.PluginDatabase;

        public GameHowLongToBeat GameHowLongToBeat { get; set; }
        private Game GameContext { get; set; }

        public HowLongToBeatSelect(Game game, List<HltbDataUser> data)
        {
            InitializeComponent();

            GameContext = game;
            SearchElement.Text = GameContext.Name;

            if (PluginDatabase.PluginSettings.UseHtltbClassic)
            {
                lbSelectable.Tag = DataType.Classic;
            }
            if (PluginDatabase.PluginSettings.UseHtltbAverage)
            {
                lbSelectable.Tag = DataType.Average;
            }
            if (PluginDatabase.PluginSettings.UseHtltbMedian)
            {
                lbSelectable.Tag = DataType.Median;
            }
            if (PluginDatabase.PluginSettings.UseHtltbRushed)
            {
                lbSelectable.Tag = DataType.Rushed;
            }
            if (PluginDatabase.PluginSettings.UseHtltbLeisure)
            {
                lbSelectable.Tag = DataType.Leisure;
            }

            if (PART_VndbSpeedType.Items.Count > 0)
            {
                // Default VNDB speed corresponds to the current global data type selection.
                var initialType = lbSelectable.Tag is DataType dt ? dt : DataType.Average;
                SelectVndbSpeedByDataType(initialType);
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
            bool isVndb = (bool)PART_Vndb.IsChecked;

            if (isVndb && Item != null)
            {
                Item.ApplyVndbSpeedSelection(GetSelectedVndbDataType());
            }

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
            lbSelectable.Tag = isVndb ? GetSelectedVndbDataType() : lbSelectable.Tag;

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
                        var hlResults = await PluginDatabase.HowLongToBeatApi.SearchTwoMethod(gameSearch, gamePlatform, includeExtendedTimes: true);
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

        private DataType GetSelectedVndbDataType()
        {
            if (PART_VndbSpeedType?.SelectedItem is ComboBoxItem cbi && cbi.Tag is DataType dataType)
            {
                return dataType;
            }

            return DataType.Average;
        }

        private void SelectVndbSpeedByDataType(DataType dataType)
        {
            foreach (var item in PART_VndbSpeedType.Items)
            {
                if (item is ComboBoxItem cbi && cbi.Tag is DataType tag && tag == dataType)
                {
                    PART_VndbSpeedType.SelectedItem = cbi;
                    return;
                }
            }

            PART_VndbSpeedType.SelectedIndex = 1; // Normal
        }

        private void PART_Vndb_Checked(object sender, RoutedEventArgs e)
        {
            bool isVndb = (bool)PART_Vndb.IsChecked;
            if (isVndb)
            {
                lbSelectable.Tag = GetSelectedVndbDataType();
            }
        }

        private void PART_VndbSpeedType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((bool)PART_Vndb.IsChecked)
            {
                lbSelectable.Tag = GetSelectedVndbDataType();
            }
        }

        /// <summary>
        /// Show or not the ToolTip.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBlock_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var textBlock = sender as TextBlock;
            if (textBlock == null) return;

            var toolTip = textBlock.ToolTip as ToolTip;
            if (toolTip == null) return;

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

        private void PART_Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source != PART_Tabs) return;
            bool manual = PART_Tabs.SelectedItem == PART_TabManual;
            ButtonSelect.Visibility = manual ? Visibility.Collapsed : Visibility.Visible;
            ButtonConfirmManual.Visibility = manual ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ButtonOpenOnWebsite_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var query = Uri.EscapeDataString(GameContext.Name ?? string.Empty);
                var url = "https://howlongtobeat.com/?q=" + query;
                var psi = new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true };
                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private static long ParseHoursMinutesToSeconds(string hours, string minutes)
        {
            double.TryParse(hours?.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var h);
            double.TryParse(minutes?.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var m);
            var total = (long)(h * 3600 + m * 60);
            return total < 0 ? 0 : total;
        }

        private void ButtonAddManual_Click(object sender, RoutedEventArgs e)
        {
            var mainStory = ParseHoursMinutesToSeconds(PART_ManualMainStoryH.Text, PART_ManualMainStoryM.Text);
            var mainExtra = ParseHoursMinutesToSeconds(PART_ManualMainExtraH.Text, PART_ManualMainExtraM.Text);
            var completionist = ParseHoursMinutesToSeconds(PART_ManualCompletionistH.Text, PART_ManualCompletionistM.Text);
            var solo = ParseHoursMinutesToSeconds(PART_ManualSoloH.Text, PART_ManualSoloM.Text);
            var coOp = ParseHoursMinutesToSeconds(PART_ManualCoOpH.Text, PART_ManualCoOpM.Text);

            var hasMulti = solo > 0 || coOp > 0;

            var resolvedType = hasMulti && mainStory == 0 && mainExtra == 0 && completionist == 0
                ? GameType.Multi
                : GameType.Game;

            var manualEntry = new HltbDataUser
            {
                Name = GameContext.Name,
                Id = PART_ManualHltbId.Text.Trim(),
                GameType = resolvedType,
                GameHltbData = new HltbData
                {
                    GameType = resolvedType,
                    MainStoryClassic = mainStory,
                    MainExtraClassic = mainExtra,
                    CompletionistClassic = completionist,
                    SoloClassic = solo,
                    CoOpClassic = coOp
                }
            };

            GameHowLongToBeat = HowLongToBeat.PluginDatabase.GetDefault(GameContext);
            GameHowLongToBeat.Items = new List<HltbDataUser> { manualEntry };

            ((Window)Parent).Close();
        }
    }
}