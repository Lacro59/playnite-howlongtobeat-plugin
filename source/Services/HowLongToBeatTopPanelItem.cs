using CommonPluginsShared;
using HowLongToBeat.Views;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HowLongToBeat.Services
{
    public class HowLongToBeatTopPanelItem : TopPanelItem
    {
        public HowLongToBeatTopPanelItem(HowLongToBeat plugin)
        {
            Icon = new TextBlock
            {
                Text = "\ue90d",
                FontSize = 20,
                FontFamily = ResourceProvider.GetResource("CommonFont") as FontFamily
            };
            Title = ResourceProvider.GetString("LOCHowLongToBeat");
            Activated = () =>
            {
                WindowOptions windowOptions = new WindowOptions
                {
                    ShowMinimizeButton = false,
                    ShowMaximizeButton = true,
                    ShowCloseButton = true,
                    Width = 1280,
                    Height = 740
                };

                HowLongToBeatUserView ViewExtension = new HowLongToBeatUserView(plugin);
                Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(HowLongToBeat.PluginDatabase.PluginName, ViewExtension, windowOptions);
                windowExtension.ResizeMode = ResizeMode.CanResize;
                windowExtension.ShowDialog();
            };
            Visible = plugin.PluginSettings.Settings.EnableIntegrationButtonHeader;
        }
    }
}
