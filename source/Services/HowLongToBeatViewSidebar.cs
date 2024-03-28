using CommonPluginsShared.Controls;
using HowLongToBeat.Views;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace HowLongToBeat.Services
{
    public class HowLongToBeatViewSidebar : SidebarItem
    {
        public HowLongToBeatViewSidebar(HowLongToBeat plugin)
        {
            Type = SiderbarItemType.View;
            Title = ResourceProvider.GetString("LOCHowLongToBeat");
            Icon = new TextBlock
            {
                Text = "\ue90d",
                FontFamily = ResourceProvider.GetResource("CommonFont") as FontFamily
            };
            Opened = () =>
            {
                if (plugin.SidebarItemControl == null)
                {
                    plugin.SidebarItemControl = new SidebarItemControl();
                    plugin.SidebarItemControl.SetTitle(ResourceProvider.GetString("LOCHowLongToBeat"));
                    plugin.SidebarItemControl.AddContent(new HowLongToBeatUserView(plugin));
                }

                return plugin.SidebarItemControl;
            };
            Visible = plugin.PluginSettings.Settings.EnableIntegrationButtonSide;
        }
    }
}
