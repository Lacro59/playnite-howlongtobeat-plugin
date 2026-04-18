using CommonPluginsControls.Views;
using CommonPluginsShared;
using CommonPluginsShared.Interfaces;
using CommonPluginsShared.Plugins;
using HowLongToBeat.Models;
using HowLongToBeat.Views;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System.Windows;

namespace HowLongToBeat.Services
{
    public class HowLongToBeatWindows : PluginWindows
    {
        private HowLongToBeatDatabase Database => (HowLongToBeatDatabase)PluginDatabase;

        public HowLongToBeatWindows(string pluginName, IPluginDatabase pluginDatabase) : base(pluginName, pluginDatabase)
        {
        }

        public override void ShowPluginGameDataWindow(GenericPlugin plugin)
        {
            WindowOptions windowOptions = new WindowOptions
            {
                ShowMinimizeButton = false,
                ShowMaximizeButton = true,
                ShowCloseButton = true,
                Width = 1280,
                Height = 740
            };

            HowLongToBeatUserView viewExtension = new HowLongToBeatUserView((HowLongToBeat)plugin);
            Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PluginName, viewExtension, windowOptions);
            windowExtension.ResizeMode = ResizeMode.CanResize;
            _ = windowExtension.ShowDialog();
        }

        public override void ShowPluginGameDataWindow(Game gameContext)
        {
            if (gameContext == null)
            {
                return;
            }

            GameHowLongToBeat gameHowLongToBeat = Database.Get(gameContext);
            if (gameHowLongToBeat?.HasData != true)
            {
                return;
            }

            HowLongToBeatView viewExtension = new HowLongToBeatView(gameHowLongToBeat);
            Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PluginName, viewExtension);
            _ = windowExtension.ShowDialog();
        }

        public override void ShowPluginGameNoDataWindow()
        {
            WindowOptions windowOptions = new WindowOptions
            {
                ShowMinimizeButton = false,
                ShowMaximizeButton = false,
                ShowCloseButton = true
            };

            ListWithNoData viewExtension = new ListWithNoData(Database);
            Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PluginName, viewExtension, windowOptions);
            windowExtension.Show();
        }
    }
}
