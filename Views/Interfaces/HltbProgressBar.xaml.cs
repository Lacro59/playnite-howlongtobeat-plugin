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


            int ElIndicator = 0;
            long MaxValue = 0;
            long MaxHltb = 0;

            if (gameData.GameHltbData.MainStory != 0)
            {
                ElIndicator += 1;
                SetDataInView(ElIndicator, gameData.GameHltbData.MainStory);
                if (MaxValue < gameData.GameHltbData.MainStory)
                {
                    MaxValue = gameData.GameHltbData.MainStory;
                }
            }

            if (gameData.GameHltbData.MainExtra != 0)
            {
                ElIndicator += 1;
                SetDataInView(ElIndicator, gameData.GameHltbData.MainExtra);
                if (MaxValue < gameData.GameHltbData.MainExtra)
                {
                    MaxValue = gameData.GameHltbData.MainExtra;
                }
            }

            if (gameData.GameHltbData.Completionist != 0)
            {
                ElIndicator += 1;
                SetDataInView(ElIndicator, gameData.GameHltbData.Completionist);
                if (MaxValue < gameData.GameHltbData.Completionist)
                {
                    MaxValue = gameData.GameHltbData.Completionist;
                }
            }

            if (gameData.GameHltbData.Solo != 0)
            {
                ElIndicator += 1;
                SetDataInView(ElIndicator, gameData.GameHltbData.Solo);
                if (MaxValue < gameData.GameHltbData.Solo)
                {
                    MaxValue = gameData.GameHltbData.Solo;
                }
            }

            if (gameData.GameHltbData.CoOp != 0)
            {
                ElIndicator += 1;
                SetDataInView(ElIndicator, gameData.GameHltbData.CoOp);
                if (MaxValue < gameData.GameHltbData.CoOp)
                {
                    MaxValue = gameData.GameHltbData.CoOp;
                }
            }

            if (gameData.GameHltbData.Vs != 0)
            {
                ElIndicator += 1;
                SetDataInView(ElIndicator, gameData.GameHltbData.Vs);
                if (MaxValue < gameData.GameHltbData.Vs)
                {
                    MaxValue = gameData.GameHltbData.Vs;
                }
            }



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
            ProgressHltb_El1.Maximum = MaxValue;
            ProgressHltb_El2.Maximum = MaxValue;
            ProgressHltb_El3.Maximum = MaxValue;

            SliderPlaytime.Value = Playtime;
            SliderPlaytime.Maximum = MaxValue;


            // Set Binding data
            DataContext = this;
        }

        private void SetDataInView(int ElIndicator, long ElValue)
        {
            switch (ElIndicator)
            {
                case 1:
                    ProgressHltb_El1.Value = ElValue;
                    break;

                case 2:
                    ProgressHltb_El2.Value = ElValue;
                    break;

                case 3:
                    ProgressHltb_El3.Value = ElValue;
                    break;
            }
        }
    }
}
