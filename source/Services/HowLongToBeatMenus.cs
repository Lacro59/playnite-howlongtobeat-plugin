using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Interfaces;
using HowLongToBeat.Models;
using HowLongToBeat.Models.Enumerations;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HowLongToBeat.Services
{
    public class HowLongToBeatMenus : PluginMenus
    {
        private readonly GenericPlugin plugin;
        private HowLongToBeatDatabase Database => (HowLongToBeatDatabase)_database;

        public HowLongToBeatMenus(IPluginSettings settings, IPluginDatabase database, GenericPlugin plugin) : base(settings, database)
        {
            this.plugin = plugin;
        }

        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            if (args?.Games == null || !args.Games.Any())
            {
                return Enumerable.Empty<GameMenuItem>();
            }

            Game gameMenu = args.Games.First();
            List<Guid> ids = args.Games.Select(x => x.Id).ToList();
            GameHowLongToBeat gameHowLongToBeat = Database.Get(gameMenu, true);

            List<GameMenuItem> gameMenuItems = new List<GameMenuItem>
            {
                new GameMenuItem
                {
                    MenuSection = ResourceProvider.GetString("LOCHowLongToBeat"),
                    Description = ResourceProvider.GetString("LOCHowLongToBeatPluginView"),
                    Action = (gameMenuItem) =>
                    {
                        try
                        {
                            Database.PluginWindows.ShowPluginGameDataWindow(gameMenu);
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false, $"Error to load game data for {args.Games.First().Name}");
                            API.Instance.Dialogs.ShowErrorMessage(ResourceProvider.GetString("LOCDatabaseErroTitle"), Database.PluginName);
                        }
                    }
                }
            };

            if (gameHowLongToBeat?.HasData == true)
            {
                HltbDataUser gameData = gameHowLongToBeat.Items?.FirstOrDefault();

                gameMenuItems.Add(new GameMenuItem
                {
                    MenuSection = ResourceProvider.GetString("LOCHowLongToBeat"),
                    Description = "-"
                });

                if (!gameHowLongToBeat?.GetData()?.IsVndb ?? false)
                {
                    gameMenuItems.Add(new GameMenuItem
                    {
                        MenuSection = ResourceProvider.GetString("LOCHowLongToBeat"),
                        Description = ResourceProvider.GetString("LOCHowLongToBeatSetCurrentTimeManual"),
                        Action = (mainMenuItem) => Database.SetCurrentPlaytime(ids, true)
                    });

                    if (gameData != null && gameMenu.Playtime > 0)
                    {
                        if (gameData.GameType != GameType.Multi && gameMenu.LastActivity != null)
                        {
                            gameMenuItems.Add(new GameMenuItem
                            {
                                MenuSection = ResourceProvider.GetString("LOCHowLongToBeat") + "|" + ResourceProvider.GetString("LOCHowLongToBeatSetCurrentCompletedTimeManualOn"),
                                Description = ResourceProvider.GetString("LOCHowLongToBeatMainStory"),
                                Action = (mainMenuItem) => Database.SetCurrentPlaytime(ids, true, true, true)
                            });

                            gameMenuItems.Add(new GameMenuItem
                            {
                                MenuSection = ResourceProvider.GetString("LOCHowLongToBeat") + "|" + ResourceProvider.GetString("LOCHowLongToBeatSetCurrentCompletedTimeManualOn"),
                                Description = ResourceProvider.GetString("LOCHowLongToBeatMainExtra"),
                                Action = (mainMenuItem) => Database.SetCurrentPlaytime(ids, true, true, false, true)
                            });

                            gameMenuItems.Add(new GameMenuItem
                            {
                                MenuSection = ResourceProvider.GetString("LOCHowLongToBeat") + "|" + ResourceProvider.GetString("LOCHowLongToBeatSetCurrentCompletedTimeManualOn"),
                                Description = ResourceProvider.GetString("LOCHowLongToBeatCompletionist"),
                                Action = (mainMenuItem) => Database.SetCurrentPlaytime(ids, true, true, false, false, true)
                            });
                        }
                        else
                        {
                            gameMenuItems.Add(new GameMenuItem
                            {
                                MenuSection = ResourceProvider.GetString("LOCHowLongToBeat") + "|" + ResourceProvider.GetString("LOCHowLongToBeatSetCurrentMultiTimeManualOn"),
                                Description = ResourceProvider.GetString("LOCHowLongToBeatCoOp"),
                                Action = (mainMenuItem) => Database.SetCurrentPlaytime(ids, true, false, false, false, false, false, true)
                            });

                            gameMenuItems.Add(new GameMenuItem
                            {
                                MenuSection = ResourceProvider.GetString("LOCHowLongToBeat") + "|" + ResourceProvider.GetString("LOCHowLongToBeatSetCurrentMultiTimeManualOn"),
                                Description = ResourceProvider.GetString("LOCHowLongToBeatVs"),
                                Action = (mainMenuItem) => Database.SetCurrentPlaytime(ids, true, false, false, false, false, false, false, true)
                            });
                        }
                    }
                }

                gameMenuItems.Add(new GameMenuItem
                {
                    MenuSection = ResourceProvider.GetString("LOCHowLongToBeat"),
                    Description = ResourceProvider.GetString("LOCCommonRefreshGameData"),
                    Action = (gameMenuItem) =>
                    {
                        if (ids.Count == 1)
                        {
                            Database.Refresh(gameMenu.Id);
                        }
                        else
                        {
                            Database.Refresh(ids);
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
                        if (ids.Count == 1)
                        {
                            Database.Remove(gameMenu.Id);
                        }
                        else
                        {
                            Database.Remove(ids);
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
                Action = (gameMenuItem) => { }
            });
#endif

            return gameMenuItems;
        }

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            string menuInExtensions = _settings.MenuInExtensions ? "@" : string.Empty;
            string section = menuInExtensions + ResourceProvider.GetString("LOCHowLongToBeat");

            List<MainMenuItem> mainMenuItems = new List<MainMenuItem>
            {
                new MainMenuItem
                {
                    MenuSection = section,
                    Description = ResourceProvider.GetString("LOCHowLongToBeatPluginUserView"),
                    Action = (mainMenuItem) => Database.PluginWindows.ShowPluginGameDataWindow(plugin)
                },
                new MainMenuItem
                {
                    MenuSection = section,
                    Description = "-"
                },
                new MainMenuItem
                {
                    MenuSection = section,
                    Description = ResourceProvider.GetString("LOCCommonDownloadPluginData"),
                    Action = (mainMenuItem) => Database.GetSelectData()
                },
                new MainMenuItem
                {
                    MenuSection = section,
                    Description = ResourceProvider.GetString("LOCCommonClearAllDatas"),
                    Action = (mainMenuItem) =>
                    {
                        if (Database.ClearDatabase())
                        {
                            API.Instance.Dialogs.ShowMessage(ResourceProvider.GetString("LOCCommonDataRemove"), Database.PluginName);
                        }
                        else
                        {
                            API.Instance.Dialogs.ShowErrorMessage(ResourceProvider.GetString("LOCCommonDataErrorRemove"), Database.PluginName);
                        }
                    }
                },
                new MainMenuItem
                {
                    MenuSection = section,
                    Description = "-"
                },
                new MainMenuItem
                {
                    MenuSection = section,
                    Description = ResourceProvider.GetString("LOCHowLongToBeatSetCurrentTimeManualAll"),
                    Action = (mainMenuItem) =>
                    {
                        IEnumerable<Game> db = API.Instance.Database.Games.Where(x => !x.Hidden && x.Playtime > 0);
                        Database.SetCurrentPlaytime(db.Select(x => x.Id), true);
                    }
                },
                new MainMenuItem
                {
                    MenuSection = section,
                    Description = ResourceProvider.GetString("LOCHowLongToBeatActualiseUserData"),
                    Action = (mainMenuItem) => Database.RefreshUserData()
                },
                new MainMenuItem
                {
                    MenuSection = section,
                    Description = ResourceProvider.GetString("LOCCommonRefreshAllData"),
                    Action = (mainMenuItem) => Database.RefreshAll()
                },
                new MainMenuItem
                {
                    MenuSection = section,
                    Description = ResourceProvider.GetString("LOCCommonExtractToCsv"),
                    Action = (mainMenuItem) => Database.ExtractToCsv()
                }
            };

            if (_settings.EnableTag)
            {
                mainMenuItems.Add(new MainMenuItem
                {
                    MenuSection = section,
                    Description = "-"
                });

                mainMenuItems.Add(new MainMenuItem
                {
                    MenuSection = section,
                    Description = ResourceProvider.GetString("LOCCommonAddTPlugin"),
                    Action = (mainMenuItem) => Database.AddTagSelectData()
                });
                mainMenuItems.Add(new MainMenuItem
                {
                    MenuSection = section,
                    Description = ResourceProvider.GetString("LOCCommonAddAllTags"),
					Action = mainMenuItem => _commands.CmdAddTag.Execute(null)
				});
                mainMenuItems.Add(new MainMenuItem
                {
                    MenuSection = section,
                    Description = ResourceProvider.GetString("LOCCommonRemoveAllTags"),
					Action = mainMenuItem => _commands.CmdRemoveTag.Execute(null)
				});
            }

            mainMenuItems.Add(new MainMenuItem
            {
                MenuSection = section,
                Description = "-"
            });
            mainMenuItems.Add(new MainMenuItem
            {
                MenuSection = section,
                Description = ResourceProvider.GetString("LOCCommonViewNoData"),
                Action = (mainMenuItem) => Database.PluginWindows.ShowPluginGameNoDataWindow()
            });

#if DEBUG
            mainMenuItems.Add(new MainMenuItem
            {
                MenuSection = section,
                Description = "-"
            });
            mainMenuItems.Add(new MainMenuItem
            {
                MenuSection = section,
                Description = "Test",
                Action = (mainMenuItem) => { }
            });
#endif

            return mainMenuItems;
        }
    }
}
