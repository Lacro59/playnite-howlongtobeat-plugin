using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Converters;
using CommonPluginsShared.Interfaces;
using HowLongToBeat.Models;
using HowLongToBeat.Services;
using Playnite.SDK.Models;
using System.Collections.Generic;
using System.Globalization;

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
            DataContext = ControlDataContext;
            Loaded += OnLoaded;
        }

        protected override void AttachStaticEvents()
        {
            base.AttachStaticEvents();
            AttachPluginEvents(PluginDatabase.PluginName, () =>
            {
                PluginDatabase.PluginSettings.PropertyChanged += CreatePluginSettingsHandler();
                PluginDatabase.DatabaseItemUpdated += CreateDatabaseItemUpdatedHandler<GameHowLongToBeat>();
                PluginDatabase.DatabaseItemCollectionChanged += CreateDatabaseCollectionChangedHandler<GameHowLongToBeat>();
            });
        }


        public override void SetDefaultDataContext()
        {
            ControlDataContext.IsActivated = PluginDatabase.PluginSettings.EnableIntegrationViewItem;
            ControlDataContext.Text = string.Empty;
        }


        public override void SetData(Game newContext, PluginGameEntry PluginGameData)
        {
            GameHowLongToBeat gameHowLongToBeat = (GameHowLongToBeat)PluginGameData;

            if (gameHowLongToBeat?.GetData().GameHltbData != null)
            {
                PlayTimeToStringConverterWithZero converter = new PlayTimeToStringConverterWithZero();
                PlayTimeFormat playTimeFormat = PluginDatabase.PluginSettings.IntegrationViewItemOnlyHour ? PlayTimeFormat.RoundHour : PlayTimeFormat.DefaultFormat;
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
