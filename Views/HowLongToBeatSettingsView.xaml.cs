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
using Newtonsoft.Json;

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
            HowLongToBeat.PluginDatabase.AddTagAllGame();
        }

        private void ButtonRemoveTag_Click(object sender, RoutedEventArgs e)
        {
            HowLongToBeat.PluginDatabase.RemoveTagAllGame();
        }

        private void BtAddData_Click(object sender, RoutedEventArgs e)
        {
            HowLongToBeat.PluginDatabase.GetAllDatas();
        }

        private void BtRemoveData_Click(object sender, RoutedEventArgs e)
        {
            HowLongToBeat.PluginDatabase.ClearDatabase();
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
