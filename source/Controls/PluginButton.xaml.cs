using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Interfaces;
using HowLongToBeat.Models;
using HowLongToBeat.Services;
using Playnite.SDK.Models;
using System.Collections.Generic;
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
            set => ControlDataContext = (PluginButtonDataContext)value;
        }

        public PluginButton()
        {
#if DEBUG
			var timer = new DebugTimer("PluginButton.ctor");
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
			var timer = new DebugTimer("PluginButton.AttachStaticEvents");
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
            var timer = new DebugTimer("PluginButton.SetDefaultDataContext");
#endif

            ControlDataContext.IsActivated = PluginDatabase.PluginSettings.EnableIntegrationButton;
            ControlDataContext.Text = "\ue90d";

#if DEBUG
            timer.Stop(string.Format("IsActivated={0}", ControlDataContext.IsActivated));
#endif
        }


        public override void SetData(Game newContext, PluginGameEntry PluginGameData)
        {
#if DEBUG
            var timer = new DebugTimer(string.Format("PluginButton.SetData(game='{0}')", newContext?.Name ?? "null"));
#endif

            _ = (GameHowLongToBeat)PluginGameData;

#if DEBUG
            timer.Stop();
#endif
        }


        #region Events

        private void PART_PluginButton_Click(object sender, RoutedEventArgs e)
        {
            PluginDatabase.PluginWindows.ShowPluginGameDataWindow(CurrentGame);
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
