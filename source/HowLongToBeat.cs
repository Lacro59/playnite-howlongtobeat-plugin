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
using System.IO;
using QuickSearch.SearchItems;
using StartPage.SDK;
using LiveCharts.Configurations;
using CommonPluginsControls.LiveChartsCommon;
using LiveCharts;
using Playnite.SDK.Data;
using HowLongToBeat.Models.Enumerations;

namespace HowLongToBeat
{
    public class HowLongToBeat : PluginExtended<HowLongToBeatSettingsViewModel, HowLongToBeatDatabase>, IStartPageExtension
    {
        public override Guid Id => Guid.Parse("e08cd51f-9c9a-4ee3-a094-fde03b55492f");

        internal TopPanelItem TopPanelItem { get; set; }
        internal SidebarItem SidebarItem { get; set; }
        internal SidebarItemControl SidebarItemControl { get; set; }

        private bool PreventLibraryUpdatedOnStart { get; set; } = true;


        public HowLongToBeat(IPlayniteAPI playniteAPI) : base(playniteAPI, "HowLongToBeat")
        {
            _menus = new HowLongToBeatMenus(PluginSettingsViewModel.Settings, PluginDatabase, this);

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
                SettingsRoot = $"{nameof(PluginSettingsViewModel)}.{nameof(PluginSettingsViewModel.Settings)}"
            });

            // Initialize top & side bar
            if (API.Instance.ApplicationInfo.Mode == ApplicationMode.Desktop)
            {
                TopPanelItem = new HowLongToBeatTopPanelItem(this);
                SidebarItem = new HowLongToBeatViewSidebar(this);
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
                    PluginDatabase.PluginWindows.ShowPluginGameDataWindow(this);
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
                SidebarItem
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
            return _menus.GetGameMenuItems(args);
        }

        // To add new main menu items override GetMainMenuItems
        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            return _menus.GetMainMenuItems(args);
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
            if (PluginDatabase.PluginSettings.AutoSetCurrentPlayTime)
            {
                try
                {
                    MessageBoxResult result = MessageBoxResult.Yes;
                    if (!PluginDatabase.PluginSettings.AutoSetCurrentPlayTimeWithoutConfirmation)
                    {
                        result = PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCHowLongToBeatSetCurrentTime"), PluginDatabase.PluginName, MessageBoxButton.YesNo);
                    }

                    if (result == MessageBoxResult.Yes)
                    {
                        _ = Task.Run(() =>
                        {
                            Thread.Sleep(2000);
                            if (PluginDatabase.SetCurrentPlayTime(args.Game))
                            {
                                if (PluginDatabase.PluginSettings.EnableSucessNotification)
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
            try
            {
                try
                {
                    var initTask = CommonPluginsShared.Web.InitializeAsync(createInBackground: true);
                    initTask.ContinueWith(t =>
                    {
                        try { Common.LogError(t.Exception?.GetBaseException() ?? new Exception("Web.InitializeAsync failed"), false, "Web initialization"); } catch { }
                    }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
                }
                catch { }

                try
                {
                    PluginDatabase.InitializeClient(this);
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                }

                try
                {
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                }
            }
            catch { }

            _ = Task.Run(async () =>
            {
                try
                {
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    const int maxWaitMs = 30000;
                    while (!PluginDatabase.IsLoaded && sw.ElapsedMilliseconds < maxWaitMs)
                    {
                        await Task.Delay(100).ConfigureAwait(false);
                    }
                }
                catch { }
                finally
                {
                    PreventLibraryUpdatedOnStart = false;
                }
            });


             // QuickSearch support
             try
             {
                 string icon = Path.Combine(PluginDatabase.Paths.PluginPath, "Resources", "hltb.png");

                 SubItemsAction HltbSubItemsAction = new SubItemsAction() { Action = () => { }, Name = "", CloseAfterExecute = false, SubItemSource = new QuickSearchItemSource() };
                 CommandItem HltbCommand = new CommandItem(PluginDatabase.PluginName, new List<CommandAction>(), ResourceProvider.GetString("LOCHltbQuickSearchDescription"), icon);
                 HltbCommand.Keys.Add(new CommandItemKey() { Key = "hltb", Weight = 1 });
                 HltbCommand.Actions.Add(HltbSubItemsAction);
                 _ = QuickSearch.QuickSearchSDK.AddCommand(HltbCommand);
             }
             catch (Exception ex)
             {
                 try
                 {
                     if (PluginDatabase?.PluginSettings is HowLongToBeatSettings s && s.EnableVerboseLogging)
                     {
                         Common.LogError(ex, false, true, PluginDatabase.PluginName);
                     }
                 }
                 catch { }
             }
         }

        // Add code to be executed when Playnite is shutting down.
        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {

        }

        #endregion

        // Add code to be executed when library is updated.
        public override void OnLibraryUpdated(OnLibraryUpdatedEventArgs args)
        {
            if (PluginDatabase.PluginSettings.AutoImport && !PreventLibraryUpdatedOnStart)
            {
                PluginDatabase.RefreshRecent();
                PluginDatabase.PluginSettings.LastAutoLibUpdateAssetsDownload = DateTime.Now;
                SavePluginSettings(PluginDatabase.PluginSettings);
            }
        }

        #region Settings

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return PluginSettingsViewModel;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new HowLongToBeatSettingsView(PluginSettingsViewModel.Settings);
        }

        #endregion
    }
}