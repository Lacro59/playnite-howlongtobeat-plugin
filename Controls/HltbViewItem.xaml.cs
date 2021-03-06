using CommonPluginsShared;
using CommonPluginsShared.Controls;
using HowLongToBeat.Models;
using HowLongToBeat.Services;
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
    /// Logique d'interaction pour HltbViewItem.xaml
    /// </summary>
    public partial class HltbViewItem : PluginUserControlExtend
    {
        private HowLongToBeatDatabase PluginDatabase = HowLongToBeat.PluginDatabase;


        public HltbViewItem()
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
        private static void SettingsPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            HltbProgressBar obj = sender as HltbProgressBar;
            if (obj != null && e.NewValue != e.OldValue)
            {
                obj.PluginSettings_PropertyChanged(null, null);
            }
        }

        private static void ControlsPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            HltbProgressBar obj = sender as HltbProgressBar;
            if (obj != null && e.NewValue != e.OldValue)
            {
                obj.GameContextChanged(null, obj.GameContext);
            }
        }

        // When settings is updated
        public override void PluginSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (IgnoreSettings)
            {
                this.DataContext = new
                {

                };
            }
            else
            {
                this.DataContext = new
                {

                };
            }

            // Publish changes for the currently displayed game
            GameContextChanged(null, GameContext);
        }

        // When game is changed
        public override void GameContextChanged(Game oldContext, Game newContext)
        {
            if (IgnoreSettings)
            {
                MustDisplay = true;
            }
            else
            {
                MustDisplay = PluginDatabase.PluginSettings.Settings.EnableIntegrationViewItem;

                // When control is not used
                if (!PluginDatabase.PluginSettings.Settings.EnableIntegrationViewItem)
                {
                    return;
                }
            }

            if (newContext != null)
            {
                GameHowLongToBeat gameHowLongToBeat = PluginDatabase.Get(newContext, true);

                if (!gameHowLongToBeat.HasData)
                {
                    MustDisplay = false;
                    return;
                }

                PART_Text.Text = gameHowLongToBeat.GetData().GameHltbData.TimeToBeatFormat;
            }
            else
            {

            }
        }
        #endregion
    }
}
