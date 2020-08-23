using HowLongToBeat.Models;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Shapes;

namespace HowLongToBeat.Views.Interfaces
{
    /// <summary>
    /// Logique d'interaction pour HltbProgressBar.xaml
    /// </summary>
    public partial class HltbProgressBar : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private HltbDataUser gameData;
        private long Playtime;
        private HowLongToBeatSettings settings;

        public bool ShowToolTip { get; set; }
        public bool ShowTime { get; set; }

        public HltbProgressBar(long Playtime, HltbDataUser gameData, HowLongToBeatSettings settings)
        {
            InitializeComponent();

            this.gameData = gameData;
            this.Playtime = Playtime;
            this.settings = settings;

            ShowToolTip = settings.ProgressBarShowToolTip;
            ShowTime = settings.ProgressBarShowTime;

            // Set Binding data
            DataContext = this;
        }

        private void SetDataInView(int ElIndicator, long ElValue, string ElFormat)
        {
            Decorator indicator = null;
            Rectangle track = null;
            switch (ElIndicator)
            {
                case 1:
                    ProgressHltb_El1.Value = ElValue;

                    indicator = (Decorator)ProgressHltb_El1.Template.FindName("PART_Indicator", ProgressHltb_El1);
                    track = (Rectangle)ProgressHltb_El1.Template.FindName("PART_Track", ProgressHltb_El1);

                    // ToolTip
                    spHltb_El1.Width = indicator.Width;
                    tpHltb_El1.Content = ElFormat;

                    if (track.Width == indicator.Width)
                    {
                        spHltb_El1.Width = track.ActualWidth;
                    }

                    // Time
                    spHltbTime_El1.Content = ElFormat;
                    break;

                case 2:
                    ProgressHltb_El2.Value = ElValue;

                    indicator = (Decorator)ProgressHltb_El2.Template.FindName("PART_Indicator", ProgressHltb_El2);

                    // ToolTip
                    spHltb_El2.Width = indicator.Width - spHltb_El1.Width;
                    tpHltb_El2.Content = ElFormat;

                    // Time
                    spHltbTime_El2.Content = ElFormat;
                    break;

                case 3:
                    ProgressHltb_El3.Value = ElValue;

                    indicator = (Decorator)ProgressHltb_El3.Template.FindName("PART_Indicator", ProgressHltb_El3);

                    // ToolTip
                    spHltb_El3.Width = indicator.Width - spHltb_El2.Width - spHltb_El1.Width;
                    tpHltb_El3.Content = ElFormat;

                    // Time
                    spHltbTime_El3.Content = ElFormat;
                    break;
            }
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
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

            if (settings.ProgressBarShowTime)
            {
                ((FrameworkElement)sender).Height = ((FrameworkElement)sender).Height - spShowTime.Height;
            }
            else
            {
                spShowTime.Height = 0;
            }

            if (settings.ProgressBarShowTimeAbove)
            {
                Grid.SetRow(spShowTime, 0);
                Grid.SetRow(PART_HltbProgressBar_Contener, 1);
            }
            if (settings.ProgressBarShowTimeInterior)
            {
                Grid.SetRow(spShowTime, 0);
            }


            // Definied data value in different component.
            int ElIndicator = 0;
            long MaxValue = 0;
            long MaxHltb = 0;
            List<ListProgressBar> listProgressBars = new List<ListProgressBar>();
            if (gameData != null)
            {
                if (gameData.GameHltbData.MainStory != 0)
                {
                    ElIndicator += 1;
                    listProgressBars.Add(new ListProgressBar { Indicator = ElIndicator, Value = gameData.GameHltbData.MainStory, Format = gameData.GameHltbData.MainStoryFormat });
                    if (MaxValue < gameData.GameHltbData.MainStory)
                    {
                        MaxValue = gameData.GameHltbData.MainStory;
                    }
                }

                if (gameData.GameHltbData.MainExtra != 0)
                {
                    ElIndicator += 1;
                    listProgressBars.Add(new ListProgressBar { Indicator = ElIndicator, Value = gameData.GameHltbData.MainExtra, Format = gameData.GameHltbData.MainExtraFormat });
                    if (MaxValue < gameData.GameHltbData.MainExtra)
                    {
                        MaxValue = gameData.GameHltbData.MainExtra;
                    }
                }

                if (gameData.GameHltbData.Completionist != 0)
                {
                    ElIndicator += 1;
                    listProgressBars.Add(new ListProgressBar { Indicator = ElIndicator, Value = gameData.GameHltbData.Completionist, Format = gameData.GameHltbData.CompletionistFormat });
                    if (MaxValue < gameData.GameHltbData.Completionist)
                    {
                        MaxValue = gameData.GameHltbData.Completionist;
                    }
                }

                if (gameData.GameHltbData.Solo != 0)
                {
                    ElIndicator += 1;
                    listProgressBars.Add(new ListProgressBar { Indicator = ElIndicator, Value = gameData.GameHltbData.Solo, Format = gameData.GameHltbData.SoloFormat });
                    if (MaxValue < gameData.GameHltbData.Solo)
                    {
                        MaxValue = gameData.GameHltbData.Solo;
                    }
                }

                if (gameData.GameHltbData.CoOp != 0)
                {
                    ElIndicator += 1;
                    listProgressBars.Add(new ListProgressBar { Indicator = ElIndicator, Value = gameData.GameHltbData.CoOp, Format = gameData.GameHltbData.CoOpFormat });
                    if (MaxValue < gameData.GameHltbData.CoOp)
                    {
                        MaxValue = gameData.GameHltbData.CoOp;
                    }
                }

                if (gameData.GameHltbData.Vs != 0)
                {
                    ElIndicator += 1;
                    listProgressBars.Add(new ListProgressBar { Indicator = ElIndicator, Value = gameData.GameHltbData.Vs, Format = gameData.GameHltbData.VsFormat });
                    if (MaxValue < gameData.GameHltbData.Vs)
                    {
                        MaxValue = gameData.GameHltbData.Vs;
                    }
                }
            }

            // Define the maxvalue for progressbar & slider
            MaxHltb = MaxValue;
            if (Playtime > MaxValue)
            {
                MaxValue = Playtime;
            }

            // Adjust position tracker
            if (Playtime > 69)
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

            SliderPlaytime.Value = Playtime;
            SliderPlaytime.Maximum = MaxValue;

            foreach (var listProgressBar in listProgressBars)
            {
                SetDataInView(listProgressBar.Indicator, listProgressBar.Value, listProgressBar.Format);
            }


            SliderPlaytime.UpdateLayout();
        }
    }

    public class ListProgressBar
    {
        public int Indicator { get; set; }
        public long Value { get; set; }
        public string Format { get; set; }
    }
}
