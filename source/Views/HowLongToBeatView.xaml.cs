using HowLongToBeat.Models;
using HowLongToBeat.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using System.Linq;
using System.Windows;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System;
using CommonPluginsPlaynite.Converters;
using HowLongToBeat.Controls;
using System.Collections.Generic;

namespace HowLongToBeat.Views
{
    /// <summary>
    /// Logique d'interaction pour HowLongToBeatView.xaml
    /// </summary>
    public partial class HowLongToBeatView : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        private HowLongToBeatDatabase PluginDatabase = HowLongToBeat.PluginDatabase;

        private GameHowLongToBeat _gameHowLongToBeat { get; set; }


        public HowLongToBeatView(GameHowLongToBeat gameHowLongToBeat)
        {
            _gameHowLongToBeat = gameHowLongToBeat;

            InitializeComponent();
            DataContext = new HowLongToBeatViewData();


            HltbDataUser gameData = _gameHowLongToBeat?.Items?.FirstOrDefault();

            if (gameData == null || gameData.Name.IsNullOrEmpty())
            {
                return;
            }


            if (_gameHowLongToBeat.HasData || _gameHowLongToBeat.HasDataEmpty)
            {
                ((HowLongToBeatViewData)DataContext).CoverImage = gameData.UrlImg;

                if (!PluginDatabase.PluginSettings.Settings.ShowHltbImg)
                {
                    if (!_gameHowLongToBeat.CoverImage.IsNullOrEmpty())
                    {
                        ((HowLongToBeatViewData)DataContext).CoverImage = PluginDatabase.PlayniteApi.Database.GetFullFilePath(_gameHowLongToBeat.CoverImage);
                    }
                }

                ((HowLongToBeatViewData)DataContext).GameName = _gameHowLongToBeat.Name;
                ((HowLongToBeatViewData)DataContext).HltbName = gameData.Name;
            }

            if (_gameHowLongToBeat.HasData)
            {
                int ElIndicator = 0;

                Hltb_El1.Visibility = Visibility.Hidden;
                Hltb_El1_Color.Visibility = Visibility.Hidden;
                Hltb_El2.Visibility = Visibility.Hidden;
                Hltb_El2_Color.Visibility = Visibility.Hidden;
                Hltb_El3.Visibility = Visibility.Hidden;
                Hltb_El3_Color.Visibility = Visibility.Hidden;

                TitleList titleList = PluginDatabase.GetUserHltbData(_gameHowLongToBeat.GetData().Id);

                if (gameData.GameHltbData.MainStory != 0)
                {
                    ElIndicator += 1;
                    SetDataInView(ElIndicator, resources.GetString("LOCHowLongToBeatMainStory"), gameData.GameHltbData.MainStoryFormat, (titleList != null) ? titleList.HltbUserData.MainStoryFormat : string.Empty);
                    SetColor(ElIndicator, PluginDatabase.PluginSettings.Settings.ColorFirst);
                }

                if (gameData.GameHltbData.MainExtra != 0)
                {
                    ElIndicator += 1;
                    SetDataInView(ElIndicator, resources.GetString("LOCHowLongToBeatMainExtra"), gameData.GameHltbData.MainExtraFormat, (titleList != null) ? titleList.HltbUserData.MainExtraFormat : string.Empty);
                    SetColor(ElIndicator, PluginDatabase.PluginSettings.Settings.ColorSecond);
                }

                if (gameData.GameHltbData.Completionist != 0)
                {
                    ElIndicator += 1;
                    SetDataInView(ElIndicator, resources.GetString("LOCHowLongToBeatCompletionist"), gameData.GameHltbData.CompletionistFormat, (titleList != null) ? titleList.HltbUserData.CompletionistFormat : string.Empty);
                    SetColor(ElIndicator, PluginDatabase.PluginSettings.Settings.ColorThird);
                }

                if (gameData.GameHltbData.Solo != 0)
                {
                    ElIndicator += 1;
                    SetDataInView(ElIndicator, resources.GetString("LOCHowLongToBeatSolo"), gameData.GameHltbData.SoloFormat, (titleList != null) ? titleList.HltbUserData.SoloFormat : string.Empty);
                    SetColor(ElIndicator, PluginDatabase.PluginSettings.Settings.ColorFirstMulti);
                }

                if (gameData.GameHltbData.CoOp != 0)
                {
                    ElIndicator += 1;
                    SetDataInView(ElIndicator, resources.GetString("LOCHowLongToBeatCoOp"), gameData.GameHltbData.CoOpFormat, (titleList != null) ? titleList.HltbUserData.CoOpFormat : string.Empty);
                    SetColor(ElIndicator, PluginDatabase.PluginSettings.Settings.ColorSecondMulti);
                }

                if (gameData.GameHltbData.Vs != 0)
                {
                    ElIndicator += 1;
                    SetDataInView(ElIndicator, resources.GetString("LOCHowLongToBeatVs"), gameData.GameHltbData.VsFormat, (titleList != null) ? titleList.HltbUserData.VsFormat : string.Empty);
                    SetColor(ElIndicator, PluginDatabase.PluginSettings.Settings.ColorThirdMulti);
                }


                ((HowLongToBeatViewData)DataContext).GameContext = PluginDatabase.PlayniteApi.Database.Games.Get(_gameHowLongToBeat.Id);
            }

            PlayTimeToStringConverter converter = new PlayTimeToStringConverter();
            ((HowLongToBeatViewData)DataContext).PlaytimeFormat = (string)converter.Convert((long)_gameHowLongToBeat.Playtime, null, null, CultureInfo.CurrentCulture);
        }


        private void SetColor(int ElIndicator, Color color)
        {
            switch (ElIndicator)
            {
                case 1:
                    Hltb_El1_Color.Background = new SolidColorBrush(color);
                    break;

                case 2:
                    Hltb_El2_Color.Background = new SolidColorBrush(color);
                    break;

                case 3:
                    Hltb_El3_Color.Background = new SolidColorBrush(color);
                    break;
            }
        }

        private void SetDataInView(int ElIndicator, string ElText, string ElData, string ElDataUser)
        {
            switch (ElIndicator)
            {
                case 1:
                    Hltb_El1.Text = ElText;
                    Hltb_El1_Data.Text = ElData;
                    Hltb_El1_DataUser.Text = ElDataUser;
                    Hltb_El1.Visibility = Visibility.Visible;
                    Hltb_El1_Color.Visibility = Visibility.Visible;
                    break;

                case 2:
                    Hltb_El2.Text = ElText;
                    Hltb_El2_Data.Text = ElData;
                    Hltb_El2_DataUser.Text = ElDataUser;
                    Hltb_El2.Visibility = Visibility.Visible;
                    Hltb_El2_Color.Visibility = Visibility.Visible;
                    break;

                case 3:
                    Hltb_El3.Text = ElText;
                    Hltb_El3_Data.Text = ElData;
                    Hltb_El3_DataUser.Text = ElDataUser;
                    Hltb_El3.Visibility = Visibility.Visible;
                    Hltb_El3_Color.Visibility = Visibility.Visible;
                    break;
            }
        }


        private void PART_SourceLink_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!_gameHowLongToBeat.GetData().Url.IsNullOrEmpty())
            {
                Process.Start(_gameHowLongToBeat.GetData().Url);
            }
        }


        private void ButtonDelete_Click(object sender, RoutedEventArgs e)
        {
            HowLongToBeat.PluginDatabase.Remove(_gameHowLongToBeat.Id);
            ((Window)this.Parent).Close();
        }


        private void TextBlock_MouseEnter(object sender, MouseEventArgs e)
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
    }


    public class HowLongToBeatViewData : ObservableObject
    {
        private string _CoverImage { get; set; } = string.Empty;
        public string CoverImage
        {
            get => _CoverImage;
            set
            {
                _CoverImage = value;
                OnPropertyChanged();
            }
        }

        private string _GameName { get; set; } = string.Empty;
        public string GameName
        {
            get => _GameName;
            set
            {
                _GameName = value;
                OnPropertyChanged();
            }
        }

        private string _HltbName { get; set; } = string.Empty;
        public string HltbName
        {
            get => _HltbName;
            set
            {
                _HltbName = value;
                OnPropertyChanged();
            }
        }

        private string _PlaytimeFormat { get; set; } = string.Empty;
        public string PlaytimeFormat
        {
            get => _PlaytimeFormat;
            set
            {
                _PlaytimeFormat = value;
                OnPropertyChanged();
            }
        }

        private Game _GameContext { get; set; }
        public Game GameContext
        {
            get => _GameContext;
            set
            {
                _GameContext = value;
                OnPropertyChanged();
            }
        }
    }
}
