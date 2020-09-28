using HowLongToBeat.Models;
using HowLongToBeat.Services;
using HowLongToBeat.Views;
using HowLongToBeat.Views.Interfaces;
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
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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

        private readonly IntegrationUI ui = new IntegrationUI();

        
        public HowLongToBeat(IPlayniteAPI api) : base(api)
        {
            settings = new HowLongToBeatSettings(this);


            // Get plugin's location 
            string pluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // Add plugin localization in application ressource.
            PluginCommon.Localization.SetPluginLanguage(pluginFolder, api.Paths.ConfigurationPath);
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

            // Custom theme button
            if (settings.EnableIntegrationInCustomTheme)
            {
                EventManager.RegisterClassHandler(typeof(Button), Button.ClickEvent, new RoutedEventHandler(OnCustomThemeButtonClick));
            }
        }

        public override IEnumerable<ExtensionFunction> GetFunctions()
        {
            List<ExtensionFunction> listFunctions = new List<ExtensionFunction>();

            listFunctions.Add(
                new ExtensionFunction(
                        resources.GetString("LOCHowLongToBeat"),
                        () =>
                        {
                                // Add code to be execute when user invokes this menu entry.

                                try
                            {
                                HowLongToBeatData data = new HowLongToBeatData(GameSelected, this.GetPluginUserDataPath(), PlayniteApi);
                                if (data.hasData)
                                {
                                    if (settings.EnableTag)
                                    {
                                        data.AddTag();
                                    }

                                    new Views.HowLongToBeat(data, GameSelected, PlayniteApi, settings).ShowDialog();
                                    try
                                    {
                                        Integration();
                                    }
                                    catch (Exception ex)
                                    {
                                        Common.LogError(ex, "HowLongToBeat", $"Error on Integration");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Common.LogError(ex, "HowLongToBeat", "Error to load database");
                                PlayniteApi.Dialogs.ShowErrorMessage(resources.GetString("LOCDatabaseErroTitle"), "HowLongToBeat");
                            }
                        })
                );

#if DEBUG
            listFunctions.Add(
                new ExtensionFunction(
                    "HowLongToBeat Test",
                    () =>
                    {

                    })
                );
#endif

            return listFunctions;
        }


        #region Interface integration
        private Game GameSelected { get; set; }

        /// <summary>
        /// Button event for call plugin view in custom theme.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCustomThemeButtonClick(object sender, RoutedEventArgs e)
        {
            string ButtonName = string.Empty;
            try
            {
                ButtonName = ((Button)sender).Name;
                if (ButtonName == "PART_HltbCustomButton")
                {
                    OnBtGameSelectedActionBarClick(sender, e);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "HowLongToBeat", "OnCustomThemeButtonClick() error");
            }
        }

        public override void OnGameSelected(GameSelectionEventArgs args)
        {
            try
            {
                if (args.NewValue != null && args.NewValue.Count == 1)
                {
                    GameSelected = args.NewValue[0];
                    try
                    {
                        Integration();
                    }
                    catch(Exception ex)
                    {
                        Common.LogError(ex, "HowLongToBeat", $"Error on Integration");
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "HowLongToBeat", $"Error on OnGameSelected()");
            }
        }

        private void OnBtGameSelectedActionBarClick(object sender, RoutedEventArgs e)
        {
            try
            {
                HowLongToBeatData data = new HowLongToBeatData(GameSelected, this.GetPluginUserDataPath(), PlayniteApi);
                if (data.GetData() != null)
                {
                    if (settings.EnableTag)
                    {
                        data.AddTag();
                    }

                    new Views.HowLongToBeat(data, GameSelected, PlayniteApi, settings).ShowDialog();
                    try
                    {
                        Integration();
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, "HowLongToBeat", $"Error on Integration");
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "HowLongToBeat", "Error to load data");
                PlayniteApi.Dialogs.ShowErrorMessage(resources.GetString("LOCDatabaseErroTitle"), "HowLongToBeat");
            }
        }

        private HowLongToBeatData LoadData(IPlayniteAPI PlayniteApi, string PluginUserDataPath, HowLongToBeatSettings settings)
        {
            HowLongToBeatData HltbGameData = null;
            try
            {
                HltbGameData = new HowLongToBeatData(GameSelected, this.GetPluginUserDataPath(), PlayniteApi, false);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "HowLongToBeat", "Error to load data");
                PlayniteApi.Dialogs.ShowErrorMessage(resources.GetString("LOCDatabaseErroTitle"), "HowLongToBeat");
            }

            return HltbGameData;
        }

        /// <summary>
        /// Integration plugin interface in application.
        /// </summary>
        private void Integration(Game game = null)
        {
            try
            {
                if (game != null)
                {
                    GameSelected = game;
                }

                // Delete
                logger.Info("HowLongToBeat - Delete integeration");
                ui.RemoveButtonInGameSelectedActionBarButtonOrToggleButton("PART_HltbButton");
                ui.RemoveElementInGameSelectedDescription("PART_HltbProgressBarIntegration");
                ui.ClearElementInCustomTheme("PART_hltbProgressBarWithTitle");
                ui.ClearElementInCustomTheme("PART_hltbProgressBar");

                // Reset resources
                List<ResourcesList> resourcesLists = new List<ResourcesList>();
                resourcesLists.Add(new ResourcesList { Key = "Htlb_HasData", Value = false });
                resourcesLists.Add(new ResourcesList { Key = "Htlb_MainStory", Value = 0 });
                resourcesLists.Add(new ResourcesList { Key = "Htlb_MainStoryFormat", Value = string.Empty });
                resourcesLists.Add(new ResourcesList { Key = "Htlb_MainExtra", Value = 0 });
                resourcesLists.Add(new ResourcesList { Key = "Htlb_MainExtraFormat", Value = string.Empty });
                resourcesLists.Add(new ResourcesList { Key = "Htlb_Completionist", Value = 0 });
                resourcesLists.Add(new ResourcesList { Key = "Htlb_CompletionistFormat", Value = string.Empty });
                resourcesLists.Add(new ResourcesList { Key = "Htlb_Solo", Value = 0 });
                resourcesLists.Add(new ResourcesList { Key = "Htlb_SoloFormat", Value = string.Empty });
                resourcesLists.Add(new ResourcesList { Key = "Htlb_CoOp", Value = 0 });
                resourcesLists.Add(new ResourcesList { Key = "Htlb_CoOpFormat", Value = string.Empty });
                resourcesLists.Add(new ResourcesList { Key = "Htlb_Vs", Value = 0 });
                resourcesLists.Add(new ResourcesList { Key = "Htlb_VsFormat", Value = string.Empty });

                resourcesLists.Add(new ResourcesList { Key = "Htlb_EnableIntegrationInCustomTheme", Value = settings.EnableIntegrationInCustomTheme });
                ui.AddResources(resourcesLists);


                var taskIntegration = Task.Run(() => LoadData(PlayniteApi, this.GetPluginUserDataPath(), settings))
                    .ContinueWith(antecedent =>
                    {
                        HowLongToBeatData HltbGameData = antecedent.Result;

                        if (settings.EnableTag)
                        {
                            HltbGameData.AddTag();
                        }

                        Application.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            // Add button in action bar
                            if (settings.EnableIntegrationButton)
                            {
                                Button HltbButton = new Button();
                                HltbButton.Name = "PART_HltbButton";
                                HltbButton.FontFamily = new FontFamily(new Uri("pack://application:,,,/PluginCommon;component/Resources/"), "./#font");
                                HltbButton.Margin = new Thickness(10, 0, 0, 0);
                                HltbButton.Click += OnBtGameSelectedActionBarClick;
                                HltbButton.Content = TransformIcon.Get("HowLongToBeat");

                                ui.AddButtonInGameSelectedActionBarButtonOrToggleButton(HltbButton);
                            }

                            if (HltbGameData.hasData)
                            {
                                // Add resources
                                resourcesLists = new List<ResourcesList>();
                                resourcesLists.Add(new ResourcesList { Key = "Htlb_HasData", Value = HltbGameData.hasData });
                                resourcesLists.Add(new ResourcesList { Key = "Htlb_MainStory", Value = HltbGameData.GetData().GameHltbData.MainStory });
                                resourcesLists.Add(new ResourcesList { Key = "Htlb_MainStoryFormat", Value = HltbGameData.GetData().GameHltbData.MainStoryFormat });
                                resourcesLists.Add(new ResourcesList { Key = "Htlb_MainExtra", Value = HltbGameData.GetData().GameHltbData.MainExtra });
                                resourcesLists.Add(new ResourcesList { Key = "Htlb_MainExtraFormat", Value = HltbGameData.GetData().GameHltbData.MainExtraFormat });
                                resourcesLists.Add(new ResourcesList { Key = "Htlb_Completionist", Value = HltbGameData.GetData().GameHltbData.Completionist });
                                resourcesLists.Add(new ResourcesList { Key = "Htlb_CompletionistFormat", Value = HltbGameData.GetData().GameHltbData.CompletionistFormat });
                                resourcesLists.Add(new ResourcesList { Key = "Htlb_Solo", Value = HltbGameData.GetData().GameHltbData.Solo });
                                resourcesLists.Add(new ResourcesList { Key = "Htlb_SoloFormat", Value = HltbGameData.GetData().GameHltbData.SoloFormat });
                                resourcesLists.Add(new ResourcesList { Key = "Htlb_CoOp", Value = HltbGameData.GetData().GameHltbData.CoOp });
                                resourcesLists.Add(new ResourcesList { Key = "Htlb_CoOpFormat", Value = HltbGameData.GetData().GameHltbData.CoOpFormat });
                                resourcesLists.Add(new ResourcesList { Key = "Htlb_Vs", Value = HltbGameData.GetData().GameHltbData.Vs });
                                resourcesLists.Add(new ResourcesList { Key = "Htlb_VsFormat", Value = HltbGameData.GetData().GameHltbData.VsFormat });
                                ui.AddResources(resourcesLists);

                                // Auto integration
                                if (settings.EnableIntegrationInDescription)
                                {
                                    StackPanel spHltb = CreateHltb(GameSelected.Playtime, HltbGameData.GetData(), settings.IntegrationShowTitle);
                                    spHltb.Name = "PART_HltbProgressBarIntegration";

                                    ui.AddElementInGameSelectedDescription(spHltb, settings.IntegrationTopGameDetails);
                                }

                                // Custom theme
                                if (settings.EnableIntegrationInCustomTheme)
                                {
                                    // Create 
                                    StackPanel spHltb = CreateHltb(GameSelected.Playtime, HltbGameData.GetData(), true);
                                    UserControl hltbProgressBar = new HltbProgressBar(GameSelected.Playtime, HltbGameData.GetData(), settings);

                                    ui.AddElementInCustomTheme(spHltb, "PART_hltbProgressBarWithTitle");
                                    ui.AddElementInCustomTheme(hltbProgressBar, "PART_hltbProgressBar");
                                }
                            }
                        }));
                    });
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "HowLongToBeat", "Impossible integration");
            }
        }

        /// <summary>
        /// Create the StackPanel with the ProgressBar.
        /// </summary>
        /// <param name="Playtime"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private StackPanel CreateHltb(long Playtime, HltbDataUser data, bool IntegrationShowTitle)
        {
            StackPanel spHltb = new StackPanel();

            TextBlock tbHltb = new TextBlock();
            Separator hltbsep = new Separator();
            if (IntegrationShowTitle)
            {
                tbHltb.Name = "PART_tbHltb";
                tbHltb.Text = resources.GetString("LOCHowLongToBeatTitle");
                tbHltb.Style = (Style)resources.GetResource("BaseTextBlockStyle");
                tbHltb.Margin = new Thickness(0, 15, 0, 10);

                hltbsep.Name = "PART_hltbsep";
                hltbsep.Background = (Brush)resources.GetResource("PanelSeparatorBrush");
            }

            UserControl hltbProgressBar = new HltbProgressBar(Playtime, data, settings);
            hltbProgressBar.Name = "PART_hltbProgressBar";
            hltbProgressBar.Margin = new Thickness(0, 5, 0, 5);

            if (IntegrationShowTitle)
            {
                spHltb.Children.Add(tbHltb);
                spHltb.Children.Add(hltbsep);
            }
            spHltb.Children.Add(hltbProgressBar);
            spHltb.UpdateLayout();

            return spHltb;
        }
        #endregion


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
                Integration(game);
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
