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
using CommonPluginsShared.Converters;
using System.Globalization;
using CommonPluginsShared.Extensions;

namespace HowLongToBeat.Views
{
    /// <summary>
    /// Logique d'interaction pour HowLongToBeatView.xaml
    /// </summary>
    public partial class HowLongToBeatView : UserControl
    {
        private HowLongToBeatDatabase PluginDatabase => HowLongToBeat.PluginDatabase;
        private GameHowLongToBeat GameHowLongToBeat { get; set; }


        public HowLongToBeatView(GameHowLongToBeat gameHowLongToBeat)
        {
            GameHowLongToBeat = gameHowLongToBeat;

            InitializeComponent();
            DataContext = new HowLongToBeatViewData();

            Init(gameHowLongToBeat);


            switch (gameHowLongToBeat.GetData().GameHltbData.DataType)
            {
                case DataType.Classic:
                    Hltb_DataType.Text = ResourceProvider.GetString("LOCHltbSelectDataTypeClassic");
                    break;
                case DataType.Average:
                    Hltb_DataType.Text = ResourceProvider.GetString("LOCHltbSelectDataTypeAverage");
                    break;
                case DataType.Median:
                    Hltb_DataType.Text = ResourceProvider.GetString("LOCHltbSelectDataTypeMedian");
                    break;
                case DataType.Rushed:
                    Hltb_DataType.Text = ResourceProvider.GetString("LOCHltbSelectDataTypeRushed");
                    break;
                case DataType.Leisure:
                    Hltb_DataType.Text = ResourceProvider.GetString("LOCHltbSelectDataTypeLeisure");
                    break;
                default:
                    Hltb_DataType.Text = ResourceProvider.GetString("LOCHltbSelectDataTypeLeisure");
                    break;
            }
        }


        private void Init(GameHowLongToBeat gameHowLongToBeat)
        {
            Hltb_El1_Data.Text = string.Empty;
            Hltb_El1_DataUser.Text = string.Empty;
            Hltb_El1_DataUser_1.Text = string.Empty;
            Hltb_El1_DataUser_2.Text = string.Empty;
            Hltb_El1_DataUser_3.Text = string.Empty;

            Hltb_El2_Data.Text = string.Empty;
            Hltb_El2_DataUser.Text = string.Empty;
            Hltb_El2_DataUser_1.Text = string.Empty;
            Hltb_El2_DataUser_2.Text = string.Empty;
            Hltb_El2_DataUser_3.Text = string.Empty;

            Hltb_El3_Data.Text = string.Empty;
            Hltb_El3_DataUser.Text = string.Empty;
            Hltb_El3_DataUser_1.Text = string.Empty;
            Hltb_El3_DataUser_2.Text = string.Empty;
            Hltb_El3_DataUser_3.Text = string.Empty;


            HltbDataUser gameData = gameHowLongToBeat?.Items?.FirstOrDefault();

            if (gameData == null || gameData.Name.IsNullOrEmpty())
            {
                return;
            }

            if (gameHowLongToBeat.HasData)
            {
                ((HowLongToBeatViewData)DataContext).CoverImage = gameData.UrlImg;

                if (!PluginDatabase.PluginSettings.Settings.ShowHltbImg)
                {
                    if (!gameHowLongToBeat.CoverImage.IsNullOrEmpty())
                    {
                        ((HowLongToBeatViewData)DataContext).CoverImage = API.Instance.Database.GetFullFilePath(gameHowLongToBeat.CoverImage);
                    }
                }

                ((HowLongToBeatViewData)DataContext).GameContext = API.Instance.Database.Games.Get(gameHowLongToBeat.Id);
                ((HowLongToBeatViewData)DataContext).SourceLink = gameHowLongToBeat.SourceLink;
            }

            var Items = gameHowLongToBeat.Items;
            var a = Items?.Count > 0 && Items?.First() != null && !Items.First().IsEmpty;
            var aa = Items?.Count > 0;
            var aaa = Items?.First() != null;
            var aaaa = !Items.First().IsEmpty;

            if (!gameHowLongToBeat.HasData)
            {
                PART_GridProgressBar.Visibility = Visibility.Hidden;
                PART_TextBlock.Visibility = Visibility.Hidden;
            }

            if (gameHowLongToBeat.HasData)
            {
                Hltb_El1.Visibility = Visibility.Hidden;
                Hltb_El1_Color.Visibility = Visibility.Hidden;
                Hltb_El2.Visibility = Visibility.Hidden;
                Hltb_El2_Color.Visibility = Visibility.Hidden;
                Hltb_El3.Visibility = Visibility.Hidden;
                Hltb_El3_Color.Visibility = Visibility.Hidden;

                List<TitleList> titleLists = PluginDatabase.GetUserHltbDataAll(gameHowLongToBeat.GetData().Id);
                TitleList titleList = null;
                if (titleLists != null && titleLists.Count > 0)
                {
                    for (int idx = 0; idx < titleLists.Count; idx++)
                    {
                        int ElIndicator = 0;
                        titleList = titleLists[idx];

                        RadioButton Hltb_0 = new RadioButton();
                        switch (idx)
                        {
                            case 0:
                                Hltb_0 = Hltb_El0;
                                break;
                            case 1:
                                Hltb_0 = Hltb_El0_1;
                                break;
                            case 2:
                                Hltb_0 = Hltb_El0_2;
                                break;
                            case 3:
                                Hltb_0 = Hltb_El0_3;
                                break;
                            default:
                                break;
                        }


                        if (!gameHowLongToBeat.UserGameId.IsNullOrEmpty() && gameHowLongToBeat.UserGameId.IsEqual(titleList.UserGameId))
                        {
                            Hltb_0.IsChecked = true;
                        }


                        LocalDateConverter localDateConverter = new LocalDateConverter();
                        Hltb_0.Content = localDateConverter.Convert(titleList.LastUpdate, null, null, CultureInfo.CurrentCulture).ToString();
                        Hltb_0.Tag = titleList.UserGameId;
                        Hltb_0.Visibility = Visibility.Visible;

                        if (idx == 0)
                        {
                            Hltb_El0_tb.Text = localDateConverter.Convert(titleList.LastUpdate, null, null, CultureInfo.CurrentCulture).ToString();
                            Hltb_El0_tb.Tag = titleList.UserGameId;
                            Hltb_El0_tb.Visibility = Visibility.Visible;
                            Hltb_0.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            Hltb_El0.Visibility = Visibility.Visible;
                            Hltb_El0_tb.Visibility = Visibility.Collapsed;
                        }

                        if (gameData.GameType != GameType.Multi)
                        {
                            if (gameData.GameHltbData.MainStory != 0 || titleList.HltbUserData?.MainStory != 0)
                            {
                                ElIndicator += 1;
                                SetDataInView(ElIndicator, ResourceProvider.GetString("LOCHowLongToBeatMainStory"), gameData.GameHltbData.MainStoryFormat, (titleList != null) ? titleList.HltbUserData.MainStoryFormat : string.Empty, idx);
                                SetColor(ElIndicator, PluginDatabase.PluginSettings.Settings.ColorFirst.Color);
                            }

                            if (gameData.GameHltbData.MainExtra != 0 || titleList.HltbUserData?.MainExtra != 0)
                            {
                                ElIndicator += 1;
                                SetDataInView(ElIndicator, ResourceProvider.GetString("LOCHowLongToBeatMainExtra"), gameData.GameHltbData.MainExtraFormat, (titleList != null) ? titleList.HltbUserData.MainExtraFormat : string.Empty, idx);
                                SetColor(ElIndicator, PluginDatabase.PluginSettings.Settings.ColorSecond.Color);
                            }

                            if (gameData.GameHltbData.Completionist != 0 || titleList.HltbUserData?.Completionist != 0)
                            {
                                ElIndicator += 1;
                                SetDataInView(ElIndicator, ResourceProvider.GetString("LOCHowLongToBeatCompletionist"), gameData.GameHltbData.CompletionistFormat, (titleList != null) ? titleList.HltbUserData.CompletionistFormat : string.Empty, idx);
                                SetColor(ElIndicator, PluginDatabase.PluginSettings.Settings.ColorThird.Color);
                            }
                        }
                        else
                        {
                            if (gameData.GameHltbData.Solo != 0 || titleList.HltbUserData?.Solo != 0)
                            {
                                ElIndicator += 1;
                                SetDataInView(ElIndicator, ResourceProvider.GetString("LOCHowLongToBeatSolo"), gameData.GameHltbData.SoloFormat, (titleList != null) ? titleList.HltbUserData.SoloFormat : string.Empty, idx);
                                SetColor(ElIndicator, PluginDatabase.PluginSettings.Settings.ColorFirstMulti.Color);
                            }

                            if (gameData.GameHltbData.CoOp != 0 || titleList.HltbUserData?.CoOp != 0)
                            {
                                ElIndicator += 1;
                                SetDataInView(ElIndicator, ResourceProvider.GetString("LOCHowLongToBeatCoOp"), gameData.GameHltbData.CoOpFormat, (titleList != null) ? titleList.HltbUserData.CoOpFormat : string.Empty, idx);
                            }

                            if (gameData.GameHltbData.Vs != 0 || titleList.HltbUserData?.Vs != 0)
                            {
                                ElIndicator += 1;
                                SetDataInView(ElIndicator, ResourceProvider.GetString("LOCHowLongToBeatVs"), gameData.GameHltbData.VsFormat, (titleList != null) ? titleList.HltbUserData.VsFormat : string.Empty, idx);
                            }
                        }
                    }
                }
                else
                {
                    Hltb_El0.Visibility = Visibility.Collapsed;
                    int ElIndicator = 0;

                    if (gameData.GameType != GameType.Multi)
                    {
                        if (gameData.GameHltbData.MainStory != 0)
                        {
                            ElIndicator += 1;
                            SetDataInView(ElIndicator, ResourceProvider.GetString("LOCHowLongToBeatMainStory"), gameData.GameHltbData.MainStoryFormat, (titleList != null) ? titleList.HltbUserData.MainStoryFormat : string.Empty, 0);
                            SetColor(ElIndicator, PluginDatabase.PluginSettings.Settings.ColorFirst.Color);
                        }

                        if (gameData.GameHltbData.MainExtra != 0)
                        {
                            ElIndicator += 1;
                            SetDataInView(ElIndicator, ResourceProvider.GetString("LOCHowLongToBeatMainExtra"), gameData.GameHltbData.MainExtraFormat, (titleList != null) ? titleList.HltbUserData.MainExtraFormat : string.Empty, 0);
                            SetColor(ElIndicator, PluginDatabase.PluginSettings.Settings.ColorSecond.Color);
                        }

                        if (gameData.GameHltbData.Completionist != 0)
                        {
                            ElIndicator += 1;
                            SetDataInView(ElIndicator, ResourceProvider.GetString("LOCHowLongToBeatCompletionist"), gameData.GameHltbData.CompletionistFormat, (titleList != null) ? titleList.HltbUserData.CompletionistFormat : string.Empty, 0);
                            SetColor(ElIndicator, PluginDatabase.PluginSettings.Settings.ColorThird.Color);
                        }
                    }
                    else
                    {
                        if (gameData.GameHltbData.Solo != 0)
                        {
                            ElIndicator += 1;
                            SetDataInView(ElIndicator, ResourceProvider.GetString("LOCHowLongToBeatSolo"), gameData.GameHltbData.SoloFormat, (titleList != null) ? titleList.HltbUserData.SoloFormat : string.Empty, 0);
                            SetColor(ElIndicator, PluginDatabase.PluginSettings.Settings.ColorFirstMulti.Color);
                        }

                        if (gameData.GameHltbData.CoOp != 0)
                        {
                            ElIndicator += 1;
                            SetDataInView(ElIndicator, ResourceProvider.GetString("LOCHowLongToBeatCoOp"), gameData.GameHltbData.CoOpFormat, (titleList != null) ? titleList.HltbUserData.CoOpFormat : string.Empty, 0);
                        }

                        if (gameData.GameHltbData.Vs != 0)
                        {
                            ElIndicator += 1;
                            SetDataInView(ElIndicator, ResourceProvider.GetString("LOCHowLongToBeatVs"), gameData.GameHltbData.VsFormat, (titleList != null) ? titleList.HltbUserData.VsFormat : string.Empty, 0);
                        }
                    }
                }

                long rt = (gameHowLongToBeat.GetData()?.GameHltbData?.TimeToBeat ?? 0) - (long)gameHowLongToBeat.Game.Playtime;
                PlayTimeToStringConverterWithZero playTimeToStringConverterWithZero = new PlayTimeToStringConverterWithZero();
                TbRemainingTime.Text = rt > 0 ? (string)playTimeToStringConverterWithZero.Convert(rt, null, null, CultureInfo.CurrentCulture) : string.Empty;
            }
            else
            {
                Hltb_El0.Visibility = Visibility.Collapsed;
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

                default:
                    break;
            }
        }

        private void SetDataInView(int ElIndicator, string ElText, string ElData, string ElDataUser, int idx)
        {
            TextBlock Hltb_1 = new TextBlock();
            TextBlock Hltb_2 = new TextBlock();
            TextBlock Hltb_3 = new TextBlock();

            switch (idx)
            {
                case 0:
                    Hltb_1 = Hltb_El1_DataUser;
                    Hltb_2 = Hltb_El2_DataUser;
                    Hltb_3 = Hltb_El3_DataUser;
                    break;

                case 1:
                    Hltb_1 = Hltb_El1_DataUser_1;
                    Hltb_2 = Hltb_El2_DataUser_1;
                    Hltb_3 = Hltb_El3_DataUser_1;
                    break;

                case 2:
                    Hltb_1 = Hltb_El1_DataUser_2;
                    Hltb_2 = Hltb_El2_DataUser_2;
                    Hltb_3 = Hltb_El3_DataUser_2;
                    break;

                case 3:
                    Hltb_1 = Hltb_El1_DataUser_3;
                    Hltb_2 = Hltb_El2_DataUser_3;
                    Hltb_3 = Hltb_El3_DataUser_3;
                    break;

                default:
                    break;
            }


            switch (ElIndicator)
            {
                case 1:
                    Hltb_El1.Text = ElText;
                    Hltb_El1_Data.Text = ElData;
                    Hltb_1.Text = ElDataUser;

                    Hltb_1.Visibility = Visibility.Visible;
                    Hltb_El1.Visibility = Visibility.Visible;
                    Hltb_El1_Color.Visibility = Visibility.Visible;
                    break;

                case 2:
                    Hltb_El2.Text = ElText;
                    Hltb_El2_Data.Text = ElData;
                    Hltb_2.Text = ElDataUser;

                    Hltb_2.Visibility = Visibility.Visible;
                    Hltb_El2.Visibility = Visibility.Visible;
                    Hltb_El2_Color.Visibility = Visibility.Visible;
                    break;

                case 3:
                    Hltb_El3.Text = ElText;
                    Hltb_El3_Data.Text = ElData;
                    Hltb_3.Text = ElDataUser;

                    Hltb_3.Visibility = Visibility.Visible;
                    Hltb_El3.Visibility = Visibility.Visible;
                    Hltb_El3_Color.Visibility = Visibility.Visible;
                    break;

                default:
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
                if (Id != default)
                {
                    _ = HowLongToBeat.PluginDatabase.Remove(Id);
                    ((Window)this.Parent).Close();
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private void ButtonRefresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Guid Id = (Guid)((FrameworkElement)sender).Tag;
                if (Id != default)
                {
                    HowLongToBeat.PluginDatabase.Refresh(Id);
                    GameHowLongToBeat gameHowLongToBeat = PluginDatabase.Get(Id, true);
                    Init(gameHowLongToBeat);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }


        private void TextBlock_MouseEnter(object sender, MouseEventArgs e)
        {
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

            ((ToolTip)((TextBlock)sender).ToolTip).Visibility = formattedText.Width > textBlock.DesiredSize.Width ? Visibility.Visible : Visibility.Hidden;
        }


        private void Hltb_El0_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                RadioButton rb = sender as RadioButton;
                if (rb.Tag != null)
                {
                    GameHowLongToBeat.UserGameId = rb.Tag.ToString();
                    PluginDatabase.Update(GameHowLongToBeat);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }
    }


    public class HowLongToBeatViewData : ObservableObject
    {
        private Game _GameContext;
        public Game GameContext { get => _GameContext; set => SetValue(ref _GameContext, value); }

        private SourceLink _SourceLink;
        public SourceLink SourceLink { get => _SourceLink; set => SetValue(ref _SourceLink, value); }

        private string _CoverImage = string.Empty;
        public string CoverImage { get => _CoverImage; set => SetValue(ref _CoverImage, value); }
    }
}
