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
#if DEBUG
			var timer = new DebugTimer("PluginViewItem.ctor");
#endif

			InitializeComponent();

#if DEBUG
			timer.Step("InitializeComponent done");
#endif

			DataContext = ControlDataContext;
			Loaded += OnLoaded;

#if DEBUG
			timer.Stop();
#endif
        }

		protected override void AttachStaticEvents()
		{
#if DEBUG
			var timer = new DebugTimer("PluginViewItem.AttachStaticEvents");
#endif

			base.AttachStaticEvents();

#if DEBUG
			timer.Step("base done");
#endif

			AttachPluginEvents(PluginDatabase.PluginName, () =>
			{
#if DEBUG
				timer.Step("registering plugin-specific handlers");
#endif

				PluginDatabase.PluginSettings.PropertyChanged += CreatePluginSettingsHandler(); 
				PluginDatabase.DatabaseItemUpdated += CreateDatabaseItemUpdatedHandler<GameHowLongToBeat>();
				PluginDatabase.DatabaseItemCollectionChanged += CreateDatabaseCollectionChangedHandler<GameHowLongToBeat>();
			});

#if DEBUG
			timer.Stop();
#endif
		}


        public override void SetDefaultDataContext()
        {
#if DEBUG
            var timer = new DebugTimer("PluginViewItem.SetDefaultDataContext");
#endif

            ControlDataContext.IsActivated = PluginDatabase.PluginSettings.EnableIntegrationViewItem;
            ControlDataContext.Text = string.Empty;

#if DEBUG
            timer.Stop(string.Format("IsActivated={0}", ControlDataContext.IsActivated));
#endif
        }


        public override void SetData(Game newContext, PluginGameEntry PluginGameData)
        {
#if DEBUG
            var timer = new DebugTimer(string.Format("PluginViewItem.SetData(game='{0}')", newContext?.Name ?? "null"));
#endif

            GameHowLongToBeat gameHowLongToBeat = (GameHowLongToBeat)PluginGameData;

            if (gameHowLongToBeat?.GetData().GameHltbData != null)
            {
                PlayTimeToStringConverterWithZero converter = new PlayTimeToStringConverterWithZero();
                PlayTimeFormat playTimeFormat = PluginDatabase.PluginSettings.IntegrationViewItemOnlyHour ? PlayTimeFormat.RoundHour : PlayTimeFormat.DefaultFormat;
                ControlDataContext.Text = (string)converter.Convert(gameHowLongToBeat.GetData().GameHltbData.TimeToBeat, null, playTimeFormat, CultureInfo.CurrentCulture);
            }

#if DEBUG
            timer.Stop();
#endif
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
