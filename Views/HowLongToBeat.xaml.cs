using HowLongToBeat.Models;
using HowLongToBeat.Services;
using Playnite.Controls;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginCommon;
using System;
using System.Diagnostics;
using System.Windows.Media.Imaging;

namespace HowLongToBeat.Views
{
    /// <summary>
    /// Logique d'interaction pour HowLongToBeat.xaml
    /// </summary>
    public partial class HowLongToBeat : WindowBase
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        public string CoverImage { get; set; }
        public string GameName { get; set; }
        public string MainStoryFormat { get; set; }
        public string MaintExtraFormat { get; set; }
        public string CompletionistFormat { get; set; }

        public string PlaytimeFormat { get; set; }

        private HowLongToBeatData data { get; set; }

        public HowLongToBeat(HowLongToBeatData data, Game game, IPlayniteAPI PlayniteApi)
        {
            this.data = data;

            InitializeComponent();

            HltbDataUser gameData = data.GetData();

            CoverImage = PlayniteApi.Database.GetFullFilePath(game.CoverImage);
            GameName = game.Name;
            MainStoryFormat = gameData.GameHltbData.MainStoryFormat;
            MaintExtraFormat = gameData.GameHltbData.MaintExtraFormat;
            CompletionistFormat = gameData.GameHltbData.CompletionistFormat;

            PlaytimeFormat = (int)TimeSpan.FromSeconds(game.Playtime).TotalHours + "h " + TimeSpan.FromSeconds(game.Playtime).ToString(@"mm") + "min";

            long MaxValue = gameData.GameHltbData.Completionist;
            long MaxHltb = gameData.GameHltbData.Completionist;
            if (gameData.GameHltbData.Completionist != 0)
            {
                if (game.Playtime > gameData.GameHltbData.Completionist)
                {
                    MaxValue = game.Playtime;
                }
            }
            else
            {
                MaxValue = gameData.GameHltbData.MaintExtra;
                MaxHltb = gameData.GameHltbData.MaintExtra;
                if (game.Playtime > gameData.GameHltbData.MaintExtra)
                {
                    MaxValue = game.Playtime;
                }
            }

            // Limit MaxValue when playtime is more than MaxHltb
            long MaxPercent = (long)Math.Ceiling((double)(10 * MaxHltb / 100));
            if (MaxValue > MaxHltb + MaxPercent)
            {
                MaxValue = MaxHltb + MaxPercent;
            }

            ProgressMainStory.Value = gameData.GameHltbData.MainStory;
            ProgressMainStory.Maximum = MaxValue;

            ProgressMainExtra.Value = gameData.GameHltbData.MaintExtra;
            ProgressMainExtra.Maximum = MaxValue;

            ProgressCompletionist.Value = gameData.GameHltbData.Completionist;
            ProgressCompletionist.Maximum = MaxValue;

            SliderPlaytime.Value = game.Playtime;
            SliderPlaytime.Maximum = MaxValue;



            // Set Binding data
            DataContext = this;
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
