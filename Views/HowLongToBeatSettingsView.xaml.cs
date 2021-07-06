using Playnite.SDK;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HowLongToBeat.Services;
using CommonPluginsShared;
using HowLongToBeat.Models;

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
            HowLongToBeat.PluginDatabase.GetSelectData();
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
                Common.LogError(ex, false);
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
                Common.LogError(ex, false);
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
                logger.Warn("One control is undefined");
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
            PART_LbAuthenticate.Content = resources.GetString("LOCCommonLoginChecking");

            var task = Task.Run(() => PluginDatabase.howLongToBeatClient.GetIsUserLoggedIn());
        }

        private void PART_BtAuthenticate_Click(object sender, RoutedEventArgs e)
        {
            PART_LbUserLogin.Visibility = Visibility.Collapsed;
            var task = Task.Run(() => {
                PluginDatabase.howLongToBeatClient.Login();
            });
        }


        protected void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(() => {
                if ((bool)PluginDatabase.howLongToBeatClient.IsConnected)
                {
                    PART_LbAuthenticate.Content = resources.GetString("LOCCommonLoggedIn");
                    PART_LbUserLogin.Visibility = Visibility.Visible;

                    string UserLogin = PluginDatabase.howLongToBeatClient.UserLogin;
                    if (UserLogin.IsNullOrEmpty())
                    {
                        UserLogin = PluginDatabase.Database.UserHltbData.Login;
                    }

                    PART_LbUserLogin.Content = resources.GetString("LOCCommonAccountName") + " " + UserLogin;
                }
                else
                {
                    PART_LbAuthenticate.Content = resources.GetString("LOCCommonNotLoggedIn");
                }
            }));
        }
        #endregion


        private void CbDefaultSorting_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string index = ((ComboBoxItem)cbDefaultSorting.SelectedItem).Tag.ToString();
            switch(index)
            {
                case "0":
                    PluginDatabase.PluginSettings.Settings.TitleListSort = TitleListSort.GameName;
                    break;
                case "1":
                    PluginDatabase.PluginSettings.Settings.TitleListSort = TitleListSort.Platform;
                    break;
                case "2":
                    PluginDatabase.PluginSettings.Settings.TitleListSort = TitleListSort.Completion;
                    break;
                case "3":
                    PluginDatabase.PluginSettings.Settings.TitleListSort = TitleListSort.CurrentTime;
                    break;
            }
        }
    }
}
