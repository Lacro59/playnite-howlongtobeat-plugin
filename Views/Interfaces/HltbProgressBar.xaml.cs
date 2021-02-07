using CommonPluginsShared;
using HowLongToBeat.Models;
using HowLongToBeat.Services;
using Newtonsoft.Json;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace HowLongToBeat.Views.Interfaces
{
    /// <summary>
    /// Logique d'interaction pour HltbProgressBar.xaml
    /// </summary>
    public partial class HltbProgressBar : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private HowLongToBeatDatabase PluginDatabase = HowLongToBeat.PluginDatabase;

        private GameHowLongToBeat _gameHowLongToBeat = null;
        private long _Playtime;

        public bool ShowToolTip { get; set; }
        public bool ShowTime { get; set; }

        public PointCollection ThumbPoint { get; set; }
        public SolidColorBrush solidColorBrushFirst { get; set; }
        public SolidColorBrush solidColorBrushSecond { get; set; }
        public SolidColorBrush solidColorBrushThird { get; set; }


        public HltbProgressBar()
        {
            InitializeComponent();

            PluginDatabase.PropertyChanged += OnPropertyChanged;
        }


        protected void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
                if (e.PropertyName == "GameSelectedData" || e.PropertyName == "PluginSettings")
                {
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                    {   
                        if (!PluginDatabase.GameSelectedData.HasData)
                        {
                            this.Visibility = Visibility.Collapsed;
                            return;
                        }
                        else
                        {
                            this.Visibility = Visibility.Hidden;
                        }
                    }));

                    this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
                    {
                        SetHltbData(PluginDatabase.GameSelectedData);

                        this.Visibility = Visibility.Visible;
                    }));
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "HowLongToBeat");
            }
        }


        private void SetDataInView(int ElIndicator, long ElValue, string ElFormat)
        {
            Decorator indicator = null;
            Rectangle track = null;

            try
            {
                switch (ElIndicator)
                {
                    case 1:
                        if (ElValue != 0)
                        {
                            ProgressHltb_El1.Visibility = Visibility.Visible;
                            spHltb_El1.Visibility = Visibility.Visible;
                            spHltbTime_El1.Visibility = Visibility.Visible;
                        }

                        ProgressHltb_El1.Value = ElValue;
                        spHltb_El1.Visibility = Visibility.Visible;

                        indicator = (Decorator)ProgressHltb_El1.Template.FindName("PART_Indicator", ProgressHltb_El1);
                        track = (Rectangle)ProgressHltb_El1.Template.FindName("PART_Track", ProgressHltb_El1);

                        // ToolTip
                        if (indicator != null && track != null)
                        {
                            spHltb_El1.Width = indicator.Width;
                            tpHltb_El1.Content = ElFormat;

                            if (track.Width == indicator.Width)
                            {
                                spHltb_El1.Width = track.ActualWidth;
                            }
                        }

                        // Time
                        spHltbTime_El1.Content = ElFormat;
                        break;

                    case 2:
                        if (ElValue != 0)
                        {
                            ProgressHltb_El2.Visibility = Visibility.Visible;
                            spHltb_El2.Visibility = Visibility.Visible;
                            spHltbTime_El2.Visibility = Visibility.Visible;
                        }

                        ProgressHltb_El2.Value = ElValue;


                        indicator = (Decorator)ProgressHltb_El2.Template.FindName("PART_Indicator", ProgressHltb_El2);

                        // ToolTip
                        if (indicator != null && (indicator.Width - spHltb_El1.Width >= 0))
                        {
                            spHltb_El2.Width = indicator.Width - spHltb_El1.Width;
                            tpHltb_El2.Content = ElFormat;
                        }

                        // Time
                        spHltbTime_El2.Content = ElFormat;
                        break;

                    case 3:
                        if (ElValue != 0)
                        {
                            ProgressHltb_El3.Visibility = Visibility.Visible;
                            spHltb_El3.Visibility = Visibility.Visible;
                            spHltbTime_El3.Visibility = Visibility.Visible;
                        }

                        ProgressHltb_El3.Value = ElValue;

                        indicator = (Decorator)ProgressHltb_El3.Template.FindName("PART_Indicator", ProgressHltb_El3);

                        // ToolTip
                        if (indicator != null && (indicator.Width - spHltb_El2.Width - spHltb_El1.Width >= 0))
                        {
                            spHltb_El3.Width = indicator.Width - spHltb_El2.Width - spHltb_El1.Width;
                            tpHltb_El3.Content = ElFormat;
                        }

                        // Time
                        spHltbTime_El3.Content = ElFormat;
                        break;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "HowLongToBeat", $"Error on SetDataInView({ElIndicator}, {ElValue}, {ElFormat})");
            }
        }

        public void SetHltbData(GameHowLongToBeat gameHowLongToBeat)
        {
            _gameHowLongToBeat = gameHowLongToBeat;
            _Playtime = gameHowLongToBeat.Playtime;


            ShowToolTip = PluginDatabase.PluginSettings.ProgressBarShowToolTip;
            ShowTime = PluginDatabase.PluginSettings.ProgressBarShowTime;


            if (ShowToolTip)
            {
                PART_ShowToolTip.Visibility = Visibility.Visible;
            }
            else
            {
                PART_ShowToolTip.Visibility = Visibility.Collapsed;
            }

            LoadData();
        }
         

        private void SetColor(int ElIndicator, Color color)
        {
            switch (ElIndicator)
            {
                case 1:
                    ProgressHltb_El1.Foreground = new SolidColorBrush(color);
                    break;

                case 2:
                    ProgressHltb_El2.Foreground = new SolidColorBrush(color);
                    break;

                case 3:
                    ProgressHltb_El3.Foreground = new SolidColorBrush(color);
                    break;
            }
        }


        private void LoadData()
        {
            try
            {
                // Definied data value in different component.
                int ElIndicator = 0;
                long MaxValue = 0;
                long MaxHltb = 0;
                List<ListProgressBar> listProgressBars = new List<ListProgressBar>();
                if (_gameHowLongToBeat.HasData)
                {
                    var HltbData = _gameHowLongToBeat.GetData();

                    if (HltbData.GameHltbData.MainStory != 0)
                    {
                        ElIndicator += 1;
                        listProgressBars.Add(new ListProgressBar { Indicator = ElIndicator, Value = HltbData.GameHltbData.MainStory, Format = HltbData.GameHltbData.MainStoryFormat });
                        if (MaxValue < HltbData.GameHltbData.MainStory)
                        {
                            MaxValue = HltbData.GameHltbData.MainStory;
                        }
                        SetColor(ElIndicator, PluginDatabase.PluginSettings.ColorFirst);
                    }

                    if (HltbData.GameHltbData.MainExtra != 0)
                    {
                        ElIndicator += 1;
                        listProgressBars.Add(new ListProgressBar { Indicator = ElIndicator, Value = HltbData.GameHltbData.MainExtra, Format = HltbData.GameHltbData.MainExtraFormat });
                        if (MaxValue < HltbData.GameHltbData.MainExtra)
                        {
                            MaxValue = HltbData.GameHltbData.MainExtra;
                        }
                        SetColor(ElIndicator, PluginDatabase.PluginSettings.ColorSecond);
                    }

                    if (HltbData.GameHltbData.Completionist != 0)
                    {
                        ElIndicator += 1;
                        listProgressBars.Add(new ListProgressBar { Indicator = ElIndicator, Value = HltbData.GameHltbData.Completionist, Format = HltbData.GameHltbData.CompletionistFormat });
                        if (MaxValue < HltbData.GameHltbData.Completionist)
                        {
                            MaxValue = HltbData.GameHltbData.Completionist;
                        }
                        SetColor(ElIndicator, PluginDatabase.PluginSettings.ColorThird);
                    }

                    if (HltbData.GameHltbData.Solo != 0)
                    {
                        ElIndicator += 1;
                        listProgressBars.Add(new ListProgressBar { Indicator = ElIndicator, Value = HltbData.GameHltbData.Solo, Format = HltbData.GameHltbData.SoloFormat });
                        if (MaxValue < HltbData.GameHltbData.Solo)
                        {
                            MaxValue = HltbData.GameHltbData.Solo;
                        }
                        SetColor(ElIndicator, PluginDatabase.PluginSettings.ColorFirstMulti);
                    }

                    if (HltbData.GameHltbData.CoOp != 0)
                    {
                        ElIndicator += 1;
                        listProgressBars.Add(new ListProgressBar { Indicator = ElIndicator, Value = HltbData.GameHltbData.CoOp, Format = HltbData.GameHltbData.CoOpFormat });
                        if (MaxValue < HltbData.GameHltbData.CoOp)
                        {
                            MaxValue = HltbData.GameHltbData.CoOp;
                        }
                        SetColor(ElIndicator, PluginDatabase.PluginSettings.ColorSecondMulti);
                    }

                    if (HltbData.GameHltbData.Vs != 0)
                    {
                        ElIndicator += 1;
                        listProgressBars.Add(new ListProgressBar { Indicator = ElIndicator, Value = HltbData.GameHltbData.Vs, Format = HltbData.GameHltbData.VsFormat });
                        if (MaxValue < HltbData.GameHltbData.Vs)
                        {
                            MaxValue = HltbData.GameHltbData.Vs;
                        }
                        SetColor(ElIndicator, PluginDatabase.PluginSettings.ColorThirdMulti);
                    }
                }

                // Define the maxvalue for progressbar & slider
                MaxHltb = MaxValue;
                if (_Playtime > MaxValue)
                {
                    MaxValue = _Playtime;
                }

                // Adjust position tracker
                if (_Playtime > 69)
                {
                    SliderPlaytime.Margin = new Thickness(-8, 0, -3, 0);
                }

                // Limit MaxValue when playtime is more than MaxHltb
                long MaxPercent = (long)Math.Ceiling((double)(10 * MaxHltb / 100));
                if (MaxValue > MaxHltb + MaxPercent)
                {
                    MaxValue = MaxHltb + MaxPercent;
                }


                // Add data
                ProgressHltb_El1.Maximum = MaxValue;
                ProgressHltb_El2.Maximum = MaxValue;
                ProgressHltb_El3.Maximum = MaxValue;

                SliderPlaytime.Value = _Playtime;
                SliderPlaytime.Maximum = MaxValue;

#if DEBUG
                logger.Debug($"HowLongToBeat [Ignored] - listProgressBars: {JsonConvert.SerializeObject(listProgressBars)}");
#endif

                ProgressHltb_El1.Visibility = Visibility.Hidden;
                spHltb_El1.Visibility = Visibility.Hidden;
                spHltbTime_El1.Visibility = Visibility.Hidden;

                ProgressHltb_El2.Visibility = Visibility.Hidden;
                spHltb_El2.Visibility = Visibility.Hidden;
                spHltbTime_El2.Visibility = Visibility.Hidden;

                ProgressHltb_El3.Visibility = Visibility.Hidden;
                spHltb_El3.Visibility = Visibility.Hidden;
                spHltbTime_El3.Visibility = Visibility.Hidden;

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

                if (_gameHowLongToBeat.HasData && PluginDatabase.PluginSettings.ProgressBarShowTimeUser)
                {
                    TitleList titleList = PluginDatabase.GetUserHltbData(_gameHowLongToBeat.GetData().Id);
#if DEBUG
                    logger.Debug($"HowLongToBeat [Ignored] - titleList: {JsonConvert.SerializeObject(titleList)}");
#endif
                    if (titleList != null)
                    {
                        ElIndicator = 0;

                        if (titleList.HltbUserData.MainStory != 0)
                        {
                            ElIndicator++;
                            SetUserData(ElIndicator, titleList.HltbUserData.MainStory, PluginDatabase.PluginSettings.ColorFirst);
                        }

                        if (titleList.HltbUserData.MainExtra != 0)
                        {
                            ElIndicator++;
                            SetUserData(ElIndicator, titleList.HltbUserData.MainExtra, PluginDatabase.PluginSettings.ColorSecond);
                        }

                        if (titleList.HltbUserData.Completionist != 0)
                        {
                            ElIndicator++;
                            SetUserData(ElIndicator, titleList.HltbUserData.Completionist, PluginDatabase.PluginSettings.ColorThird);
                        }

                        if (titleList.HltbUserData.Solo != 0)
                        {
                            ElIndicator++;
                            SetUserData(ElIndicator, titleList.HltbUserData.Solo, PluginDatabase.PluginSettings.ColorFirstMulti);
                        }

                        if (titleList.HltbUserData.CoOp != 0)
                        {
                            ElIndicator++;
                            SetUserData(ElIndicator, titleList.HltbUserData.CoOp, PluginDatabase.PluginSettings.ColorSecondMulti);
                        }

                        if (titleList.HltbUserData.Vs != 0)
                        {
                            ElIndicator++;
                            SetUserData(ElIndicator, titleList.HltbUserData.Vs, PluginDatabase.PluginSettings.ColorThirdMulti);
                        }

                        this.DataContext = new
                        {
                            ThumbFirst = solidColorBrushFirst,
                            ThumbSecond = solidColorBrushSecond,
                            ThumbThird = solidColorBrushThird,

                            ThumbPoint = ThumbPoint
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "HowLongToBeat", "Error on LoadData()");
            }
        }

        public void SetUserData(int ElIndicator, long Value, Color color)
        {
            switch (ElIndicator)
            {
                case 1:
                    PartSliderFirst.Value = Value;
                    PartSliderFirst.Visibility = Visibility.Visible;
                    solidColorBrushFirst = new SolidColorBrush(color);
                    break;
                case 2:
                    PartSliderSecond.Value = Value;
                    PartSliderSecond.Visibility = Visibility.Visible;
                    solidColorBrushSecond = new SolidColorBrush(color);

                    break;
                case 3:
                    PartSliderThird.Value = Value;
                    PartSliderThird.Visibility = Visibility.Visible;
                    solidColorBrushThird = new SolidColorBrush(color);
                    break;
            }
        }


        public void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            IntegrationUI.SetControlSize(PART_HltbProgressBar_Contener);

            try
            {
                if (PluginDatabase.PluginSettings.ProgressBarShowTime && !PluginDatabase.PluginSettings.ProgressBarShowTimeInterior)
                {
                    PART_HltbProgressBar_Contener.Height = PART_HltbProgressBar_Contener.Height - spShowTime.Height;
                }
                else
                {
                    spShowTime.Height = 0;
                }

                if (PluginDatabase.PluginSettings.ProgressBarShowTime && PluginDatabase.PluginSettings.ProgressBarShowTimeAbove)
                {
                    Grid.SetRow(spShowTime, 0);
                    Grid.SetRow(PART_HltbProgressBar_Contener, 1);
                }
                if (PluginDatabase.PluginSettings.ProgressBarShowTime && PluginDatabase.PluginSettings.ProgressBarShowTimeInterior)
                {
                    Grid.SetRow(spShowTime, 0);
                    spShowTime.Height = PART_HltbProgressBar_Contener.Height;
                }

                double SliderHeight = PART_HltbProgressBar_Contener.Height / 2;
                PartSliderFirst.Height = SliderHeight;
                PartSliderSecond.Height = SliderHeight;
                PartSliderThird.Height = SliderHeight;

                PartSliderFirst.Margin = SliderPlaytime.Margin;
                PartSliderSecond.Margin = SliderPlaytime.Margin;
                PartSliderThird.Margin = SliderPlaytime.Margin;

                Point Point1 = new Point(SliderHeight/2.5, 0);
                Point Point2 = new Point(SliderHeight/1.25, SliderHeight / 1.25);
                Point Point3 = new Point(0, SliderHeight / 1.25);
                ThumbPoint = new PointCollection();
                ThumbPoint.Add(Point1);
                ThumbPoint.Add(Point2);
                ThumbPoint.Add(Point3);

                this.DataContext = new
                {
                    ThumbFirst = solidColorBrushFirst,
                    ThumbSecond = solidColorBrushSecond,
                    ThumbThird = solidColorBrushThird,

                    ThumbPoint = ThumbPoint
                };
            }
            catch
            {

            }
        }


        private void PART_Indicator1_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            spHltb_El1.Width = ((Decorator)sender).ActualWidth;
        }
        private void PART_Indicator2_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (((Decorator)sender).ActualWidth - spHltb_El1.Width >= 0)
            {
                spHltb_El2.Width = ((Decorator)sender).ActualWidth - spHltb_El1.Width;
            }
        }
        private void PART_Indicator3_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (((Decorator)sender).ActualWidth - spHltb_El2.Width - spHltb_El1.Width >= 0)
            {
                spHltb_El3.Width = ((Decorator)sender).ActualWidth - spHltb_El2.Width - spHltb_El1.Width;
            }
        }
    }

    internal class ListProgressBar
    {
        public int Indicator { get; set; }
        public long Value { get; set; }
        public string Format { get; set; }
    }
}