using CommonPluginsShared;
using CommonPluginsShared.Controls;
using HowLongToBeat.Models;
using HowLongToBeat.Services;
using HowLongToBeat.Views;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HowLongToBeat.Controls
{
    /// <summary>
    /// Logique d'interaction pour HowLongToBeatButton.xaml
    /// </summary>
    public partial class HltbButton : PluginUserControlExtend
    {
        private HowLongToBeatDatabase PluginDatabase = HowLongToBeat.PluginDatabase;


        public HltbButton()
        {
            InitializeComponent();

            PluginDatabase.PluginSettings.PropertyChanged += PluginSettings_PropertyChanged;
            PluginDatabase.Database.ItemUpdated += Database_ItemUpdated;
            PluginDatabase.Database.ItemCollectionChanged += Database_ItemCollectionChanged;
            PluginDatabase.PlayniteApi.Database.Games.ItemUpdated += Games_ItemUpdated;

            // Apply settings
            PluginSettings_PropertyChanged(null, null);
        }


        #region OnPropertyChange
        // When settings is updated
        public override void PluginSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Apply settings
            this.DataContext = new
            {

            };

            // Publish changes for the currently displayed game
            GameContextChanged(null, GameContext);
        }

        // When game is changed
        public override void GameContextChanged(Game oldContext, Game newContext)
        {
            MustDisplay = PluginDatabase.PluginSettings.Settings.EnableIntegrationButton;

            // When control is not used
            if (!PluginDatabase.PluginSettings.Settings.EnableIntegrationButton)
            {
                return;
            }
        }
        #endregion


        private void PART_HltbButton_Click(object sender, RoutedEventArgs e)
        {
            GameHowLongToBeat gameHowLongToBeat = PluginDatabase.Get(GameContext);

            if (gameHowLongToBeat.HasData || gameHowLongToBeat.HasDataEmpty)
            {
                var ViewExtension = new Views.HowLongToBeatView(PluginDatabase.PlayniteApi, PluginDatabase.PluginSettings.Settings, gameHowLongToBeat);
                Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PluginDatabase.PlayniteApi, "HowLongToBeat", ViewExtension);
                windowExtension.ShowDialog();
            }
        }
    }
}
