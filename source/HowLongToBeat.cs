using HowLongToBeat.Services;
using HowLongToBeat.Views;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using HowLongToBeat.Models;
using CommonPluginsShared;
using CommonPluginsShared.PlayniteExtended;
using CommonPluginsShared.Controls;
using HowLongToBeat.Controls;
using CommonPluginsControls.Views;
using System.Diagnostics;
using System.IO;
using QuickSearch.SearchItems;
using StartPage.SDK;
using LiveCharts.Configurations;
using CommonPluginsControls.LiveChartsCommon;
using LiveCharts;

namespace HowLongToBeat
{
    public class HowLongToBeat : PluginExtended<HowLongToBeatSettingsViewModel, HowLongToBeatDatabase>, IStartPageExtension
    {
        public override Guid Id { get; } = Guid.Parse("e08cd51f-9c9a-4ee3-a094-fde03b55492f");

        internal TopPanelItem TopPanelItem { get; set; }
        internal HowLongToBeatViewSidebar HowLongToBeatViewSidebar { get; set; }
        internal SidebarItemControl SidebarItemControl { get; set; }

        private bool PreventLibraryUpdatedOnStart { get; set; } = true;


        public HowLongToBeat(IPlayniteAPI playniteAPI) : base(playniteAPI)
        {
            PluginDatabase.InitializeClient(this);

            // Custom theme button
            EventManager.RegisterClassHandler(typeof(Button), Button.ClickEvent, new RoutedEventHandler(OnCustomThemeButtonClick));

            // Custom elements integration
            AddCustomElementSupport(new AddCustomElementSupportArgs
            {
                ElementList = new List<string> { "PluginButton", "PluginProgressBar", "PluginViewItem" },
                SourceName = "HowLongToBeat"
            });

            // Settings integration
            AddSettingsSupport(new AddSettingsSupportArgs
            {
                SourceName = "HowLongToBeat",
                SettingsRoot = $"{nameof(PluginSettings)}.{nameof(PluginSettings.Settings)}"
            });

            // Initialize top & side bar
            if (API.Instance.ApplicationInfo.Mode == ApplicationMode.Desktop)
            {
                TopPanelItem = new HowLongToBeatTopPanelItem(this);
                HowLongToBeatViewSidebar = new HowLongToBeatViewSidebar(this);
            }

            //Playnite search integration
            Searches = new List<SearchSupport>
            {
                new SearchSupport("hltb", "HowLongToBeat", new HowLongToBeatSearch())
            };


            //let create a mapper so LiveCharts know how to plot our CustomerViewModel class
            CartesianMapper<CustomerForSingle> customerVmMapper = Mappers.Xy<CustomerForSingle>()
                .X((value, index) => index)
                .Y(value => value.Values);

            //lets save the mapper globally
            Charting.For<CustomerForSingle>(customerVmMapper);
        }


        #region Custom event
        public void OnCustomThemeButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                string ButtonName = ((Button)sender).Name;
                if (ButtonName == "PART_CustomHowLongToBeatButton")
                {
                    Common.LogDebug(true, $"OnCustomThemeButtonClick()");

                    WindowOptions windowOptions = new WindowOptions
                    {
                        ShowMinimizeButton = false,
                        ShowMaximizeButton = true,
                        ShowCloseButton = true,
                        Width = 1280,
                        Height = 740
                    };

                    HowLongToBeatUserView ViewExtension = new HowLongToBeatUserView(this);
                    Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PluginDatabase.PluginName, ViewExtension, windowOptions);
                    windowExtension.ResizeMode = ResizeMode.CanResize;
                    _ = windowExtension.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }
        #endregion


        #region Theme integration
        // Button on top panel
        public override IEnumerable<TopPanelItem> GetTopPanelItems()
        {
            yield return TopPanelItem;
        }

        // List custom controls
        public override Control GetGameViewControl(GetGameViewControlArgs args)
        {
            if (args.Name == "PluginButton")
            {
                return new PluginButton();
            }

            if (args.Name == "PluginProgressBar")
            {
                return new PluginProgressBar();
            }

            if (args.Name == "PluginViewItem")
            {
                return new PluginViewItem();
            }

            return null;
        }

        public override IEnumerable<SidebarItem> GetSidebarItems()
        {
            return new List<SidebarItem>
            {
                HowLongToBeatViewSidebar
            };
        }
        #endregion


        #region StartPageExtension
        public StartPageExtensionArgs GetAvailableStartPageViews()
        {
            List<StartPageViewArgsBase> views = new List<StartPageViewArgsBase> {
                new StartPageViewArgsBase { Name = ResourceProvider.GetString("LOCHtlbChartStats"), ViewId = "HltbChartStats", HasSettings = true }
            };
            return new StartPageExtensionArgs { ExtensionName = PluginDatabase.PluginName, Views = views };
        }

        public object GetStartPageView(string viewId, Guid instanceId)
        {
            switch (viewId)
            {
                case "HltbChartStats":
                    return new Views.StartPage.HltbChartStats();

                default:
                    return null;
            }
        }

        public Control GetStartPageViewSettings(string viewId, Guid instanceId)
        {
            switch (viewId)
            {
                case "HltbChartStats":
                    return new Views.StartPage.HltbChartStatsSettings(this);

                default:
                    return null;
            }
        }

        public void OnViewRemoved(string viewId, Guid instanceId)
        {

        }
        #endregion


        #region Menus
        // To add new game menu items override GetGameMenuItems
        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            Game GameMenu = args.Games.First();
            List<Guid> Ids = args.Games.Select(x => x.Id).ToList();
            GameHowLongToBeat gameHowLongToBeat = PluginDatabase.Get(GameMenu, true);

            List<GameMenuItem> gameMenuItems = new List<GameMenuItem>
            {
                new GameMenuItem {
                    MenuSection = ResourceProvider.GetString("LOCHowLongToBeat"),
                    Description = ResourceProvider.GetString("LOCHowLongToBeatPluginView"),
                    Action = (gameMenuItem) =>
                    {
                        try
                        {
                            gameHowLongToBeat = PluginDatabase.Get(GameMenu);

                            if (gameHowLongToBeat.HasData || gameHowLongToBeat.HasDataEmpty)
                            {
                                HowLongToBeatView ViewExtension = new HowLongToBeatView(gameHowLongToBeat);
                                Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PluginDatabase.PluginName, ViewExtension);
                                windowExtension.ShowDialog();
                            }
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false, $"Error to load game data for {args.Games.First().Name}");
                            PlayniteApi.Dialogs.ShowErrorMessage(ResourceProvider.GetString("LOCDatabaseErroTitle"), PluginDatabase.PluginName);
                        }
                    }
                }
            };
            
            if (gameHowLongToBeat.HasData || gameHowLongToBeat.HasDataEmpty)
            {
                gameMenuItems.Add(new GameMenuItem
                {
                    MenuSection = ResourceProvider.GetString("LOCHowLongToBeat"),
                    Description = "-"
                });

                // Set current time manually
                gameMenuItems.Add(new GameMenuItem
                {
                    MenuSection = ResourceProvider.GetString("LOCHowLongToBeat"),
                    Description = ResourceProvider.GetString("LOCHowLongToBeatSetCurrentTimeManual"),
                    Action = (mainMenuItem) =>
                    {
                        GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                            $"{PluginDatabase.PluginName} - {ResourceProvider.GetString("LOCCommonProcessing")}",
                            false
                        );

                        globalProgressOptions.IsIndeterminate = args.Games.Count == 1;

                        PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
                        {
                            activateGlobalProgress.ProgressMaxValue = args.Games.Count;
                            activateGlobalProgress.CurrentProgressValue = -1;
                            foreach (Game game in args.Games)
                            {
                                activateGlobalProgress.CurrentProgressValue += 1;
                                PluginDatabase.SetCurrentPlayTime(game, 0, true);
                            }
                        }, globalProgressOptions);
                    }
                });

                // Set current time manually in Complet
                if (GameMenu.Playtime > 0 && GameMenu.LastActivity != null)
                {
                    gameMenuItems.Add(new GameMenuItem
                    {
                        MenuSection = ResourceProvider.GetString("LOCHowLongToBeat") + "|" + ResourceProvider.GetString("LOCHowLongToBeatSetCurrentCompletedTimeManualOn"),
                        Description = ResourceProvider.GetString("LOCHowLongToBeatMainStory"),
                        Action = (mainMenuItem) =>
                        {
                            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                                $"{PluginDatabase.PluginName} - {ResourceProvider.GetString("LOCCommonProcessing")}",
                                false
                            );

                            globalProgressOptions.IsIndeterminate = args.Games.Count == 1;

                            PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
                            {
                                activateGlobalProgress.ProgressMaxValue = args.Games.Count;
                                activateGlobalProgress.CurrentProgressValue = -1;
                                foreach (Game game in args.Games)
                                {
                                    activateGlobalProgress.CurrentProgressValue += 1;
                                    PluginDatabase.SetCurrentPlayTime(game, 0, true, true, true);
                                }
                            }, globalProgressOptions);
                        }
                    });

                    gameMenuItems.Add(new GameMenuItem
                    {
                        MenuSection = ResourceProvider.GetString("LOCHowLongToBeat") + "|" + ResourceProvider.GetString("LOCHowLongToBeatSetCurrentCompletedTimeManualOn"),
                        Description = ResourceProvider.GetString("LOCHowLongToBeatMainExtra"),
                        Action = (mainMenuItem) =>
                        {
                            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                                $"{PluginDatabase.PluginName} - {ResourceProvider.GetString("LOCCommonProcessing")}",
                                false
                            );

                            globalProgressOptions.IsIndeterminate = args.Games.Count == 1;

                            PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
                            {
                                activateGlobalProgress.ProgressMaxValue = args.Games.Count;
                                activateGlobalProgress.CurrentProgressValue = -1;
                                foreach (Game game in args.Games)
                                {
                                    activateGlobalProgress.CurrentProgressValue += 1;
                                    PluginDatabase.SetCurrentPlayTime(game, 0, true, true, false, true);
                                }
                            }, globalProgressOptions);
                        }
                    });

                    gameMenuItems.Add(new GameMenuItem
                    {
                        MenuSection = ResourceProvider.GetString("LOCHowLongToBeat") + "|" + ResourceProvider.GetString("LOCHowLongToBeatSetCurrentCompletedTimeManualOn"),
                        Description = ResourceProvider.GetString("LOCHowLongToBeatCompletionist"),
                        Action = (mainMenuItem) =>
                        {
                            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                                $"{PluginDatabase.PluginName} - {ResourceProvider.GetString("LOCCommonProcessing")}",
                                false
                            );

                            globalProgressOptions.IsIndeterminate = args.Games.Count == 1;

                            PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
                            {
                                activateGlobalProgress.ProgressMaxValue = args.Games.Count;
                                activateGlobalProgress.CurrentProgressValue = -1;
                                foreach (Game game in args.Games)
                                {
                                    activateGlobalProgress.CurrentProgressValue += 1;
                                    PluginDatabase.SetCurrentPlayTime(game, 0, true, true, false, false, true);
                                }
                            }, globalProgressOptions);
                        }
                    });
                }

                // Refresh plugin data for the selected game
                gameMenuItems.Add(new GameMenuItem
                {
                    MenuSection = ResourceProvider.GetString("LOCHowLongToBeat"),
                    Description = ResourceProvider.GetString("LOCCommonRefreshGameData"),
                    Action = (gameMenuItem) =>
                    {
                        if (Ids.Count == 1)
                        {
                            PluginDatabase.Refresh(GameMenu.Id);
                        }
                        else
                        {
                            PluginDatabase.Refresh(Ids);
                        }
                    }
                });

                gameMenuItems.Add(new GameMenuItem
                {
                    MenuSection = ResourceProvider.GetString("LOCHowLongToBeat"),
                    Description = "-"
                });

                gameMenuItems.Add(new GameMenuItem
                {
                    MenuSection = ResourceProvider.GetString("LOCHowLongToBeat"),
                    Description = ResourceProvider.GetString("LOCCommonDeleteGameData"),
                    Action = (gameMenuItem) =>
                    {
                        if (Ids.Count == 1)
                        {
                            PluginDatabase.Remove(GameMenu.Id);
                        }
                        else
                        {
                            PluginDatabase.Remove(Ids);
                        }
                    }
                });
            }

#if DEBUG
            gameMenuItems.Add(new GameMenuItem
            {
                MenuSection = ResourceProvider.GetString("LOCHowLongToBeat"),
                Description = "-"
            });
            gameMenuItems.Add(new GameMenuItem
            {
                MenuSection = ResourceProvider.GetString("LOCHowLongToBeat"),
                Description = "Test",
                Action = (mainMenuItem) => { }
            });
#endif

            return gameMenuItems;
        }

        // To add new main menu items override GetMainMenuItems
        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            string MenuInExtensions = string.Empty;
            if (PluginSettings.Settings.MenuInExtensions)
            {
                MenuInExtensions = "@";
            }

            List <MainMenuItem> mainMenuItems = new List<MainMenuItem>
            {
                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCHowLongToBeat"),
                    Description = ResourceProvider.GetString("LOCHowLongToBeatPluginUserView"),
                    Action = (mainMenuItem) =>
                    {
                        HowLongToBeatUserView ViewExtension = new HowLongToBeatUserView(this);
                        ViewExtension.Height = 660;
                        ViewExtension.Width = 1290;
                        Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PluginDatabase.PluginName, ViewExtension);
                        windowExtension.ShowDialog();
                    }
                },

                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCHowLongToBeat"),
                    Description = "-"
                },

                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCHowLongToBeat"),
                    Description = ResourceProvider.GetString("LOCCommonDownloadPluginData"),
                    Action = (mainMenuItem) =>
                    {
                        PluginDatabase.GetSelectData();
                    }
                },
                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCHowLongToBeat"),
                    Description = ResourceProvider.GetString("LOCCommonClearAllDatas"),
                    Action = (mainMenuItem) =>
                    {
                        if (PluginDatabase.ClearDatabase())
                        {
                            PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCCommonDataRemove"), PluginDatabase.PluginName);
                        }
                        else
                        {
                            PlayniteApi.Dialogs.ShowErrorMessage(ResourceProvider.GetString("LOCCommonDataErrorRemove"), PluginDatabase.PluginName);
                        }
                    }
                },

                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCHowLongToBeat"),
                    Description = "-"
                },

                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCHowLongToBeat"),
                    Description = ResourceProvider.GetString("LOCHowLongToBeatSetCurrentTimeManualAll"),
                    Action = (mainMenuItem) =>
                    {
                        var db = PlayniteApi.Database.Games.Where(x => !x.Hidden && x.Playtime > 0);

                        GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                            $"{PluginDatabase.PluginName} - {ResourceProvider.GetString("LOCCommonProcessing")}",
                            true
                        );

                        globalProgressOptions.IsIndeterminate = db.Count() == 1;

                        PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
                        {
                            activateGlobalProgress.ProgressMaxValue = db.Count();
                            activateGlobalProgress.CurrentProgressValue = -1;
                            foreach (Game game in db)
                            {
                                if (activateGlobalProgress.CancelToken.IsCancellationRequested)
                                {
                                    break;
                                }

                                activateGlobalProgress.CurrentProgressValue += 1;
                                PluginDatabase.SetCurrentPlayTime(game, 0, true);
                            }
                        }, globalProgressOptions);
                    }
                },

                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCHowLongToBeat"),
                    Description = ResourceProvider.GetString("LOCHowLongToBeatActualiseUserData"),
                    Action = (mainMenuItem) =>
                    {
                        PluginDatabase.RefreshUserData();
                    }
                },

                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCHowLongToBeat"),
                    Description = ResourceProvider.GetString("LOCCommonRefreshAllData"),
                    Action = (mainMenuItem) =>
                    {
                        PluginDatabase.RefreshAll();
                    }
                }
            };


            if (PluginDatabase.PluginSettings.Settings.EnableTag)
            {
                mainMenuItems.Add(new MainMenuItem
                {
                    MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCHowLongToBeat"),
                    Description = "-"
                });

                // Add tag for selected game in database if data exists
                mainMenuItems.Add(new MainMenuItem
                {
                    MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCHowLongToBeat"),
                    Description = ResourceProvider.GetString("LOCCommonAddTPlugin"),
                    Action = (mainMenuItem) =>
                    {
                        PluginDatabase.AddTagSelectData();
                    }
                });
                // Add tag for all games
                mainMenuItems.Add(new MainMenuItem
                {
                    MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCHowLongToBeat"),
                    Description = ResourceProvider.GetString("LOCCommonAddAllTags"),
                    Action = (mainMenuItem) =>
                    {
                        PluginDatabase.AddTagAllGame();
                    }
                });
                // Remove tag for all game in database
                mainMenuItems.Add(new MainMenuItem
                {
                    MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCHowLongToBeat"),
                    Description = ResourceProvider.GetString("LOCCommonRemoveAllTags"),
                    Action = (mainMenuItem) =>
                    {
                        PluginDatabase.RemoveTagAllGame();
                    }
                });
            }


            mainMenuItems.Add(new MainMenuItem
            {
                MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCHowLongToBeat"),
                Description = "-"
            });
            mainMenuItems.Add(new MainMenuItem
            {
                MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCHowLongToBeat"),
                Description = ResourceProvider.GetString("LOCCommonViewNoData"),
                Action = (mainMenuItem) =>
                {
                    WindowOptions windowOptions = new WindowOptions
                    {
                        ShowMinimizeButton = false,
                        ShowMaximizeButton = false,
                        ShowCloseButton = true
                    };

                    ListWithNoData ViewExtension = new ListWithNoData(PluginDatabase);
                    Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PluginDatabase.PluginName, ViewExtension, windowOptions);
                    windowExtension.ShowDialog();
                }
            });

#if DEBUG
            mainMenuItems.Add(new MainMenuItem
            {
                MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCHowLongToBeat"),
                Description = "-"
            });
            mainMenuItems.Add(new MainMenuItem
            {
                MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCHowLongToBeat"),
                Description = "Test",
                Action = (mainMenuItem) =>
                {

                }
            });
#endif

            return mainMenuItems;
        }
        #endregion


        #region Game event
        public override void OnGameSelected(OnGameSelectedEventArgs args)
        {
            try
            {
                if (args.NewValue?.Count == 1 && PluginDatabase.IsLoaded)
                {
                    PluginDatabase.GameContext = args.NewValue[0];
                    PluginDatabase.SetThemesResources(PluginDatabase.GameContext);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        // Add code to be executed when game is finished installing.
        public override void OnGameInstalled(OnGameInstalledEventArgs args)
        {

        }

        // Add code to be executed when game is uninstalled.
        public override void OnGameUninstalled(OnGameUninstalledEventArgs args)
        {

        }

        // Add code to be executed when game is preparing to be started.
        public override void OnGameStarting(OnGameStartingEventArgs args)
        {

        }

        // Add code to be executed when game is started running.
        public override void OnGameStarted(OnGameStartedEventArgs args)
        {

        }

        // Add code to be executed when game is preparing to be started.
        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            try
            {
                _ = Task.Run(() =>
                {
                    if (args.Game.Id == PluginDatabase.GameContext.Id)
                    {
                        PluginDatabase.SetThemesResources(PluginDatabase.GameContext);
                    }
                });
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            // AutoSetCurrentPlayTime
            if (PluginSettings.Settings.AutoSetCurrentPlayTime)
            {
                try
                {
                    MessageBoxResult result = MessageBoxResult.Yes;
                    if (!PluginSettings.Settings.AutoSetCurrentPlayTimeWithoutConfirmation)
                    {
                        result = PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCHowLongToBeatSetCurrentTime"), PluginDatabase.PluginName, MessageBoxButton.YesNo);
                    }
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        _ = Task.Run(() =>
                        {
                            Thread.Sleep(5000);
                            if (PluginDatabase.SetCurrentPlayTime(args.Game))
                            {
                                if (PluginDatabase.PluginSettings.Settings.EnableSucessNotification)
                                {
                                    PlayniteApi.Notifications.Add(new NotificationMessage(
                                        $"{PluginDatabase.PluginName}-SetCurrentPlayTime",
                                        PluginDatabase.PluginName + Environment.NewLine +
                                        string.Format(ResourceProvider.GetString("LOCHowLongToBeatCurrentPlayTimeSetted"), args.Game.Name),
                                        NotificationType.Info));
                                }
                            }
                        });
                    }

                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                }
            }
        }
        #endregion


        #region Application event
        // Add code to be executed when Playnite is initialized.
        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            _ = Task.Run(() =>
            {
                Thread.Sleep(10000);
                PreventLibraryUpdatedOnStart = false;
            });

            // QuickSearch support
            try
            {
                string icon = Path.Combine(PluginDatabase.Paths.PluginPath, "Resources", "hltb.png");

                SubItemsAction HltbSubItemsAction = new SubItemsAction() { Action = () => { }, Name = "", CloseAfterExecute = false, SubItemSource = new QuickSearchItemSource() };
                CommandItem HltbCommand = new CommandItem(PluginDatabase.PluginName, new List<CommandAction>(), Playnite.SDK.ResourceProvider.GetString("LOCHltbQuickSearchDescription"), icon);
                HltbCommand.Keys.Add(new CommandItemKey() { Key = "hltb", Weight = 1 });
                HltbCommand.Actions.Add(HltbSubItemsAction);
                _ = QuickSearch.QuickSearchSDK.AddCommand(HltbCommand);
            }
            catch { }

            PluginDatabase.UpdatedCookies();
        }

        // Add code to be executed when Playnite is shutting down.
        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
            
        }
        #endregion


        // Add code to be executed when library is updated.
        public override void OnLibraryUpdated(OnLibraryUpdatedEventArgs args)
        {
            if (PluginSettings.Settings.AutoImport && !PreventLibraryUpdatedOnStart)
            {
                List<Game> PlayniteDb = PlayniteApi.Database.Games
                        .Where(x => x.Added != null && x.Added > PluginSettings.Settings.LastAutoLibUpdateAssetsDownload)
                        .ToList();

                GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                    $"{PluginDatabase.PluginName} - {ResourceProvider.GetString("LOCCommonGettingData")}",
                    true
                );
                globalProgressOptions.IsIndeterminate = false;

                _ = PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
                {
                    try
                    {
                        Stopwatch stopWatch = new Stopwatch();
                        stopWatch.Start();

                        activateGlobalProgress.ProgressMaxValue = (double)PlayniteDb.Count();

                        string CancelText = string.Empty;

                        foreach (Game game in PlayniteDb)
                        {
                            if (activateGlobalProgress.CancelToken.IsCancellationRequested)
                            {
                                CancelText = " canceled";
                                break;
                            }

                            Thread.Sleep(10);
                            PluginDatabase.AddData(game);

                            activateGlobalProgress.CurrentProgressValue++;
                        }

                        stopWatch.Stop();
                        TimeSpan ts = stopWatch.Elapsed;
                        Logger.Info($"Task OnLibraryUpdated(){CancelText} - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {activateGlobalProgress.CurrentProgressValue}/{(double)PlayniteDb.Count()} items");
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, true, PluginDatabase.PluginName);
                    }
                }, globalProgressOptions);

                PluginSettings.Settings.LastAutoLibUpdateAssetsDownload = DateTime.Now;
                SavePluginSettings(PluginSettings.Settings);
            }
        }


        #region Settings
        public override ISettings GetSettings(bool firstRunSettings)
        {
            return PluginSettings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new HowLongToBeatSettingsView(PluginSettings.Settings);
        }
        #endregion
    }
}
