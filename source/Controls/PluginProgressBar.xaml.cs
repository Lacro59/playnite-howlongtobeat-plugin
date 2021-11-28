using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Interfaces;
using HowLongToBeat.Models;
using HowLongToBeat.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
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
        private HowLongToBeatDatabase PluginDatabase = HowLongToBeat.PluginDatabase;
        internal override IPluginDatabase _PluginDatabase
        {
            get
            {
                return PluginDatabase;
            }
            set
            {
                PluginDatabase = (HowLongToBeatDatabase)_PluginDatabase;
            }
        }

        private PluginProgressBarDataContext ControlDataContext = new PluginProgressBarDataContext();
        internal override IDataContext _ControlDataContext
        {
            get
            {
                return ControlDataContext;
            }
            set
            {
                ControlDataContext = (PluginProgressBarDataContext)_ControlDataContext;
            }
        }

        private bool ShowUserData = true;


        public PluginProgressBar()
        {
            InitializeComponent();
            this.DataContext = ControlDataContext;

            Task.Run(() =>
            {
                // Wait extension database are loaded
                System.Threading.SpinWait.SpinUntil(() => PluginDatabase.IsLoaded, -1);

                this.Dispatcher.BeginInvoke((Action)delegate
                {
                    PluginDatabase.PluginSettings.PropertyChanged += PluginSettings_PropertyChanged;
                    PluginDatabase.Database.ItemUpdated += Database_ItemUpdated;
                    PluginDatabase.Database.ItemCollectionChanged += Database_ItemCollectionChanged;
                    PluginDatabase.PlayniteApi.Database.Games.ItemUpdated += Games_ItemUpdated;

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
                TitleList titleList = PluginDatabase.GetUserHltbData(gameHowLongToBeat.GetData().Id);

                if (gameHowLongToBeat.HasData)
                {
                    var HltbData = gameHowLongToBeat.GetData();

                    if (HltbData.GameHltbData.MainStory > 0)
                    {
                        ElIndicator += 1;
                        listProgressBars.Add(new ListProgressBar { Indicator = ElIndicator, Value = HltbData.GameHltbData.MainStory, Format = HltbData.GameHltbData.MainStoryFormat });
                        if (MaxValue < HltbData.GameHltbData.MainStory)
                        {
                            MaxValue = HltbData.GameHltbData.MainStory;
                        }
                        SetColor(ElIndicator, PluginDatabase.PluginSettings.Settings.ColorFirst.Color);


                        // Show user hltb datas
                        if (gameHowLongToBeat.HasData && ShowUserData)
                        {
                            if (titleList?.HltbUserData?.MainStory > 0)
                            {
                                SetUserData(ElIndicator, titleList.HltbUserData.MainStory, PluginDatabase.PluginSettings.Settings.ColorFirst.Color);
                            }
                        }
                    }

                    if (HltbData.GameHltbData.MainExtra > 0)
                    {
                        ElIndicator += 1;
                        listProgressBars.Add(new ListProgressBar { Indicator = ElIndicator, Value = HltbData.GameHltbData.MainExtra, Format = HltbData.GameHltbData.MainExtraFormat });
                        if (MaxValue < HltbData.GameHltbData.MainExtra)
                        {
                            MaxValue = HltbData.GameHltbData.MainExtra;
                        }
                        SetColor(ElIndicator, PluginDatabase.PluginSettings.Settings.ColorSecond.Color);


                        // Show user hltb datas
                        if (gameHowLongToBeat.HasData && ShowUserData)
                    {
                            if (titleList?.HltbUserData?.MainExtra > 0)
                            {
                                SetUserData(ElIndicator, titleList.HltbUserData.MainExtra, PluginDatabase.PluginSettings.Settings.ColorSecond.Color);
                            }
                        }
                    }

                    if (HltbData.GameHltbData.Completionist != 0)
                    {
                        ElIndicator += 1;
                        listProgressBars.Add(new ListProgressBar { Indicator = ElIndicator, Value = HltbData.GameHltbData.Completionist, Format = HltbData.GameHltbData.CompletionistFormat });
                        if (MaxValue < HltbData.GameHltbData.Completionist)
                        {
                            MaxValue = HltbData.GameHltbData.Completionist;
                        }
                        SetColor(ElIndicator, PluginDatabase.PluginSettings.Settings.ColorThird.Color);


                        // Show user hltb datas
                        if (gameHowLongToBeat.HasData && ShowUserData)
                        {
                            if (titleList?.HltbUserData?.Completionist > 0)
                            {
                                SetUserData(ElIndicator, titleList.HltbUserData.Completionist, PluginDatabase.PluginSettings.Settings.ColorThird.Color);
                            }
                        }
                    }

                    if (HltbData.GameHltbData.Solo != 0)
                    {
                        ElIndicator += 1;
                        listProgressBars.Add(new ListProgressBar { Indicator = ElIndicator, Value = HltbData.GameHltbData.Solo, Format = HltbData.GameHltbData.SoloFormat });
                        if (MaxValue < HltbData.GameHltbData.Solo)
                        {
                            MaxValue = HltbData.GameHltbData.Solo;
                        }
                        SetColor(ElIndicator, PluginDatabase.PluginSettings.Settings.ColorFirstMulti.Color);


                        // Show user hltb datas
                        if (gameHowLongToBeat.HasData && ShowUserData)
                        {
                            if (titleList?.HltbUserData?.Solo > 0)
                            {
                                SetUserData(ElIndicator, titleList.HltbUserData.Solo, PluginDatabase.PluginSettings.Settings.ColorFirstMulti.Color);
                            }
                        }
                    }

                    if (HltbData.GameHltbData.CoOp != 0)
                    {
                        ElIndicator += 1;
                        listProgressBars.Add(new ListProgressBar { Indicator = ElIndicator, Value = HltbData.GameHltbData.CoOp, Format = HltbData.GameHltbData.CoOpFormat });
                        if (MaxValue < HltbData.GameHltbData.CoOp)
                        {
                            MaxValue = HltbData.GameHltbData.CoOp;
                        }
                        SetColor(ElIndicator, PluginDatabase.PluginSettings.Settings.ColorSecondMulti.Color);


                        // Show user hltb datas
                        if (gameHowLongToBeat.HasData && ShowUserData)
                        {
                            if (titleList?.HltbUserData?.CoOp > 0)
                            {
                                SetUserData(ElIndicator, titleList.HltbUserData.CoOp, PluginDatabase.PluginSettings.Settings.ColorSecondMulti.Color);
                            }
                        }
                    }

                    if (HltbData.GameHltbData.Vs != 0)
                    {
                        ElIndicator += 1;
                        listProgressBars.Add(new ListProgressBar { Indicator = ElIndicator, Value = HltbData.GameHltbData.Vs, Format = HltbData.GameHltbData.VsFormat });
                        if (MaxValue < HltbData.GameHltbData.Vs)
                        {
                            MaxValue = HltbData.GameHltbData.Vs;
                        }
                        SetColor(ElIndicator, PluginDatabase.PluginSettings.Settings.ColorThirdMulti.Color);


                        // Show user hltb datas
                        if (gameHowLongToBeat.HasData && ShowUserData)
                        {
                            if (titleList?.HltbUserData?.Vs > 0)
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


                foreach (var listProgressBar in listProgressBars)
                {
                    SetDataInView(listProgressBar.Indicator, listProgressBar.Value, listProgressBar.Format);
                }


                ControlDataContext.MaxValue = MaxValue;
                ControlDataContext.PlaytimeValue = Playtime;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, "HowLongToBeat");
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
            }
        }

        private void SetColor(int ElIndicator, Color color)
        {
            switch (ElIndicator)
            {
                case 1:
                    ControlDataContext.ThumbFirst = new SolidColorBrush(color);
                    break;

                case 2:
                    ControlDataContext.ThumbSecond = new SolidColorBrush(color);
                    break;

                case 3:
                    ControlDataContext.ThumbThird = new SolidColorBrush(color);
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
            double Width1 = PART_ProgressBarFirst.GetIndicatorWidth();
            double Width2 = PART_ProgressBarSecond.GetIndicatorWidth();
            double Width3 = PART_ProgressBarThird.GetIndicatorWidth();


            if (PART_ProgressBarThird.IsVisible)
            {
                spHltb_El3.Width = (Width3 - Width2 > 0) ? Width3 - Width2 : 0;
                PART_ProgressBarThird.MarginWidth = Width2;
                PART_ProgressBarThird.TextWidth = (Width3 - Width2 > 0) ? Width3 - Width2 : 0;
            }

            if (PART_ProgressBarSecond.IsVisible)
            {
                spHltb_El2.Width = (Width2 - Width1 > 0) ? Width2 - Width1 : 0;
                PART_ProgressBarSecond.MarginWidth = Width1;
                PART_ProgressBarSecond.TextWidth = (Width2 - Width1 > 0) ? Width2 - Width1 : 0;
            }

            if (PART_ProgressBarFirst.IsVisible)
            {
                spHltb_El1.Width = Width1;
                PART_ProgressBarFirst.TextWidth = Width1;
            }
        }
        #endregion
    }


    public class PluginProgressBarDataContext : ObservableObject, IDataContext
    {
        private bool _IsActivated;
        public bool IsActivated { get => _IsActivated; set => SetValue(ref _IsActivated, value); }

        private bool _ShowToolTip;
        public bool ShowToolTip { get => _ShowToolTip; set => SetValue(ref _ShowToolTip, value); }

        private bool _TextAboveVisibility;
        public bool TextAboveVisibility { get => _TextAboveVisibility; set => SetValue(ref _TextAboveVisibility, value); }

        private bool _TextInsideVisibility = true;
        public bool TextInsideVisibility { get => _TextInsideVisibility; set => SetValue(ref _TextInsideVisibility, value); }

        private bool _TextBelowVisibility;
        public bool TextBelowVisibility { get => _TextBelowVisibility; set => SetValue(ref _TextBelowVisibility, value); }

        private SolidColorBrush _ThumbFirst = new SolidColorBrush(Brushes.DarkCyan.Color);
        public SolidColorBrush ThumbFirst { get => _ThumbFirst; set => SetValue(ref _ThumbFirst, value); }

        private SolidColorBrush _ThumbSecond = new SolidColorBrush(Brushes.RoyalBlue.Color);
        public SolidColorBrush ThumbSecond { get => _ThumbSecond; set => SetValue(ref _ThumbSecond, value); }

        private SolidColorBrush _ThumbThird = new SolidColorBrush(Brushes.ForestGreen.Color);
        public SolidColorBrush ThumbThird { get => _ThumbThird; set => SetValue(ref _ThumbThird, value); }

        private double _PlaytimeValue = 75;
        public double PlaytimeValue { get => _PlaytimeValue; set => SetValue(ref _PlaytimeValue, value); }

        private double _MaxValue = 100;
        public double MaxValue { get => _MaxValue; set => SetValue(ref _MaxValue, value); }

        private double _ProgressBarFirstValue;
        public double ProgressBarFirstValue { get => _ProgressBarFirstValue; set => SetValue(ref _ProgressBarFirstValue, value); }

        private Visibility _ProgressBarFirstVisibility;
        public Visibility ProgressBarFirstVisibility { get => _ProgressBarFirstVisibility; set => SetValue(ref _ProgressBarFirstVisibility, value); }

        private string _ToolTipFirst;
        public string ToolTipFirst { get => _ToolTipFirst; set => SetValue(ref _ToolTipFirst, value); }

        private double _ProgressBarSecondValue;
        public double ProgressBarSecondValue { get => _ProgressBarSecondValue; set => SetValue(ref _ProgressBarSecondValue, value); }

        private Visibility _ProgressBarSecondVisibility;
        public Visibility ProgressBarSecondVisibility { get => _ProgressBarSecondVisibility; set => SetValue(ref _ProgressBarSecondVisibility, value); }

        private string _ToolTipSecond;
        public string ToolTipSecond { get => _ToolTipSecond; set => SetValue(ref _ToolTipSecond, value); }

        private double _ProgressBarThirdValue;
        public double ProgressBarThirdValue { get => _ProgressBarThirdValue; set => SetValue(ref _ProgressBarThirdValue, value); }

        private Visibility _ProgressBarThirdVisibility;
        public Visibility ProgressBarThirdVisibility { get => _ProgressBarThirdVisibility; set => SetValue(ref _ProgressBarThirdVisibility, value); }

        private string _ToolTipThird; 
        public string ToolTipThird { get => _ToolTipThird; set => SetValue(ref _ToolTipThird, value); }

        private double _SliderFirstValue;
        public double SliderFirstValue { get => _SliderFirstValue; set => SetValue(ref _SliderFirstValue, value); }

        private Visibility _SliderFirstVisibility;
        public Visibility SliderFirstVisibility { get => _SliderFirstVisibility; set => SetValue(ref _SliderFirstVisibility, value); }

        private double _SliderSecondValue;
        public double SliderSecondValue { get => _SliderSecondValue; set => SetValue(ref _SliderSecondValue, value); }

        private Visibility _SliderSecondVisibility;
        public Visibility SliderSecondVisibility { get => _SliderSecondVisibility; set => SetValue(ref _SliderSecondVisibility, value); }

        private double _SliderThirdValue;
        public double SliderThirdValue { get => _SliderThirdValue; set => SetValue(ref _SliderThirdValue, value); }

        private Visibility _SliderThirdVisibility;
        public Visibility SliderThirdVisibility { get => _SliderThirdVisibility; set => SetValue(ref _SliderThirdVisibility, value); }
    }


    internal class ListProgressBar
    {
        public int Indicator { get; set; }
        public long Value { get; set; }
        public string Format { get; set; }
    }
}
