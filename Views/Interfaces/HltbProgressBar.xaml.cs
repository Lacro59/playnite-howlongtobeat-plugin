using HowLongToBeat.Models;
using System;
using System.Windows.Controls;


namespace HowLongToBeat.Views.Interfaces
{
    /// <summary>
    /// Logique d'interaction pour HltbProgressBar.xaml
    /// </summary>
    public partial class HltbProgressBar : UserControl
    {
        public HltbProgressBar(long Playtime, HltbDataUser gameData)
        {
            InitializeComponent();

            // Time format
            string MainStoryFormat = gameData.GameHltbData.MainStoryFormat;
            string MaintExtraFormat = gameData.GameHltbData.MaintExtraFormat;
            string CompletionistFormat = gameData.GameHltbData.CompletionistFormat;

            string PlaytimeFormat = (int)TimeSpan.FromSeconds(Playtime).TotalHours + "h " + TimeSpan.FromSeconds(Playtime).ToString(@"mm") + "min";

            // TODO Slider is on top
            //MainStoryToolTip.Content = MainStoryFormat;
            //MainExtraToolTip.Content = MaintExtraFormat;
            //CompletionistToolTip.Content = CompletionistFormat;

            //PlaytimeToolTip.Content = PlaytimeFormat;



            long MaxValue = gameData.GameHltbData.Completionist;
            long MaxHltb = gameData.GameHltbData.Completionist;
            if (gameData.GameHltbData.Completionist != 0)
            {
                if (Playtime > gameData.GameHltbData.Completionist)
                {
                    MaxValue = Playtime;
                }
            }
            else
            {
                MaxValue = gameData.GameHltbData.MaintExtra;
                MaxHltb = gameData.GameHltbData.MaintExtra;
                if (Playtime > gameData.GameHltbData.MaintExtra)
                {
                    MaxValue = Playtime;
                }
            }


            // Limit MaxValue when playtime is more than MaxHltb
            long MaxPercent = (long)Math.Ceiling((double)(10 * MaxHltb / 100));
            if (MaxValue > MaxHltb + MaxPercent)
            {
                MaxValue = MaxHltb + MaxPercent;
            }


            // Add data
            ProgressMainStory.Value = gameData.GameHltbData.MainStory;
            ProgressMainStory.Maximum = MaxValue;

            ProgressMainExtra.Value = gameData.GameHltbData.MaintExtra;
            ProgressMainExtra.Maximum = MaxValue;

            ProgressCompletionist.Value = gameData.GameHltbData.Completionist;
            ProgressCompletionist.Maximum = MaxValue;

            SliderPlaytime.Value = Playtime;
            SliderPlaytime.Maximum = MaxValue;


            // Set Binding data
            DataContext = this;
        }
    }
}
