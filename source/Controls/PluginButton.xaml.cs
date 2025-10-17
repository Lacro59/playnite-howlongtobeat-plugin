using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Interfaces;
using HowLongToBeat.Models;
using HowLongToBeat.Services;
using HowLongToBeat.Views;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace HowLongToBeat.Controls
{
    /// <summary>
    /// Logique d'interaction pour PluginButton.xaml
    /// </summary>
    public partial class PluginButton : PluginUserControlExtend
    {
        private static HowLongToBeatDatabase PluginDatabase => HowLongToBeat.PluginDatabase;
        protected override IPluginDatabase pluginDatabase => PluginDatabase;

        private PluginButtonDataContext ControlDataContext =  new PluginButtonDataContext();
        protected override IDataContext controlDataContext
        {
            get => ControlDataContext;
            set => ControlDataContext = (PluginButtonDataContext)controlDataContext;
        }

        public PluginButton()
        {
            AlwaysShow = true;

            InitializeComponent();
            this.DataContext = ControlDataContext;

            _ = Task.Run(() =>
            {
                // Wait extension database are loaded
                _ = System.Threading.SpinWait.SpinUntil(() => PluginDatabase.IsLoaded, -1);

                this.Dispatcher.BeginInvoke((Action)delegate
                {
                    PluginDatabase.PluginSettings.PropertyChanged += PluginSettings_PropertyChanged;
                    PluginDatabase.Database.ItemUpdated += Database_ItemUpdated;
                    PluginDatabase.Database.ItemCollectionChanged += Database_ItemCollectionChanged;
                    API.Instance.Database.Games.ItemUpdated += Games_ItemUpdated;

                    // Apply settings
                    PluginSettings_PropertyChanged(null, null);
                });
            });
        }


        public override void SetDefaultDataContext()
        {
            ControlDataContext.IsActivated = PluginDatabase.PluginSettings.Settings.EnableIntegrationButton;
            ControlDataContext.Text = "\ue90d";
        }


        public override void SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
        {
            GameHowLongToBeat gameHowLongToBeat = (GameHowLongToBeat)PluginGameData;
        }


        #region Events
        private void PART_PluginButton_Click(object sender, RoutedEventArgs e)
        {
            GameHowLongToBeat gameHowLongToBeat = PluginDatabase.Get(GameContext);

            if (gameHowLongToBeat.HasData)
            {
                HowLongToBeatView ViewExtension = new HowLongToBeatView(gameHowLongToBeat);
                Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PluginDatabase.PluginName, ViewExtension);
                _ = windowExtension.ShowDialog();
            }
        }
        #endregion
    }


    public class PluginButtonDataContext : ObservableObject, IDataContext
    {
        private bool isActivated;
        public bool IsActivated { get => isActivated; set => SetValue(ref isActivated, value); }

        private string text = "\ue90d";
        public string Text { get => text; set => SetValue(ref text, value); }
    }
}
