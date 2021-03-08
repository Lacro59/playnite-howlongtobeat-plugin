using CommonPluginsShared;
using CommonPluginsShared.Controls;
using HowLongToBeat.Models;
using HowLongToBeat.Services;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace HowLongToBeat.Controls
{
    /// <summary>
    /// Logique d'interaction pour HltbProgressBar.xaml
    /// </summary>
    public partial class HltbProgressBar : PluginUserControlExtend
    {
        private HowLongToBeatDatabase PluginDatabase = HowLongToBeat.PluginDatabase;

        public PointCollection ThumbPoint { get; set; }
        public SolidColorBrush SolidColorBrushFirst { get; set; }
        public SolidColorBrush SolidColorBrushSecond { get; set; }
        public SolidColorBrush SolidColorBrushThird { get; set; }

        public bool TextAboveVisibility { get; set; }
        public bool TextInsideVisibility { get; set; }
        public bool TextBelowVisibility { get; set; }


        public HltbProgressBar()
        {
            InitializeComponent();

            PluginDatabase.PluginSettings.PropertyChanged += PluginSettings_PropertyChanged;
            PluginDatabase.Database.ItemUpdated += Database_ItemUpdated;
            PluginDatabase.Database.ItemCollectionChanged += Database_ItemCollectionChanged;
            PluginDatabase.PlayniteApi.Database.Games.ItemUpdated += Games_ItemUpdated;

            // Apply settings
            PluginSettings_PropertyChanged(null, null);
        }

        #region OnPropertyChange
        private static void SettingsPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            HltbProgressBar obj = sender as HltbProgressBar;
            if (obj != null && e.NewValue != e.OldValue)
            {
                obj.PluginSettings_PropertyChanged(null, null);
            }
        }

        private static void ControlsPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            HltbProgressBar obj = sender as HltbProgressBar;
            if (obj != null && e.NewValue != e.OldValue)
            {
                obj.GameContextChanged(null, obj.GameContext);
            }
        }

        // When settings is updated
        public override void PluginSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            TextAboveVisibility = PluginDatabase.PluginSettings.Settings.ProgressBarShowTimeAbove;
            TextInsideVisibility = PluginDatabase.PluginSettings.Settings.ProgressBarShowTimeInterior;
            TextBelowVisibility = PluginDatabase.PluginSettings.Settings.ProgressBarShowTimeBelow;

            if (!PluginDatabase.PluginSettings.Settings.ProgressBarShowTime)
            {
                TextAboveVisibility = false;
                TextInsideVisibility = false;
                TextBelowVisibility = false;
            }

            if (IgnoreSettings)
            {
                this.DataContext = new
                {
                    ShowToolTip = PluginDatabase.PluginSettings.Settings.ProgressBarShowToolTip,
                    TextAboveVisibility,
                    TextInsideVisibility,
                    TextBelowVisibility,

                    ThumbFirst = SolidColorBrushFirst,
                    ThumbSecond = SolidColorBrushSecond,
                    ThumbThird = SolidColorBrushThird,
                    ThumbPoint
                };
            }
            else
            {
                this.DataContext = new
                {
                    ShowToolTip = PluginDatabase.PluginSettings.Settings.ProgressBarShowToolTip,
                    TextAboveVisibility,
                    TextInsideVisibility,
                    TextBelowVisibility,

                    ThumbFirst = SolidColorBrushFirst,
                    ThumbSecond = SolidColorBrushSecond,
                    ThumbThird = SolidColorBrushThird,
                    ThumbPoint
                };
            }

            // Publish changes for the currently displayed game
            GameContextChanged(null, GameContext);
            PART_GridContener_Loaded(null, null);
        }

        // When game is changed
        public override void GameContextChanged(Game oldContext, Game newContext)
        {
            if (IgnoreSettings)
            {
                MustDisplay = true;
            }
            else
            {
                MustDisplay = PluginDatabase.PluginSettings.Settings.EnableIntegrationProgressBar;

                // When control is not used
                if (!PluginDatabase.PluginSettings.Settings.EnableIntegrationProgressBar)
                {
                    return;
                }
            }

            if (newContext != null)
            {
                GameHowLongToBeat gameHowLongToBeat = PluginDatabase.Get(newContext, true);

                if (!gameHowLongToBeat.HasData)
                {
                    MustDisplay = false;
                    return;
                }

                tpHltb_El1.Content = string.Empty;
                tpHltb_El2.Content = string.Empty;
                tpHltb_El3.Content = string.Empty;

                SetHltbData(gameHowLongToBeat);
            }
            else
            {

            }
        }
        #endregion


        private void SetDataInView(int ElIndicator, long ElValue, string ElFormat)
        {
            try
            {
                switch (ElIndicator)
                {
                    case 1:
                        if (ElValue != 0)
                        {
                            PART_ProgressBarFirst.Visibility = Visibility.Visible;
                        }

                        PART_ProgressBarFirst.Value = ElValue;
                        PART_ProgressBarFirst.TextValue = ElFormat;

                        // ToolTip
                        tpHltb_El1.Content = ElFormat;

                        break;

                    case 2:
                        if (ElValue != 0)
                        {
                            PART_ProgressBarSecond.Visibility = Visibility.Visible;
                        }

                        PART_ProgressBarSecond.Value = ElValue;
                        PART_ProgressBarSecond.TextValue = ElFormat;

                        // ToolTip
                        tpHltb_El2.Content = ElFormat;

                        break;

                    case 3:
                        if (ElValue != 0)
                        {
                            PART_ProgressBarThird.Visibility = Visibility.Visible;
                        }

                        PART_ProgressBarThird.Value = ElValue;
                        PART_ProgressBarThird.TextValue = ElFormat;

                        // ToolTip
                        tpHltb_El3.Content = ElFormat;

                        break;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Error on SetDataInView({ElIndicator}, {ElValue}, {ElFormat})");
            }
        }

        public void SetHltbData(GameHowLongToBeat gameHowLongToBeat)
        {
            LoadData(gameHowLongToBeat);
        }

        private void SetColor(int ElIndicator, Color color)
        {
            switch (ElIndicator)
            {
                case 1:
                    PART_ProgressBarFirst.Foreground = new SolidColorBrush(color);
                    break;

                case 2:
                    PART_ProgressBarSecond.Foreground = new SolidColorBrush(color);
                    break;

                case 3:
                    PART_ProgressBarThird.Foreground = new SolidColorBrush(color);
                    break;
            }
        }

        private void LoadData(GameHowLongToBeat gameHowLongToBeat)
        {
            try
            {
                // Definied data value in different component.
                int ElIndicator = 0;
                long MaxValue = 0;
                long MaxHltb = 0;
                long Playtime = gameHowLongToBeat.Playtime;
                List<ListProgressBar> listProgressBars = new List<ListProgressBar>();
                if (gameHowLongToBeat.HasData)
                {
                    var HltbData = gameHowLongToBeat.GetData();

                    if (HltbData.GameHltbData.MainStory != 0)
                    {
                        ElIndicator += 1;
                        listProgressBars.Add(new ListProgressBar { Indicator = ElIndicator, Value = HltbData.GameHltbData.MainStory, Format = HltbData.GameHltbData.MainStoryFormat });
                        if (MaxValue < HltbData.GameHltbData.MainStory)
                        {
                            MaxValue = HltbData.GameHltbData.MainStory;
                        }
                        SetColor(ElIndicator, PluginDatabase.PluginSettings.Settings.ColorFirst);
                    }

                    if (HltbData.GameHltbData.MainExtra != 0)
                    {
                        ElIndicator += 1;
                        listProgressBars.Add(new ListProgressBar { Indicator = ElIndicator, Value = HltbData.GameHltbData.MainExtra, Format = HltbData.GameHltbData.MainExtraFormat });
                        if (MaxValue < HltbData.GameHltbData.MainExtra)
                        {
                            MaxValue = HltbData.GameHltbData.MainExtra;
                        }
                        SetColor(ElIndicator, PluginDatabase.PluginSettings.Settings.ColorSecond);
                    }

                    if (HltbData.GameHltbData.Completionist != 0)
                    {
                        ElIndicator += 1;
                        listProgressBars.Add(new ListProgressBar { Indicator = ElIndicator, Value = HltbData.GameHltbData.Completionist, Format = HltbData.GameHltbData.CompletionistFormat });
                        if (MaxValue < HltbData.GameHltbData.Completionist)
                        {
                            MaxValue = HltbData.GameHltbData.Completionist;
                        }
                        SetColor(ElIndicator, PluginDatabase.PluginSettings.Settings.ColorThird);
                    }

                    if (HltbData.GameHltbData.Solo != 0)
                    {
                        ElIndicator += 1;
                        listProgressBars.Add(new ListProgressBar { Indicator = ElIndicator, Value = HltbData.GameHltbData.Solo, Format = HltbData.GameHltbData.SoloFormat });
                        if (MaxValue < HltbData.GameHltbData.Solo)
                        {
                            MaxValue = HltbData.GameHltbData.Solo;
                        }
                        SetColor(ElIndicator, PluginDatabase.PluginSettings.Settings.ColorFirstMulti);
                    }

                    if (HltbData.GameHltbData.CoOp != 0)
                    {
                        ElIndicator += 1;
                        listProgressBars.Add(new ListProgressBar { Indicator = ElIndicator, Value = HltbData.GameHltbData.CoOp, Format = HltbData.GameHltbData.CoOpFormat });
                        if (MaxValue < HltbData.GameHltbData.CoOp)
                        {
                            MaxValue = HltbData.GameHltbData.CoOp;
                        }
                        SetColor(ElIndicator, PluginDatabase.PluginSettings.Settings.ColorSecondMulti);
                    }

                    if (HltbData.GameHltbData.Vs != 0)
                    {
                        ElIndicator += 1;
                        listProgressBars.Add(new ListProgressBar { Indicator = ElIndicator, Value = HltbData.GameHltbData.Vs, Format = HltbData.GameHltbData.VsFormat });
                        if (MaxValue < HltbData.GameHltbData.Vs)
                        {
                            MaxValue = HltbData.GameHltbData.Vs;
                        }
                        SetColor(ElIndicator, PluginDatabase.PluginSettings.Settings.ColorThirdMulti);
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


                // Add data
                PART_ProgressBarFirst.Maximum = MaxValue;
                PART_ProgressBarSecond.Maximum = MaxValue;
                PART_ProgressBarThird.Maximum = MaxValue;

                SliderPlaytime.Value = Playtime;
                SliderPlaytime.Maximum = MaxValue;


                PART_ProgressBarFirst.Visibility = Visibility.Hidden;
                PART_ProgressBarSecond.Visibility = Visibility.Hidden;
                PART_ProgressBarThird.Visibility = Visibility.Hidden;


                foreach (var listProgressBar in listProgressBars)
                {
                    SetDataInView(listProgressBar.Indicator, listProgressBar.Value, listProgressBar.Format);
                }


                SliderPlaytime.UpdateLayout();


                // Show user hltb datas
                PartSliderFirst.Maximum = MaxValue;
                PartSliderFirst.Visibility = Visibility.Hidden;
                PartSliderSecond.Maximum = MaxValue;
                PartSliderSecond.Visibility = Visibility.Hidden;
                PartSliderThird.Maximum = MaxValue;
                PartSliderThird.Visibility = Visibility.Hidden;

                if (gameHowLongToBeat.HasData && PluginDatabase.PluginSettings.Settings.ProgressBarShowTimeUser)
                {
                    TitleList titleList = PluginDatabase.GetUserHltbData(gameHowLongToBeat.GetData().Id);

                    if (titleList != null)
                    {
                        ElIndicator = 0;

                        if (titleList.HltbUserData.MainStory != 0)
                        {
                            ElIndicator++;
                            SetUserData(ElIndicator, titleList.HltbUserData.MainStory, PluginDatabase.PluginSettings.Settings.ColorFirst);
                        }

                        if (titleList.HltbUserData.MainExtra != 0)
                        {
                            ElIndicator++;
                            SetUserData(ElIndicator, titleList.HltbUserData.MainExtra, PluginDatabase.PluginSettings.Settings.ColorSecond);
                        }

                        if (titleList.HltbUserData.Completionist != 0)
                        {
                            ElIndicator++;
                            SetUserData(ElIndicator, titleList.HltbUserData.Completionist, PluginDatabase.PluginSettings.Settings.ColorThird);
                        }

                        if (titleList.HltbUserData.Solo != 0)
                        {
                            ElIndicator++;
                            SetUserData(ElIndicator, titleList.HltbUserData.Solo, PluginDatabase.PluginSettings.Settings.ColorFirstMulti);
                        }

                        if (titleList.HltbUserData.CoOp != 0)
                        {
                            ElIndicator++;
                            SetUserData(ElIndicator, titleList.HltbUserData.CoOp, PluginDatabase.PluginSettings.Settings.ColorSecondMulti);
                        }

                        if (titleList.HltbUserData.Vs != 0)
                        {
                            ElIndicator++;
                            SetUserData(ElIndicator, titleList.HltbUserData.Vs, PluginDatabase.PluginSettings.Settings.ColorThirdMulti);
                        }
                    }
                }

                PART_GridContener_Loaded(null, null);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }
        }


        public void SetUserData(int ElIndicator, long Value, Color color)
        {
            switch (ElIndicator)
            {
                case 1:
                    PartSliderFirst.Value = Value;
                    PartSliderFirst.Visibility = Visibility.Visible;
                    SolidColorBrushFirst = new SolidColorBrush(color);
                    break;
                case 2:
                    PartSliderSecond.Value = Value;
                    PartSliderSecond.Visibility = Visibility.Visible;
                    SolidColorBrushSecond = new SolidColorBrush(color);

                    break;
                case 3:
                    PartSliderThird.Value = Value;
                    PartSliderThird.Visibility = Visibility.Visible;
                    SolidColorBrushThird = new SolidColorBrush(color);
                    break;
            }
        }


        public void PART_GridContener_Loaded(object sender, RoutedEventArgs e)
        {
            if (PART_ProgressBarThird.IsVisible)
            {
                spHltb_El3.Width = PART_ProgressBarThird.GetIndicatorWidth() - PART_ProgressBarSecond.GetIndicatorWidth();
                PART_ProgressBarThird.MarginWidth = PART_ProgressBarSecond.GetIndicatorWidth();
                PART_ProgressBarThird.TextWidth = PART_ProgressBarThird.GetIndicatorWidth() - PART_ProgressBarSecond.GetIndicatorWidth();
            }

            if (PART_ProgressBarSecond.IsVisible)
            {
                spHltb_El2.Width = PART_ProgressBarSecond.GetIndicatorWidth() - PART_ProgressBarFirst.GetIndicatorWidth();
                PART_ProgressBarSecond.MarginWidth = PART_ProgressBarFirst.GetIndicatorWidth();
                PART_ProgressBarSecond.TextWidth = PART_ProgressBarSecond.GetIndicatorWidth() - PART_ProgressBarFirst.GetIndicatorWidth();
            }

            if (PART_ProgressBarFirst.IsVisible)
            {
                spHltb_El1.Width = PART_ProgressBarFirst.GetIndicatorWidth();
                PART_ProgressBarFirst.TextWidth = PART_ProgressBarFirst.GetIndicatorWidth();
            }


            double SliderHeight = PART_ProgressBarFirst.GetIndicatorHeight() / 2;
            PartSliderFirst.Height = SliderHeight;
            PartSliderSecond.Height = SliderHeight;
            PartSliderThird.Height = SliderHeight;

            PartSliderFirst.Margin = SliderPlaytime.Margin;
            PartSliderSecond.Margin = SliderPlaytime.Margin;
            PartSliderThird.Margin = SliderPlaytime.Margin;

            Point Point1 = new Point(SliderHeight / 2.5, 0);
            Point Point2 = new Point(SliderHeight / 1.25, SliderHeight / 1.25);
            Point Point3 = new Point(0, SliderHeight / 1.25);
            ThumbPoint = new PointCollection();
            ThumbPoint.Add(Point1);
            ThumbPoint.Add(Point2);
            ThumbPoint.Add(Point3);


            this.DataContext = new
            {
                ShowToolTip = PluginDatabase.PluginSettings.Settings.ProgressBarShowToolTip,
                TextAboveVisibility,
                TextInsideVisibility,
                TextBelowVisibility,

                ThumbFirst = SolidColorBrushFirst,
                ThumbSecond = SolidColorBrushSecond,
                ThumbThird = SolidColorBrushThird,
                ThumbPoint
            };


            if (PART_SliderContener != null)
            {
                PART_SliderContener.Height = PART_ProgressBarFirst.GetIndicatorHeight();

                PART_SliderContener.VerticalAlignment = VerticalAlignment.Stretch;
                if (PluginDatabase.PluginSettings.Settings.ProgressBarShowTimeAbove)
                {
                    PART_SliderContener.VerticalAlignment = VerticalAlignment.Bottom;
                }
                if (PluginDatabase.PluginSettings.Settings.ProgressBarShowTimeBelow)
                {
                    PART_SliderContener.VerticalAlignment = VerticalAlignment.Top;
                }
            }

            if (PART_SliderUserContener != null)
            {
                PART_SliderUserContener.Height = PART_ProgressBarFirst.GetIndicatorHeight();

                PART_SliderUserContener.VerticalAlignment = VerticalAlignment.Stretch;
                if (PluginDatabase.PluginSettings.Settings.ProgressBarShowTimeAbove)
                {
                    PART_SliderUserContener.VerticalAlignment = VerticalAlignment.Bottom;
                }
                if (PluginDatabase.PluginSettings.Settings.ProgressBarShowTimeBelow)
                {
                    PART_SliderUserContener.VerticalAlignment = VerticalAlignment.Top;
                }
            }

            if (PART_ShowToolTip != null)
            {
                PART_ShowToolTip.Height = PART_ProgressBarFirst.GetIndicatorHeight();

                PART_ShowToolTip.VerticalAlignment = VerticalAlignment.Stretch;
                if (PluginDatabase.PluginSettings.Settings.ProgressBarShowTimeAbove)
                {
                    PART_ShowToolTip.VerticalAlignment = VerticalAlignment.Bottom;
                }
                if (PluginDatabase.PluginSettings.Settings.ProgressBarShowTimeBelow)
                {
                    PART_ShowToolTip.VerticalAlignment = VerticalAlignment.Top;
                }
            }
        }

        private void PART_GridContener_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            PART_GridContener_Loaded(null, null);
        }
    }

    internal class ListProgressBar
    {
        public int Indicator { get; set; }
        public long Value { get; set; }
        public string Format { get; set; }
    }
}
