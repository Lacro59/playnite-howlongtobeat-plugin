using CommonPluginsControls.Controls;
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
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
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

        private PluginProgressBarDataContext ControlDataContext;
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


        public PluginProgressBar()
        {
            InitializeComponent();

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
            bool IsActivated = PluginDatabase.PluginSettings.Settings.EnableIntegrationViewItem;
            bool TextAboveVisibility = PluginDatabase.PluginSettings.Settings.ProgressBarShowTimeAbove; 
            bool TextInsideVisibility = PluginDatabase.PluginSettings.Settings.ProgressBarShowTimeInterior;
            bool TextBelowVisibility = PluginDatabase.PluginSettings.Settings.ProgressBarShowTimeBelow;
            if (IgnoreSettings)
            {
                IsActivated = true;
                TextAboveVisibility = false;
                TextInsideVisibility = true;
                TextBelowVisibility = false;
            }

            ControlDataContext = new PluginProgressBarDataContext
            {
                IsActivated = IsActivated,
                ShowToolTip = PluginDatabase.PluginSettings.Settings.ProgressBarShowToolTip,

                TextAboveVisibility = TextAboveVisibility,
                TextInsideVisibility = TextInsideVisibility,
                TextBelowVisibility = TextBelowVisibility,

                PlaytimeValue = 0,
                MaxValue = 0,

                ProgressBarFirstValue = 0,
                ProgressBarFirstVisibility = Visibility.Collapsed,
                ToolTipFirst = string.Empty,

                ProgressBarSecondValue = 0,
                ProgressBarSecondVisibility = Visibility.Collapsed,
                ToolTipSecond = string.Empty,

                ProgressBarThirdValue = 0,
                ProgressBarThirdVisibility = Visibility.Collapsed,
                ToolTipThird = string.Empty,

                SliderFirstValue = 0,
                SliderFirstVisibility = Visibility.Collapsed,
                ThumbFirst = null,

                SliderSecondValue = 0,
                SliderSecondVisibility = Visibility.Collapsed,
                ThumbSecond = null,

                SliderThirdValue = 0,
                SliderThirdVisibility = Visibility.Collapsed,
                ThumbThird = null
            };
        }


        public override Task<bool> SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
        {
            return Task.Run(() =>
            {
                GameHowLongToBeat gameHowLongToBeat = (GameHowLongToBeat)PluginGameData;

                this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                {
                    LoadData(gameHowLongToBeat);
                    this.DataContext = ControlDataContext;
                }));

                return true;
            });
        }


        private void LoadData(GameHowLongToBeat gameHowLongToBeat)
        {
            try
            {
                // Definied data value in different component.
                int ElIndicator = 0;
                double MaxValue = 0;
                double MaxHltb = 0;
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


                foreach (var listProgressBar in listProgressBars)
                {
                    SetDataInView(listProgressBar.Indicator, listProgressBar.Value, listProgressBar.Format);
                }


                // Show user hltb datas
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


                ControlDataContext.MaxValue = MaxValue;
                ControlDataContext.PlaytimeValue = Playtime;
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


    public class PluginProgressBarDataContext : IDataContext
    {
        public bool IsActivated { get; set; }
        public bool ShowToolTip { get; set; }

        public bool TextAboveVisibility { get; set; }
        public bool TextInsideVisibility { get; set; }
        public bool TextBelowVisibility { get; set; }
        public SolidColorBrush ThumbFirst { get; set; }
        public SolidColorBrush ThumbSecond { get; set; }
        public SolidColorBrush ThumbThird { get; set; }

        public double PlaytimeValue { get; set; }
        public double MaxValue { get; set; }


        public double ProgressBarFirstValue { get; set; }
        public Visibility ProgressBarFirstVisibility { get; set; }
        public string ToolTipFirst { get; set; }

        public double ProgressBarSecondValue { get; set; }
        public Visibility ProgressBarSecondVisibility { get; set; }
        public string ToolTipSecond { get; set; }

        public double ProgressBarThirdValue { get; set; }
        public Visibility ProgressBarThirdVisibility { get; set; }
        public string ToolTipThird { get; set; }


        public double SliderFirstValue { get; set; }
        public Visibility SliderFirstVisibility { get; set; }

        public double SliderSecondValue { get; set; }
        public Visibility SliderSecondVisibility { get; set; }

        public double SliderThirdValue { get; set; }
        public Visibility SliderThirdVisibility { get; set; }
    }

    internal class ListProgressBar
    {
        public int Indicator { get; set; }
        public long Value { get; set; }
        public string Format { get; set; }
    }


    public class CalcWidthConverter : IValueConverter
    {
        private static ILogger logger = LogManager.GetLogger();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                double.TryParse(value.ToString(), out double valueDouble);

                if (parameter is ProgressBarExtend)
                {
                    double Width = ((ProgressBarExtend)parameter).IndicatorWidth - valueDouble;

                    return (Width > 0) ? Width : 0;
                }

                return 0;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return 0;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
