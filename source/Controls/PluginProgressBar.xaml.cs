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
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.ComponentModel;

namespace HowLongToBeat.Controls
{
    public partial class PluginProgressBar : PluginUserControlExtend
    {
        private static HowLongToBeatDatabase PluginDatabase => HowLongToBeat.PluginDatabase;
        protected override IPluginDatabase pluginDatabase => PluginDatabase;

        private PluginProgressBarDataContext ControlDataContext = new PluginProgressBarDataContext();
        protected override IDataContext controlDataContext
        {
            get => ControlDataContext;
            set => ControlDataContext = (PluginProgressBarDataContext)value;
        }

        private CancellationTokenSource _loadCts;
        private readonly Brush[] _cachedThumbBrushes = new Brush[3];
        private readonly Brush[] _cachedThumbUserBrushes = new Brush[3];
        private ProgressSnapshot _lastSnapshot = null;
        private readonly int _debounceMs = 120;

        private bool ShowUserData = true;

        private Game _currentGame;
        private PluginDataBaseGameBase _currentPluginGameData;

        public PluginProgressBar()
        {
            InitializeComponent();
            DataContext = ControlDataContext;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    while (!PluginDatabase.IsLoaded)
                    {
                        Thread.Sleep(100);
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        PluginDatabase.PluginSettings.PropertyChanged += PluginSettings_PropertyChanged;
                        PluginDatabase.Database.ItemUpdated += Database_ItemUpdated;
                        PluginDatabase.Database.ItemCollectionChanged += Database_ItemCollectionChanged;
                        API.Instance.Database.Games.ItemUpdated += Games_ItemUpdated;

                        PluginSettings_PropertyChanged(null, null);
                    });
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                }
            });
        }

        protected override void PluginSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        ShowUserData = PluginDatabase.PluginSettings.Settings.ProgressBarShowTimeUser;

                        ControlDataContext.ShowToolTip = PluginDatabase.PluginSettings.Settings.ProgressBarShowToolTip;
                        ControlDataContext.TextBelowVisibility = PluginDatabase.PluginSettings.Settings.ProgressBarShowTime;

                        _cachedThumbBrushes[0] = PluginDatabase.PluginSettings.Settings.FirstLinearGradient != null
                            ? PluginDatabase.PluginSettings.Settings.FirstLinearGradient.ToLinearGradientBrush as Brush
                            : PluginDatabase.PluginSettings.Settings.FirstColorBrush as Brush;
                        _cachedThumbBrushes[1] = PluginDatabase.PluginSettings.Settings.SecondLinearGradient != null
                            ? PluginDatabase.PluginSettings.Settings.SecondLinearGradient.ToLinearGradientBrush as Brush
                            : PluginDatabase.PluginSettings.Settings.SecondColorBrush as Brush;
                        _cachedThumbBrushes[2] = PluginDatabase.PluginSettings.Settings.ThirdLinearGradient != null
                            ? PluginDatabase.PluginSettings.Settings.ThirdLinearGradient.ToLinearGradientBrush as Brush
                            : PluginDatabase.PluginSettings.Settings.ThirdColorBrush as Brush;

                        _cachedThumbUserBrushes[0] = PluginDatabase.PluginSettings.Settings.FirstMultiLinearGradient != null
                            ? PluginDatabase.PluginSettings.Settings.FirstMultiLinearGradient.ToLinearGradientBrush as Brush
                            : PluginDatabase.PluginSettings.Settings.FirstMultiColorBrush as Brush;
                        _cachedThumbUserBrushes[1] = PluginDatabase.PluginSettings.Settings.SecondMultiLinearGradient != null
                            ? PluginDatabase.PluginSettings.Settings.SecondMultiLinearGradient.ToLinearGradientBrush as Brush
                            : PluginDatabase.PluginSettings.Settings.SecondMultiColorBrush as Brush;
                        _cachedThumbUserBrushes[2] = PluginDatabase.PluginSettings.Settings.ThirdMultiLinearGradient != null
                            ? PluginDatabase.PluginSettings.Settings.ThirdMultiLinearGradient.ToLinearGradientBrush as Brush
                            : PluginDatabase.PluginSettings.Settings.ThirdMultiColorBrush as Brush;

                        ControlDataContext.ThumbFirst = _cachedThumbBrushes[0];
                        ControlDataContext.ThumbSecond = _cachedThumbBrushes[1];
                        ControlDataContext.ThumbThird = _cachedThumbBrushes[2];

                        ControlDataContext.ThumbFirstUser = _cachedThumbUserBrushes[0];
                        ControlDataContext.ThumbSecondUser = _cachedThumbUserBrushes[1];
                        ControlDataContext.ThumbThirdUser = _cachedThumbUserBrushes[2];

                        if (_currentPluginGameData != null)
                        {
                            try
                            {
                                SetData(_currentGame, _currentPluginGameData);
                            }
                            catch (Exception ex)
                            {
                                Common.LogError(ex, false, true, PluginDatabase.PluginName);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, true, PluginDatabase.PluginName);
                    }
                }));
            }
            catch { }
        }

        public override void SetDefaultDataContext()
        {
            ShowUserData = PluginDatabase.PluginSettings.Settings.ProgressBarShowTimeUser;

            bool isActivated = PluginDatabase.PluginSettings.Settings.EnableIntegrationProgressBar;
            if (IgnoreSettings)
            {
                isActivated = true;
                ControlDataContext.TextBelowVisibility = true;
                ShowUserData = true;
            }
            else
            {
                ControlDataContext.TextBelowVisibility = PluginDatabase.PluginSettings.Settings.ProgressBarShowTime;
            }

            ControlDataContext.IsActivated = isActivated;
            ControlDataContext.ShowToolTip = PluginDatabase.PluginSettings.Settings.ProgressBarShowToolTip;

            ControlDataContext.TextBelowVisibility = PluginDatabase.PluginSettings.Settings.ProgressBarShowTime;

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
                ? PluginDatabase.PluginSettings.Settings.ThumbLinearGradient?.ToLinearGradientBrush as Brush
                : PluginDatabase.PluginSettings.Settings.ThumbSolidColorBrush as Brush;

            _cachedThumbBrushes[0] = PluginDatabase.PluginSettings.Settings.FirstLinearGradient != null
                ? PluginDatabase.PluginSettings.Settings.FirstLinearGradient.ToLinearGradientBrush as Brush
                : PluginDatabase.PluginSettings.Settings.FirstColorBrush as Brush;
            _cachedThumbBrushes[1] = PluginDatabase.PluginSettings.Settings.SecondLinearGradient != null
                ? PluginDatabase.PluginSettings.Settings.SecondLinearGradient.ToLinearGradientBrush as Brush
                : PluginDatabase.PluginSettings.Settings.SecondColorBrush as Brush;
            _cachedThumbBrushes[2] = PluginDatabase.PluginSettings.Settings.ThirdLinearGradient != null
                ? PluginDatabase.PluginSettings.Settings.ThirdLinearGradient.ToLinearGradientBrush as Brush
                : PluginDatabase.PluginSettings.Settings.ThirdColorBrush as Brush;

            _cachedThumbUserBrushes[0] = PluginDatabase.PluginSettings.Settings.FirstMultiLinearGradient != null
                ? PluginDatabase.PluginSettings.Settings.FirstMultiLinearGradient.ToLinearGradientBrush as Brush
                : PluginDatabase.PluginSettings.Settings.FirstMultiColorBrush as Brush;
            _cachedThumbUserBrushes[1] = PluginDatabase.PluginSettings.Settings.SecondMultiLinearGradient != null
                ? PluginDatabase.PluginSettings.Settings.SecondMultiLinearGradient.ToLinearGradientBrush as Brush
                : PluginDatabase.PluginSettings.Settings.SecondMultiColorBrush as Brush;
            _cachedThumbUserBrushes[2] = PluginDatabase.PluginSettings.Settings.ThirdMultiLinearGradient != null
                ? PluginDatabase.PluginSettings.Settings.ThirdMultiLinearGradient.ToLinearGradientBrush as Brush
                : PluginDatabase.PluginSettings.Settings.ThirdMultiColorBrush as Brush;

            ControlDataContext.ThumbFirst = _cachedThumbBrushes[0];
            ControlDataContext.ThumbSecond = _cachedThumbBrushes[1];
            ControlDataContext.ThumbThird = _cachedThumbBrushes[2];

            ControlDataContext.ThumbFirstUser = _cachedThumbUserBrushes[0];
            ControlDataContext.ThumbSecondUser = _cachedThumbUserBrushes[1];
            ControlDataContext.ThumbThirdUser = _cachedThumbUserBrushes[2];
        }

        public override async void SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
        {
            _currentGame = newContext;
            _currentPluginGameData = PluginGameData;

            GameHowLongToBeat gameHowLongToBeat = (GameHowLongToBeat)PluginGameData;
            try
            {
                try { _loadCts?.Cancel(); } catch { }
                try { _loadCts?.Dispose(); } catch { }
                _loadCts = new CancellationTokenSource();
                try
                {
                    await Task.Delay(_debounceMs, _loadCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                var snapshot = await LoadDataAsync(gameHowLongToBeat, _loadCts.Token).ConfigureAwait(false);

                if (snapshot == null) return;

                if (_lastSnapshot != null && SnapshotEquals(_lastSnapshot, snapshot))
                {
                    return;
                }

                try
                {
                    var op = Dispatcher?.InvokeAsync(() =>
                    {
                        try
                        {
                            SliderPlaytime.Maximum = snapshot.MaxValue;
                            SliderPlaytime.Value = snapshot.PlaytimeValue;

                            PART_ProgressBarFirst.Value = snapshot.ProgressValues[0];
                            PART_ProgressBarFirst.Foreground = snapshot.ThumbBrushes[0];
                            PART_ProgressBarFirst.Maximum = snapshot.MaxValue;
                            PART_ProgressBarFirst.TextBelowVisibility = ControlDataContext.TextBelowVisibility;

                            PART_ProgressBarSecond.Value = snapshot.ProgressValues[1];
                            PART_ProgressBarSecond.Foreground = snapshot.ThumbBrushes[1];
                            PART_ProgressBarSecond.Maximum = snapshot.MaxValue;
                            PART_ProgressBarSecond.TextBelowVisibility = ControlDataContext.TextBelowVisibility;

                            PART_ProgressBarThird.Value = snapshot.ProgressValues[2];
                            PART_ProgressBarThird.Foreground = snapshot.ThumbBrushes[2];
                            PART_ProgressBarThird.Maximum = snapshot.MaxValue;
                            PART_ProgressBarThird.TextBelowVisibility = ControlDataContext.TextBelowVisibility;

                            PartSliderFirst.ThumbFill = snapshot.ThumbUserBrushes[0];
                            PartSliderFirst.Visibility = snapshot.SliderVisibilities[0];
                            PartSliderFirst.Value = snapshot.SliderValues[0];
                            PartSliderFirst.Maximum = snapshot.MaxValue;

                            PartSliderSecond.ThumbFill = snapshot.ThumbUserBrushes[1];
                            PartSliderSecond.Visibility = snapshot.SliderVisibilities[1];
                            PartSliderSecond.Value = snapshot.SliderValues[1];
                            PartSliderSecond.Maximum = snapshot.MaxValue;

                            PartSliderThird.ThumbFill = snapshot.ThumbUserBrushes[2];
                            PartSliderThird.Visibility = snapshot.SliderVisibilities[2];
                            PartSliderThird.Value = snapshot.SliderValues[2];
                            PartSliderThird.Maximum = snapshot.MaxValue;

                            ControlDataContext.MaxValue = snapshot.MaxValue;
                            ControlDataContext.PlaytimeValue = snapshot.PlaytimeValue;
                            ControlDataContext.ThumbFirst = snapshot.ThumbBrushes?[0] ?? ControlDataContext.ThumbFirst;
                            ControlDataContext.ThumbSecond = snapshot.ThumbBrushes?[1] ?? ControlDataContext.ThumbSecond;
                            ControlDataContext.ThumbThird = snapshot.ThumbBrushes?[2] ?? ControlDataContext.ThumbThird;
                            ControlDataContext.ThumbFirstUser = snapshot.ThumbUserBrushes?[0] ?? ControlDataContext.ThumbFirstUser;
                            ControlDataContext.ThumbSecondUser = snapshot.ThumbUserBrushes?[1] ?? ControlDataContext.ThumbSecondUser;
                            ControlDataContext.ThumbThirdUser = snapshot.ThumbUserBrushes?[2] ?? ControlDataContext.ThumbThirdUser;

                            // Update DataContext values
                            ControlDataContext.ToolTipFirst = snapshot.ProgressFormats != null && snapshot.ProgressFormats.Length > 0 ? snapshot.ProgressFormats[0] : string.Empty;
                            ControlDataContext.ToolTipSecond = snapshot.ProgressFormats != null && snapshot.ProgressFormats.Length > 1 ? snapshot.ProgressFormats[1] : string.Empty;
                            ControlDataContext.ToolTipThird = snapshot.ProgressFormats != null && snapshot.ProgressFormats.Length > 2 ? snapshot.ProgressFormats[2] : string.Empty;

                            ControlDataContext.HasFirst = !string.IsNullOrEmpty(ControlDataContext.ToolTipFirst);
                            ControlDataContext.HasSecond = !string.IsNullOrEmpty(ControlDataContext.ToolTipSecond);
                            ControlDataContext.HasThird = !string.IsNullOrEmpty(ControlDataContext.ToolTipThird);

                            // Also update the named TextBlocks directly to avoid binding/render delays
                            try
                            {
                                PART_TimeText1.Text = ControlDataContext.ToolTipFirst;
                                PART_TimeText2.Text = ControlDataContext.ToolTipSecond;
                                PART_TimeText3.Text = ControlDataContext.ToolTipThird;

                                PART_TimeText1.Foreground = ControlDataContext.ThumbFirst;
                                PART_TimeText2.Foreground = ControlDataContext.ThumbSecond;
                                PART_TimeText3.Foreground = ControlDataContext.ThumbThird;

                                PART_TimeText1.Visibility = ControlDataContext.HasFirst ? Visibility.Visible : Visibility.Collapsed;
                                PART_TimeSep12.Visibility = (ControlDataContext.HasFirst && ControlDataContext.HasSecond) ? Visibility.Visible : Visibility.Collapsed;
                                PART_TimeText2.Visibility = ControlDataContext.HasSecond ? Visibility.Visible : Visibility.Collapsed;
                                PART_TimeSep23.Visibility = (ControlDataContext.HasSecond && ControlDataContext.HasThird) ? Visibility.Visible : Visibility.Collapsed;
                                PART_TimeText3.Visibility = ControlDataContext.HasThird ? Visibility.Visible : Visibility.Collapsed;

                                PART_TimeTexts?.InvalidateMeasure();
                                PART_TimeTexts?.InvalidateArrange();
                                PART_TimeTexts?.UpdateLayout();
                            }
                            catch { }
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false, true, PluginDatabase.PluginName);
                        }
                    });

                    if (op != null)
                    {
                        try
                        {
                            await op.Task.ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false, true, PluginDatabase.PluginName);
                        }
                    }
                    else
                    {
                        try
                        {
                            ControlDataContext.MaxValue = snapshot.MaxValue;
                            ControlDataContext.PlaytimeValue = snapshot.PlaytimeValue;
                        }
                        catch { }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                }

                _lastSnapshot = snapshot;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private bool SnapshotEquals(ProgressSnapshot a, ProgressSnapshot b)
        {
            if (a == null || b == null) return false;
            if (a.MaxValue != b.MaxValue) return false;
            if (a.PlaytimeValue != b.PlaytimeValue) return false;
            for (int i = 0; i < 3; i++)
            {
                if (a.ProgressValues?[i] != b.ProgressValues?[i]) return false;
                if (!string.Equals(a.ProgressFormats?[i], b.ProgressFormats?[i])) return false;
                if (a.SliderValues?[i] != b.SliderValues?[i]) return false;
                if (a.SliderVisibilities?[i] != b.SliderVisibilities?[i]) return false;
                if (!object.ReferenceEquals(a.ThumbBrushes?[i], b.ThumbBrushes?[i])) return false;
                if (!object.ReferenceEquals(a.ThumbUserBrushes?[i], b.ThumbUserBrushes?[i])) return false;
            }
            return true;
        }

        private Task<ProgressSnapshot> LoadDataAsync(GameHowLongToBeat gameHowLongToBeat, CancellationToken cancellationToken)
        {
            bool showUserData = this.ShowUserData;
            bool ignoreSettings = false;

            try
            {
                Dispatcher.Invoke(() =>
                {
                    ignoreSettings = this.IgnoreSettings;
                });
            }
            catch
            {
                ignoreSettings = false;
            }

            return Task.Run(() =>
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested) return null;

                    int elIndicator = 0;
                    int elIndicatorUser = 0;
                    double maxValue = 0;
                    double maxHltb = 0;
                    ulong playtime = gameHowLongToBeat.Playtime;
                    TitleList titleList = PluginDatabase.GetUserHltbDataCurrent(gameHowLongToBeat.GetData().Id, gameHowLongToBeat.UserGameId);

                    long[] progressValues = new long[3];
                    string[] progressFormats = new string[3];
                    Brush[] thumbBrushes = new Brush[3];
                    Brush[] thumbUserBrushes = new Brush[3];
                    long[] sliderValues = new long[3];
                    Visibility[] sliderVisibilities = new Visibility[3] { Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed };

                    if (gameHowLongToBeat.HasData)
                    {
                        HltbDataUser HltbData = gameHowLongToBeat.GetData();

                        if (HltbData.GameType != GameType.Multi)
                        {
                            Brush color = _cachedThumbBrushes[0] ?? (PluginDatabase.PluginSettings.Settings.FirstLinearGradient != null ? PluginDatabase.PluginSettings.Settings.FirstLinearGradient.ToLinearGradientBrush as Brush : PluginDatabase.PluginSettings.Settings.FirstColorBrush as Brush);

                            if ((ignoreSettings || PluginDatabase.PluginSettings.Settings.ShowMainTime) && HltbData?.GameHltbData?.MainStory != null && HltbData.GameHltbData.MainStory > 0)
                            {
                                progressValues[elIndicator] = HltbData.GameHltbData.MainStory;
                                progressFormats[elIndicator] = HltbData.GameHltbData.MainStoryFormat;
                                thumbBrushes[elIndicator] = color;
                                if (maxValue < HltbData.GameHltbData.MainStory) maxValue = HltbData.GameHltbData.MainStory;
                                elIndicator++;
                            }

                            if (titleList?.HltbUserData?.MainStory != null && titleList?.HltbUserData?.MainStory > 0 && showUserData)
                            {
                                thumbUserBrushes[elIndicatorUser] = color;
                                sliderValues[elIndicatorUser] = titleList.HltbUserData.MainStory;
                                sliderVisibilities[elIndicatorUser] = Visibility.Visible;
                                elIndicatorUser++;
                            }

                            color = _cachedThumbBrushes[1] ?? (PluginDatabase.PluginSettings.Settings.SecondLinearGradient != null ? PluginDatabase.PluginSettings.Settings.SecondLinearGradient.ToLinearGradientBrush as Brush : PluginDatabase.PluginSettings.Settings.SecondColorBrush as Brush);

                            if ((ignoreSettings || PluginDatabase.PluginSettings.Settings.ShowExtraTime) && HltbData?.GameHltbData?.MainExtra != null && HltbData.GameHltbData.MainExtra > 0)
                            {
                                progressValues[elIndicator] = HltbData.GameHltbData.MainExtra;
                                progressFormats[elIndicator] = HltbData.GameHltbData.MainExtraFormat;
                                thumbBrushes[elIndicator] = color;
                                if (maxValue < HltbData.GameHltbData.MainExtra) maxValue = HltbData.GameHltbData.MainExtra;
                                elIndicator++;
                            }

                            if (titleList?.HltbUserData?.MainExtra != null && titleList?.HltbUserData?.MainExtra > 0 && showUserData)
                            {
                                thumbUserBrushes[elIndicatorUser] = color;
                                sliderValues[elIndicatorUser] = titleList.HltbUserData.MainExtra;
                                sliderVisibilities[elIndicatorUser] = Visibility.Visible;
                                elIndicatorUser++;
                            }

                            color = _cachedThumbBrushes[2] ?? (PluginDatabase.PluginSettings.Settings.ThirdLinearGradient != null ? PluginDatabase.PluginSettings.Settings.ThirdLinearGradient.ToLinearGradientBrush as Brush : PluginDatabase.PluginSettings.Settings.ThirdColorBrush as Brush);

                            if ((ignoreSettings || PluginDatabase.PluginSettings.Settings.ShowCompletionistTime) && HltbData?.GameHltbData?.Completionist != null && HltbData.GameHltbData.Completionist != 0)
                            {
                                progressValues[elIndicator] = HltbData.GameHltbData.Completionist;
                                progressFormats[elIndicator] = HltbData.GameHltbData.CompletionistFormat;
                                thumbBrushes[elIndicator] = color;
                                if (maxValue < HltbData.GameHltbData.Completionist) maxValue = HltbData.GameHltbData.Completionist;
                                elIndicator++;
                            }

                            if (titleList?.HltbUserData?.Completionist != null && titleList?.HltbUserData?.Completionist > 0 && showUserData)
                            {
                                thumbUserBrushes[elIndicatorUser] = color;
                                sliderValues[elIndicatorUser] = titleList.HltbUserData.Completionist;
                                sliderVisibilities[elIndicatorUser] = Visibility.Visible;
                                elIndicatorUser++;
                            }
                        }
                        else
                        {
                            Brush color = _cachedThumbBrushes[0] ?? (PluginDatabase.PluginSettings.Settings.FirstMultiLinearGradient != null ? PluginDatabase.PluginSettings.Settings.FirstMultiLinearGradient.ToLinearGradientBrush as Brush : PluginDatabase.PluginSettings.Settings.FirstMultiColorBrush as Brush);

                            if ((ignoreSettings || PluginDatabase.PluginSettings.Settings.ShowSoloTime) && HltbData?.GameHltbData?.Solo != null && HltbData.GameHltbData.Solo != 0)
                            {
                                progressValues[elIndicator] = HltbData.GameHltbData.Solo;
                                progressFormats[elIndicator] = HltbData.GameHltbData.SoloFormat;
                                thumbBrushes[elIndicator] = color;
                                if (maxValue < HltbData.GameHltbData.Solo) maxValue = HltbData.GameHltbData.Solo;
                                elIndicator++;
                            }

                            if (titleList?.HltbUserData?.Solo != null && titleList?.HltbUserData?.Solo > 0 && showUserData)
                            {
                                thumbUserBrushes[elIndicatorUser] = color;
                                sliderValues[elIndicatorUser] = titleList.HltbUserData.Solo;
                                sliderVisibilities[elIndicatorUser] = Visibility.Visible;
                                elIndicatorUser++;
                            }

                            color = _cachedThumbBrushes[1] ?? (PluginDatabase.PluginSettings.Settings.SecondMultiLinearGradient != null ? PluginDatabase.PluginSettings.Settings.SecondMultiLinearGradient.ToLinearGradientBrush as Brush : PluginDatabase.PluginSettings.Settings.SecondMultiColorBrush as Brush);

                            if ((ignoreSettings || PluginDatabase.PluginSettings.Settings.ShowCoOpTime) && HltbData?.GameHltbData?.CoOp != null && HltbData.GameHltbData.CoOp != 0)
                            {
                                progressValues[elIndicator] = HltbData.GameHltbData.CoOp;
                                progressFormats[elIndicator] = HltbData.GameHltbData.CoOpFormat;
                                thumbBrushes[elIndicator] = color;
                                if (maxValue < HltbData.GameHltbData.CoOp) maxValue = HltbData.GameHltbData.CoOp;
                                elIndicator++;
                            }

                            if (titleList?.HltbUserData?.CoOp != null && titleList?.HltbUserData?.CoOp > 0 && showUserData)
                            {
                                thumbUserBrushes[elIndicatorUser] = color;
                                sliderValues[elIndicatorUser] = titleList.HltbUserData.CoOp;
                                sliderVisibilities[elIndicatorUser] = Visibility.Visible;
                                elIndicatorUser++;
                            }

                            color = _cachedThumbBrushes[2] ?? (PluginDatabase.PluginSettings.Settings.ThirdMultiLinearGradient != null ? PluginDatabase.PluginSettings.Settings.ThirdMultiLinearGradient.ToLinearGradientBrush as Brush : PluginDatabase.PluginSettings.Settings.ThirdMultiColorBrush as Brush);

                            if ((ignoreSettings || PluginDatabase.PluginSettings.Settings.ShowVsTime) && HltbData?.GameHltbData?.Vs != null && HltbData.GameHltbData.Vs != 0)
                            {
                                progressValues[elIndicator] = HltbData.GameHltbData.Vs;
                                progressFormats[elIndicator] = HltbData.GameHltbData.VsFormat;
                                thumbBrushes[elIndicator] = color;
                                if (maxValue < HltbData.GameHltbData.Vs) maxValue = HltbData.GameHltbData.Vs;
                                elIndicator++;
                            }

                            if (titleList?.HltbUserData?.Vs != null && titleList?.HltbUserData?.Vs > 0 && showUserData)
                            {
                                thumbUserBrushes[elIndicatorUser] = color;
                                sliderValues[elIndicatorUser] = titleList.HltbUserData.Vs;
                                sliderVisibilities[elIndicatorUser] = Visibility.Visible;
                                elIndicatorUser++;
                            }
                        }
                    }

                    maxHltb = maxValue;
                    if (playtime > maxValue)
                    {
                        maxValue = playtime;
                    }

                    long MaxPercent = (long)Math.Ceiling((double)(10 * maxHltb / 100));
                    if (maxValue > maxHltb + MaxPercent)
                    {
                        maxValue = maxHltb + MaxPercent;
                    }

                    if (cancellationToken.IsCancellationRequested) return null;

                    var snapshot = new ProgressSnapshot
                    {
                        ProgressValues = progressValues,
                        ProgressFormats = progressFormats,
                        ThumbBrushes = thumbBrushes,
                        ThumbUserBrushes = thumbUserBrushes,
                        SliderValues = sliderValues,
                        SliderVisibilities = sliderVisibilities,
                        MaxValue = maxValue,
                        PlaytimeValue = playtime
                    };

                    return snapshot;
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                }
                return null;
            });
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

        private void SetColor(int elIndicator, Brush color)
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

        private void SetColorUser(int elIndicator, Brush color)
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
            // Defer update to ensure all indicators have their final ActualWidth
            _ = Dispatcher?.BeginInvoke((Action)(() =>
            {
                try
                {
                    double width1 = PART_ProgressBarFirst.IndicatorWidth;
                    double width2 = PART_ProgressBarSecond.IndicatorWidth;
                    double width3 = PART_ProgressBarThird.IndicatorWidth;

                    PART_ProgressBarSecond.MarginLeft = width1;
                    // third indicator should be offset by sum of previous indicator widths
                    PART_ProgressBarThird.MarginLeft = width1 + width2;

                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, "Error updating progress bar layout");
                }
            }));
        }
        #endregion
    }


    public class PluginProgressBarDataContext : ObservableObject, IDataContext
    {
        private bool _isActivated;
        public bool IsActivated { get => _isActivated; set => SetValue(ref _isActivated, value); }

        private bool _showToolTip;
        public bool ShowToolTip { get => _showToolTip; set => SetValue(ref _showToolTip, value); }

        private bool _textBelowVisibility;
        public bool TextBelowVisibility { get => _textBelowVisibility; set => SetValue(ref _textBelowVisibility, value); }

        private Brush _thumbColor = ResourceProvider.GetResource("NormalBrush") as Brush;
        public Brush ThumbColor { get => _thumbColor; set => SetValue(ref _thumbColor, value); }

        private Brush _thumbFirst = new SolidColorBrush(Brushes.DarkCyan.Color);
        public Brush ThumbFirst { get => _thumbFirst; set => SetValue(ref _thumbFirst, value); }

        private Brush _thumbSecond = new SolidColorBrush(Brushes.RoyalBlue.Color);
        public Brush ThumbSecond { get => _thumbSecond; set => SetValue(ref _thumbSecond, value); }

        private Brush _thumbThird = new SolidColorBrush(Brushes.ForestGreen.Color);
        public Brush ThumbThird { get => _thumbThird; set => SetValue(ref _thumbThird, value); }

        private Brush _thumbFirstUser = new SolidColorBrush(Brushes.DarkCyan.Color);
        public Brush ThumbFirstUser { get => _thumbFirstUser; set => SetValue(ref _thumbFirstUser, value); }

        private Brush _thumbSecondUser = new SolidColorBrush(Brushes.RoyalBlue.Color);
        public Brush ThumbSecondUser { get => _thumbSecondUser; set => SetValue(ref _thumbSecondUser, value); }

        private Brush _thumbThirdUser = new SolidColorBrush(Brushes.ForestGreen.Color);
        public Brush ThumbThirdUser { get => _thumbThirdUser; set => SetValue(ref _thumbThirdUser, value); }

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

        private bool _hasFirst;
        public bool HasFirst { get => _hasFirst; set => SetValue(ref _hasFirst, value); }

        private bool _hasSecond;
        public bool HasSecond { get => _hasSecond; set => SetValue(ref _hasSecond, value); }

        private bool _hasThird;
        public bool HasThird { get => _hasThird; set => SetValue(ref _hasThird, value); }
    }

    internal class ListProgressBar
    {
        public int Indicator { get; set; }
        public long Value { get; set; }
        public string Format { get; set; }
    }

    internal class ProgressSnapshot
    {
        public long[] ProgressValues { get; set; }
        public string[] ProgressFormats { get; set; }
        public Brush[] ThumbBrushes { get; set; }
        public Brush[] ThumbUserBrushes { get; set; }
        public long[] SliderValues { get; set; }
        public Visibility[] SliderVisibilities { get; set; }
        public double MaxValue { get; set; }
        public ulong PlaytimeValue { get; set; }
    }
}
