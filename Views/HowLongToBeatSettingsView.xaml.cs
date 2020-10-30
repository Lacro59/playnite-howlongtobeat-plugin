using HowLongToBeat.Models;
using HowLongToBeat.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginCommon;
using PluginCommon.PlayniteResources;
using PluginCommon.PlayniteResources.API;
using PluginCommon.PlayniteResources.Common;
using PluginCommon.PlayniteResources.Converters;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HowLongToBeat.Views
{
    public partial class HowLongToBeatSettingsView : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        private IPlayniteAPI _PlayniteApi;
        private string _PluginUserDataPath;

        public static bool WithoutMessage = false;
        public static CancellationTokenSource tokenSource;
        private CancellationToken ct;

        private TextBlock tbControl;
        public static Color ColorFirst = Brushes.DarkCyan.Color;
        public static Color ColorSecond = Brushes.RoyalBlue.Color;
        public static Color ColorThird = Brushes.ForestGreen.Color;


        public HowLongToBeatSettingsView(IPlayniteAPI PlayniteApi, string PluginUserDataPath, HowLongToBeatSettings settings)
        {
            _PlayniteApi = PlayniteApi;
            _PluginUserDataPath = PluginUserDataPath;

            InitializeComponent();

            PART_SelectorColorPicker.OnlySimpleColor = true;
            PART_SelectorColorPicker.IsSimpleColor = true;

            tbColorFirst.Background = new SolidColorBrush(settings.ColorFirst);
            tbColorSecond.Background = new SolidColorBrush(settings.ColorSecond);
            tbColorThird.Background = new SolidColorBrush(settings.ColorThird);

            DataLoad.Visibility = Visibility.Collapsed;
            spSettings.Visibility = Visibility.Visible;
        }


        private void Checkbox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox cb = (CheckBox)sender;            
                
            if((cb.Name == "HltB_IntegrationButton") && (bool)cb.IsChecked)
            {
                HltB_IntegrationInCustomTheme.IsChecked = false;
            }

            if ((cb.Name == "HltB_IntegrationInDescription") && (bool)cb.IsChecked)
            {
                HltB_IntegrationInCustomTheme.IsChecked = false;
            }

            if ((cb.Name == "HltB_IntegrationInCustomTheme") && (bool)cb.IsChecked)
            {
                HltB_IntegrationButton.IsChecked = false;
                HltB_IntegrationInDescription.IsChecked = false;
            }
        }

        private void ButtonAddTag_Click(object sender, RoutedEventArgs e)
        {
            tbDataLoad.Text = resources.GetString("LOCHowLongToBeatProgressBarTag");
            pbDataLoad.IsIndeterminate = true;

            DataLoad.Visibility = Visibility.Visible;
            spSettings.Visibility = Visibility.Hidden;

            tokenSource = new CancellationTokenSource();
            ct = tokenSource.Token;

            var taskSystem = Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                foreach (Game game in _PlayniteApi.Database.Games)
                {
                    HowLongToBeatData.AddAllTag(_PlayniteApi, game, _PluginUserDataPath);

                    if (ct.IsCancellationRequested)
                    {
                        ct.ThrowIfCancellationRequested();
                    }
                }
            }, tokenSource.Token)
            .ContinueWith(antecedent =>
            {
                Application.Current.Dispatcher.Invoke(new Action(() => {
                    DataLoad.Visibility = Visibility.Collapsed;
                    spSettings.Visibility = Visibility.Visible;
                }));
            });
        }

        private void ButtonRemoveTag_Click(object sender, RoutedEventArgs e)
        {
            tbDataLoad.Text = resources.GetString("LOCHowLongToBeatProgressBarTag");
            pbDataLoad.IsIndeterminate = true;

            DataLoad.Visibility = Visibility.Visible;
            spSettings.Visibility = Visibility.Hidden;

            tokenSource = new CancellationTokenSource();
            ct = tokenSource.Token;

            var taskSystem = Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                foreach (Game game in _PlayniteApi.Database.Games)
                {
                    HowLongToBeatData.RemoveAllTag(_PlayniteApi, game);

                    if (ct.IsCancellationRequested)
                    {
                        ct.ThrowIfCancellationRequested();
                    }
                }

                HowLongToBeatData.RemoveAllTagDb(_PlayniteApi);
            }, tokenSource.Token)
            .ContinueWith(antecedent =>
            {
                Application.Current.Dispatcher.Invoke(new Action(() => {
                    DataLoad.Visibility = Visibility.Collapsed;
                    spSettings.Visibility = Visibility.Visible;
                }));
            });
        }

        private void BtAddData_Click(object sender, RoutedEventArgs e)
        {
            bool AutoAccept = (bool)cbAutoAccept.IsChecked;
            bool ShowWhenMismatch = (bool)cbShowWhenMismatch.IsChecked;
            bool EnableTag = (bool)cbEnableTag.IsChecked;

            pbDataLoad.IsIndeterminate = false;
            pbDataLoad.Minimum = 0;
            pbDataLoad.Value = 0;
            pbDataLoad.Maximum = _PlayniteApi.Database.Games.Count;

            DataLoad.Visibility = Visibility.Visible;
            spSettings.Visibility = Visibility.Hidden;

            tokenSource = new CancellationTokenSource();
            ct = tokenSource.Token;

            int TotalAdded = 0;
            int TotalAlready = 0;
            int TotalMultiFind = 0;
            int TotlaNotFind = 0;

            var taskSystem = Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                foreach (Game game in _PlayniteApi.Database.Games)
                {
                    try
                    {
                        if (!HowLongToBeatData.HaveData(game.Id, _PluginUserDataPath))
                        {
                            List<HltbData> dataSearch = new HowLongToBeatClient().Search(game.Name);

                            if (dataSearch.Count == 1 && AutoAccept)
                            {
                                HowLongToBeatData.SaveData(game.Id, dataSearch[0], _PluginUserDataPath);

                                if (EnableTag)
                                {
                                    HowLongToBeatData.AddAllTag(_PlayniteApi, game, _PluginUserDataPath);
                                }

                                TotalAdded += 1;
                            }
                            else
                            {
                                TotalMultiFind += 1;
                                if (dataSearch.Count > 0 && ShowWhenMismatch)
                                {
                                    Application.Current.Dispatcher.Invoke(new Action(() =>
                                    {
                                        string FileGameData = _PluginUserDataPath + "\\howlongtobeat\\" + game.Id.ToString() + ".json";

                                        var ViewExtension = new HowLongToBeatSelect(dataSearch, FileGameData, game.Name);
                                        Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(_PlayniteApi, resources.GetString("LOCSelection"), ViewExtension);
                                        windowExtension.ShowDialog();

                                        if (EnableTag)
                                        {
                                            HowLongToBeatData.AddAllTag(_PlayniteApi, game, _PluginUserDataPath);
                                        }
                                    }));
                                }
                                else
                                {
                                    TotlaNotFind += 1;
                                }
                            }
                        }
                        else
                        {
                            TotalAlready += 1;
                            logger.Debug($"HowLongToBeat - {game.Name}");
                        }
                        Application.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            tbDataLoad.Text = string.Format(resources.GetString("LOCHowLongToBeatProgressBar"), TotalAdded, TotalAlready, TotlaNotFind);
                            pbDataLoad.Value += 1;
                        }));
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, "HowLongToBeat", $"Error on BtAddData_Click()");
                    }

                    if (ct.IsCancellationRequested)
                    {
                        ct.ThrowIfCancellationRequested();
                    }
                }
            }, tokenSource.Token)
            .ContinueWith(antecedent =>
            {
                Application.Current.Dispatcher.Invoke(new Action(() => {
                    DataLoad.Visibility = Visibility.Collapsed;
                    spSettings.Visibility = Visibility.Visible;
                }));
            });
        }

        private void BtRemoveData_Click(object sender, RoutedEventArgs e)
        {
            tbDataLoad.Text = resources.GetString("LOCHowLongToBeatProgressBarTag");
            pbDataLoad.IsIndeterminate = true;

            DataLoad.Visibility = Visibility.Visible;
            spSettings.Visibility = Visibility.Hidden;

            tokenSource = new CancellationTokenSource();
            ct = tokenSource.Token;

            var taskSystem = Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                HowLongToBeatData.ClearAllData(_PluginUserDataPath, _PlayniteApi);

                foreach (Game game in _PlayniteApi.Database.Games)
                {
                    HowLongToBeatData.RemoveAllTag(_PlayniteApi, game);

                    if (ct.IsCancellationRequested)
                    {
                        ct.ThrowIfCancellationRequested();
                    }
                }
            }, tokenSource.Token)
            .ContinueWith(antecedent =>
            {
                Application.Current.Dispatcher.Invoke(new Action(() => {
                    DataLoad.Visibility = Visibility.Collapsed;
                    spSettings.Visibility = Visibility.Visible;
                }));
            });
        }


        #region ProgressBar color
        private void BtPickColor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                tbControl = ((StackPanel)((FrameworkElement)sender).Parent).Children.OfType<TextBlock>().FirstOrDefault();

                if (tbControl.Background is SolidColorBrush)
                {
                    Color color = ((SolidColorBrush)tbControl.Background).Color;
                    PART_SelectorColorPicker.SetColors(color);
                }

                PART_SelectorColor.Visibility = Visibility.Visible;
                spSettings.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "ThemeModifier", "Error on BtPickColor_Click()");
            }
        }

        private void BtRestore_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TextBlock tbControl = ((StackPanel)((FrameworkElement)sender).Parent).Children.OfType<TextBlock>().FirstOrDefault();

                switch ((string)((Button)sender).Tag)
                {
                    case "1":
                        tbControl.Background = Brushes.DarkCyan;
                        break;

                    case "2":
                        tbControl.Background = Brushes.RoyalBlue;
                        break;

                    case "3":
                        tbControl.Background = Brushes.ForestGreen;
                        break;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "ThemeModifier", "Error on BtRestore_Click()");
            }
        }

        private void PART_TM_ColorOK_Click(object sender, RoutedEventArgs e)
        {
            Color color = default(Color);

            if (tbControl != null)
            {
                if (PART_SelectorColorPicker.IsSimpleColor)
                {
                    color = PART_SelectorColorPicker.SimpleColor;
                    tbControl.Background = new SolidColorBrush(color);

                    switch ((string)tbControl.Tag)
                    {
                        case "1":
                            ColorFirst = color;
                            break;

                        case "2":
                            ColorSecond = color;
                            break;

                        case "3":
                            ColorThird = color;
                            break;
                    }
                }
            }
            else
            {
                logger.Warn("ThemeModifier - One control is undefined");
            }

            PART_SelectorColor.Visibility = Visibility.Collapsed;
            spSettings.Visibility = Visibility.Visible;
        }

        private void PART_TM_ColorCancel_Click(object sender, RoutedEventArgs e)
        {
            PART_SelectorColor.Visibility = Visibility.Collapsed;
            spSettings.Visibility = Visibility.Visible;
        }
        #endregion


        private void ButtonCancelTask_Click(object sender, RoutedEventArgs e)
        {
            tokenSource.Cancel();
        }
    }
}
