﻿using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Interfaces;
using HowLongToBeat.Models;
using HowLongToBeat.Models.Enumerables;
using HowLongToBeat.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace HowLongToBeat.Controls
{
    /// <summary>
    /// Logique d'interaction pour PluginProgressBar.xaml
    /// </summary>
    public partial class PluginProgressBar : PluginUserControlExtend
    {
        // TODO Rewrite this control to resolve latency
        private static HowLongToBeatDatabase PluginDatabase => HowLongToBeat.PluginDatabase;
        internal override IPluginDatabase pluginDatabase => PluginDatabase;

        private PluginProgressBarDataContext ControlDataContext = new PluginProgressBarDataContext();
        internal override IDataContext controlDataContext
        {
            get => ControlDataContext;
            set => ControlDataContext = (PluginProgressBarDataContext)controlDataContext;
        }

        private bool ShowUserData = true;


        public PluginProgressBar()
        {
            InitializeComponent();
            DataContext = ControlDataContext;

            _ = Task.Run(() =>
            {
                // Wait extension database are loaded
                _ = System.Threading.SpinWait.SpinUntil(() => PluginDatabase.IsLoaded, -1);

                _ = Dispatcher.BeginInvoke((Action)delegate
                {
                    PluginDatabase.PluginSettings.PropertyChanged += PluginSettings_PropertyChanged;
                    PluginDatabase.Database.ItemUpdated += Database_ItemUpdated;
                    PluginDatabase.Database.ItemCollectionChanged += Database_ItemCollectionChanged;
                    API.Instance.Database.Games.ItemUpdated += Games_ItemUpdated;

                    // Apply settings
                    PluginSettings_PropertyChanged(null, null);
                });
            });
        }


        public override void SetDefaultDataContext()
        {
            ShowUserData = PluginDatabase.PluginSettings.Settings.ProgressBarShowTimeUser;

            bool IsActivated = PluginDatabase.PluginSettings.Settings.EnableIntegrationProgressBar;
            bool TextAboveVisibility = PluginDatabase.PluginSettings.Settings.ProgressBarShowTime ? PluginDatabase.PluginSettings.Settings.ProgressBarShowTimeAbove : false; 
            bool TextInsideVisibility = PluginDatabase.PluginSettings.Settings.ProgressBarShowTime ? PluginDatabase.PluginSettings.Settings.ProgressBarShowTimeInterior : false;
            bool TextBelowVisibility = PluginDatabase.PluginSettings.Settings.ProgressBarShowTime ? PluginDatabase.PluginSettings.Settings.ProgressBarShowTimeBelow : false;
            if (IgnoreSettings)
            {
                IsActivated = true;
                TextAboveVisibility = false;
                TextInsideVisibility = true;
                TextBelowVisibility = false;
                ShowUserData = true;
            }

            ControlDataContext.IsActivated = IsActivated;
            ControlDataContext.ShowToolTip = PluginDatabase.PluginSettings.Settings.ProgressBarShowToolTip;

            ControlDataContext.TextAboveVisibility = TextAboveVisibility;
            ControlDataContext.TextInsideVisibility = TextInsideVisibility;
            ControlDataContext.TextBelowVisibility = TextBelowVisibility;

            ControlDataContext.PlaytimeValue = 0;
            ControlDataContext.MaxValue = 0;

            ControlDataContext.ProgressBarFirstValue = 0;
            ControlDataContext.ProgressBarFirstVisibility = Visibility.Collapsed;
            ControlDataContext.ToolTipFirst = string.Empty;

            ControlDataContext.ProgressBarSecondValue = 0;
            ControlDataContext.ProgressBarSecondVisibility = Visibility.Collapsed;
            ControlDataContext.ToolTipSecond = string.Empty;

            ControlDataContext.ProgressBarThirdValue = 0;
            ControlDataContext.ProgressBarThirdVisibility = Visibility.Collapsed;
            ControlDataContext.ToolTipThird = string.Empty;

            ControlDataContext.SliderFirstValue = 0;
            ControlDataContext.SliderFirstVisibility = Visibility.Collapsed;
            ControlDataContext.ThumbFirst = null;

            ControlDataContext.SliderSecondValue = 0;
            ControlDataContext.SliderSecondVisibility = Visibility.Collapsed;
            ControlDataContext.ThumbSecond = null;

            ControlDataContext.SliderThirdValue = 0;
            ControlDataContext.SliderThirdVisibility = Visibility.Collapsed;
            ControlDataContext.ThumbThird = null;

            if (PluginDatabase.PluginSettings.Settings.ThumbSolidColorBrush == null)
            {
                ControlDataContext.ThumbColor = PluginDatabase.PluginSettings.Settings.ThumbLinearGradient.ToLinearGradientBrush;
            }
            else
            {
                ControlDataContext.ThumbColor = PluginDatabase.PluginSettings.Settings.ThumbSolidColorBrush;
            }
        }


        public override void SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
        {
            GameHowLongToBeat gameHowLongToBeat = (GameHowLongToBeat)PluginGameData;
            LoadData(gameHowLongToBeat);


            SliderPlaytime.Maximum = ControlDataContext.MaxValue;
            SliderPlaytime.Value = ControlDataContext.PlaytimeValue;


            PART_ProgressBarFirst.Value = ControlDataContext.ProgressBarFirstValue;
            PART_ProgressBarFirst.TextValue = ControlDataContext.ToolTipFirst;
            PART_ProgressBarFirst.Foreground = ControlDataContext.ThumbFirst;
            PART_ProgressBarFirst.Maximum = ControlDataContext.MaxValue;
            PART_ProgressBarFirst.TextAboveVisibility = ControlDataContext.TextAboveVisibility;
            PART_ProgressBarFirst.TextInsideVisibility = ControlDataContext.TextInsideVisibility;
            PART_ProgressBarFirst.TextBelowVisibility = ControlDataContext.TextBelowVisibility;

            PART_ProgressBarSecond.Value = ControlDataContext.ProgressBarSecondValue;
            PART_ProgressBarSecond.TextValue = ControlDataContext.ToolTipSecond;
            PART_ProgressBarSecond.Foreground = ControlDataContext.ThumbSecond;
            PART_ProgressBarSecond.Maximum = ControlDataContext.MaxValue;
            PART_ProgressBarSecond.TextAboveVisibility = ControlDataContext.TextAboveVisibility;
            PART_ProgressBarSecond.TextInsideVisibility = ControlDataContext.TextInsideVisibility;
            PART_ProgressBarSecond.TextBelowVisibility = ControlDataContext.TextBelowVisibility;

            PART_ProgressBarThird.Value = ControlDataContext.ProgressBarThirdValue;
            PART_ProgressBarThird.TextValue = ControlDataContext.ToolTipThird;
            PART_ProgressBarThird.Foreground = ControlDataContext.ThumbThird;
            PART_ProgressBarThird.Maximum = ControlDataContext.MaxValue;
            PART_ProgressBarThird.TextAboveVisibility = ControlDataContext.TextAboveVisibility;
            PART_ProgressBarThird.TextInsideVisibility = ControlDataContext.TextInsideVisibility;
            PART_ProgressBarThird.TextBelowVisibility = ControlDataContext.TextBelowVisibility;


            PartSliderFirst.ThumbFill = ControlDataContext.ThumbSecond;
            PartSliderFirst.Visibility = ControlDataContext.SliderSecondVisibility;
            PartSliderFirst.Value = ControlDataContext.SliderSecondValue;
            PartSliderFirst.Maximum = ControlDataContext.MaxValue;

            PartSliderSecond.ThumbFill = ControlDataContext.ThumbFirst;
            PartSliderSecond.Visibility = ControlDataContext.SliderFirstVisibility;
            PartSliderSecond.Value = ControlDataContext.SliderFirstValue;
            PartSliderSecond.Maximum = ControlDataContext.MaxValue;

            PartSliderThird.ThumbFill = ControlDataContext.ThumbThird;
            PartSliderThird.Visibility = ControlDataContext.SliderThirdVisibility;
            PartSliderThird.Value = ControlDataContext.SliderThirdValue;
            PartSliderThird.Maximum = ControlDataContext.MaxValue;
        }


        private void LoadData(GameHowLongToBeat gameHowLongToBeat)
        {
            try
            {
                // Definied data value in different component.
                int ElIndicator = 0;
                double MaxValue = 0;
                double MaxHltb = 0;
                ulong Playtime = gameHowLongToBeat.Playtime;
                List<ListProgressBar> listProgressBars = new List<ListProgressBar>();
                TitleList titleList = PluginDatabase.GetUserHltbDataCurrent(gameHowLongToBeat.GetData().Id, gameHowLongToBeat.UserGameId);
                dynamic color;

                if (gameHowLongToBeat.HasData)
                {
                    HltbDataUser HltbData = gameHowLongToBeat.GetData();

                    if (HltbData.GameType != GameType.Multi)
                    {
                        if ((HltbData?.GameHltbData?.MainStory != null && HltbData.GameHltbData.MainStory > 0) || (titleList?.HltbUserData?.MainStory != null && titleList?.HltbUserData?.MainStory > 0 && ShowUserData))
                        {
                            ElIndicator += 1;

                            if (HltbData.GameHltbData.MainStory > 0)
                            {
                                listProgressBars.Add(new ListProgressBar { Indicator = ElIndicator, Value = HltbData.GameHltbData.MainStory, Format = HltbData.GameHltbData.MainStoryFormat });
                            }

                            if (MaxValue < HltbData.GameHltbData.MainStory)
                            {
                                MaxValue = HltbData.GameHltbData.MainStory;
                            }

                            color = PluginDatabase.PluginSettings.Settings.FirstLinearGradient != null
                                ? (dynamic)PluginDatabase.PluginSettings.Settings.FirstLinearGradient.ToLinearGradientBrush
                                : (dynamic)PluginDatabase.PluginSettings.Settings.FirstColorBrush;

                            SetColor(ElIndicator, color);

                            // Show user hltb datas
                            if (titleList?.HltbUserData?.MainStory > 0 && ShowUserData)
                            {
                                SetUserData(ElIndicator, titleList.HltbUserData.MainStory, PluginDatabase.PluginSettings.Settings.ColorFirst.Color);
                            }
                        }

                        if ((HltbData?.GameHltbData?.MainStory != null && HltbData.GameHltbData.MainExtra > 0) || (titleList?.HltbUserData?.MainExtra != null && titleList?.HltbUserData?.MainExtra > 0 && ShowUserData))
                        {
                            ElIndicator += 1;

                            if (HltbData.GameHltbData.MainExtra > 0)
                            {
                                listProgressBars.Add(new ListProgressBar { Indicator = ElIndicator, Value = HltbData.GameHltbData.MainExtra, Format = HltbData.GameHltbData.MainExtraFormat });
                            }

                            if (MaxValue < HltbData.GameHltbData.MainExtra)
                            {
                                MaxValue = HltbData.GameHltbData.MainExtra;
                            }

                            color = PluginDatabase.PluginSettings.Settings.SecondLinearGradient != null
                                ? (dynamic)PluginDatabase.PluginSettings.Settings.SecondLinearGradient.ToLinearGradientBrush
                                : (dynamic)PluginDatabase.PluginSettings.Settings.SecondColorBrush;

                            SetColor(ElIndicator, color);

                            // Show user hltb datas
                            if (titleList?.HltbUserData?.MainExtra > 0 && ShowUserData)
                            {
                                SetUserData(ElIndicator, titleList.HltbUserData.MainExtra, PluginDatabase.PluginSettings.Settings.ColorSecond.Color);
                            }
                        }

                        if ((HltbData?.GameHltbData?.Completionist != null && HltbData.GameHltbData.Completionist != 0) || (titleList?.HltbUserData?.Completionist != null && titleList?.HltbUserData?.Completionist > 0 && ShowUserData))
                        {
                            ElIndicator += 1;

                            if (HltbData.GameHltbData.Completionist != 0)
                            {
                                listProgressBars.Add(new ListProgressBar { Indicator = ElIndicator, Value = HltbData.GameHltbData.Completionist, Format = HltbData.GameHltbData.CompletionistFormat });
                            }

                            if (MaxValue < HltbData.GameHltbData.Completionist)
                            {
                                MaxValue = HltbData.GameHltbData.Completionist;
                            }

                            color = PluginDatabase.PluginSettings.Settings.ThirdLinearGradient != null
                                ? (dynamic)PluginDatabase.PluginSettings.Settings.ThirdLinearGradient.ToLinearGradientBrush
                                : (dynamic)PluginDatabase.PluginSettings.Settings.ThirdColorBrush;

                            SetColor(ElIndicator, color);

                            // Show user hltb datas
                            if (titleList?.HltbUserData?.Completionist > 0 && ShowUserData)
                            {
                                SetUserData(ElIndicator, titleList.HltbUserData.Completionist, PluginDatabase.PluginSettings.Settings.ColorThird.Color);
                            }
                        }
                    }
                    else
                    {
                        if ((HltbData?.GameHltbData?.Solo != null && HltbData.GameHltbData.Solo != 0) || (titleList?.HltbUserData?.Solo != null && titleList?.HltbUserData?.Solo > 0 && ShowUserData))
                        {
                            ElIndicator += 1;

                            if (HltbData.GameHltbData.Solo != 0)
                            {
                                listProgressBars.Add(new ListProgressBar { Indicator = ElIndicator, Value = HltbData.GameHltbData.Solo, Format = HltbData.GameHltbData.SoloFormat });
                            }

                            if (MaxValue < HltbData.GameHltbData.Solo)
                            {
                                MaxValue = HltbData.GameHltbData.Solo;
                            }

                            color = PluginDatabase.PluginSettings.Settings.FirstMultiLinearGradient != null
                                ? (dynamic)PluginDatabase.PluginSettings.Settings.FirstMultiLinearGradient.ToLinearGradientBrush
                                : (dynamic)PluginDatabase.PluginSettings.Settings.FirstMultiColorBrush;

                            SetColor(ElIndicator, color);

                            // Show user hltb datas
                            if (titleList?.HltbUserData?.Solo > 0 && ShowUserData)
                            {
                                SetUserData(ElIndicator, titleList.HltbUserData.Solo, PluginDatabase.PluginSettings.Settings.ColorFirstMulti.Color);
                            }
                        }

                        if ((HltbData?.GameHltbData?.CoOp != null && HltbData.GameHltbData.CoOp != 0) || (titleList?.HltbUserData?.CoOp != null && titleList?.HltbUserData?.CoOp > 0 && ShowUserData))
                        {
                            ElIndicator += 1;

                            if (HltbData.GameHltbData.CoOp != 0)
                            {
                                listProgressBars.Add(new ListProgressBar { Indicator = ElIndicator, Value = HltbData.GameHltbData.CoOp, Format = HltbData.GameHltbData.CoOpFormat });
                            }

                            if (MaxValue < HltbData.GameHltbData.CoOp)
                            {
                                MaxValue = HltbData.GameHltbData.CoOp;
                            }

                            color = PluginDatabase.PluginSettings.Settings.SecondMultiLinearGradient != null
                                ? (dynamic)PluginDatabase.PluginSettings.Settings.SecondMultiLinearGradient.ToLinearGradientBrush
                                : (dynamic)PluginDatabase.PluginSettings.Settings.SecondMultiColorBrush;

                            SetColor(ElIndicator, color);

                            // Show user hltb datas
                            if (titleList?.HltbUserData?.CoOp > 0 && ShowUserData)
                            {
                                SetUserData(ElIndicator, titleList.HltbUserData.CoOp, PluginDatabase.PluginSettings.Settings.ColorSecondMulti.Color);
                            }
                        }

                        if ((HltbData?.GameHltbData?.Vs != null && HltbData.GameHltbData.Vs != 0) || (titleList?.HltbUserData?.Vs != null && titleList?.HltbUserData?.Vs > 0 && ShowUserData))
                        {
                            ElIndicator += 1;

                            if (HltbData.GameHltbData.Vs != 0)
                            {
                                listProgressBars.Add(new ListProgressBar { Indicator = ElIndicator, Value = HltbData.GameHltbData.Vs, Format = HltbData.GameHltbData.VsFormat });
                            }

                            if (MaxValue < HltbData.GameHltbData.Vs)
                            {
                                MaxValue = HltbData.GameHltbData.Vs;
                            }

                            color = PluginDatabase.PluginSettings.Settings.ThirdMultiLinearGradient != null
                                ? (dynamic)PluginDatabase.PluginSettings.Settings.ThirdMultiLinearGradient.ToLinearGradientBrush
                                : (dynamic)PluginDatabase.PluginSettings.Settings.ThirdMultiColorBrush;

                            SetColor(ElIndicator, color);

                            // Show user hltb datas
                            if (titleList?.HltbUserData?.Vs > 0 && ShowUserData)
                            {
                                SetUserData(ElIndicator, titleList.HltbUserData.Vs, PluginDatabase.PluginSettings.Settings.ColorThirdMulti.Color);
                            }
                        }
                    }
                }

                // Define the maxvalue for progressbar & slider
                MaxHltb = MaxValue;
                if (Playtime > MaxValue)
                {
                    MaxValue = Playtime;
                }

                // Limit MaxValue when playtime is more than MaxHltb
                long MaxPercent = (long)Math.Ceiling((double)(10 * MaxHltb / 100));
                if (MaxValue > MaxHltb + MaxPercent)
                {
                    MaxValue = MaxHltb + MaxPercent;
                }

                foreach (ListProgressBar listProgressBar in listProgressBars)
                {
                    SetDataInView(listProgressBar.Indicator, listProgressBar.Value, listProgressBar.Format);
                }

                ControlDataContext.MaxValue = MaxValue;
                ControlDataContext.PlaytimeValue = Playtime;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }


        public void SetUserData(int ElIndicator, long Value, Color color)
        {
            switch (ElIndicator)
            {
                case 1:
                    ControlDataContext.SliderFirstValue = Value;
                    ControlDataContext.SliderFirstVisibility = Visibility.Visible;
                    break;

                case 2:
                    ControlDataContext.SliderSecondValue = Value;
                    ControlDataContext.SliderSecondVisibility = Visibility.Visible;
                    break;

                case 3:
                    ControlDataContext.SliderThirdValue = Value;
                    ControlDataContext.SliderThirdVisibility = Visibility.Visible;
                    break;

                default:
                    break;
            }
        }

        private void SetColor(int ElIndicator, dynamic color)
        {
            switch (ElIndicator)
            {
                case 1:
                    ControlDataContext.ThumbFirst = color;
                    break;

                case 2:
                    ControlDataContext.ThumbSecond = color;
                    break;

                case 3:
                    ControlDataContext.ThumbThird = color;
                    break;

                default:
                    break;
            }
        }

        private void SetDataInView(int ElIndicator, long ElValue, string ElFormat)
        {
            try
            {
                switch (ElIndicator)
                {
                    case 1:
                        if (ElValue != 0)
                        {
                            ControlDataContext.ProgressBarFirstVisibility = Visibility.Visible;
                            ControlDataContext.ProgressBarFirstValue = ElValue;
                            ControlDataContext.ToolTipFirst = ElFormat;
                        }
                        break;

                    case 2:
                        if (ElValue != 0)
                        {
                            ControlDataContext.ProgressBarSecondVisibility = Visibility.Visible;
                            ControlDataContext.ProgressBarSecondValue = ElValue;
                            ControlDataContext.ToolTipSecond = ElFormat;
                        }
                        break;

                    case 3:
                        if (ElValue != 0)
                        {
                            ControlDataContext.ProgressBarThirdVisibility = Visibility.Visible;
                            ControlDataContext.ProgressBarThirdValue = ElValue;
                            ControlDataContext.ToolTipThird = ElFormat;
                        }
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Error on SetDataInView({ElIndicator}, {ElValue}, {ElFormat})");
            }
        }


        #region Events
        private void PART_ProgressBarFirst_LayoutUpdated(object sender, EventArgs e)
        {
            double width1 = PART_ProgressBarFirst.IndicatorWidth;
            double width2 = PART_ProgressBarSecond.IndicatorWidth;
            double width3 = PART_ProgressBarThird.IndicatorWidth;

            PART_ProgressBarSecond.MarginLeft = width1;
            PART_ProgressBarThird.MarginLeft = width2;

            spHltb_El1.Width = width1;
            spHltb_El2.Width = width2;
            spHltb_El3.Width = width3;
        }
        #endregion
    }


    public class PluginProgressBarDataContext : ObservableObject, IDataContext
    {
        private bool isActivated;
        public bool IsActivated { get => isActivated; set => SetValue(ref isActivated, value); }

        private bool showToolTip;
        public bool ShowToolTip { get => showToolTip; set => SetValue(ref showToolTip, value); }

        private bool textAboveVisibility;
        public bool TextAboveVisibility { get => textAboveVisibility; set => SetValue(ref textAboveVisibility, value); }

        private bool textInsideVisibility = true;
        public bool TextInsideVisibility { get => textInsideVisibility; set => SetValue(ref textInsideVisibility, value); }

        private bool textBelowVisibility;
        public bool TextBelowVisibility { get => textBelowVisibility; set => SetValue(ref textBelowVisibility, value); }

        private dynamic thumbColor = ResourceProvider.GetResource("NormalBrush");
        public dynamic ThumbColor { get => thumbColor; set => SetValue(ref thumbColor, value); }

        private dynamic thumbFirst = new SolidColorBrush(Brushes.DarkCyan.Color);
        public dynamic ThumbFirst { get => thumbFirst; set => SetValue(ref thumbFirst, value); }

        private dynamic thumbSecond = new SolidColorBrush(Brushes.RoyalBlue.Color);
        public dynamic ThumbSecond { get => thumbSecond; set => SetValue(ref thumbSecond, value); }

        private dynamic thumbThird = new SolidColorBrush(Brushes.ForestGreen.Color);
        public dynamic ThumbThird { get => thumbThird; set => SetValue(ref thumbThird, value); }

        private double playtimeValue = 75;
        public double PlaytimeValue { get => playtimeValue; set => SetValue(ref playtimeValue, value); }

        private double maxValue = 100;
        public double MaxValue { get => maxValue; set => SetValue(ref maxValue, value); }

        private double progressBarFirstValue;
        public double ProgressBarFirstValue { get => progressBarFirstValue; set => SetValue(ref progressBarFirstValue, value); }

        private Visibility progressBarFirstVisibility;
        public Visibility ProgressBarFirstVisibility { get => progressBarFirstVisibility; set => SetValue(ref progressBarFirstVisibility, value); }

        private string toolTipFirst;
        public string ToolTipFirst { get => toolTipFirst; set => SetValue(ref toolTipFirst, value); }

        private double progressBarSecondValue;
        public double ProgressBarSecondValue { get => progressBarSecondValue; set => SetValue(ref progressBarSecondValue, value); }

        private Visibility progressBarSecondVisibility;
        public Visibility ProgressBarSecondVisibility { get => progressBarSecondVisibility; set => SetValue(ref progressBarSecondVisibility, value); }

        private string toolTipSecond;
        public string ToolTipSecond { get => toolTipSecond; set => SetValue(ref toolTipSecond, value); }

        private double progressBarThirdValue;
        public double ProgressBarThirdValue { get => progressBarThirdValue; set => SetValue(ref progressBarThirdValue, value); }

        private Visibility progressBarThirdVisibility;
        public Visibility ProgressBarThirdVisibility { get => progressBarThirdVisibility; set => SetValue(ref progressBarThirdVisibility, value); }

        private string toolTipThird;
        public string ToolTipThird { get => toolTipThird; set => SetValue(ref toolTipThird, value); }

        private double sliderFirstValue;
        public double SliderFirstValue { get => sliderFirstValue; set => SetValue(ref sliderFirstValue, value); }

        private Visibility sliderFirstVisibility;
        public Visibility SliderFirstVisibility { get => sliderFirstVisibility; set => SetValue(ref sliderFirstVisibility, value); }

        private double sliderSecondValue;
        public double SliderSecondValue { get => sliderSecondValue; set => SetValue(ref sliderSecondValue, value); }

        private Visibility sliderSecondVisibility;
        public Visibility SliderSecondVisibility { get => sliderSecondVisibility; set => SetValue(ref sliderSecondVisibility, value); }

        private double sliderThirdValue;
        public double SliderThirdValue { get => sliderThirdValue; set => SetValue(ref sliderThirdValue, value); }

        private Visibility sliderThirdVisibility;
        public Visibility SliderThirdVisibility { get => sliderThirdVisibility; set => SetValue(ref sliderThirdVisibility, value); }
    }


    internal class ListProgressBar
    {
        public int Indicator { get; set; }
        public long Value { get; set; }
        public string Format { get; set; }
    }
}
