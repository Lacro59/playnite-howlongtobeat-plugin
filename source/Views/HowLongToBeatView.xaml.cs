using HowLongToBeat.Models;
using HowLongToBeat.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using System.Linq;
using System.Windows;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System;
using System.Collections.Generic;
using System.Windows.Documents;
using CommonPluginsShared.Models;
using CommonPluginsShared;

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


        public HowLongToBeatView(GameHowLongToBeat gameHowLongToBeat)
        {
            InitializeComponent();
            DataContext = new HowLongToBeatViewData();


            HltbDataUser gameData = gameHowLongToBeat?.Items?.FirstOrDefault();

            if (gameData == null || gameData.Name.IsNullOrEmpty())
            {
                return;
            }

            if (gameHowLongToBeat.HasData || gameHowLongToBeat.HasDataEmpty)
            {
                ((HowLongToBeatViewData)DataContext).CoverImage = gameData.UrlImg;

                if (!PluginDatabase.PluginSettings.Settings.ShowHltbImg)
                {
                    if (!gameHowLongToBeat.CoverImage.IsNullOrEmpty())
                    {
                        ((HowLongToBeatViewData)DataContext).CoverImage = PluginDatabase.PlayniteApi.Database.GetFullFilePath(gameHowLongToBeat.CoverImage);
                    }
                }

                ((HowLongToBeatViewData)DataContext).GameContext = PluginDatabase.PlayniteApi.Database.Games.Get(gameHowLongToBeat.Id);
                ((HowLongToBeatViewData)DataContext).SourceLink = gameHowLongToBeat.SourceLink;
            }

            if (!gameHowLongToBeat.HasData || gameHowLongToBeat.HasDataEmpty)
            {
                PART_GridProgressBar.Visibility = Visibility.Hidden;
                PART_TextBlock.Visibility = Visibility.Hidden;
            }

            if (gameHowLongToBeat.HasData)
            {
                int ElIndicator = 0;

                Hltb_El1.Visibility = Visibility.Hidden;
                Hltb_El1_Color.Visibility = Visibility.Hidden;
                Hltb_El2.Visibility = Visibility.Hidden;
                Hltb_El2_Color.Visibility = Visibility.Hidden;
                Hltb_El3.Visibility = Visibility.Hidden;
                Hltb_El3_Color.Visibility = Visibility.Hidden;

                TitleList titleList = PluginDatabase.GetUserHltbData(gameHowLongToBeat.GetData().Id);

                if (gameData.GameHltbData.MainStory != 0)
                {
                    ElIndicator += 1;
                    SetDataInView(ElIndicator, resources.GetString("LOCHowLongToBeatMainStory"), gameData.GameHltbData.MainStoryFormat, (titleList != null) ? titleList.HltbUserData.MainStoryFormat : string.Empty);
                    SetColor(ElIndicator, PluginDatabase.PluginSettings.Settings.ColorFirst.Color);
                }

                if (gameData.GameHltbData.MainExtra != 0)
                {
                    ElIndicator += 1;
                    SetDataInView(ElIndicator, resources.GetString("LOCHowLongToBeatMainExtra"), gameData.GameHltbData.MainExtraFormat, (titleList != null) ? titleList.HltbUserData.MainExtraFormat : string.Empty);
                    SetColor(ElIndicator, PluginDatabase.PluginSettings.Settings.ColorSecond.Color);
                }

                if (gameData.GameHltbData.Completionist != 0)
                {
                    ElIndicator += 1;
                    SetDataInView(ElIndicator, resources.GetString("LOCHowLongToBeatCompletionist"), gameData.GameHltbData.CompletionistFormat, (titleList != null) ? titleList.HltbUserData.CompletionistFormat : string.Empty);
                    SetColor(ElIndicator, PluginDatabase.PluginSettings.Settings.ColorThird.Color);
                }

                if (gameData.GameHltbData.Solo != 0)
                {
                    ElIndicator += 1;
                    SetDataInView(ElIndicator, resources.GetString("LOCHowLongToBeatSolo"), gameData.GameHltbData.SoloFormat, (titleList != null) ? titleList.HltbUserData.SoloFormat : string.Empty);
                    SetColor(ElIndicator, PluginDatabase.PluginSettings.Settings.ColorFirstMulti.Color);
                }

                if (gameData.GameHltbData.CoOp != 0)
                {
                    ElIndicator += 1;
                    SetDataInView(ElIndicator, resources.GetString("LOCHowLongToBeatCoOp"), gameData.GameHltbData.CoOpFormat, (titleList != null) ? titleList.HltbUserData.CoOpFormat : string.Empty);
                    SetColor(ElIndicator, PluginDatabase.PluginSettings.Settings.ColorSecondMulti.Color);
                }

                if (gameData.GameHltbData.Vs != 0)
                {
                    ElIndicator += 1;
                    SetDataInView(ElIndicator, resources.GetString("LOCHowLongToBeatVs"), gameData.GameHltbData.VsFormat, (titleList != null) ? titleList.HltbUserData.VsFormat : string.Empty);
                    SetColor(ElIndicator, PluginDatabase.PluginSettings.Settings.ColorThirdMulti.Color);
                }
            }
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
            string Url = (string)((Hyperlink)sender).Tag;
            if (!Url.IsNullOrEmpty())
            {
                Process.Start(((string)((Hyperlink)sender).Tag));
            }
        }


        private void ButtonDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Guid Id = (Guid)((FrameworkElement)sender).Tag;
                if (Id != default(Guid))
                {
                    HowLongToBeat.PluginDatabase.Remove(Id);
                    ((Window)this.Parent).Close();
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, "HowLongToBeat");
            }
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

        private SourceLink _SourceLink { get; set; }
        public SourceLink SourceLink
        {
            get => _SourceLink;
            set
            {
                _SourceLink = value;
                OnPropertyChanged();
            }
        }

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
    }
}
