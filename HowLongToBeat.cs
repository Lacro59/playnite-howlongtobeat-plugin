using HowLongToBeat.Services;
using HowLongToBeat.Views;
using HowLongToBeat.Views.Interfaces;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PluginCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
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
                            new Views.HowLongToBeat(data, GameSelected, PlayniteApi).ShowDialog();
                        }
                    })
            };
        }

        private StackPanel PART_ElemDescription = null;

        public override void OnGameSelected(GameSelectionEventArgs args)
        {
            try
            {
                GameSelected = args.NewValue[0];

                // Search parent game description
                if (PART_ElemDescription == null)
                {
                    foreach (StackPanel sp in Tools.FindVisualChildren<StackPanel>(Application.Current.MainWindow))
                    {
                        if (sp.Name == "PART_ElemDescription")
                        {
                            PART_ElemDescription = sp;
                            break;
                        }
                    }
                }

                if (args.NewValue != null && settings.enableIntegrationInDescription)
                {
                    if (args.NewValue.Count == 1)
                    {
                        try
                        {
                            Game SelectedGame = args.NewValue[0];

                            HowLongToBeatData data = new HowLongToBeatData(GameSelected, this.GetPluginUserDataPath(), false);

                            DockPanel dpParent = (DockPanel)(PART_ElemDescription).Parent;

                            // Delete
                            StackPanel PART_Hltb = (StackPanel)LogicalTreeHelper.FindLogicalNode(dpParent, "PART_Hltb");
                            if (PART_Hltb != null)
                            {
                                logger.Debug("HowLongToBeat - Delete old PART_Hltb");
                                dpParent.Children.Remove(PART_Hltb);
                            }
                            else
                            {
                                logger.Debug("HowLongToBeat - No delete old PART_Hltb");
                            }


                            if (data.GetData() != null)
                            {
                                // Create 
                                StackPanel spHltb = new StackPanel();
                                spHltb.Name = "PART_Hltb";
                                spHltb.Orientation = Orientation.Vertical;

                                TextBlock tbHltb = new TextBlock();
                                tbHltb.Name = "PART_tbHltb";
                                tbHltb.Text = SelectedGame.Name;//resources.GetString("LOCSucessStoryAchievements");
                                tbHltb.Style = (Style)resources.GetResource("BaseTextBlockStyle");
                                
                                Separator hltbsep = new Separator();
                                hltbsep.Name = "PART_hltbsep";
                                hltbsep.Background = (Brush)resources.GetResource("PanelSeparatorBrush");

                                UserControl hltbProgressBar = new HltbProgressBar(SelectedGame.Playtime, data.GetData());
                                hltbProgressBar.Name = "PART_hltbProgressBar";

                                spHltb.Children.Add(tbHltb);
                                spHltb.Children.Add(hltbsep);
                                spHltb.Children.Add(hltbProgressBar);
                                spHltb.UpdateLayout();


                                // Add
                                int index = dpParent.Children.Count - 1;
                                dpParent.Children.Insert(index, spHltb);
                                dpParent.UpdateLayout();

                                PART_ElemDescription.Orientation = Orientation.Vertical;
                                PART_ElemDescription.UpdateLayout();
                            }
                        }
                        catch (Exception ex)
                        {
                            var LineNumber = new StackTrace(ex, true).GetFrame(0).GetFileLineNumber();
                            string FileName = new StackTrace(ex, true).GetFrame(0).GetFileName();
                            logger.Error(ex, $"HowLongToBeat [{FileName} {LineNumber}] - Impossible integration ");
                        }
                    }
                }
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

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new HowLongToBeatSettingsView();
        }
    }
}