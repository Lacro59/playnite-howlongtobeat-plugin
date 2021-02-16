using HowLongToBeat.Services;
using HowLongToBeat.Views;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
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
using HowLongToBeat.Models;
using CommonPluginsShared;
using System.Windows.Threading;

namespace HowLongToBeat
{
    public class HowLongToBeat : Plugin
    {
        private static readonly ILogger logger = LogManager.GetLogger("HowLongToBeat");
        private static IResourceProvider resources = new ResourceProvider();

        private HowLongToBeatSettings settings { get; set; }
        public override Guid Id { get; } = Guid.Parse("e08cd51f-9c9a-4ee3-a094-fde03b55492f");

        public static HowLongToBeatDatabase PluginDatabase;
        public static HowLongToBeatUI howLongToBeatUI { get; set; }

        private OldToNew oldToNew;


        public HowLongToBeat(IPlayniteAPI api) : base(api)
        {
            settings = new HowLongToBeatSettings(this);

            // Old database
            oldToNew = new OldToNew(this.GetPluginUserDataPath());

            // Loading plugin database 
            PluginDatabase = new HowLongToBeatDatabase(this, PlayniteApi, settings, this.GetPluginUserDataPath());
            PluginDatabase.InitializeDatabase();

            // Get plugin's location 
            string pluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // Add plugin localization in application ressource.
            PluginLocalization.SetPluginLanguage(pluginFolder, api.ApplicationSettings.Language);
            // Add common in application ressource.
            Common.Load(pluginFolder);
            Common.SetEvent(PlayniteApi);

            // Check version
            if (settings.EnableCheckVersion)
            {
                CheckVersion cv = new CheckVersion();
                cv.Check("HowLongToBeat", pluginFolder, api);
            }

            // Init ui interagration
            howLongToBeatUI = new HowLongToBeatUI(api, settings, this.GetPluginUserDataPath());

            // Custom theme button
            EventManager.RegisterClassHandler(typeof(Button), Button.ClickEvent, new RoutedEventHandler(howLongToBeatUI.OnCustomThemeButtonClick));

            // Add event fullScreen
            if (api.ApplicationInfo.Mode == ApplicationMode.Fullscreen)
            {
                EventManager.RegisterClassHandler(typeof(Button), Button.ClickEvent, new RoutedEventHandler(BtFullScreen_ClickEvent));
            }
        }


        #region Custom event
        private void BtFullScreen_ClickEvent(object sender, System.EventArgs e)
        {
            try
            {
                if (((Button)sender).Name == "PART_ButtonDetails")
                {
                    var TaskIntegrationUI = Task.Run(() =>
                    {
                        howLongToBeatUI.Initial();
                        howLongToBeatUI.taskHelper.Check();
                        var dispatcherOp = howLongToBeatUI.AddElementsFS();
                        dispatcherOp.Completed += (s, ev) => { howLongToBeatUI.RefreshElements(HowLongToBeatDatabase.GameSelected); };
                    });
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "HowLongToBeat");
            }
        }
        #endregion


        // To add new game menu items override GetGameMenuItems
        public override List<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            Game GameMenu = args.Games.First();
            GameHowLongToBeat gameHowLongToBeat = PluginDatabase.Get(GameMenu, true);

            List<GameMenuItem> gameMenuItems = new List<GameMenuItem>
            {
                new GameMenuItem {
                    MenuSection = resources.GetString("LOCHowLongToBeat"),
                    Description = resources.GetString("LOCHowLongToBeatPluginView"),
                    Action = (gameMenuItem) =>
                    {
                        try
                        {
                            gameHowLongToBeat = PluginDatabase.Get(GameMenu);

                            if (gameHowLongToBeat.HasData)
                            {
                                var ViewExtension = new HowLongToBeatView(PlayniteApi, settings, gameHowLongToBeat);
                                Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PlayniteApi, "HowLongToBeat", ViewExtension);
                                windowExtension.ShowDialog();

                                var TaskIntegrationUI = Task.Run(() =>
                                {
                                    howLongToBeatUI.RefreshElements(HowLongToBeatDatabase.GameSelected);
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, "HowLongToBeat", $"Error to load game data for {args.Games.First().Name}");
                            PlayniteApi.Dialogs.ShowErrorMessage(resources.GetString("LOCDatabaseErroTitle"), "HowLongToBeat");
                        }
                    }
                }
            };


            if (gameHowLongToBeat.HasData)
            {
                gameMenuItems.Add(new GameMenuItem
                {
                    MenuSection = resources.GetString("LOCHowLongToBeat"),
                    Description = resources.GetString("LOCHowLongToBeatSetCurrentTimeManual"),
                    Action = (mainMenuItem) =>
                    {
                        GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                            $"HowLongToBeat - {resources.GetString("LOCCommonProcessing")}",
                            false
                        );
                        globalProgressOptions.IsIndeterminate = true;

                        PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
                        {
                            PluginDatabase.SetCurrentPlayTime(GameMenu, 0);
                        }, globalProgressOptions);
                    }
                });

                gameMenuItems.Add(new GameMenuItem
                {
                    MenuSection = resources.GetString("LOCHowLongToBeat"),
                    Description = resources.GetString("LOCCommonDeleteGameData"),
                    Action = (gameMenuItem) =>
                    {
                        PluginDatabase.Remove(GameMenu.Id);
                        howLongToBeatUI.RefreshElements(HowLongToBeatDatabase.GameSelected);
                    }
                });
            }

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
                    Description = resources.GetString("LOCHowLongToBeatPluginUserView"),
                    Action = (mainMenuItem) =>
                    {
                        var ViewExtension = new HowLongToBeatUserView();
                        Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PlayniteApi, "HowLongToBeat", ViewExtension);
                        windowExtension.ShowDialog();
                    }
                },

                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + resources.GetString("LOCHowLongToBeat"),
                    Description = resources.GetString("LOCCommonGetAllDatas"),
                    Action = (mainMenuItem) =>
                    {
                        PluginDatabase.GetAllDatas();
                    }
                },

                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + resources.GetString("LOCHowLongToBeat"),
                    Description = resources.GetString("LOCCommonSelectData"),
                    Action = (mainMenuItem) =>
                    {
                        PluginDatabase.GetSelectDatas();
                    }
                },
                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + resources.GetString("LOCHowLongToBeat"),
                    Description = resources.GetString("LOCCommonClearAllDatas"),
                    Action = (mainMenuItem) =>
                    {
                        if (PluginDatabase.ClearDatabase())
                        {
                            PlayniteApi.Dialogs.ShowMessage(resources.GetString("LOCCommonDataRemove"), "HowLongToBeat");
                        }
                        else
                        {
                            PlayniteApi.Dialogs.ShowErrorMessage(resources.GetString("LOCCommonDataErrorRemove"), "HowLongToBeat");
                        }
                    }
                },

                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + resources.GetString("LOCHowLongToBeat"),
                    Description = resources.GetString("LOCHowLongToBeatActualiseUserData"),
                    Action = (mainMenuItem) =>
                    {
                        PluginDatabase.RefreshUserData();
                    }
                },

                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + resources.GetString("LOCHowLongToBeat"),
                    Description = resources.GetString("LOCCommonAddAllTags"),
                    Action = (mainMenuItem) =>
                    {
                        PluginDatabase.AddTagAllGame();
                    }
                },
                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + resources.GetString("LOCHowLongToBeat"),
                    Description = resources.GetString("LOCCommonRemoveAllTags"),
                    Action = (mainMenuItem) =>
                    {
                        PluginDatabase.RemoveTagAllGame();
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
            // Old database
            if (oldToNew.IsOld)
            {
                oldToNew.ConvertDB(PlayniteApi);
            }

            try
            {
                if (args.NewValue != null && args.NewValue.Count == 1)
                {
                    HowLongToBeatDatabase.GameSelected = args.NewValue[0];

                    var TaskIntegrationUI = Task.Run(() =>
                    {
                        howLongToBeatUI.Initial();
                        howLongToBeatUI.taskHelper.Check();

                        DispatcherOperation dispatcherOp = null;
                        if (PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Desktop)
                        {
                            dispatcherOp = howLongToBeatUI.AddElements();
                        }
                        else if (PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Fullscreen)
                        {
                            dispatcherOp = howLongToBeatUI.AddElementsFS();
                        }

                        if (dispatcherOp != null)
                        {
                            dispatcherOp.Completed += (s, e) => { howLongToBeatUI.RefreshElements(args.NewValue[0]); };
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "HowLongToBeat", $"Error on OnGameSelected()");
            }
        }

        // Add code to be executed when game is finished installing.
        public override void OnGameInstalled(Game game)
        {

        }

        // Add code to be executed when game is started running.
        public override void OnGameStarted(Game game)
        {

        }

        // Add code to be executed when game is preparing to be started.
        public override void OnGameStarting(Game game)
        {

        }

        // Add code to be executed when game is preparing to be started.
        public override void OnGameStopped(Game game, long elapsedSeconds)
        {
            try
            {
                var TaskIntegrationUI = Task.Run(() =>
                {
                    DispatcherOperation dispatcherOp = null;
                    if (PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Desktop)
                    {
                        dispatcherOp = howLongToBeatUI.AddElements();
                    }
                    else if (PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Fullscreen)
                    {
                        dispatcherOp = howLongToBeatUI.AddElementsFS();
                    }

                    if (dispatcherOp != null)
                    {
                        dispatcherOp.Completed += (s, e) => { howLongToBeatUI.RefreshElements(HowLongToBeatDatabase.GameSelected); };
                    }
                });
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "HowLongToBeat");
            }

            // AutoSetCurrentPlayTime
            if (settings.AutoSetCurrentPlayTime)
            {
                try
                {
                    /*
                    Task.Run(() =>
                    {
                        PluginDatabase.SetCurrentPlayTime(game, elapsedSeconds);
                    });
                    */

                    var result = PlayniteApi.Dialogs.ShowMessage(resources.GetString("LOCHowLongToBeatSetCurrentTime"), "HowLongToBeat", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.Yes)
                    {
                        GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                            $"HowLongToBeat - {resources.GetString("LOCCommonProcessing")}", 
                            false
                        );
                        globalProgressOptions.IsIndeterminate = true;

                        PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
                        {
                            PluginDatabase.SetCurrentPlayTime(game, elapsedSeconds);
                        }, globalProgressOptions);
                    }

                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "HowLongToBeat");
                }
            }
        }

        // Add code to be executed when game is uninstalled.
        public override void OnGameUninstalled(Game game)
        {

        }


        // Add code to be executed when Playnite is initialized.
        public override void OnApplicationStarted()
        {

        }

        // Add code to be executed when Playnite is shutting down.
        public override void OnApplicationStopped()
        {

        }


        // Add code to be executed when library is updated.
        public override void OnLibraryUpdated()
        {

        }


        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new HowLongToBeatSettingsView(PlayniteApi, this.GetPluginUserDataPath(), settings);
        }
    }
}
