using CommonPluginsShared;
using CommonPluginsShared.Commands;
using CommonPluginsShared.Models;
using HowLongToBeat.Models;
using HowLongToBeat.Services;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace HowLongToBeat.Views
{
    public partial class HltbSettingsView : UserControl
    {
        private static ILogger Logger => LogManager.GetLogger();

        private static HowLongToBeatDatabase PluginDatabase => HowLongToBeat.PluginDatabase;

        private TextBlock tbControl;
        private HowLongToBeatSettings _settingsRef;
        private ObservableCollection<Game> _ignoreSyncGames;
        private bool _ignoreSyncListInitialized;

        private HltbGeneralSettingsSection generalSection;
        private HltbSyncSettingsSection syncIgnoredGamesSection;
        private HltbSyncStatusSettingsSection syncStatusSection;
        private HltbDataSettingsSection dataAliasesSection;
        private HltbDataExportSettingsSection dataExportSection;
        private HltbDataDatabaseSettingsSection dataDatabaseSection;
        private HltbDisplayProgressBarSettingsSection displayProgressBarSection;
        private HltbHelpSettingsSection helpSection;
        private HltbSettingsMasterDetailControl syncMasterDetail;
        private HltbSettingsMasterDetailControl dataMasterDetail;
        private HltbSettingsMasterDetailControl displayMasterDetail;

        public HltbSettingsView(HowLongToBeatSettings settings)
        {
            _settingsRef = settings;
            try
            {
                if (PluginDatabase?.HowLongToBeatApi != null)
                {
                    PluginDatabase.HowLongToBeatApi.PropertyChanged += OnPropertyChanged;
                    PluginDatabase.HowLongToBeatApi.LoginCompleted += HowLongToBeatApi_LoginCompleted;
                }
            }
            catch { }

            InitializeComponent();
            InitializeSectionContent();
            InitializeDisplayProgressBarColors(settings);

            // Run authentication check in fire-and-forget to avoid async void from constructor.
            try
            {
                TaskHelpers.FireAndForget(CheckAuthenticateAsync(), "SettingsView-CheckAuthenticate", Logger);
            }
            catch { }

            try
            {
                this.Unloaded += HltbSettingsView_Unloaded;
            }
            catch { }

            helpSection.PART_TTB.Source = BitmapExtensions.BitmapFromFile(Path.Combine(PluginDatabase.Paths.PluginPath, "Resources", "ttb.png"));

            // Ensure aliases list is hydrated for the UI.
            try
            {
                if (_settingsRef != null)
                {
                    if ((_settingsRef.GameNameAliases == null || _settingsRef.GameNameAliases.Count == 0) && (_settingsRef.GameNameAliasesList == null || _settingsRef.GameNameAliasesList.Count == 0))
                    {
                        _settingsRef.GameNameAliases = GameNameAliases.GetDefaultPokemonAliases() ?? new Dictionary<string, string>();
                    }

                    _settingsRef.SyncAliasesListFromDictionary();
                }
            }
            catch { }
        }

        #region Tag

        private void ButtonAddTag_Click(object sender, RoutedEventArgs e)
        {
			var commandsPlugin = new CommandsPlugin(PluginDatabase.PluginName, PluginDatabase);
            commandsPlugin.CmdAddTag.Execute(null);
		}

        private void ButtonRemoveTag_Click(object sender, RoutedEventArgs e)
        {
			var commandsPlugin = new CommandsPlugin(PluginDatabase.PluginName, PluginDatabase);
			commandsPlugin.CmdRemoveTag.Execute(null);
		}

        #endregion

        #region Export

        private void ButtonBrowseExportFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selected = API.Instance.Dialogs.SelectFolder();
                if (!selected.IsNullOrEmpty())
                {
                    CreateDataExportSection();
                    dataExportSection.PART_ExportFolder.Text = selected;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private void ButtonExportCsvComma_Click(object sender, RoutedEventArgs e)
        {
            ExportCsv(',');
        }

        private void ButtonExportCsvSemicolon_Click(object sender, RoutedEventArgs e)
        {
            ExportCsv(';');
        }

        private void ExportCsv(char delimiter)
        {
            try
            {
                CreateDataExportSection();
                var folder = dataExportSection.PART_ExportFolder.Text?.Trim();
                if (folder.IsNullOrEmpty())
                {
                    API.Instance.Dialogs.ShowMessage(ResourceProvider.GetString("LOCExportSelectFolderFirst"));
                    return;
                }
                if (!Directory.Exists(folder))
                {
                    API.Instance.Dialogs.ShowMessage(ResourceProvider.GetString("LOCExportFolderNotExist"));
                    return;
                }
                var path = Path.Combine(folder, $"HLTB_Export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
                var lines = new List<string>
                {
                    string.Join(delimiter.ToString(), new[] {
                    "GameId","Name","Platform","Type",
                    "Main (formatted)","Main+Extra (formatted)","Completionist (formatted)",
                    "Solo (formatted)","Co-Op (formatted)","Vs (formatted)",
                    "Developers","Publishers","Date added","Last activity"
                })
                };
                int exportedCount = 0;
                int failedCount = 0;
                foreach (var game in API.Instance.Database.Games)
                {
                    try
                    {
                        var entry = PluginDatabase.Get(game.Id, true);
                        var data = entry?.GetData()?.GameHltbData;
                        if (entry != null && data != null)
                        {
                            var name = entry.GetData()?.Name ?? game.Name;
                            var platform = entry.GetData()?.Platform ?? string.Empty;
                            var type = data.GameType.ToString();
                            var developers = game.Developers?.Select(d => d.Name)?.ToList() ?? new List<string>();
                            var publishers = game.Publishers?.Select(p => p.Name)?.ToList() ?? new List<string>();

                            string csvLine = string.Join(delimiter.ToString(),
                                new string[]
                                {
                                    game.Id.ToString(),
                                    EscapeCsvWithDelimiter(name, delimiter),
                                    EscapeCsvWithDelimiter(platform, delimiter),
                                    EscapeCsvWithDelimiter(type, delimiter),
                                    EscapeCsvWithDelimiter(data.MainStoryFormat, delimiter),
                                    EscapeCsvWithDelimiter(data.MainExtraFormat, delimiter),
                                    EscapeCsvWithDelimiter(data.CompletionistFormat, delimiter),
                                    EscapeCsvWithDelimiter(data.SoloFormat, delimiter),
                                    EscapeCsvWithDelimiter(data.CoOpFormat, delimiter),
                                    EscapeCsvWithDelimiter(data.VsFormat, delimiter),
                                    EscapeCsvWithDelimiter(string.Join(", ", developers), delimiter),
                                    EscapeCsvWithDelimiter(string.Join(", ", publishers), delimiter),
                                    EscapeCsvWithDelimiter(game.Added?.ToString("yyyy-MM-ddTHH:mm:ss"), delimiter),
                                    EscapeCsvWithDelimiter(game.LastActivity?.ToString("yyyy-MM-ddTHH:mm:ss"), delimiter)
                                });
                            lines.Add(csvLine);
                            exportedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        Common.LogError(ex, false, false, PluginDatabase.PluginName);
                    }
                }
                var utf8Bom = new System.Text.UTF8Encoding(true);
                File.WriteAllLines(path, lines, utf8Bom);
                var msg = string.Format(ResourceProvider.GetString("LOCExportedCsvMessage"), exportedCount, path, delimiter);
                if (failedCount > 0)
                {
                    msg += "\n" + string.Format(ResourceProvider.GetString("LOCExportFailedCount"), failedCount);
                }
                API.Instance.Dialogs.ShowMessage(msg);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private void ButtonExportJson_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CreateDataExportSection();
                var folder = dataExportSection.PART_ExportFolder.Text?.Trim();
                if (folder.IsNullOrEmpty())
                {
                    API.Instance.Dialogs.ShowMessage(ResourceProvider.GetString("LOCExportSelectFolderFirst"));
                    return;
                }
                if (!Directory.Exists(folder))
                {
                    API.Instance.Dialogs.ShowMessage(ResourceProvider.GetString("LOCExportFolderNotExist"));
                    return;
                }
                var path = Path.Combine(folder, $"HLTB_Export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json");
                var items = new List<object>();
                int exportedCount = 0;
                int failedCount = 0;
                foreach (var game in API.Instance.Database.Games)
                {
                    try
                    {
                        var entry = PluginDatabase.Get(game.Id, true);
                        var data = entry?.GetData()?.GameHltbData;
                        if (entry != null && data != null)
                        {
                            var developers = game.Developers?.Select(d => d.Name)?.ToList() ?? new List<string>();
                            var publishers = game.Publishers?.Select(p => p.Name)?.ToList() ?? new List<string>();

                            items.Add(new
                            {
                                GameId = game.Id,
                                Name = entry.GetData()?.Name ?? game.Name,
                                Platform = entry.GetData()?.Platform ?? string.Empty,
                                Type = data.GameType.ToString(),

                                Main = data.MainStoryClassic,
                                MainExtra = data.MainExtraClassic,
                                Completionist = data.CompletionistClassic,
                                Solo = data.SoloClassic,
                                CoOp = data.CoOpClassic,
                                Vs = data.VsClassic,

                                MainFormatted = data.MainStoryFormat,
                                MainExtraFormatted = data.MainExtraFormat,
                                CompletionistFormatted = data.CompletionistFormat,
                                SoloFormatted = data.SoloFormat,
                                CoOpFormatted = data.CoOpFormat,
                                VsFormatted = data.VsFormat,

                                Developers = developers,
                                Publishers = publishers,
                                DateAdded = game.Added,
                                LastActivity = game.LastActivity
                            });
                            exportedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        Common.LogError(ex, false, false, PluginDatabase.PluginName);
                    }
                }
                var json = Serialization.ToJson(items, true);
                File.WriteAllText(path, json);
                var msgJson = string.Format(ResourceProvider.GetString("LOCExportedJsonMessage"), exportedCount, path);
                if (failedCount > 0)
                {
                    msgJson += "\n" + string.Format(ResourceProvider.GetString("LOCExportFailedCount"), failedCount);
                }
                API.Instance.Dialogs.ShowMessage(msgJson);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private static string EscapeCsvWithDelimiter(string input, char delimiter)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            bool needsQuotes = input.Contains(delimiter.ToString()) || input.Contains("\"") || input.Contains("\n") || input.Contains("\r");
            string escaped = input.Replace("\"", "\"\"");
            return needsQuotes ? $"\"{escaped}\"" : escaped;
        }

        #endregion

        #region Database

        private void BtAddData_Click(object sender, RoutedEventArgs e)
        {
            PluginDatabase.GetSelectData();
        }

        private void BtRemoveData_Click(object sender, RoutedEventArgs e)
        {
            PluginDatabase.ClearDatabase();
        }

        #endregion

        #region ProgressBar color

        private void BtPickColor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                tbControl = ((StackPanel)((FrameworkElement)sender).Parent).Children.OfType<TextBlock>().FirstOrDefault();

                if (tbControl.Background is SolidColorBrush sBrush)
                {
                    displayProgressBarSection.PART_SelectorColorPicker.IsSimpleColor = true;

                    Color color = sBrush.Color;
                    displayProgressBarSection.PART_SelectorColorPicker.SetColors(color);
                }
                if (tbControl.Background is LinearGradientBrush lBrush)
                {
                    displayProgressBarSection.PART_SelectorColorPicker.IsSimpleColor = false;

                    LinearGradientBrush linearGradientBrush = lBrush;
                    displayProgressBarSection.PART_SelectorColorPicker.SetColors(linearGradientBrush);
                }

                displayProgressBarSection.PART_SelectorColor.Visibility = Visibility.Visible;
                displayProgressBarSection.spSettings.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private void BtRestore_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TextBlock tbControl = ((StackPanel)((FrameworkElement)sender).Parent).Children.OfType<TextBlock>().FirstOrDefault();

                switch ((string)((Button)sender).Tag)
                {
                    case "0":
                        if (ResourceProvider.GetResource("NormalBrush") is LinearGradientBrush)
                        {
                            displayProgressBarSection.tbThumb.Background = (LinearGradientBrush)ResourceProvider.GetResource("NormalBrush");
                            global::HowLongToBeat.Views.HowLongToBeatSettingsView.ThumbSolidColorBrush = null;
                            global::HowLongToBeat.Views.HowLongToBeatSettingsView.ThumbLinearGradient = ThemeLinearGradient.ToThemeLinearGradient((LinearGradientBrush)ResourceProvider.GetResource("NormalBrush"));
                        }
                        else
                        {
                            displayProgressBarSection.tbThumb.Background = (SolidColorBrush)ResourceProvider.GetResource("NormalBrush");
                            global::HowLongToBeat.Views.HowLongToBeatSettingsView.ThumbSolidColorBrush = (SolidColorBrush)ResourceProvider.GetResource("NormalBrush");
                            global::HowLongToBeat.Views.HowLongToBeatSettingsView.ThumbLinearGradient = null;
                        }

                        break;

                    case "1":
                        tbControl.Background = Brushes.DarkCyan;
                        global::HowLongToBeat.Views.HowLongToBeatSettingsView.FirstColorBrush = new SolidColorBrush(Brushes.DarkCyan.Color);
                        global::HowLongToBeat.Views.HowLongToBeatSettingsView.FirstLinearGradient = null;
                        break;

                    case "2":
                        tbControl.Background = Brushes.RoyalBlue;
                        global::HowLongToBeat.Views.HowLongToBeatSettingsView.SecondColorBrush = new SolidColorBrush(Brushes.RoyalBlue.Color);
                        global::HowLongToBeat.Views.HowLongToBeatSettingsView.SecondLinearGradient = null;
                        break;

                    case "3":
                        tbControl.Background = Brushes.ForestGreen;
                        global::HowLongToBeat.Views.HowLongToBeatSettingsView.ThirdColorBrush = new SolidColorBrush(Brushes.ForestGreen.Color);
                        global::HowLongToBeat.Views.HowLongToBeatSettingsView.ThirdLinearGradient = null;
                        break;

                    case "4":
                        tbControl.Background = Brushes.DarkCyan;
                        global::HowLongToBeat.Views.HowLongToBeatSettingsView.FirstMultiColorBrush = new SolidColorBrush(Brushes.DarkCyan.Color);
                        global::HowLongToBeat.Views.HowLongToBeatSettingsView.FirstMultiLinearGradient = null;
                        break;

                    case "5":
                        tbControl.Background = Brushes.RoyalBlue;
                        global::HowLongToBeat.Views.HowLongToBeatSettingsView.SecondMultiColorBrush = new SolidColorBrush(Brushes.RoyalBlue.Color);
                        global::HowLongToBeat.Views.HowLongToBeatSettingsView.SecondMultiLinearGradient = null;
                        break;

                    case "6":
                        tbControl.Background = Brushes.ForestGreen;
                        global::HowLongToBeat.Views.HowLongToBeatSettingsView.ThirdMultiColorBrush = new SolidColorBrush(Brushes.ForestGreen.Color);
                        global::HowLongToBeat.Views.HowLongToBeatSettingsView.ThirdMultiLinearGradient = null;
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private void PART_TM_ColorOK_Click(object sender, RoutedEventArgs e)
        {
            Color color = default;

            if (tbControl != null)
            {
                if (displayProgressBarSection.PART_SelectorColorPicker.IsSimpleColor)
                {
                    color = displayProgressBarSection.PART_SelectorColorPicker.SimpleColor;
                    tbControl.Background = new SolidColorBrush(color);

                    switch ((string)tbControl.Tag)
                    {
                        case "0":
                            global::HowLongToBeat.Views.HowLongToBeatSettingsView.ThumbSolidColorBrush = new SolidColorBrush(color);
                            global::HowLongToBeat.Views.HowLongToBeatSettingsView.ThumbLinearGradient = null;
                            break;

                        case "1":
                            global::HowLongToBeat.Views.HowLongToBeatSettingsView.FirstColorBrush = new SolidColorBrush(color);
                            global::HowLongToBeat.Views.HowLongToBeatSettingsView.FirstLinearGradient = null;
                            break;

                        case "2":
                            global::HowLongToBeat.Views.HowLongToBeatSettingsView.SecondColorBrush = new SolidColorBrush(color);
                            global::HowLongToBeat.Views.HowLongToBeatSettingsView.SecondLinearGradient = null;
                            break;

                        case "3":
                            global::HowLongToBeat.Views.HowLongToBeatSettingsView.ThirdColorBrush = new SolidColorBrush(color);
                            global::HowLongToBeat.Views.HowLongToBeatSettingsView.ThirdLinearGradient = null;
                            break;

                        case "4":
                            global::HowLongToBeat.Views.HowLongToBeatSettingsView.FirstMultiColorBrush = new SolidColorBrush(color);
                            global::HowLongToBeat.Views.HowLongToBeatSettingsView.FirstMultiLinearGradient = null;
                            break;

                        case "5":
                            global::HowLongToBeat.Views.HowLongToBeatSettingsView.SecondMultiColorBrush = new SolidColorBrush(color);
                            global::HowLongToBeat.Views.HowLongToBeatSettingsView.SecondMultiLinearGradient = null;
                            break;

                        case "6":
                            global::HowLongToBeat.Views.HowLongToBeatSettingsView.ThirdMultiColorBrush = new SolidColorBrush(color);
                            global::HowLongToBeat.Views.HowLongToBeatSettingsView.ThirdMultiLinearGradient = null;
                            break;

                        default:
                            break;
                    }
                }
                else
                {
                    tbControl.Background = displayProgressBarSection.PART_SelectorColorPicker.GetLinearGradientBrush();

                    switch ((string)tbControl.Tag)
                    {
                        case "0":
                            global::HowLongToBeat.Views.HowLongToBeatSettingsView.ThumbSolidColorBrush = null;
                            global::HowLongToBeat.Views.HowLongToBeatSettingsView.ThumbLinearGradient = ThemeLinearGradient.ToThemeLinearGradient(displayProgressBarSection.PART_SelectorColorPicker.GetLinearGradientBrush());
                            break;

                        case "1":
                            global::HowLongToBeat.Views.HowLongToBeatSettingsView.FirstColorBrush = null;
                            global::HowLongToBeat.Views.HowLongToBeatSettingsView.FirstLinearGradient = ThemeLinearGradient.ToThemeLinearGradient(displayProgressBarSection.PART_SelectorColorPicker.GetLinearGradientBrush());
                            break;

                        case "2":
                            global::HowLongToBeat.Views.HowLongToBeatSettingsView.SecondColorBrush = null;
                            global::HowLongToBeat.Views.HowLongToBeatSettingsView.SecondLinearGradient = ThemeLinearGradient.ToThemeLinearGradient(displayProgressBarSection.PART_SelectorColorPicker.GetLinearGradientBrush());
                            break;

                        case "3":
                            global::HowLongToBeat.Views.HowLongToBeatSettingsView.ThirdColorBrush = null;
                            global::HowLongToBeat.Views.HowLongToBeatSettingsView.ThirdLinearGradient = ThemeLinearGradient.ToThemeLinearGradient(displayProgressBarSection.PART_SelectorColorPicker.GetLinearGradientBrush());
                            break;

                        case "4":
                            global::HowLongToBeat.Views.HowLongToBeatSettingsView.FirstMultiColorBrush = null;
                            global::HowLongToBeat.Views.HowLongToBeatSettingsView.FirstMultiLinearGradient = ThemeLinearGradient.ToThemeLinearGradient(displayProgressBarSection.PART_SelectorColorPicker.GetLinearGradientBrush());
                            break;

                        case "5":
                            global::HowLongToBeat.Views.HowLongToBeatSettingsView.SecondMultiColorBrush = null;
                            global::HowLongToBeat.Views.HowLongToBeatSettingsView.SecondMultiLinearGradient = ThemeLinearGradient.ToThemeLinearGradient(displayProgressBarSection.PART_SelectorColorPicker.GetLinearGradientBrush());
                            break;

                        case "6":
                            global::HowLongToBeat.Views.HowLongToBeatSettingsView.ThirdMultiColorBrush = null;
                            global::HowLongToBeat.Views.HowLongToBeatSettingsView.ThirdMultiLinearGradient = ThemeLinearGradient.ToThemeLinearGradient(displayProgressBarSection.PART_SelectorColorPicker.GetLinearGradientBrush());
                            break;

                        default:
                            break;
                    }
                }
            }
            else
            {
                Logger.Warn("One control is undefined");
            }

            displayProgressBarSection.PART_SelectorColor.Visibility = Visibility.Collapsed;
            displayProgressBarSection.spSettings.Visibility = Visibility.Visible;
        }

        private void PART_TM_ColorCancel_Click(object sender, RoutedEventArgs e)
        {
            displayProgressBarSection.PART_SelectorColor.Visibility = Visibility.Collapsed;
            displayProgressBarSection.spSettings.Visibility = Visibility.Visible;
        }

        #endregion

        #region Authenticate

        private async Task CheckAuthenticateAsync()
        {
            generalSection.PART_LbUserLogin.Visibility = Visibility.Collapsed;
            generalSection.PART_LbAuthenticate.Content = ResourceProvider.GetString("LOCCommonLoginChecking");

            try { Logger.Info("HLTB Auth UI: CheckAuthenticate start"); } catch { }

            bool isLoggedIn = false;
            try
            {
                var api = PluginDatabase?.HowLongToBeatApi;
                if (api != null)
                {
                    isLoggedIn = await api.GetIsUserLoggedInAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                try { Logger.Warn(ex, "HLTB Auth UI: CheckAuthenticate failed"); } catch { }
                isLoggedIn = false;
            }

            try
            {
                await Dispatcher.InvokeAsync(() => UpdateAuthUi(isLoggedIn));
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private void UpdateAuthUi(bool isLoggedIn)
        {
            try
            {
                var api = PluginDatabase?.HowLongToBeatApi;

                // Ensure UI is updated even if the API state was already set before we subscribed.
                if (isLoggedIn || (api != null && (bool?)(api.IsConnected) == true))
                {
                    generalSection.PART_LbAuthenticate.Content = ResourceProvider.GetString("LOCCommonLoggedIn");
                    generalSection.PART_LbUserLogin.Visibility = Visibility.Visible;

                    string userLogin = api?.UserLogin;
                    if (userLogin.IsNullOrEmpty())
                    {
                        userLogin = PluginDatabase?.UserHltbData?.Login ?? string.Empty;
                    }

                    generalSection.PART_LbUserLogin.Content = ResourceProvider.GetString("LOCCommonAccountName") + " " + userLogin;
                }
                else
                {
                    generalSection.PART_LbAuthenticate.Content = ResourceProvider.GetString("LOCCommonNotLoggedIn");
                    generalSection.PART_LbUserLogin.Visibility = Visibility.Collapsed;
                }

                try { Logger.Info($"HLTB Auth UI: CheckAuthenticate done isLoggedIn={isLoggedIn} api.IsConnected={(api?.IsConnected?.ToString() ?? "<null>")}"); } catch { }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private void HowLongToBeatApi_LoginCompleted(object sender, EventArgs e)
        {
            try
            {
                Logger.Info("HLTB Auth UI: login dialog closed, refreshing auth state");
                TaskHelpers.FireAndForget(CheckAuthenticateAsync(), "SettingsView-CheckAuthenticateAfterLogin", Logger);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private void PART_BtAuthenticate_Click(object sender, RoutedEventArgs e)
        {
            generalSection.PART_LbUserLogin.Visibility = Visibility.Collapsed;

            try { Logger.Info("HLTB Auth UI: Login button clicked"); } catch { }
            try
            {
                PluginDatabase.HowLongToBeatApi.Login();
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        protected void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
                this.Dispatcher.Invoke(new Action(() =>
                {
                    var api = PluginDatabase?.HowLongToBeatApi;
                    bool isLoggedIn = api != null && (bool?)(api.IsConnected) == true;
                    UpdateAuthUi(isLoggedIn);
                }));
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }
        
        #endregion

        private void HltbSettingsView_Unloaded(object sender, RoutedEventArgs e)
        {
            DetachFromHost();
            try { this.Unloaded -= HltbSettingsView_Unloaded; } catch { }
        }

        /// <summary>
        /// Unsubscribes event handlers when the legacy view is hosted outside its own visual tree.
        /// </summary>
        private void DetachFromHost()
        {
            try
            {
                if (PluginDatabase?.HowLongToBeatApi != null)
                {
                    PluginDatabase.HowLongToBeatApi.PropertyChanged -= OnPropertyChanged;
                    PluginDatabase.HowLongToBeatApi.LoginCompleted -= HowLongToBeatApi_LoginCompleted;
                }
            }
            catch { }
        }

        private void HltB_IntegrationProgressBarShowTime_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                PluginDatabase.PluginSettings.ProgressBarShowTime = true;
            }
            catch { }
        }

        private void HltB_IntegrationProgressBarShowTime_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                PluginDatabase.PluginSettings.ProgressBarShowTime = false;
            }
            catch { }
        }

        private void HltB_ProgressBarTimeAbove_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                PluginDatabase.PluginSettings.ProgressBarShowTimeAbove = true;
                PluginDatabase.PluginSettings.ProgressBarShowTimeInterior = false;
                PluginDatabase.PluginSettings.ProgressBarShowTimeBelow = false;
            }
            catch { }
        }

        private void HltB_ProgressBarTimeInterior_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                PluginDatabase.PluginSettings.ProgressBarShowTimeAbove = false;
                PluginDatabase.PluginSettings.ProgressBarShowTimeInterior = true;
                PluginDatabase.PluginSettings.ProgressBarShowTimeBelow = false;
            }
            catch { }
        }

        private void HltB_ProgressBarTimeBelow_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                PluginDatabase.PluginSettings.ProgressBarShowTimeAbove = false;
                PluginDatabase.PluginSettings.ProgressBarShowTimeInterior = false;
                PluginDatabase.PluginSettings.ProgressBarShowTimeBelow = true;
            }
            catch { }
        }

        private void ButtonAliasAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_settingsRef == null)
                {
                    return;
                }

                _settingsRef.GameNameAliasesList.Add(new GameNameAliasEntry(string.Empty, string.Empty));
                try
                {
                    dataAliasesSection.PART_AliasesGrid?.ScrollIntoView(_settingsRef.GameNameAliasesList.LastOrDefault());
                    dataAliasesSection.PART_AliasesGrid?.Focus();
                }
                catch { }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private void ButtonAliasRemove_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_settingsRef == null)
                {
                    return;
                }

                var selected = dataAliasesSection.PART_AliasesGrid?.SelectedItems;
                if (selected == null || selected.Count == 0)
                {
                    return;
                }

                // Copy to avoid modifying collection while iterating WPF SelectedItems
                var toRemove = selected.Cast<object>()
                    .Select(o => o as GameNameAliasEntry)
                    .Where(a => a != null)
                    .ToList();

                foreach (var a in toRemove)
                {
                    _settingsRef.GameNameAliasesList.Remove(a);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private void ButtonAliasReset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_settingsRef == null)
                {
                    return;
                }

                var confirm = API.Instance.Dialogs.ShowMessage(
                    ResourceProvider.GetString("LOCHowLongToBeatAliasesResetConfirm"),
                    PluginDatabase.PluginName,
                    MessageBoxButton.YesNo);

                if (confirm != MessageBoxResult.Yes)
                {
                    return;
                }

                var defaults = GameNameAliases.GetDefaultPokemonAliases();
                _settingsRef.GameNameAliases = defaults ?? new Dictionary<string, string>();
                _settingsRef.SyncAliasesListFromDictionary();
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private void ButtonAliasImport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_settingsRef == null)
                {
                    return;
                }

                var userDataPath = PluginDatabase?.Paths?.PluginUserDataPath;
                if (string.IsNullOrEmpty(userDataPath))
                {
                    API.Instance.Dialogs.ShowMessage(ResourceProvider.GetString("LOCHowLongToBeatAliasesFilePathUnavailable"));
                    return;
                }

                if (!GameNameAliases.TryImportAliasesFromFile(userDataPath, out var aliases, out var filePath, out var error))
                {
                    if (error != null)
                    {
                        Common.LogError(error, false, true, PluginDatabase.PluginName);
                        API.Instance.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCHowLongToBeatAliasesImportFailed"), filePath ?? GameNameAliases.AliasFileName));
                    }
                    else
                    {
                        API.Instance.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCHowLongToBeatAliasesImportNotFound"), filePath ?? GameNameAliases.AliasFileName));
                    }
                    return;
                }

                var confirm = API.Instance.Dialogs.ShowMessage(
                    string.Format(ResourceProvider.GetString("LOCHowLongToBeatAliasesImportConfirm"), filePath),
                    PluginDatabase.PluginName,
                    MessageBoxButton.YesNo);

                if (confirm != MessageBoxResult.Yes)
                {
                    return;
                }

                _settingsRef.GameNameAliases = aliases ?? new Dictionary<string, string>();
                _settingsRef.SyncAliasesListFromDictionary();

                API.Instance.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCHowLongToBeatAliasesImportOk"), filePath));
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private void ButtonAliasExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_settingsRef == null)
                {
                    return;
                }

                var userDataPath = PluginDatabase?.Paths?.PluginUserDataPath;
                if (string.IsNullOrEmpty(userDataPath))
                {
                    API.Instance.Dialogs.ShowMessage(ResourceProvider.GetString("LOCHowLongToBeatAliasesFilePathUnavailable"));
                    return;
                }

                // Ensure latest edits are captured
                try { _settingsRef.SyncAliasesDictionaryFromList(); } catch { }

                if (!GameNameAliases.TryExportAliasesToFile(userDataPath, _settingsRef.GameNameAliases, out var filePath, out var error))
                {
                    if (error != null)
                    {
                        Common.LogError(error, false, true, PluginDatabase.PluginName);
                    }
                    API.Instance.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCHowLongToBeatAliasesExportFailed"), filePath ?? GameNameAliases.AliasFileName));
                    return;
                }

                API.Instance.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCHowLongToBeatAliasesExportOk"), filePath));
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private void ButtonAliasOpenFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var userDataPath = PluginDatabase?.Paths?.PluginUserDataPath;
                var file = Services.GameNameAliases.GetAliasFilePath(userDataPath);
                if (string.IsNullOrEmpty(file) || !File.Exists(file))
                {
                    API.Instance.Dialogs.ShowMessage(ResourceProvider.GetString("LOCHowLongToBeatAliasesFilePathUnavailable"));
                    return;
                }

                var psi = new System.Diagnostics.ProcessStartInfo(file) { UseShellExecute = true };
                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private void PART_AliasesGrid_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            try
            {
                // Find nearest ancestor ScrollViewer
                DependencyObject dep = (DependencyObject)sender;
                while (dep != null && !(dep is ScrollViewer))
                {
                    dep = VisualTreeHelper.GetParent(dep);
                }

                var sv = dep as ScrollViewer;
                if (sv != null)
                {
                    // Adjust scroll amount. Use a divisor so wheel feels natural.
                    double newOffset = sv.VerticalOffset - (e.Delta / 3.0);
                    newOffset = Math.Max(0, Math.Min(newOffset, sv.ScrollableHeight));
                    sv.ScrollToVerticalOffset(newOffset);
                    e.Handled = true;
                }
            }
            catch { }
        }

        private void TabIgnoreSync_GotFocus(object sender, RoutedEventArgs e)
        {
            EnsureIgnoreSyncListInitialized();
        }

        private void EnsureIgnoreSyncListInitialized()
        {
            try
            {
                if (_ignoreSyncListInitialized || PluginDatabase == null)
                {
                    return;
                }

                CreateSyncIgnoredGamesSection();
                _ignoreSyncGames = new ObservableCollection<Game>(PluginDatabase.GetGamesIgnoredForPlaytimeSync());
                syncIgnoredGamesSection.PART_IgnoreSyncList.ItemsSource = _ignoreSyncGames;
                SyncEditingIgnoreSyncGameIds();
                _ignoreSyncListInitialized = true;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase?.PluginName);
            }
        }

        private void SyncEditingIgnoreSyncGameIds()
        {
            global::HowLongToBeat.Views.HowLongToBeatSettingsView.EditingIgnoreSyncGameIds = _ignoreSyncGames?.Select(g => g.Id).ToList() ?? new List<Guid>();
        }

        private void ButtonIgnoreSyncAddGame_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureIgnoreSyncListInitialized();
                if (_ignoreSyncGames == null)
                {
                    return;
                }

                IgnoreSyncAddGamesView view = new IgnoreSyncAddGamesView(
                    PluginDatabase,
                    _ignoreSyncGames.Select(g => g.Id));
                Window window = PlayniteUiHelper.CreateExtensionWindow(
                    PluginDatabase.PluginName + " - " + ResourceProvider.GetString("LOCHowLongToBeatIgnoreSyncAddDialogTitle"),
                    view);
                _ = window.ShowDialog();

                if (!view.Confirmed)
                {
                    return;
                }

                foreach (Game game in view.GetSelectedGames())
                {
                    if (game == null || _ignoreSyncGames.Any(g => g.Id == game.Id))
                    {
                        continue;
                    }

                    _ignoreSyncGames.Add(game);
                }

                List<Game> ordered = _ignoreSyncGames.OrderBy(g => g.Name).ToList();
                _ignoreSyncGames.Clear();
                foreach (Game game in ordered)
                {
                    _ignoreSyncGames.Add(game);
                }

                SyncEditingIgnoreSyncGameIds();
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private void ButtonIgnoreSyncRemoveItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureIgnoreSyncListInitialized();
                if (_ignoreSyncGames == null || !(sender is Button button))
                {
                    return;
                }

                Game game = button.Tag as Game ?? button.DataContext as Game;
                if (game == null)
                {
                    return;
                }

                Game toRemove = _ignoreSyncGames.FirstOrDefault(g => g.Id == game.Id);
                if (toRemove != null)
                {
                    _ = _ignoreSyncGames.Remove(toRemove);
                    SyncEditingIgnoreSyncGameIds();
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private void InitializeSectionContent()
        {
            helpSection = new HltbHelpSettingsSection();
            PART_TabHelp.Content = helpSection;

            syncMasterDetail = CreateMasterDetailHost();
            PART_TabSync.Content = syncMasterDetail;
            dataMasterDetail = CreateMasterDetailHost();
            PART_TabData.Content = dataMasterDetail;
            displayMasterDetail = CreateMasterDetailHost();
            PART_TabDisplay.Content = displayMasterDetail;
            PART_TabMapping.Content = new HltbMappingSettingsSection(_settingsRef);

            ConfigureSyncNavigation();
            CreateGeneralSection();
            ConfigureDataNavigation();
            ConfigureDisplayNavigation();
        }

        private static string GetLoc(string key)
        {
            return ResourceProvider.GetString(key);
        }

        private static HltbSettingsMasterDetailControl CreateMasterDetailHost()
        {
            return new HltbSettingsMasterDetailControl
            {
                ShowSearch = false
            };
        }

        private static void ConfigureMasterDetailNavigation(
            HltbSettingsMasterDetailControl masterDetail,
            IList<HltbSettingsNavigationItem> items)
        {
            masterDetail.ItemsSource = items;
            if (items.Count > 0)
            {
                masterDetail.SelectedItem = items[0];
            }
        }

        private void ConfigureSyncNavigation()
        {
            var items = new List<HltbSettingsNavigationItem>
            {
                new HltbSettingsNavigationItem(
                    "sync-account",
                    GetLoc("LOCCommonAccountSection"),
                    viewFactory: CreateGeneralSection),
                new HltbSettingsNavigationItem(
                    "sync-playtime",
                    GetLoc("LOCHltbSettingsNavSyncPlaytime"),
                    viewFactory: () => new HltbSyncPlaytimeSettingsSection()),
                new HltbSettingsNavigationItem(
                    "sync-status",
                    GetLoc("LOCHltbSettingsNavSyncStatus"),
                    viewFactory: CreateSyncStatusSection),
                new HltbSettingsNavigationItem(
                    "sync-ignored",
                    GetLoc("LOCHowLongToBeatIgnoreSyncTab"),
                    viewFactory: CreateSyncIgnoredGamesSection),
            };

            ConfigureMasterDetailNavigation(syncMasterDetail, items);
        }

        private UserControl CreateGeneralSection()
        {
            if (generalSection != null)
            {
                return generalSection;
            }

            generalSection = new HltbGeneralSettingsSection();
            generalSection.PART_BtAuthenticate.Click += PART_BtAuthenticate_Click;
            return generalSection;
        }

        private void ConfigureDataNavigation()
        {
            var items = new List<HltbSettingsNavigationItem>
            {
                new HltbSettingsNavigationItem(
                    "data-preferences",
                    GetLoc("LOCHltbSettingsNavDataPreferences"),
                    viewFactory: () => new HltbDataPreferencesSettingsSection()),
                new HltbSettingsNavigationItem(
                    "data-database",
                    GetLoc("LOCCommonDatabase"),
                    viewFactory: CreateDataDatabaseSection),
                new HltbSettingsNavigationItem(
                    "data-export",
                    GetLoc("LOCHowLongToBeatExport"),
                    viewFactory: CreateDataExportSection),
                new HltbSettingsNavigationItem(
                    "data-tags",
                    GetLoc("LOCHltbSettingsNavDataTags"),
                    viewFactory: CreateDataTagsSection),
                new HltbSettingsNavigationItem(
                    "data-aliases",
                    GetLoc("LOCHowLongToBeatAliases"),
                    viewFactory: CreateDataAliasesSection),
            };

            ConfigureMasterDetailNavigation(dataMasterDetail, items);
        }

        private void ConfigureDisplayNavigation()
        {
            var items = new List<HltbSettingsNavigationItem>
            {
                new HltbSettingsNavigationItem(
                    "display-navigation",
                    GetLoc("LOCHltbSettingsNavDisplayNavigation"),
                    viewFactory: () => new HltbDisplayNavigationSettingsSection()),
                new HltbSettingsNavigationItem(
                    "display-controls",
                    GetLoc("LOCHltbSettingsNavDisplayControls"),
                    viewFactory: CreateDisplayProgressBarSection),
            };

            ConfigureMasterDetailNavigation(displayMasterDetail, items);
        }

        private UserControl CreateSyncStatusSection()
        {
            if (syncStatusSection != null)
            {
                return syncStatusSection;
            }

            syncStatusSection = new HltbSyncStatusSettingsSection();
            IItemCollection<CompletionStatus> gameStatus = API.Instance.Database.CompletionStatuses;
            syncStatusSection.PART_GameStatusPlaying.ItemsSource = gameStatus;
            syncStatusSection.PART_GameStatusCompleted.ItemsSource = gameStatus;
            syncStatusSection.PART_GameStatusCompletionist.ItemsSource = gameStatus;
            syncStatusSection.PART_GameStatusBacklog.ItemsSource = gameStatus;
            syncStatusSection.PART_GameStatusReplays.ItemsSource = gameStatus;
            syncStatusSection.PART_GameStatusRetired.ItemsSource = gameStatus;
            return syncStatusSection;
        }

        private UserControl CreateSyncIgnoredGamesSection()
        {
            if (syncIgnoredGamesSection != null)
            {
                return syncIgnoredGamesSection;
            }

            syncIgnoredGamesSection = new HltbSyncSettingsSection();
            syncIgnoredGamesSection.PART_BtnIgnoreSyncAddGame.Click += ButtonIgnoreSyncAddGame_Click;
            syncIgnoredGamesSection.AddHandler(Button.ClickEvent, new RoutedEventHandler(SyncSection_ButtonClick), true);
            EnsureIgnoreSyncListInitialized();
            return syncIgnoredGamesSection;
        }

        private UserControl CreateDataDatabaseSection()
        {
            if (dataDatabaseSection != null)
            {
                return dataDatabaseSection;
            }

            dataDatabaseSection = new HltbDataDatabaseSettingsSection();
            dataDatabaseSection.btAddData.Click += BtAddData_Click;
            dataDatabaseSection.btRemoveData.Click += BtRemoveData_Click;
            return dataDatabaseSection;
        }

        private UserControl CreateDataExportSection()
        {
            if (dataExportSection != null)
            {
                return dataExportSection;
            }

            dataExportSection = new HltbDataExportSettingsSection();
            dataExportSection.PART_BtnExportCsvComma.Click += ButtonExportCsvComma_Click;
            dataExportSection.PART_BtnExportCsvSemicolon.Click += ButtonExportCsvSemicolon_Click;
            dataExportSection.PART_BtnExportJson.Click += ButtonExportJson_Click;
            dataExportSection.PART_BtnBrowseExportFolder.Click += ButtonBrowseExportFolder_Click;
            return dataExportSection;
        }

        private UserControl CreateDataTagsSection()
        {
            var section = new HltbDataTagsSettingsSection();
            section.PART_BtnAddTag.Click += ButtonAddTag_Click;
            section.PART_BtnRemoveTag.Click += ButtonRemoveTag_Click;
            return section;
        }

        private UserControl CreateDataAliasesSection()
        {
            if (dataAliasesSection != null)
            {
                return dataAliasesSection;
            }

            dataAliasesSection = new HltbDataSettingsSection();
            try
            {
                dataAliasesSection.PART_AliasesGrid.PreviewMouseWheel += PART_AliasesGrid_PreviewMouseWheel;
            }
            catch
            {
            }

            dataAliasesSection.PART_BtnAliasAdd.Click += ButtonAliasAdd_Click;
            dataAliasesSection.PART_BtnAliasRemove.Click += ButtonAliasRemove_Click;
            dataAliasesSection.PART_BtnAliasImport.Click += ButtonAliasImport_Click;
            dataAliasesSection.PART_BtnAliasExport.Click += ButtonAliasExport_Click;
            dataAliasesSection.PART_BtnAliasReset.Click += ButtonAliasReset_Click;
            dataAliasesSection.PART_BtnAliasOpenFile.Click += ButtonAliasOpenFile_Click;
            return dataAliasesSection;
        }

        private UserControl CreateDisplayProgressBarSection()
        {
            if (displayProgressBarSection != null)
            {
                return displayProgressBarSection;
            }

            displayProgressBarSection = new HltbDisplayProgressBarSettingsSection();
            displayProgressBarSection.PART_TM_ColorOK.Click += PART_TM_ColorOK_Click;
            displayProgressBarSection.PART_TM_ColorCancel.Click += PART_TM_ColorCancel_Click;
            displayProgressBarSection.HltB_IntegrationProgressBarShowTime.Checked += HltB_IntegrationProgressBarShowTime_Checked;
            displayProgressBarSection.HltB_IntegrationProgressBarShowTime.Unchecked += HltB_IntegrationProgressBarShowTime_Unchecked;

            foreach (Button button in EnumerateButtons(displayProgressBarSection))
            {
                string content = button.Content as string;
                if (content == "\u270f")
                {
                    button.Click += BtPickColor_Click;
                }
                else if (button.Tag is string)
                {
                    button.Click += BtRestore_Click;
                }
            }

            WireProgressBarTimeRadioButtons(displayProgressBarSection);
            return displayProgressBarSection;
        }

        private void InitializeDisplayProgressBarColors(HowLongToBeatSettings settings)
        {
            var section = (HltbDisplayProgressBarSettingsSection)CreateDisplayProgressBarSection();
            section.PART_SelectorColorPicker.OnlySimpleColor = false;

            global::HowLongToBeat.Views.HowLongToBeatSettingsView.ThumbSolidColorBrush = settings.ThumbSolidColorBrush;
            global::HowLongToBeat.Views.HowLongToBeatSettingsView.ThumbLinearGradient = settings.ThumbLinearGradient;
            section.tbThumb.Background = global::HowLongToBeat.Views.HowLongToBeatSettingsView.ThumbLinearGradient?.ToLinearGradientBrush == null ? global::HowLongToBeat.Views.HowLongToBeatSettingsView.ThumbSolidColorBrush : (Brush)global::HowLongToBeat.Views.HowLongToBeatSettingsView.ThumbLinearGradient.ToLinearGradientBrush;

            global::HowLongToBeat.Views.HowLongToBeatSettingsView.FirstColorBrush = settings.FirstColorBrush;
            global::HowLongToBeat.Views.HowLongToBeatSettingsView.FirstLinearGradient = settings.FirstLinearGradient;
            section.tbColorFirst.Background = global::HowLongToBeat.Views.HowLongToBeatSettingsView.FirstLinearGradient?.ToLinearGradientBrush == null ? global::HowLongToBeat.Views.HowLongToBeatSettingsView.FirstColorBrush : (Brush)global::HowLongToBeat.Views.HowLongToBeatSettingsView.FirstLinearGradient.ToLinearGradientBrush;

            global::HowLongToBeat.Views.HowLongToBeatSettingsView.SecondColorBrush = settings.SecondColorBrush;
            global::HowLongToBeat.Views.HowLongToBeatSettingsView.SecondLinearGradient = settings.SecondLinearGradient;
            section.tbColorSecond.Background = global::HowLongToBeat.Views.HowLongToBeatSettingsView.SecondLinearGradient?.ToLinearGradientBrush == null ? global::HowLongToBeat.Views.HowLongToBeatSettingsView.SecondColorBrush : (Brush)global::HowLongToBeat.Views.HowLongToBeatSettingsView.SecondLinearGradient.ToLinearGradientBrush;

            global::HowLongToBeat.Views.HowLongToBeatSettingsView.ThirdColorBrush = settings.ThirdColorBrush;
            global::HowLongToBeat.Views.HowLongToBeatSettingsView.ThirdLinearGradient = settings.ThirdLinearGradient;
            section.tbColorThird.Background = global::HowLongToBeat.Views.HowLongToBeatSettingsView.ThirdLinearGradient?.ToLinearGradientBrush == null ? global::HowLongToBeat.Views.HowLongToBeatSettingsView.ThirdColorBrush : (Brush)global::HowLongToBeat.Views.HowLongToBeatSettingsView.ThirdLinearGradient.ToLinearGradientBrush;

            global::HowLongToBeat.Views.HowLongToBeatSettingsView.FirstMultiColorBrush = settings.FirstMultiColorBrush;
            global::HowLongToBeat.Views.HowLongToBeatSettingsView.FirstMultiLinearGradient = settings.FirstMultiLinearGradient;
            section.tbColorFirstMulti.Background = global::HowLongToBeat.Views.HowLongToBeatSettingsView.FirstMultiLinearGradient?.ToLinearGradientBrush == null ? global::HowLongToBeat.Views.HowLongToBeatSettingsView.FirstMultiColorBrush : (Brush)global::HowLongToBeat.Views.HowLongToBeatSettingsView.FirstMultiLinearGradient.ToLinearGradientBrush;

            global::HowLongToBeat.Views.HowLongToBeatSettingsView.SecondMultiColorBrush = settings.SecondMultiColorBrush;
            global::HowLongToBeat.Views.HowLongToBeatSettingsView.SecondMultiLinearGradient = settings.SecondMultiLinearGradient;
            section.tbColorSecondMulti.Background = global::HowLongToBeat.Views.HowLongToBeatSettingsView.SecondMultiLinearGradient?.ToLinearGradientBrush == null ? global::HowLongToBeat.Views.HowLongToBeatSettingsView.SecondMultiColorBrush : (Brush)global::HowLongToBeat.Views.HowLongToBeatSettingsView.SecondMultiLinearGradient.ToLinearGradientBrush;

            global::HowLongToBeat.Views.HowLongToBeatSettingsView.ThirdMultiColorBrush = settings.ThirdMultiColorBrush;
            global::HowLongToBeat.Views.HowLongToBeatSettingsView.ThirdMultiLinearGradient = settings.ThirdMultiLinearGradient;
            section.tbColorThirdMulti.Background = global::HowLongToBeat.Views.HowLongToBeatSettingsView.ThirdMultiLinearGradient?.ToLinearGradientBrush == null ? global::HowLongToBeat.Views.HowLongToBeatSettingsView.ThirdMultiColorBrush : (Brush)global::HowLongToBeat.Views.HowLongToBeatSettingsView.ThirdMultiLinearGradient.ToLinearGradientBrush;

            section.spSettings.Visibility = Visibility.Visible;
        }

        private void SyncSection_ButtonClick(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is Button button && button.Name == "PART_RemoveButton")
            {
                ButtonIgnoreSyncRemoveItem_Click(button, e);
            }
        }

        private static IEnumerable<Button> EnumerateButtons(DependencyObject root)
        {
            if (root == null)
            {
                yield break;
            }

            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                if (child is Button button)
                {
                    yield return button;
                }

                foreach (Button nested in EnumerateButtons(child))
                {
                    yield return nested;
                }
            }
        }

        private static IEnumerable<RadioButton> EnumerateRadioButtons(DependencyObject root)
        {
            if (root == null)
            {
                yield break;
            }

            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                if (child is RadioButton radioButton)
                {
                    yield return radioButton;
                }

                foreach (RadioButton nested in EnumerateRadioButtons(child))
                {
                    yield return nested;
                }
            }
        }

        private void WireProgressBarTimeRadioButtons(DependencyObject root)
        {
            foreach (RadioButton radioButton in EnumerateRadioButtons(root))
            {
                if (radioButton.GroupName != "ProgressBarTimePlacement")
                {
                    continue;
                }

                Binding binding = System.Windows.Data.BindingOperations.GetBinding(radioButton, ToggleButton.IsCheckedProperty);
                if (binding == null)
                {
                    continue;
                }

                string path = binding.Path?.Path;
                if (path == "Settings.ProgressBarShowTimeAbove")
                {
                    radioButton.Checked += HltB_ProgressBarTimeAbove_Checked;
                }
                else if (path == "Settings.ProgressBarShowTimeInterior")
                {
                    radioButton.Checked += HltB_ProgressBarTimeInterior_Checked;
                }
                else if (path == "Settings.ProgressBarShowTimeBelow")
                {
                    radioButton.Checked += HltB_ProgressBarTimeBelow_Checked;
                }
            }
        }
    }
}


