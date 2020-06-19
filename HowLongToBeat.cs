using HowLongToBeat.Models;
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

                if (args.NewValue != null)
                {
                    if (args.NewValue.Count == 1)
                    {
                        try
                        {
                            Game SelectedGame = args.NewValue[0];
                            HowLongToBeatData data = new HowLongToBeatData(GameSelected, this.GetPluginUserDataPath(), false);

                            // Auto integration
                            if (settings.enableIntegrationInDescription)
                            {
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

                                // Delete
                                StackPanel PART_Hltb = (StackPanel)LogicalTreeHelper.FindLogicalNode(PART_ElemDescription, "PART_Hltb");
                                if (PART_Hltb != null)
                                {
                                    PART_ElemDescription.Children.Remove(PART_Hltb);
                                }

                                if (data.GetData() != null)
                                {
                                    // Create 
                                    StackPanel spHltb = CreateHltb(SelectedGame.Playtime, data.GetData());

                                    // Add
                                    PART_ElemDescription.Children.Insert(0, spHltb);
                                    PART_ElemDescription.UpdateLayout();

                                    PART_ElemDescription.Orientation = Orientation.Vertical;
                                    PART_ElemDescription.UpdateLayout();
                                }
                            }
                            // Custom theme
                            else
                            {
                                // Search custom element
                                foreach (StackPanel sp in Tools.FindVisualChildren<StackPanel>(Application.Current.MainWindow))
                                {
                                    if (sp.Name == "PART_hltbProgressBar")
                                    {
                                        if (data.GetData() != null)
                                        {
                                            // Create 
                                            StackPanel spHltb = CreateHltb(SelectedGame.Playtime, data.GetData());

                                            // Clear & add
                                            sp.Children.Clear();
                                            sp.Children.Add(spHltb);
                                            sp.UpdateLayout();
                                        }
                                        else
                                        {
                                            sp.Children.Clear();
                                            sp.UpdateLayout();
                                        }
                                        break;
                                    }
                                }
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

        /// <summary>
        /// Create the StackPanel with the ProgressBar.
        /// </summary>
        /// <param name="Playtime"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private StackPanel CreateHltb (long Playtime, HltbDataUser data)
        {
            StackPanel spHltb = new StackPanel();
            spHltb.Name = "PART_Hltb";
            spHltb.Orientation = Orientation.Vertical;

            TextBlock tbHltb = new TextBlock();
            tbHltb.Name = "PART_tbHltb";
            tbHltb.Text = resources.GetString("LOCHowLongToBeatTitle");
            tbHltb.Style = (Style)resources.GetResource("BaseTextBlockStyle");
            tbHltb.Margin = new Thickness(0, 15, 0, 10);

            Separator hltbsep = new Separator();
            hltbsep.Name = "PART_hltbsep";
            hltbsep.Background = (Brush)resources.GetResource("PanelSeparatorBrush");

            UserControl hltbProgressBar = new HltbProgressBar(Playtime, data);
            hltbProgressBar.Name = "PART_hltbProgressBar";
            hltbProgressBar.Margin = new Thickness(0, 5, 0, 5);

            spHltb.Children.Add(tbHltb);
            spHltb.Children.Add(hltbsep);
            spHltb.Children.Add(hltbProgressBar);
            spHltb.UpdateLayout();

            return spHltb;
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
