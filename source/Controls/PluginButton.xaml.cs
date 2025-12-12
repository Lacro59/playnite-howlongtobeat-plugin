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
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace HowLongToBeat.Controls
{
    public partial class PluginButton : PluginUserControlExtend
    {
        private static HowLongToBeatDatabase PluginDatabase => HowLongToBeat.PluginDatabase;
        protected override IPluginDatabase pluginDatabase => PluginDatabase;

        private PluginButtonDataContext ControlDataContext = new PluginButtonDataContext();
        protected override IDataContext controlDataContext
        {
            get => ControlDataContext;
            set => ControlDataContext = value as PluginButtonDataContext
                ?? throw new InvalidCastException($"Expected {nameof(PluginButtonDataContext)} but got {value?.GetType().FullName ?? "<null>"}");
        }

        private bool eventsWired;

        public PluginButton()
        {
            AlwaysShow = true;

            InitializeComponent();
            this.DataContext = ControlDataContext;

            this.Loaded += PluginButton_Loaded;
            this.Unloaded += PluginButton_Unloaded;
        }

        private void PluginButton_Unloaded(object sender, RoutedEventArgs e)
        {
            if (!eventsWired) return;
            eventsWired = false;

            if (PluginDatabase?.PluginSettings != null)
            {
                PluginDatabase.PluginSettings.PropertyChanged -= PluginSettings_PropertyChanged;
            }

            if (PluginDatabase?.Database != null)
            {
                PluginDatabase.Database.ItemUpdated -= Database_ItemUpdated;
                PluginDatabase.Database.ItemCollectionChanged -= Database_ItemCollectionChanged;
            }

            if (API.Instance?.Database?.Games != null)
            {
                API.Instance.Database.Games.ItemUpdated -= Games_ItemUpdated;
            }
        }

        private async void PluginButton_Loaded(object sender, RoutedEventArgs e)
        {

            try
            {
                var sw = Stopwatch.StartNew();
                while (!PluginDatabase.IsLoaded)
                {
                    await Task.Delay(100).ConfigureAwait(false);
                    if (sw.Elapsed > TimeSpan.FromSeconds(30))
                    {
                        try { Debug.WriteLine("PluginButton init timeout waiting for PluginDatabase.IsLoaded"); } catch { }
                        return;
                    }
                }

                await this.Dispatcher.InvokeAsync((Action)(() =>
                {
                    if (!eventsWired)
                    {
                        try { PluginDatabase.PluginSettings.PropertyChanged += PluginSettings_PropertyChanged; } catch { }
                        try { PluginDatabase.Database.ItemUpdated += Database_ItemUpdated; } catch { }
                        try { PluginDatabase.Database.ItemCollectionChanged += Database_ItemCollectionChanged; } catch { }
                        try { API.Instance.Database.Games.ItemUpdated += Games_ItemUpdated; } catch { }
                        eventsWired = true;
                    }

                    try { PluginSettings_PropertyChanged(null, null); } catch { }
                })).Task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                try { Common.LogError(ex, false, true, PluginDatabase.PluginName); } catch { }
            }
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
