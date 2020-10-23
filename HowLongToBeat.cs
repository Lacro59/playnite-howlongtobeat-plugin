using HowLongToBeat.Services;
using HowLongToBeat.Views;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PluginCommon;
using PluginCommon.PlayniteResources;
using PluginCommon.PlayniteResources.API;
using PluginCommon.PlayniteResources.Common;
using PluginCommon.PlayniteResources.Converters;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HowLongToBeat
{
    public class HowLongToBeat : Plugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        private HowLongToBeatSettings settings { get; set; }
        public override Guid Id { get; } = Guid.Parse("e08cd51f-9c9a-4ee3-a094-fde03b55492f");

        public static Game GameSelected { get; set; }
        public static HowLongToBeatUI howLongToBeatUI { get; set; }
        public static HowLongToBeatData HltbGameData { get; set; } = null;


        public HowLongToBeat(IPlayniteAPI api) : base(api)
        {
            settings = new HowLongToBeatSettings(this);


            // Get plugin's location 
            string pluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // Add plugin localization in application ressource.
            PluginCommon.Localization.SetPluginLanguage(pluginFolder, api.ApplicationSettings.Language);
            // Add common in application ressource.
            PluginCommon.Common.Load(pluginFolder);

            // Check version
            if (settings.EnableCheckVersion)
            {
                CheckVersion cv = new CheckVersion();

                if (cv.Check("HowLongToBeat", pluginFolder))
                {
                    cv.ShowNotification(api, "HowLongToBeat - " + resources.GetString("LOCUpdaterWindowTitle"));
                }
            }

            // Init ui interagration
            howLongToBeatUI = new HowLongToBeatUI(api, settings, this.GetPluginUserDataPath());

            // Custom theme button
            EventManager.RegisterClassHandler(typeof(Button), Button.ClickEvent, new RoutedEventHandler(howLongToBeatUI.OnCustomThemeButtonClick));
        }

        // To add new game menu items override GetGameMenuItems
        public override List<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            Game gameMenu = args.Games.First();
            HowLongToBeatData data = new HowLongToBeatData(gameMenu, this.GetPluginUserDataPath(), PlayniteApi, false);

            List<GameMenuItem> gameMenuItems = new List<GameMenuItem>
            {
                new GameMenuItem {
                    MenuSection = resources.GetString("LOCHowLongToBeat"),
                    Description = resources.GetString("LOCHowLongToBeatPluginView"),
                    Action = (gameMenuItem) =>
                    {
                        try
                        {
                            if (!data.hasData)
                            {
                                data.SearchData(GameSelected);
                            }

                            if (data.hasData)
                            {
                                var ViewExtension = new HowLongToBeatView(data, gameMenu, PlayniteApi, settings);
                                Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PlayniteApi, "HowLongToBeat", ViewExtension);
                                windowExtension.ShowDialog();
                            }
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, "HowLongToBeat", $"Error to load game data for {args.Games.First().Name}");
                            PlayniteApi.Dialogs.ShowErrorMessage(resources.GetString("LOCDatabaseErroTitle"), "HowLongToBeat");
                        }
                    }
                },
                new GameMenuItem {
                    MenuSection = resources.GetString("LOCHowLongToBeat"),
                    Description = resources.GetString("LOCHowLongToBeatPluginDelete"),
                    Action = (gameMenuItem) =>
                    {
                        data.RemoveData();

                        howLongToBeatUI.RemoveElements();
                        var TaskIntegrationUI = Task.Run(() =>
                        {
                            howLongToBeatUI.AddElements();
                            howLongToBeatUI.RefreshElements(gameMenu);
                        });
                    }
                }
            };

#if DEBUG
            gameMenuItems.Add(new GameMenuItem
            {
                MenuSection = resources.GetString("LOCHowLongToBeat"),
                Description = "Test",
                Action = (mainMenuItem) => { }
            });
#endif

            return gameMenuItems;
        }

        // To add new main menu items override GetMainMenuItems
        public override List<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            string MenuInExtensions = string.Empty;
            if (settings.MenuInExtensions)
            {
                MenuInExtensions = "@";
            }

            List <MainMenuItem> mainMenuItems = new List<MainMenuItem>
            {
                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + resources.GetString("LOCHowLongToBeat"),
                    Description = resources.GetString("LOCHowLongToBeatGetAllDatas"),
                    Action = (mainMenuItem) =>
                    {
                        HowLongToBeatData.GetAllDataFromMain(PlayniteApi, this.GetPluginUserDataPath(), settings);
                    }
                },
                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + resources.GetString("LOCHowLongToBeat"),
                    Description = resources.GetString("LOCHowLongToBeatClearAllDatas"),
                    Action = (mainMenuItem) =>
                    {
                        HowLongToBeatData.ClearAllData(this.GetPluginUserDataPath(), PlayniteApi);
                    }
                },
                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + resources.GetString("LOCHowLongToBeat"),
                    Description = resources.GetString("LOCHowLongToBeatAddAllTag"),
                    Action = (mainMenuItem) =>
                    {
                        HowLongToBeatData.AddAllTagFromMain(PlayniteApi, this.GetPluginUserDataPath());
                    }
                },
                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + resources.GetString("LOCHowLongToBeat"),
                    Description = resources.GetString("LOCHowLongToBeatRemoveAllTag"),
                    Action = (mainMenuItem) =>
                    {
                        HowLongToBeatData.RemoveAllTagFromMain(PlayniteApi, this.GetPluginUserDataPath());
                    }
                }
            };

#if DEBUG
            mainMenuItems.Add(new MainMenuItem
            {
                MenuSection = MenuInExtensions + resources.GetString("LOCHowLongToBeat"),
                Description = "Test",
                Action = (mainMenuItem) => { }
            });
#endif

            return mainMenuItems;
        }

        public override void OnGameSelected(GameSelectionEventArgs args)
        {
            try
            {
                if (args.NewValue != null && args.NewValue.Count == 1)
                {
                    GameSelected = args.NewValue[0];

                    var TaskIntegrationUI = Task.Run(() =>
                    {
                        howLongToBeatUI.AddElements();
                        howLongToBeatUI.RefreshElements(GameSelected);
                    });
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "HowLongToBeat", $"Error on OnGameSelected()");
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

            try
            {
                howLongToBeatUI.RefreshElements(GameSelected);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "HowLongToBeat", $"Error on Integration");
            }
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

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new HowLongToBeatSettingsView(PlayniteApi, this.GetPluginUserDataPath());
        }
    }
}
