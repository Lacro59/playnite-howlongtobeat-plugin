using HowLongToBeat.Models;
using HowLongToBeat.Services;
using HowLongToBeat.Views.Interfaces;
using Playnite.Controls;
using Playnite.Converters;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginCommon;
using System;
using System.Diagnostics;
using System.Globalization;

namespace HowLongToBeat.Views
{
    /// <summary>
    /// Logique d'interaction pour HowLongToBeat.xaml
    /// </summary>
    public partial class HowLongToBeat : WindowBase
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        public string CoverImage { get; set; }
        public string GameName { get; set; }

        public string PlaytimeFormat { get; set; }

        private HowLongToBeatData data { get; set; }

        public HowLongToBeat(HowLongToBeatData data, Game game, IPlayniteAPI PlayniteApi)
        {
            this.data = data;

            InitializeComponent();

            HltbDataUser gameData = data.GetData();

            CoverImage = PlayniteApi.Database.GetFullFilePath(game.CoverImage);
            GameName = game.Name;


            int ElIndicator = 0;

            Hltb_El1.Visibility = System.Windows.Visibility.Hidden;
            Hltb_El1_Color.Visibility = System.Windows.Visibility.Hidden;
            Hltb_El2.Visibility = System.Windows.Visibility.Hidden;
            Hltb_El2_Color.Visibility = System.Windows.Visibility.Hidden;
            Hltb_El3.Visibility = System.Windows.Visibility.Hidden;
            Hltb_El3_Color.Visibility = System.Windows.Visibility.Hidden;

            if (gameData.GameHltbData.MainStory != 0)
            {
                ElIndicator += 1;
                SetDataInView(ElIndicator, resources.GetString("LOCHowLongToBeatMainStory"), gameData.GameHltbData.MainStoryFormat);
            }

            if (gameData.GameHltbData.MainExtra != 0)
            {
                ElIndicator += 1;
                SetDataInView(ElIndicator, resources.GetString("LOCHowLongToBeatMainExtra"), gameData.GameHltbData.MainExtraFormat);
            }

            if (gameData.GameHltbData.Completionist != 0)
            {
                ElIndicator += 1;
                SetDataInView(ElIndicator, resources.GetString("LOCHowLongToBeatCompletionist"), gameData.GameHltbData.CompletionistFormat);
            }

            if (gameData.GameHltbData.Solo != 0)
            {
                ElIndicator += 1;
                SetDataInView(ElIndicator, resources.GetString("LOCHowLongToBeatSolo"), gameData.GameHltbData.SoloFormat);
            }

            if (gameData.GameHltbData.CoOp != 0)
            {
                ElIndicator += 1;
                SetDataInView(ElIndicator, resources.GetString("LOCHowLongToBeatCoOp"), gameData.GameHltbData.CoOpFormat);
            }

            if (gameData.GameHltbData.Vs != 0)
            {
                ElIndicator += 1;
                SetDataInView(ElIndicator, resources.GetString("LOCHowLongToBeatVs"), gameData.GameHltbData.VsFormat);
            }


            LongToTimePlayedConverter converter = new LongToTimePlayedConverter();
            PlaytimeFormat = (string)converter.Convert((long)game.Playtime, null, null, CultureInfo.CurrentCulture);


            HltbProgressBar.Children.Add(new HltbProgressBar(game.Playtime, gameData));
            HltbProgressBar.UpdateLayout();


            // Set Binding data
            DataContext = this;
        }

        private void SetDataInView(int ElIndicator, string ElText, string ElData)
        {
            switch (ElIndicator)
            {
                case 1:
                    Hltb_El1.Text = ElText;
                    Hltb_El1_Data.Text = ElData;
                    Hltb_El1.Visibility = System.Windows.Visibility.Visible;
                    Hltb_El1_Color.Visibility = System.Windows.Visibility.Visible;
                    break;

                case 2:
                    Hltb_El2.Text = ElText;
                    Hltb_El2_Data.Text = ElData;
                    Hltb_El2.Visibility = System.Windows.Visibility.Visible;
                    Hltb_El2_Color.Visibility = System.Windows.Visibility.Visible;
                    break;

                case 3:
                    Hltb_El3.Text = ElText;
                    Hltb_El3_Data.Text = ElData;
                    Hltb_El3.Visibility = System.Windows.Visibility.Visible;
                    Hltb_El3_Color.Visibility = System.Windows.Visibility.Visible;
                    break;
            }
        }

        private void DockPanel_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Tools.DesactivePlayniteWindowControl(this);
        }

        private void ButtonWeb_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (data.GetData().GameHltbData.Url != "")
            {
                Process.Start(data.GetData().GameHltbData.Url);
            }
        }

        private void ButtonDelete_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            data.RemoveData();
            this.Close();
        }
    }
}
