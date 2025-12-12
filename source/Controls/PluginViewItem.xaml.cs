using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Converters;
using CommonPluginsShared.Interfaces;
using HowLongToBeat.Models;
using HowLongToBeat.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace HowLongToBeat.Controls
{
    public partial class PluginViewItem : PluginUserControlExtend
    {
        private static HowLongToBeatDatabase PluginDatabase => HowLongToBeat.PluginDatabase;
        protected override IPluginDatabase pluginDatabase => PluginDatabase;

        private PluginViewItemDataContext ControlDataContext = new PluginViewItemDataContext();
        protected override IDataContext controlDataContext
        {
            get => ControlDataContext;
            set => ControlDataContext = (PluginViewItemDataContext)value;
        }


        public PluginViewItem()
        {
            InitializeComponent();
            this.DataContext = ControlDataContext;

            // Use async Loaded handler to perform awaited initialization without compiler warnings
            this.Loaded += PluginViewItem_Loaded;
        }

        private async void PluginViewItem_Loaded(object sender, EventArgs e)
        {
            this.Loaded -= PluginViewItem_Loaded;

            while (!PluginDatabase.IsLoaded)
            {
                await Task.Delay(100).ConfigureAwait(false);
            }

            // Ensure the registration runs on the UI thread and await its completion
            await this.Dispatcher.InvokeAsync((Action)delegate
            {
                PluginDatabase.PluginSettings.PropertyChanged += PluginSettings_PropertyChanged;
                PluginDatabase.Database.ItemUpdated += Database_ItemUpdated;
                PluginDatabase.Database.ItemCollectionChanged += Database_ItemCollectionChanged;
                API.Instance.Database.Games.ItemUpdated += Games_ItemUpdated;

                // Apply settings
                PluginSettings_PropertyChanged(null, null);
            }).Task.ConfigureAwait(false);
        }


        public override void SetDefaultDataContext()
        {
            ControlDataContext.IsActivated = PluginDatabase.PluginSettings.Settings.EnableIntegrationViewItem;
            ControlDataContext.Text = string.Empty;
        }


        public override void SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
        {
            GameHowLongToBeat gameHowLongToBeat = (GameHowLongToBeat)PluginGameData;

            if (gameHowLongToBeat?.GetData().GameHltbData != null)
            {
                PlayTimeToStringConverterWithZero converter = new PlayTimeToStringConverterWithZero();
                PlayTimeFormat playTimeFormat = PluginDatabase.PluginSettings.Settings.IntegrationViewItemOnlyHour ? PlayTimeFormat.RoundHour : PlayTimeFormat.DefaultFormat;
                ControlDataContext.Text = (string)converter.Convert(gameHowLongToBeat.GetData().GameHltbData.TimeToBeat, null, playTimeFormat, CultureInfo.CurrentCulture);
            }
        }
    }


    public class PluginViewItemDataContext : ObservableObject, IDataContext
    {
        private bool isActivated;
        public bool IsActivated { get => isActivated; set => SetValue(ref isActivated, value); }

        private string text = "1h 18m";
        public string Text { get => text; set => SetValue(ref text, value); }
    }
}
