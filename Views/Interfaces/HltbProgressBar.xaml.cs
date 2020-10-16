using HowLongToBeat.Models;
using HowLongToBeat.Services;
using Newtonsoft.Json;
using Playnite.SDK;
using PluginCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace HowLongToBeat.Views.Interfaces
{
    /// <summary>
    /// Logique d'interaction pour HltbProgressBar.xaml
    /// </summary>
    public partial class HltbProgressBar : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private HowLongToBeatData _gameData = null;
        private long _Playtime;
        private HowLongToBeatSettings _settings;

        public bool ShowToolTip { get; set; }
        public bool ShowTime { get; set; }


        public HltbProgressBar()
        {
            InitializeComponent();

            // Set Binding data
            DataContext = this;
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
                        if (indicator != null)
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
                        if (indicator != null)
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

        public void SetHltbData(long Playtime, HowLongToBeatData gameData, HowLongToBeatSettings settings)
        {
            _gameData = gameData;
            _Playtime = Playtime;
            _settings = settings;

            ShowToolTip = settings.ProgressBarShowToolTip;
            ShowTime = settings.ProgressBarShowTime;

            if (_gameData != null && _gameData.hasData && !_gameData.isEmpty) {
                if (ShowToolTip)
                {
                    PART_ShowToolTip.Visibility = Visibility.Visible;
                }
                else
                {
                    PART_ShowToolTip.Visibility = Visibility.Collapsed;
                }

                Grid_Loaded(PART_HltbProgressBar_Contener, null);
            }
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_gameData == null || !_gameData.hasData || _gameData.isEmpty)
                {
                    return;
                }

                if (e != null)
                {
                    // Define height & width
                    var parent = ((FrameworkElement)((FrameworkElement)((FrameworkElement)((FrameworkElement)sender).Parent).Parent).Parent);

                    if (!double.IsNaN(parent.Height))
                    {
                        ((FrameworkElement)sender).Height = parent.Height;
                    }

                    if (!double.IsNaN(parent.Width))
                    {
                        ((FrameworkElement)sender).Width = parent.Width;
                    }
                }

                if (_settings.ProgressBarShowTime && !_settings.ProgressBarShowTimeInterior)
                {
                    ((FrameworkElement)sender).Height = ((FrameworkElement)sender).Height - spShowTime.Height;
                }
                else
                {
                    spShowTime.Height = 0;
                }

                if (_settings.ProgressBarShowTimeAbove)
                {
                    Grid.SetRow(spShowTime, 0);
                    Grid.SetRow(PART_HltbProgressBar_Contener, 1);
                }
                if (_settings.ProgressBarShowTimeInterior)
                {
                    Grid.SetRow(spShowTime, 0);
                    spShowTime.Height = ((FrameworkElement)sender).Height;
                }

                // Definied data value in different component.
                int ElIndicator = 0;
                long MaxValue = 0;
                long MaxHltb = 0;
                List<ListProgressBar> listProgressBars = new List<ListProgressBar>();
                if (_gameData != null)
                {
                    if (_gameData.GetData().GameHltbData.MainStory != 0)
                    {
                        ElIndicator += 1;
                        listProgressBars.Add(new ListProgressBar { Indicator = ElIndicator, Value = _gameData.GetData().GameHltbData.MainStory, Format = _gameData.GetData().GameHltbData.MainStoryFormat });
                        if (MaxValue < _gameData.GetData().GameHltbData.MainStory)
                        {
                            MaxValue = _gameData.GetData().GameHltbData.MainStory;
                        }
                    }

                    if (_gameData.GetData().GameHltbData.MainExtra != 0)
                    {
                        ElIndicator += 1;
                        listProgressBars.Add(new ListProgressBar { Indicator = ElIndicator, Value = _gameData.GetData().GameHltbData.MainExtra, Format = _gameData.GetData().GameHltbData.MainExtraFormat });
                        if (MaxValue < _gameData.GetData().GameHltbData.MainExtra)
                        {
                            MaxValue = _gameData.GetData().GameHltbData.MainExtra;
                        }
                    }

                    if (_gameData.GetData().GameHltbData.Completionist != 0)
                    {
                        ElIndicator += 1;
                        listProgressBars.Add(new ListProgressBar { Indicator = ElIndicator, Value = _gameData.GetData().GameHltbData.Completionist, Format = _gameData.GetData().GameHltbData.CompletionistFormat });
                        if (MaxValue < _gameData.GetData().GameHltbData.Completionist)
                        {
                            MaxValue = _gameData.GetData().GameHltbData.Completionist;
                        }
                    }

                    if (_gameData.GetData().GameHltbData.Solo != 0)
                    {
                        ElIndicator += 1;
                        listProgressBars.Add(new ListProgressBar { Indicator = ElIndicator, Value = _gameData.GetData().GameHltbData.Solo, Format = _gameData.GetData().GameHltbData.SoloFormat });
                        if (MaxValue < _gameData.GetData().GameHltbData.Solo)
                        {
                            MaxValue = _gameData.GetData().GameHltbData.Solo;
                        }
                    }

                    if (_gameData.GetData().GameHltbData.CoOp != 0)
                    {
                        ElIndicator += 1;
                        listProgressBars.Add(new ListProgressBar { Indicator = ElIndicator, Value = _gameData.GetData().GameHltbData.CoOp, Format = _gameData.GetData().GameHltbData.CoOpFormat });
                        if (MaxValue < _gameData.GetData().GameHltbData.CoOp)
                        {
                            MaxValue = _gameData.GetData().GameHltbData.CoOp;
                        }
                    }

                    if (_gameData.GetData().GameHltbData.Vs != 0)
                    {
                        ElIndicator += 1;
                        listProgressBars.Add(new ListProgressBar { Indicator = ElIndicator, Value = _gameData.GetData().GameHltbData.Vs, Format = _gameData.GetData().GameHltbData.VsFormat });
                        if (MaxValue < _gameData.GetData().GameHltbData.Vs)
                        {
                            MaxValue = _gameData.GetData().GameHltbData.Vs;
                        }
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
                logger.Debug($"HowLongToBeat - listProgressBars: {JsonConvert.SerializeObject(listProgressBars)}");
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
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "HowLongToBeat", "Error on Grid_Loaded()");
            }
        }
    }

    public class ListProgressBar
    {
        public int Indicator { get; set; }
        public long Value { get; set; }
        public string Format { get; set; }
    }
}