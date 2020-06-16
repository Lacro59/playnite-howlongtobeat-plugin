using HowLongToBeat.Services;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace HowLongToBeat
{
    public class HowLongToBeat : Plugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private HowLongToBeatSettings settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("e08cd51f-9c9a-4ee3-a094-fde03b55492f");

        private Game GameSelected { get; set; }

        public HowLongToBeat(IPlayniteAPI api) : base(api)
        {
            settings = new HowLongToBeatSettings(this);

            // Get plugin's location 
            string pluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // Add plugin localization in application ressource.
            PluginCommon.Localization.SetPluginLanguage(pluginFolder, api.Paths.ConfigurationPath);
            // Add common in application ressource.
            PluginCommon.Common.Load(pluginFolder);
        }

        public override IEnumerable<ExtensionFunction> GetFunctions()
        {
            return new List<ExtensionFunction>
            {
                new ExtensionFunction(
                    "HowLongToBeat",
                    () =>
                    {
                        // Add code to be execute when user invokes this menu entry.

                        HowLongToBeatData data = new HowLongToBeatData(GameSelected, this.GetPluginUserDataPath());
                        if (data.GetData() != null)
                        {
                            new Views.HowLongToBeat(data.GetData(), GameSelected, PlayniteApi).ShowDialog();
                        }
                    })
            };
        }

        public override void OnGameSelected(GameSelectionEventArgs args)
        {
            try
            {
                GameSelected = args.NewValue[0];
            }
            catch (Exception ex)
            {
                var LineNumber = new StackTrace(ex, true).GetFrame(0).GetFileLineNumber();
                string FileName = new StackTrace(ex, true).GetFrame(0).GetFileName();
                logger.Error(ex, $"HowLongToBeat [{FileName} {LineNumber}] - OnGameSelected() ");
            }
        }

        public override void OnGameInstalled(Game game)
        {
            // Add code to be executed when game is finished installing.
        }

        public override void OnGameStarted(Game game)
        {
            // Add code to be executed when game is started running.
        }

        public override void OnGameStarting(Game game)
        {
            // Add code to be executed when game is preparing to be started.
        }

        public override void OnGameStopped(Game game, long elapsedSeconds)
        {
            // Add code to be executed when game is preparing to be started.
        }

        public override void OnGameUninstalled(Game game)
        {
            // Add code to be executed when game is uninstalled.
        }

        public override void OnApplicationStarted()
        {
            // Add code to be executed when Playnite is initialized.
        }

        public override void OnApplicationStopped()
        {
            // Add code to be executed when Playnite is shutting down.
        }

        public override void OnLibraryUpdated()
        {
            // Add code to be executed when library is updated.
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        //public override UserControl GetSettingsView(bool firstRunSettings)
        //{
        //    return new HowLongToBeatSettingsView();
        //}
    }
}