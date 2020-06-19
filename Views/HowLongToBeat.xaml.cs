using HowLongToBeat.Models;
using HowLongToBeat.Services;
using HowLongToBeat.Views.Interfaces;
using Playnite.Controls;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginCommon;
using System;
using System.Diagnostics;

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


            HltbProgressBar.Children.Add(new HltbProgressBar(game.Playtime, gameData));
            HltbProgressBar.UpdateLayout();


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
