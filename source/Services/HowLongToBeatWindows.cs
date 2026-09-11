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
    /// <summary>
    /// Opens HowLongToBeat plugin windows (user view, per-game data, games without data).
    /// </summary>
    public class HowLongToBeatWindows : PluginWindows
    {
        private HowLongToBeatDatabase Database => (HowLongToBeatDatabase)PluginDatabase;

        /// <summary>
        /// Initializes a new instance of the <see cref="HowLongToBeatWindows"/> class.
        /// </summary>
        /// <param name="pluginName">Display name used for window titles.</param>
        /// <param name="pluginDatabase">Plugin database (cast to <see cref="HowLongToBeatDatabase"/>).</param>
        public HowLongToBeatWindows(string pluginName, IPluginDatabase pluginDatabase) : base(pluginName, pluginDatabase)
        {
        }

        /// <inheritdoc />
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

        /// <summary>
        /// Opens the per-game HowLongToBeat view. When the game has no cached data, runs
        /// <see cref="HowLongToBeatDatabase.AddData"/> (search / auto-match) first, then opens the
        /// view if data was added.
        /// </summary>
        /// <param name="gameContext">Playnite game to show or search for.</param>
        public override void ShowPluginGameDataWindow(Game gameContext)
        {
            if (gameContext == null)
            {
                return;
            }

            // Cache-only: avoid Get() without onlyCache, which also opens SearchData and would
            // duplicate the AddData search dialog when the theme button is clicked with no data.
            GameHowLongToBeat gameHowLongToBeat = Database.Get(gameContext, true);
            if (gameHowLongToBeat?.HasData != true)
            {
                Database.AddData(gameContext);
                gameHowLongToBeat = Database.Get(gameContext, true);
                if (gameHowLongToBeat?.HasData != true)
                {
                    return;
                }
            }

            HowLongToBeatView viewExtension = new HowLongToBeatView(gameHowLongToBeat);
            Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PluginName, viewExtension);
            _ = windowExtension.ShowDialog();
        }

        /// <inheritdoc />
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
