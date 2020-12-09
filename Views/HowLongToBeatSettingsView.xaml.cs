using Playnite.SDK;
using PluginCommon;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Newtonsoft.Json;
using HowLongToBeat.Services;

namespace HowLongToBeat.Views
{
    public partial class HowLongToBeatSettingsView : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        private IPlayniteAPI _PlayniteApi;
        private string _PluginUserDataPath;

        private HowLongToBeatDatabase PluginDatabase = HowLongToBeat.PluginDatabase;

        private TextBlock tbControl;

        public static Color ColorFirst = Brushes.DarkCyan.Color;
        public static Color ColorSecond = Brushes.RoyalBlue.Color;
        public static Color ColorThird = Brushes.ForestGreen.Color;
        public static Color ColorFirstMulti = Brushes.DarkCyan.Color;
        public static Color ColorSecondMulti = Brushes.RoyalBlue.Color;
        public static Color ColorThirdMulti = Brushes.ForestGreen.Color;


        public HowLongToBeatSettingsView(IPlayniteAPI PlayniteApi, string PluginUserDataPath, HowLongToBeatSettings settings)
        {
            _PlayniteApi = PlayniteApi;
            _PluginUserDataPath = PluginUserDataPath;

            InitializeComponent();

            CheckAuthenticate();

            PART_SelectorColorPicker.OnlySimpleColor = true;
            PART_SelectorColorPicker.IsSimpleColor = true;

            ColorFirst = settings.ColorFirst;
            tbColorFirst.Background = new SolidColorBrush(settings.ColorFirst);
            ColorSecond = settings.ColorSecond;
            tbColorSecond.Background = new SolidColorBrush(settings.ColorSecond);
            ColorThird = settings.ColorThird;
            tbColorThird.Background = new SolidColorBrush(settings.ColorThird);

            ColorFirstMulti = settings.ColorFirstMulti;
            tbColorFirstMulti.Background = new SolidColorBrush(settings.ColorFirstMulti);
            ColorSecondMulti = settings.ColorSecondMulti;
            tbColorSecondMulti.Background = new SolidColorBrush(settings.ColorSecondMulti);
            ColorThirdMulti = settings.ColorThirdMulti;
            tbColorThirdMulti.Background = new SolidColorBrush(settings.ColorThirdMulti);

            spSettings.Visibility = Visibility.Visible;

            PluginDatabase.howLongToBeatClient.PropertyChanged += OnPropertyChanged;
        }


        #region Appareance
        private void Checkbox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox cb = (CheckBox)sender;

            if ((cb.Name == "HltB_IntegrationButton") && (bool)cb.IsChecked)
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
        #endregion


        #region Tag
        private void ButtonAddTag_Click(object sender, RoutedEventArgs e)
        {
            HowLongToBeat.PluginDatabase.AddTagAllGame();
        }

        private void ButtonRemoveTag_Click(object sender, RoutedEventArgs e)
        {
            HowLongToBeat.PluginDatabase.RemoveTagAllGame();
        }
        #endregion


        #region Database
        private void BtAddData_Click(object sender, RoutedEventArgs e)
        {
            HowLongToBeat.PluginDatabase.GetAllDatas();
        }

        private void BtRemoveData_Click(object sender, RoutedEventArgs e)
        {
            HowLongToBeat.PluginDatabase.ClearDatabase();
        }
        #endregion


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
                Common.LogError(ex, "HowLongToBeat");
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
                        ColorFirst = Brushes.DarkCyan.Color;
                        break;

                    case "2":
                        tbControl.Background = Brushes.RoyalBlue;
                        ColorSecond = Brushes.RoyalBlue.Color;
                        break;

                    case "3":
                        tbControl.Background = Brushes.ForestGreen;
                        ColorThird = Brushes.ForestGreen.Color;
                        break;

                    case "4":
                        tbControl.Background = Brushes.DarkCyan;
                        ColorFirstMulti = Brushes.DarkCyan.Color;
                        break;

                    case "5":
                        tbControl.Background = Brushes.RoyalBlue;
                        ColorSecondMulti = Brushes.RoyalBlue.Color;
                        break;

                    case "6":
                        tbControl.Background = Brushes.ForestGreen;
                        ColorThirdMulti = Brushes.ForestGreen.Color;
                        break;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "HowLongToBeat");
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

                        case "4":
                            ColorFirstMulti = color;
                            break;

                        case "5":
                            ColorSecondMulti = color;
                            break;

                        case "6":
                            ColorThirdMulti = color;
                            break;
                    }
                }
            }
            else
            {
                logger.Warn("HowLongToBeat - One control is undefined");
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


        #region Authenticate
        private void CheckAuthenticate()
        {
            PART_LbUserLogin.Visibility = Visibility.Collapsed;
            PART_LbAuthenticate.Content = resources.GetString("LOCLoginChecking");

            var task = Task.Run(() => PluginDatabase.howLongToBeatClient.GetIsUserLoggedIn());
        }

        private void PART_BtAuthenticate_Click(object sender, RoutedEventArgs e)
        {
            PART_LbUserLogin.Visibility = Visibility.Collapsed;
            var task = Task.Run(() => PluginDatabase.howLongToBeatClient.Login());
        }


        protected void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(() => {
                if ((bool)PluginDatabase.howLongToBeatClient.IsConnected)
                {
                    PART_LbAuthenticate.Content = resources.GetString("LOCLoggedIn");
                    PART_LbUserLogin.Visibility = Visibility.Visible;
                    PART_LbUserLogin.Content = resources.GetString("LOCGOGUseAccountName") + " " + PluginDatabase.howLongToBeatClient.UserLogin;
                }
                else
                {
                    PART_LbAuthenticate.Content = resources.GetString("LOCNotLoggedIn");
                }
            }));
        }
        #endregion
    }
}
