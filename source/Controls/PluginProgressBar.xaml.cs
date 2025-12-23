using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Interfaces;
using HowLongToBeat.Models;
using HowLongToBeat.Models.Enumerations;
using HowLongToBeat.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace HowLongToBeat.Controls
{
    /// <summary>
    /// Logique d'interaction pour PluginProgressBar.xaml
    /// </summary>
    public partial class PluginProgressBar : PluginUserControlExtend
    {
        // TODO Rewrite this control to resolve latency
        private static HowLongToBeatDatabase PluginDatabase => HowLongToBeat.PluginDatabase;
        protected override IPluginDatabase pluginDatabase => PluginDatabase;

        private PluginProgressBarDataContext ControlDataContext = new PluginProgressBarDataContext();
        protected override IDataContext controlDataContext
        {
            get => ControlDataContext;
            set => ControlDataContext = (PluginProgressBarDataContext)controlDataContext;
        }

        private bool ShowUserData = true;

        private readonly DispatcherTimer liveRefreshTimer;

        private Thumb sliderPlaytimeThumb;
        private TranslateTransform sliderPlaytimeThumbTransform;


        public PluginProgressBar()
        {
            InitializeComponent();
            DataContext = ControlDataContext;

            SliderPlaytime.Loaded += (_, __) =>
            {
                // Ensure template parts exist before we try to adjust the thumb
                Dispatcher.BeginInvoke((Action)UpdatePlaytimeThumbTransform, DispatcherPriority.Loaded);
            };
            SliderPlaytime.ValueChanged += (_, __) => UpdatePlaytimeThumbTransform();

            liveRefreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            liveRefreshTimer.Tick += LiveRefreshTimer_Tick;

            Loaded += (_, __) => UpdateLiveRefreshTimerState();
            Unloaded += (_, __) => liveRefreshTimer.Stop();
            IsVisibleChanged += (_, __) => UpdateLiveRefreshTimerState();

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


        private void UpdatePlaytimeThumbTransform()
        {
            try
            {
                if (SliderPlaytime == null)
                {
                    return;
                }

                SliderPlaytime.ApplyTemplate();

                if (sliderPlaytimeThumb == null)
                {
                    sliderPlaytimeThumb = SliderPlaytime.Template?.FindName("SliderPlaytimeThumb", SliderPlaytime) as Thumb;
                }

                if (sliderPlaytimeThumb == null)
                {
                    return;
                }

                if (sliderPlaytimeThumbTransform == null)
                {
                    sliderPlaytimeThumbTransform = new TranslateTransform();
                    sliderPlaytimeThumb.RenderTransform = sliderPlaytimeThumbTransform;
                }

                double thumbWidth = sliderPlaytimeThumb.ActualWidth;
                if (thumbWidth <= 0)
                {
                    thumbWidth = sliderPlaytimeThumb.Width;
                }

                double halfThumb = (thumbWidth > 0) ? thumbWidth / 2 : 10;
                const double endInsetPx = 1.0;

                const double epsilon = 0.000001;
                if (SliderPlaytime.Value <= SliderPlaytime.Minimum + epsilon)
                {
                    // Shift right so the left edge aligns with the bar start
                    sliderPlaytimeThumbTransform.X = halfThumb + endInsetPx;
                }
                else if (SliderPlaytime.Value >= SliderPlaytime.Maximum - epsilon)
                {
                    // Shift left so the right edge aligns with the bar end
                    sliderPlaytimeThumbTransform.X = -halfThumb - endInsetPx;
                }
                else
                {
                    sliderPlaytimeThumbTransform.X = 0;
                }
            }
            catch
            {
                // Best-effort: never crash the UI.
            }
        }


        private static string FitTimeLabel(string label, double availableWidthPx)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                return string.Empty;
            }

            // Heuristic thresholds tuned for typical Playnite font sizes.
            // If the segment is too narrow, we show a shorter label or hide it.
            if (availableWidthPx < 32)
            {
                return string.Empty;
            }

            if (availableWidthPx < 55)
            {
                // Keep only the first token (usually hours) to reduce collisions.
                var parts = label.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                return parts.Length > 0 ? parts[0] : label;
            }

            return label;
        }


        public override void GameContextChanged(Game oldContext, Game newContext)
        {
            base.GameContextChanged(oldContext, newContext);
            UpdateLiveRefreshTimerState();
        }


        private void LiveRefreshTimer_Tick(object sender, EventArgs e)
        {
            var current = GameContext;
            if (current == null)
            {
                liveRefreshTimer.Stop();
                return;
            }

            var game = API.Instance?.Database?.Games?.Get(current.Id);
            if (game?.IsRunning != true)
            {
                liveRefreshTimer.Stop();
                return;
            }

            _ = UpdateDataAsync();
        }


        private void UpdateLiveRefreshTimerState()
        {
            if (!IsLoaded || !IsVisible || GameContext == null)
            {
                liveRefreshTimer.Stop();
                return;
            }

            if (!(controlDataContext?.IsActivated ?? false) || !MustDisplay)
            {
                liveRefreshTimer.Stop();
                return;
            }

            var game = API.Instance?.Database?.Games?.Get(GameContext.Id);
            if (game?.IsRunning == true)
            {
                if (!liveRefreshTimer.IsEnabled)
                {
                    liveRefreshTimer.Start();
                }
            }
            else
            {
                liveRefreshTimer.Stop();
            }
        }


        public override void SetDefaultDataContext()
        {
            liveRefreshTimer?.Stop();

            ShowUserData = PluginDatabase.PluginSettings.Settings.ProgressBarShowTimeUser;

            bool isActivated = PluginDatabase.PluginSettings.Settings.EnableIntegrationProgressBar;
            bool textAboveVisibility = PluginDatabase.PluginSettings.Settings.ProgressBarShowTime ? PluginDatabase.PluginSettings.Settings.ProgressBarShowTimeAbove : false;
            bool textInsideVisibility = PluginDatabase.PluginSettings.Settings.ProgressBarShowTime ? PluginDatabase.PluginSettings.Settings.ProgressBarShowTimeInterior : false;
            bool textBelowVisibility = PluginDatabase.PluginSettings.Settings.ProgressBarShowTime ? PluginDatabase.PluginSettings.Settings.ProgressBarShowTimeBelow : false;
            if (IgnoreSettings)
            {
                isActivated = true;
                textAboveVisibility = false;
                textInsideVisibility = true;
                textBelowVisibility = false;
                ShowUserData = true;
            }

            ControlDataContext.IsActivated = isActivated;
            ControlDataContext.ShowToolTip = PluginDatabase.PluginSettings.Settings.ProgressBarShowToolTip;

            ControlDataContext.TextAboveVisibility = textAboveVisibility;
            ControlDataContext.TextInsideVisibility = textInsideVisibility;
            ControlDataContext.TextBelowVisibility = textBelowVisibility;

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

            ControlDataContext.ThumbColor = PluginDatabase.PluginSettings.Settings.ThumbSolidColorBrush == null
                ? (dynamic)PluginDatabase.PluginSettings.Settings.ThumbLinearGradient.ToLinearGradientBrush
                : (dynamic)PluginDatabase.PluginSettings.Settings.ThumbSolidColorBrush;
        }


        public override void SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
        {
            GameHowLongToBeat gameHowLongToBeat = (GameHowLongToBeat)PluginGameData;
            LoadData(gameHowLongToBeat);

            double ClampToMax(double value, double max)
            {
                if (max <= 0)
                {
                    return 0;
                }

                if (value < 0)
                {
                    return 0;
                }

                return value > max ? max : value;
            }


            SliderPlaytime.Maximum = ControlDataContext.MaxValue;
            SliderPlaytime.Value = ClampToMax(ControlDataContext.PlaytimeValue, ControlDataContext.MaxValue);


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


            PartSliderFirst.ThumbFill = ControlDataContext.ThumbSecondUser;
            PartSliderFirst.Visibility = ControlDataContext.SliderSecondVisibility;
            PartSliderFirst.Value = ClampToMax(ControlDataContext.SliderSecondValue, ControlDataContext.MaxValue);
            PartSliderFirst.Maximum = ControlDataContext.MaxValue;

            PartSliderSecond.ThumbFill = ControlDataContext.ThumbFirstUser;
            PartSliderSecond.Visibility = ControlDataContext.SliderFirstVisibility;
            PartSliderSecond.Value = ClampToMax(ControlDataContext.SliderFirstValue, ControlDataContext.MaxValue);
            PartSliderSecond.Maximum = ControlDataContext.MaxValue;

            PartSliderThird.ThumbFill = ControlDataContext.ThumbThirdUser;
            PartSliderThird.Visibility = ControlDataContext.SliderThirdVisibility;
            PartSliderThird.Value = ClampToMax(ControlDataContext.SliderThirdValue, ControlDataContext.MaxValue);
            PartSliderThird.Maximum = ControlDataContext.MaxValue;

            UpdateLiveRefreshTimerState();
        }


        private void LoadData(GameHowLongToBeat gameHowLongToBeat)
        {
            try
            {
                // Definied data value in different component.
                int elIndicator = 0;
                int elIndicatorUser = 0;
                double maxValue = 0;
                double maxHltb = 0;
                ulong playtime = gameHowLongToBeat.Playtime;
                List<ListProgressBar> listProgressBars = new List<ListProgressBar>();
                TitleList titleList = PluginDatabase.GetUserHltbDataCurrent(gameHowLongToBeat.GetData().Id, gameHowLongToBeat.UserGameId);
                dynamic color;

                if (gameHowLongToBeat.HasData)
                {
                    HltbDataUser HltbData = gameHowLongToBeat.GetData();

                    if (HltbData.GameType != GameType.Multi)
                    {
                        color = PluginDatabase.PluginSettings.Settings.FirstLinearGradient != null
                            ? (dynamic)PluginDatabase.PluginSettings.Settings.FirstLinearGradient.ToLinearGradientBrush
                            : (dynamic)PluginDatabase.PluginSettings.Settings.FirstColorBrush;

                        if ((IgnoreSettings || PluginDatabase.PluginSettings.Settings.ShowMainTime) && HltbData?.GameHltbData?.MainStory != null && HltbData.GameHltbData.MainStory > 0)
                        {
                            elIndicator += 1;

                            if (HltbData.GameHltbData.MainStory > 0)
                            {
                                listProgressBars.Add(new ListProgressBar { Indicator = elIndicator, Value = HltbData.GameHltbData.MainStory, Format = HltbData.GameHltbData.MainStoryFormat });
                            }

                            if (maxValue < HltbData.GameHltbData.MainStory)
                            {
                                maxValue = HltbData.GameHltbData.MainStory;
                            }

                            SetColor(elIndicator, color);
                        }
                        if (titleList?.HltbUserData?.MainStory != null && titleList?.HltbUserData?.MainStory > 0 && ShowUserData)
                        {
                            elIndicatorUser += 1;
                            SetColorUser(elIndicatorUser, color);
                            SetUserData(elIndicatorUser, titleList.HltbUserData.MainStory, PluginDatabase.PluginSettings.Settings.ColorFirst.Color);
                        }


                        color = PluginDatabase.PluginSettings.Settings.SecondLinearGradient != null
                            ? (dynamic)PluginDatabase.PluginSettings.Settings.SecondLinearGradient.ToLinearGradientBrush
                            : (dynamic)PluginDatabase.PluginSettings.Settings.SecondColorBrush;

                        if ((IgnoreSettings || PluginDatabase.PluginSettings.Settings.ShowExtraTime) && HltbData?.GameHltbData?.MainExtra != null && HltbData.GameHltbData.MainExtra > 0)
                        {
                            elIndicator += 1;

                            if (HltbData.GameHltbData.MainExtra > 0)
                            {
                                listProgressBars.Add(new ListProgressBar { Indicator = elIndicator, Value = HltbData.GameHltbData.MainExtra, Format = HltbData.GameHltbData.MainExtraFormat });
                            }

                            if (maxValue < HltbData.GameHltbData.MainExtra)
                            {
                                maxValue = HltbData.GameHltbData.MainExtra;
                            }

                            SetColor(elIndicator, color);
                        }
                        if (titleList?.HltbUserData?.MainExtra != null && titleList?.HltbUserData?.MainExtra > 0 && ShowUserData)
                        {
                            elIndicatorUser += 1;
                            SetColorUser(elIndicatorUser, color);
                            SetUserData(elIndicatorUser, titleList.HltbUserData.MainExtra, PluginDatabase.PluginSettings.Settings.ColorSecond.Color);
                        }


                        color = PluginDatabase.PluginSettings.Settings.ThirdLinearGradient != null
                            ? (dynamic)PluginDatabase.PluginSettings.Settings.ThirdLinearGradient.ToLinearGradientBrush
                            : (dynamic)PluginDatabase.PluginSettings.Settings.ThirdColorBrush;

                        if ((IgnoreSettings || PluginDatabase.PluginSettings.Settings.ShowCompletionistTime) && HltbData?.GameHltbData?.Completionist != null && HltbData.GameHltbData.Completionist != 0)
                        {
                            elIndicator += 1;

                            if (HltbData.GameHltbData.Completionist != 0)
                            {
                                listProgressBars.Add(new ListProgressBar { Indicator = elIndicator, Value = HltbData.GameHltbData.Completionist, Format = HltbData.GameHltbData.CompletionistFormat });
                            }

                            if (maxValue < HltbData.GameHltbData.Completionist)
                            {
                                maxValue = HltbData.GameHltbData.Completionist;
                            }

                            SetColor(elIndicator, color);
                        }
                        if (titleList?.HltbUserData?.Completionist != null && titleList?.HltbUserData?.Completionist > 0 && ShowUserData)
                        {
                            elIndicatorUser += 1;
                            SetColorUser(elIndicatorUser, color);
                            SetUserData(elIndicatorUser, titleList.HltbUserData.Completionist, PluginDatabase.PluginSettings.Settings.ColorThird.Color);
                        }
                    }
                    else
                    {
                        color = PluginDatabase.PluginSettings.Settings.FirstMultiLinearGradient != null
                            ? (dynamic)PluginDatabase.PluginSettings.Settings.FirstMultiLinearGradient.ToLinearGradientBrush
                            : (dynamic)PluginDatabase.PluginSettings.Settings.FirstMultiColorBrush;

                        if ((IgnoreSettings || PluginDatabase.PluginSettings.Settings.ShowSoloTime) && HltbData?.GameHltbData?.Solo != null && HltbData.GameHltbData.Solo != 0)
                        {
                            elIndicator += 1;

                            if (HltbData.GameHltbData.Solo != 0)
                            {
                                listProgressBars.Add(new ListProgressBar { Indicator = elIndicator, Value = HltbData.GameHltbData.Solo, Format = HltbData.GameHltbData.SoloFormat });
                            }

                            if (maxValue < HltbData.GameHltbData.Solo)
                            {
                                maxValue = HltbData.GameHltbData.Solo;
                            }

                            SetColor(elIndicator, color);
                        }
                        if (titleList?.HltbUserData?.Solo != null && titleList?.HltbUserData?.Solo > 0 && ShowUserData)
                        {
                            elIndicatorUser += 1;
                            SetColorUser(elIndicatorUser, color);
                            SetUserData(elIndicatorUser, titleList.HltbUserData.Solo, PluginDatabase.PluginSettings.Settings.ColorFirstMulti.Color);
                        }


                        color = PluginDatabase.PluginSettings.Settings.SecondMultiLinearGradient != null
                            ? (dynamic)PluginDatabase.PluginSettings.Settings.SecondMultiLinearGradient.ToLinearGradientBrush
                            : (dynamic)PluginDatabase.PluginSettings.Settings.SecondMultiColorBrush;

                        if ((IgnoreSettings || PluginDatabase.PluginSettings.Settings.ShowCoOpTime) && HltbData?.GameHltbData?.CoOp != null && HltbData.GameHltbData.CoOp != 0)
                        {
                            elIndicator += 1;

                            if (HltbData.GameHltbData.CoOp != 0)
                            {
                                listProgressBars.Add(new ListProgressBar { Indicator = elIndicator, Value = HltbData.GameHltbData.CoOp, Format = HltbData.GameHltbData.CoOpFormat });
                            }

                            if (maxValue < HltbData.GameHltbData.CoOp)
                            {
                                maxValue = HltbData.GameHltbData.CoOp;
                            }

                            SetColor(elIndicator, color);
                        }
                        if (titleList?.HltbUserData?.CoOp != null && titleList?.HltbUserData?.CoOp > 0 && ShowUserData)
                        {
                            elIndicatorUser += 1;
                            SetColorUser(elIndicatorUser, color);
                            SetUserData(elIndicatorUser, titleList.HltbUserData.CoOp, PluginDatabase.PluginSettings.Settings.ColorSecondMulti.Color);
                        }


                        color = PluginDatabase.PluginSettings.Settings.ThirdMultiLinearGradient != null
                            ? (dynamic)PluginDatabase.PluginSettings.Settings.ThirdMultiLinearGradient.ToLinearGradientBrush
                            : (dynamic)PluginDatabase.PluginSettings.Settings.ThirdMultiColorBrush;

                        if ((IgnoreSettings || PluginDatabase.PluginSettings.Settings.ShowVsTime) && HltbData?.GameHltbData?.Vs != null && HltbData.GameHltbData.Vs != 0)
                        {
                            elIndicator += 1;

                            if (HltbData.GameHltbData.Vs != 0)
                            {
                                listProgressBars.Add(new ListProgressBar { Indicator = elIndicator, Value = HltbData.GameHltbData.Vs, Format = HltbData.GameHltbData.VsFormat });
                            }

                            if (maxValue < HltbData.GameHltbData.Vs)
                            {
                                maxValue = HltbData.GameHltbData.Vs;
                            }

                            SetColor(elIndicator, color);
                        }
                        if (titleList?.HltbUserData?.Vs != null && titleList?.HltbUserData?.Vs > 0 && ShowUserData)
                        {
                            elIndicatorUser += 1;
                            SetColorUser(elIndicatorUser, color);
                            SetUserData(elIndicatorUser, titleList.HltbUserData.Vs, PluginDatabase.PluginSettings.Settings.ColorThirdMulti.Color);
                        }
                    }
                }

                // Define the maxvalue for progressbar & slider
                maxHltb = maxValue;
                if (playtime > maxValue)
                {
                    maxValue = playtime;
                }

                // Limit MaxValue when playtime is more than MaxHltb
                long MaxPercent = (long)Math.Ceiling((double)(10 * maxHltb / 100));
                if (maxValue > maxHltb + MaxPercent)
                {
                    maxValue = maxHltb + MaxPercent;
                }

                foreach (ListProgressBar listProgressBar in listProgressBars)
                {
                    SetDataInView(listProgressBar.Indicator, listProgressBar.Value, listProgressBar.Format);
                }

                ControlDataContext.MaxValue = maxValue;
                ControlDataContext.PlaytimeValue = playtime;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }


        public void SetUserData(int elIndicator, long value, Color color)
        {
            switch (elIndicator)
            {
                case 1:
                    ControlDataContext.SliderFirstValue = value;
                    ControlDataContext.SliderFirstVisibility = Visibility.Visible;
                    break;

                case 2:
                    ControlDataContext.SliderSecondValue = value;
                    ControlDataContext.SliderSecondVisibility = Visibility.Visible;
                    break;

                case 3:
                    ControlDataContext.SliderThirdValue = value;
                    ControlDataContext.SliderThirdVisibility = Visibility.Visible;
                    break;

                default:
                    break;
            }
        }

        private void SetColor(int elIndicator, dynamic color)
        {
            switch (elIndicator)
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

        private void SetColorUser(int elIndicator, dynamic color)
        {
            switch (elIndicator)
            {
                case 1:
                    ControlDataContext.ThumbFirstUser = color;
                    break;

                case 2:
                    ControlDataContext.ThumbSecondUser = color;
                    break;

                case 3:
                    ControlDataContext.ThumbThirdUser = color;
                    break;

                default:
                    break;
            }
        }

        private void SetDataInView(int elIndicator, long elValue, string elFormat)
        {
            try
            {
                switch (elIndicator)
                {
                    case 1:
                        if (elValue != 0)
                        {
                            ControlDataContext.ProgressBarFirstVisibility = Visibility.Visible;
                            ControlDataContext.ProgressBarFirstValue = elValue;
                            ControlDataContext.ToolTipFirst = elFormat;
                        }
                        break;

                    case 2:
                        if (elValue != 0)
                        {
                            ControlDataContext.ProgressBarSecondVisibility = Visibility.Visible;
                            ControlDataContext.ProgressBarSecondValue = elValue;
                            ControlDataContext.ToolTipSecond = elFormat;
                        }
                        break;

                    case 3:
                        if (elValue != 0)
                        {
                            ControlDataContext.ProgressBarThirdVisibility = Visibility.Visible;
                            ControlDataContext.ProgressBarThirdValue = elValue;
                            ControlDataContext.ToolTipThird = elFormat;
                        }
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Error on SetDataInView({elIndicator}, {elValue}, {elFormat})");
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

            // Prevent label overlaps when the max range is huge (segments become tiny).
            // We base available space on the *segment* width (delta between cumulative indicators).
            double seg1 = Math.Max(0, width1);
            double seg2 = Math.Max(0, width2 - width1);
            double seg3 = Math.Max(0, width3 - width2);

            try
            {
                if (ControlDataContext != null)
                {
                    PART_ProgressBarFirst.TextValue = FitTimeLabel(ControlDataContext.ToolTipFirst, seg1);
                    PART_ProgressBarSecond.TextValue = FitTimeLabel(ControlDataContext.ToolTipSecond, seg2);
                    PART_ProgressBarThird.TextValue = FitTimeLabel(ControlDataContext.ToolTipThird, seg3);
                }
            }
            catch { }
        }
        #endregion
    }


    public class PluginProgressBarDataContext : ObservableObject, IDataContext
    {
        private bool _isActivated;
        public bool IsActivated { get => _isActivated; set => SetValue(ref _isActivated, value); }

        private bool _showToolTip;
        public bool ShowToolTip { get => _showToolTip; set => SetValue(ref _showToolTip, value); }

        private bool _textAboveVisibility;
        public bool TextAboveVisibility { get => _textAboveVisibility; set => SetValue(ref _textAboveVisibility, value); }

        private bool _textInsideVisibility = true;
        public bool TextInsideVisibility { get => _textInsideVisibility; set => SetValue(ref _textInsideVisibility, value); }

        private bool _textBelowVisibility;
        public bool TextBelowVisibility { get => _textBelowVisibility; set => SetValue(ref _textBelowVisibility, value); }

        private dynamic _thumbColor = ResourceProvider.GetResource("NormalBrush");
        public dynamic ThumbColor { get => _thumbColor; set => SetValue(ref _thumbColor, value); }

        private dynamic _thumbFirst = new SolidColorBrush(Brushes.DarkCyan.Color);
        public dynamic ThumbFirst { get => _thumbFirst; set => SetValue(ref _thumbFirst, value); }

        private dynamic _thumbSecond = new SolidColorBrush(Brushes.RoyalBlue.Color);
        public dynamic ThumbSecond { get => _thumbSecond; set => SetValue(ref _thumbSecond, value); }

        private dynamic _thumbThird = new SolidColorBrush(Brushes.ForestGreen.Color);
        public dynamic ThumbThird { get => _thumbThird; set => SetValue(ref _thumbThird, value); }

        private dynamic _thumbFirstUser = new SolidColorBrush(Brushes.DarkCyan.Color);
        public dynamic ThumbFirstUser { get => _thumbFirstUser; set => SetValue(ref _thumbFirstUser, value); }

        private dynamic _thumbSecondUser = new SolidColorBrush(Brushes.RoyalBlue.Color);
        public dynamic ThumbSecondUser { get => _thumbSecondUser; set => SetValue(ref _thumbSecondUser, value); }

        private dynamic _thumbThirdUser = new SolidColorBrush(Brushes.ForestGreen.Color);
        public dynamic ThumbThirdUser { get => _thumbThirdUser; set => SetValue(ref _thumbThirdUser, value); }

        private double _playtimeValue = 75;
        public double PlaytimeValue { get => _playtimeValue; set => SetValue(ref _playtimeValue, value); }

        private double _maxValue = 100;
        public double MaxValue { get => _maxValue; set => SetValue(ref _maxValue, value); }

        private double _progressBarFirstValue;
        public double ProgressBarFirstValue { get => _progressBarFirstValue; set => SetValue(ref _progressBarFirstValue, value); }

        private Visibility _progressBarFirstVisibility;
        public Visibility ProgressBarFirstVisibility { get => _progressBarFirstVisibility; set => SetValue(ref _progressBarFirstVisibility, value); }

        private string _toolTipFirst;
        public string ToolTipFirst { get => _toolTipFirst; set => SetValue(ref _toolTipFirst, value); }

        private double _progressBarSecondValue;
        public double ProgressBarSecondValue { get => _progressBarSecondValue; set => SetValue(ref _progressBarSecondValue, value); }

        private Visibility _progressBarSecondVisibility;
        public Visibility ProgressBarSecondVisibility { get => _progressBarSecondVisibility; set => SetValue(ref _progressBarSecondVisibility, value); }

        private string _toolTipSecond;
        public string ToolTipSecond { get => _toolTipSecond; set => SetValue(ref _toolTipSecond, value); }

        private double _progressBarThirdValue;
        public double ProgressBarThirdValue { get => _progressBarThirdValue; set => SetValue(ref _progressBarThirdValue, value); }

        private Visibility _progressBarThirdVisibility;
        public Visibility ProgressBarThirdVisibility { get => _progressBarThirdVisibility; set => SetValue(ref _progressBarThirdVisibility, value); }

        private string _toolTipThird;
        public string ToolTipThird { get => _toolTipThird; set => SetValue(ref _toolTipThird, value); }

        private double _sliderFirstValue;
        public double SliderFirstValue { get => _sliderFirstValue; set => SetValue(ref _sliderFirstValue, value); }

        private Visibility _sliderFirstVisibility;
        public Visibility SliderFirstVisibility { get => _sliderFirstVisibility; set => SetValue(ref _sliderFirstVisibility, value); }

        private double _sliderSecondValue;
        public double SliderSecondValue { get => _sliderSecondValue; set => SetValue(ref _sliderSecondValue, value); }

        private Visibility _sliderSecondVisibility;
        public Visibility SliderSecondVisibility { get => _sliderSecondVisibility; set => SetValue(ref _sliderSecondVisibility, value); }

        private double _sliderThirdValue;
        public double SliderThirdValue { get => _sliderThirdValue; set => SetValue(ref _sliderThirdValue, value); }

        private Visibility _sliderThirdVisibility;
        public Visibility SliderThirdVisibility { get => _sliderThirdVisibility; set => SetValue(ref _sliderThirdVisibility, value); }
    }


    internal class ListProgressBar
    {
        public int Indicator { get; set; }
        public long Value { get; set; }
        public string Format { get; set; }
    }
}