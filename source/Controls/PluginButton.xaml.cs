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
        private HowLongToBeatDatabase PluginDatabase = HowLongToBeat.PluginDatabase;
        internal override IPluginDatabase _PluginDatabase
        {
            get => PluginDatabase;
            set => PluginDatabase = (HowLongToBeatDatabase)_PluginDatabase;
        }

        private PluginButtonDataContext ControlDataContext =  new PluginButtonDataContext();
        internal override IDataContext _ControlDataContext
        {
            get => ControlDataContext;
            set => ControlDataContext = (PluginButtonDataContext)_ControlDataContext;
        }

        public PluginButton()
        {
            AlwaysShow = true;

            InitializeComponent();
            this.DataContext = ControlDataContext;

            Task.Run(() =>
            {
                // Wait extension database are loaded
                System.Threading.SpinWait.SpinUntil(() => PluginDatabase.IsLoaded, -1);

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

            if (gameHowLongToBeat.HasData || gameHowLongToBeat.HasDataEmpty)
            {
                var ViewExtension = new HowLongToBeatView(gameHowLongToBeat);
                Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PluginDatabase.PluginName, ViewExtension);
                windowExtension.ShowDialog();
            }
        }
        #endregion
    }


    public class PluginButtonDataContext : ObservableObject, IDataContext
    {
        private bool _IsActivated;
        public bool IsActivated { get => _IsActivated; set => SetValue(ref _IsActivated, value); }

        private string _Text = "\ue90d";
        public string Text { get => _Text; set => SetValue(ref _Text, value); }
    }
}
