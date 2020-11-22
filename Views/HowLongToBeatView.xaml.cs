using HowLongToBeat.Models;
using HowLongToBeat.Services;
using HowLongToBeat.Views.Interfaces;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginCommon;
using PluginCommon.PlayniteResources.Converters;
using System.Linq;
using System.Windows;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System;

namespace HowLongToBeat.Views
{
    /// <summary>
    /// Logique d'interaction pour HowLongToBeat.xaml
    /// </summary>
    public partial class HowLongToBeatView : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        public string CoverImage { get; set; }
        public string GameName { get; set; }
        public string HltbName { get; set; }

        public string PlaytimeFormat { get; set; }

        private GameHowLongToBeat _gameHowLongToBeat { get; set; }


        public HowLongToBeatView(IPlayniteAPI PlayniteApi, HowLongToBeatSettings settings, GameHowLongToBeat gameHowLongToBeat)
        {
            _gameHowLongToBeat = gameHowLongToBeat;

            InitializeComponent();


            if (_gameHowLongToBeat.HasData)
            {
                HltbDataUser gameData = _gameHowLongToBeat.Items.First();

                if (string.IsNullOrEmpty(_gameHowLongToBeat.CoverImage))
                {
                    CoverImage = gameData.GameHltbData.UrlImg;
                }
                else
                {
                    CoverImage = gameData.GameHltbData.UrlImg;
                    if (!settings.ShowHltbImg)
                    {
                        CoverImage = PlayniteApi.Database.GetFullFilePath(_gameHowLongToBeat.CoverImage);
                    }
                }
                GameName = _gameHowLongToBeat.Name;
                HltbName = resources.GetString("LOCSourceLabel") + ": " + gameData.GameHltbData.Name;


                int ElIndicator = 0;

                Hltb_El1.Visibility = Visibility.Hidden;
                Hltb_El1_Color.Visibility = Visibility.Hidden;
                Hltb_El2.Visibility = Visibility.Hidden;
                Hltb_El2_Color.Visibility = Visibility.Hidden;
                Hltb_El3.Visibility = Visibility.Hidden;
                Hltb_El3_Color.Visibility = Visibility.Hidden;

                if (gameData.GameHltbData.MainStory != 0)
                {
                    ElIndicator += 1;
                    SetDataInView(ElIndicator, resources.GetString("LOCHowLongToBeatMainStory"), gameData.GameHltbData.MainStoryFormat);
                }

                if (gameData.GameHltbData.MainExtra != 0)
                {
                    ElIndicator += 1;
                    SetDataInView(ElIndicator, resources.GetString("LOCHowLongToBeatMainExtra"), gameData.GameHltbData.MainExtraFormat);
                }

                if (gameData.GameHltbData.Completionist != 0)
                {
                    ElIndicator += 1;
                    SetDataInView(ElIndicator, resources.GetString("LOCHowLongToBeatCompletionist"), gameData.GameHltbData.CompletionistFormat);
                }

                if (gameData.GameHltbData.Solo != 0)
                {
                    ElIndicator += 1;
                    SetDataInView(ElIndicator, resources.GetString("LOCHowLongToBeatSolo"), gameData.GameHltbData.SoloFormat);
                }

                if (gameData.GameHltbData.CoOp != 0)
                {
                    ElIndicator += 1;
                    SetDataInView(ElIndicator, resources.GetString("LOCHowLongToBeatCoOp"), gameData.GameHltbData.CoOpFormat);
                }

                if (gameData.GameHltbData.Vs != 0)
                {
                    ElIndicator += 1;
                    SetDataInView(ElIndicator, resources.GetString("LOCHowLongToBeatVs"), gameData.GameHltbData.VsFormat);
                }


                Hltb_El1_Color.Background = new SolidColorBrush(settings.ColorFirst);
                Hltb_El2_Color.Background = new SolidColorBrush(settings.ColorSecond);
                Hltb_El3_Color.Background = new SolidColorBrush(settings.ColorThird);


                LongToTimePlayedConverter converter = new LongToTimePlayedConverter();
                PlaytimeFormat = (string)converter.Convert((long)_gameHowLongToBeat.Playtime, null, null, CultureInfo.CurrentCulture);

                HltbProgressBar hltbProgressBar = new HltbProgressBar(settings);
                hltbProgressBar.SetHltbData(_gameHowLongToBeat);
                PART_HltbProgressBar.Children.Add(hltbProgressBar);
            }

            // Set Binding data
            DataContext = this;
        }

        private void SetDataInView(int ElIndicator, string ElText, string ElData)
        {
            switch (ElIndicator)
            {
                case 1:
                    Hltb_El1.Text = ElText;
                    Hltb_El1_Data.Text = ElData;
                    Hltb_El1.Visibility = Visibility.Visible;
                    Hltb_El1_Color.Visibility = Visibility.Visible;
                    break;

                case 2:
                    Hltb_El2.Text = ElText;
                    Hltb_El2_Data.Text = ElData;
                    Hltb_El2.Visibility = Visibility.Visible;
                    Hltb_El2_Color.Visibility = Visibility.Visible;
                    break;

                case 3:
                    Hltb_El3.Text = ElText;
                    Hltb_El3_Data.Text = ElData;
                    Hltb_El3.Visibility = Visibility.Visible;
                    Hltb_El3_Color.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void ButtonWeb_Click(object sender, RoutedEventArgs e)
        {
            if (!_gameHowLongToBeat.GetData().GameHltbData.Url.IsNullOrEmpty())
            {
                Process.Start(_gameHowLongToBeat.GetData().GameHltbData.Url);
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
}
