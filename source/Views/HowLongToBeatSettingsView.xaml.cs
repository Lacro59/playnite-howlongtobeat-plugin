using Playnite.SDK;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HowLongToBeat.Services;
using CommonPluginsShared;
using HowLongToBeat.Models;
using CommonPluginsShared.Models;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;
using Playnite.SDK.Models;

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

        public static SolidColorBrush ThumbSolidColorBrush;
        public static ThemeLinearGradient ThumbLinearGradient;

        public static SolidColorBrush FirstColorBrush;
        public static ThemeLinearGradient FirstLinearGradient;
        public static SolidColorBrush SecondColorBrush;
        public static ThemeLinearGradient SecondLinearGradient;
        public static SolidColorBrush ThirdColorBrush;
        public static ThemeLinearGradient ThirdLinearGradient;

        public static SolidColorBrush FirstMultiColorBrush;
        public static ThemeLinearGradient FirstMultiLinearGradient;
        public static SolidColorBrush SecondMultiColorBrush;
        public static ThemeLinearGradient SecondMultiLinearGradient;
        public static SolidColorBrush ThirdMultiColorBrush;
        public static ThemeLinearGradient ThirdMultiLinearGradient;

        public static List<HltbPlatformMatch> PlatformMatches = new List<HltbPlatformMatch>();


        public HowLongToBeatSettingsView(IPlayniteAPI PlayniteApi, string PluginUserDataPath, HowLongToBeatSettings settings)
        {
            _PlayniteApi = PlayniteApi;
            _PluginUserDataPath = PluginUserDataPath;

            InitializeComponent();

            CheckAuthenticate();

            SetPlatforms(settings);

            PART_SelectorColorPicker.OnlySimpleColor = false;


            ThumbSolidColorBrush = settings.ThumbSolidColorBrush;
            ThumbLinearGradient = settings.ThumbLinearGradient;
            if (ThumbSolidColorBrush != null)
            {
                tbThumb.Background = ThumbSolidColorBrush;
            }
            else
            {
                tbThumb.Background = ThumbLinearGradient.ToLinearGradientBrush;
            }


            FirstColorBrush = settings.FirstColorBrush;
            FirstLinearGradient = settings.FirstLinearGradient;
            if (FirstColorBrush != null)
            {
                tbColorFirst.Background = FirstColorBrush;
            }
            else
            {
                tbColorFirst.Background = FirstLinearGradient.ToLinearGradientBrush;
            }

            SecondColorBrush = settings.SecondColorBrush;
            SecondLinearGradient = settings.SecondLinearGradient;
            if (SecondColorBrush != null)
            {
                tbColorSecond.Background = SecondColorBrush;
            }
            else
            {
                tbColorSecond.Background = SecondLinearGradient.ToLinearGradientBrush;
            }

            ThirdColorBrush = settings.ThirdColorBrush;
            ThirdLinearGradient = settings.ThirdLinearGradient;
            if (ThirdColorBrush != null)
            {
                tbColorThird.Background = ThirdColorBrush;
            }
            else
            {
                tbColorThird.Background = ThirdLinearGradient.ToLinearGradientBrush;
            }


            FirstMultiColorBrush = settings.FirstMultiColorBrush;
            FirstMultiLinearGradient = settings.FirstMultiLinearGradient;
            if (FirstMultiColorBrush != null)
            {
                tbColorFirstMulti.Background = FirstMultiColorBrush;
            }
            else
            {
                tbColorFirstMulti.Background = FirstMultiLinearGradient.ToLinearGradientBrush;
            }

            SecondMultiColorBrush = settings.SecondMultiColorBrush;
            SecondMultiLinearGradient = settings.SecondMultiLinearGradient;
            if (SecondMultiColorBrush != null)
            {
                tbColorSecondMulti.Background = SecondMultiColorBrush;
            }
            else
            {
                tbColorSecondMulti.Background = SecondMultiLinearGradient.ToLinearGradientBrush;
            }

            ThirdMultiColorBrush = settings.ThirdMultiColorBrush;
            ThirdMultiLinearGradient = settings.ThirdMultiLinearGradient;
            if (ThirdMultiColorBrush != null)
            {
                tbColorThirdMulti.Background = ThirdMultiColorBrush;
            }
            else
            {
                tbColorThirdMulti.Background = ThirdMultiLinearGradient.ToLinearGradientBrush;
            }
            

            spSettings.Visibility = Visibility.Visible;

            PluginDatabase.howLongToBeatClient.PropertyChanged += OnPropertyChanged;


            PART_TTB.Source = BitmapExtensions.BitmapFromFile(Path.Combine(PluginDatabase.Paths.PluginPath, "Resources", "ttb.png"));
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
                    PART_SelectorColorPicker.IsSimpleColor = true;

                    Color color = ((SolidColorBrush)tbControl.Background).Color;
                    PART_SelectorColorPicker.SetColors(color);
                }
                if (tbControl.Background is LinearGradientBrush)
                {
                    PART_SelectorColorPicker.IsSimpleColor = false;

                    LinearGradientBrush linearGradientBrush = (LinearGradientBrush)tbControl.Background;
                    PART_SelectorColorPicker.SetColors(linearGradientBrush);
                }

                PART_SelectorColor.Visibility = Visibility.Visible;
                spSettings.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private void BtRestore_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TextBlock tbControl = ((StackPanel)((FrameworkElement)sender).Parent).Children.OfType<TextBlock>().FirstOrDefault();

                switch ((string)((Button)sender).Tag)
                {
                    case "0":
                        if (ResourceProvider.GetResource("NormalBrush") is LinearGradientBrush)
                        {
                            tbThumb.Background = (LinearGradientBrush)ResourceProvider.GetResource("NormalBrush");
                            ThumbSolidColorBrush = null;
                            ThumbLinearGradient = ThemeLinearGradient.ToThemeLinearGradient((LinearGradientBrush)ResourceProvider.GetResource("NormalBrush"));
                        }
                        else
                        {
                            tbThumb.Background = (SolidColorBrush)ResourceProvider.GetResource("NormalBrush");
                            ThumbSolidColorBrush = (SolidColorBrush)ResourceProvider.GetResource("NormalBrush");
                            ThumbLinearGradient = null;
                        }

                        break;

                    case "1":
                        tbControl.Background = Brushes.DarkCyan;
                        FirstColorBrush = new SolidColorBrush(Brushes.DarkCyan.Color);
                        FirstLinearGradient = null;
                        break;

                    case "2":
                        tbControl.Background = Brushes.RoyalBlue;
                        SecondColorBrush = new SolidColorBrush(Brushes.RoyalBlue.Color);
                        SecondLinearGradient = null;
                        break;

                    case "3":
                        tbControl.Background = Brushes.ForestGreen;
                        ThirdColorBrush = new SolidColorBrush(Brushes.ForestGreen.Color);
                        ThirdLinearGradient = null;
                        break;

                    case "4":
                        tbControl.Background = Brushes.DarkCyan;
                        FirstMultiColorBrush = new SolidColorBrush(Brushes.DarkCyan.Color);
                        FirstMultiLinearGradient = null;
                        break;

                    case "5":
                        tbControl.Background = Brushes.RoyalBlue;
                        SecondMultiColorBrush = new SolidColorBrush(Brushes.RoyalBlue.Color);
                        SecondMultiLinearGradient = null;
                        break;

                    case "6":
                        tbControl.Background = Brushes.ForestGreen;
                        ThirdMultiColorBrush = new SolidColorBrush(Brushes.ForestGreen.Color);
                        ThirdMultiLinearGradient = null;
                        break;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
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
                        case "0":
                            ThumbSolidColorBrush = new SolidColorBrush(color);
                            ThumbLinearGradient = null;
                            break;

                        case "1":
                            FirstColorBrush = new SolidColorBrush(color);
                            FirstLinearGradient = null;
                            break;

                        case "2":
                            SecondColorBrush = new SolidColorBrush(color);
                            SecondLinearGradient = null;
                            break;

                        case "3":
                            ThirdColorBrush = new SolidColorBrush(color);
                            ThirdLinearGradient = null;
                            break;

                        case "4":
                            FirstMultiColorBrush = new SolidColorBrush(color);
                            FirstMultiLinearGradient = null;
                            break;

                        case "5":
                            SecondMultiColorBrush = new SolidColorBrush(color);
                            SecondMultiLinearGradient = null;
                            break;

                        case "6":
                            ThirdMultiColorBrush = new SolidColorBrush(color);
                            ThirdMultiLinearGradient = null;
                            break;
                    }
                }
                else
                {
                    tbControl.Background = PART_SelectorColorPicker.GetLinearGradientBrush();

                    switch ((string)tbControl.Tag)
                    {
                        case "0":
                            ThumbSolidColorBrush = null;
                            ThumbLinearGradient = ThemeLinearGradient.ToThemeLinearGradient(PART_SelectorColorPicker.GetLinearGradientBrush());
                            break;

                        case "1":
                            FirstColorBrush = null;
                            FirstLinearGradient = ThemeLinearGradient.ToThemeLinearGradient(PART_SelectorColorPicker.GetLinearGradientBrush());
                            break;

                        case "2":
                            SecondColorBrush = null;
                            SecondLinearGradient = ThemeLinearGradient.ToThemeLinearGradient(PART_SelectorColorPicker.GetLinearGradientBrush());
                            break;

                        case "3":
                            ThirdColorBrush = null;
                            ThirdLinearGradient = ThemeLinearGradient.ToThemeLinearGradient(PART_SelectorColorPicker.GetLinearGradientBrush());
                            break;

                        case "4":
                            FirstMultiColorBrush = null;
                            FirstMultiLinearGradient = ThemeLinearGradient.ToThemeLinearGradient(PART_SelectorColorPicker.GetLinearGradientBrush());
                            break;

                        case "5":
                            SecondMultiColorBrush = null;
                            SecondMultiLinearGradient = ThemeLinearGradient.ToThemeLinearGradient(PART_SelectorColorPicker.GetLinearGradientBrush());
                            break;

                        case "6":
                            ThirdMultiColorBrush = null;
                            ThirdMultiLinearGradient = ThemeLinearGradient.ToThemeLinearGradient(PART_SelectorColorPicker.GetLinearGradientBrush());
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
            this.Dispatcher.Invoke(new Action(() => 
            {
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


        private void SetPlatforms(HowLongToBeatSettings settings) {
            List<Platform> platforms = PluginDatabase.PlayniteApi.Database
                    .Platforms.Distinct().OrderBy(x => x.Name).ToList();

            settings.Platforms.RemoveAll(m => !platforms.Contains(m.Platform));
            platforms.Where(p => settings.Platforms.Exists(m => m.Platform == p))
                    .ForEach(p => settings.Platforms.Add(
                            new HltbPlatformMatch { Platform = p }));
                    
            PART_GridPlatformsList.ItemsSource = settings.Platforms;
        }

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
                case "4":
                    PluginDatabase.PluginSettings.Settings.TitleListSort = TitleListSort.LastUpdate;
                    break;
            }
        }
    }

}
