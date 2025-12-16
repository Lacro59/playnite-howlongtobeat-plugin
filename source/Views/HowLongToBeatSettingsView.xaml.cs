using Playnite.SDK;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HowLongToBeat.Services;
using CommonPluginsShared;
using HowLongToBeat.Models;
using CommonPluginsShared.Models;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;
using Playnite.SDK.Models;
using HowLongToBeat.Models.Enumerations;
using Playnite.SDK.Data;

namespace HowLongToBeat.Views
{
    public partial class HowLongToBeatSettingsView : UserControl
    {
        private static ILogger Logger => LogManager.GetLogger();

        private static HowLongToBeatDatabase PluginDatabase => HowLongToBeat.PluginDatabase;

        private TextBlock tbControl;
        private HowLongToBeatSettings _settingsRef;

        public static SolidColorBrush ThumbSolidColorBrush;
        public static ThemeLinearGradient ThumbLinearGradient;

        public static SolidColorBrush FirstColorBrush;
        public static ThemeLinearGradient FirstLinearGradient;
        public static SolidColorBrush SecondColorBrush;
        public static ThemeLinearGradient SecondLinearGradient;
        public static SolidColorBrush ThirdColorBrush;
        public static ThemeLinearGradient ThirdLinearGradient;

        public static SolidColorBrush FirstMultiColorBrush;
        public static ThemeLinearGradient FirstMultiLinearGradient;
        public static SolidColorBrush SecondMultiColorBrush;
        public static ThemeLinearGradient SecondMultiLinearGradient;
        public static SolidColorBrush ThirdMultiColorBrush;
        public static ThemeLinearGradient ThirdMultiLinearGradient;

        public HowLongToBeatSettingsView(HowLongToBeatSettings settings)
        {
            _settingsRef = settings;
            try
            {
                if (PluginDatabase?.HowLongToBeatApi != null)
                {
                    PluginDatabase.HowLongToBeatApi.PropertyChanged += OnPropertyChanged;
                }
            }
            catch { }

            InitializeComponent();

            CheckAuthenticate();
            SetPlatforms(settings);

            try
            {
                API.Instance.Database.Platforms.ItemCollectionChanged += Platforms_ItemCollectionChanged;
                API.Instance.Database.Platforms.ItemUpdated += Platforms_ItemUpdated;
                this.Unloaded += HowLongToBeatSettingsView_Unloaded;
            }
            catch { }

            PART_SelectorColorPicker.OnlySimpleColor = false;


            ThumbSolidColorBrush = settings.ThumbSolidColorBrush;
            ThumbLinearGradient = settings.ThumbLinearGradient;
            tbThumb.Background = ThumbLinearGradient?.ToLinearGradientBrush == null ? ThumbSolidColorBrush : (Brush)ThumbLinearGradient.ToLinearGradientBrush;


            FirstColorBrush = settings.FirstColorBrush;
            FirstLinearGradient = settings.FirstLinearGradient;
            tbColorFirst.Background = FirstLinearGradient?.ToLinearGradientBrush == null ? FirstColorBrush : (Brush)FirstLinearGradient.ToLinearGradientBrush;

            SecondColorBrush = settings.SecondColorBrush;
            SecondLinearGradient = settings.SecondLinearGradient;
            tbColorSecond.Background = SecondLinearGradient?.ToLinearGradientBrush == null ? SecondColorBrush : (Brush)SecondLinearGradient.ToLinearGradientBrush;

            ThirdColorBrush = settings.ThirdColorBrush;
            ThirdLinearGradient = settings.ThirdLinearGradient;
            tbColorThird.Background = ThirdLinearGradient?.ToLinearGradientBrush == null ? ThirdColorBrush : (Brush)ThirdLinearGradient.ToLinearGradientBrush;


            FirstMultiColorBrush = settings.FirstMultiColorBrush;
            FirstMultiLinearGradient = settings.FirstMultiLinearGradient;
            tbColorFirstMulti.Background = FirstMultiLinearGradient?.ToLinearGradientBrush == null ? FirstMultiColorBrush : (Brush)FirstMultiLinearGradient.ToLinearGradientBrush;

            SecondMultiColorBrush = settings.SecondMultiColorBrush;
            SecondMultiLinearGradient = settings.SecondMultiLinearGradient;
            tbColorSecondMulti.Background = SecondMultiLinearGradient?.ToLinearGradientBrush == null ? SecondMultiColorBrush : (Brush)SecondMultiLinearGradient.ToLinearGradientBrush;

            ThirdMultiColorBrush = settings.ThirdMultiColorBrush;
            ThirdMultiLinearGradient = settings.ThirdMultiLinearGradient;
            tbColorThirdMulti.Background = ThirdMultiLinearGradient?.ToLinearGradientBrush == null ? ThirdMultiColorBrush : (Brush)ThirdMultiLinearGradient.ToLinearGradientBrush;


            spSettings.Visibility = Visibility.Visible;

            PART_TTB.Source = BitmapExtensions.BitmapFromFile(Path.Combine(PluginDatabase.Paths.PluginPath, "Resources", "ttb.png"));


            IItemCollection<CompletionStatus> gameStatus = API.Instance.Database.CompletionStatuses;
            PART_GameStatusPlaying.ItemsSource = gameStatus;
            PART_GameStatusCompleted.ItemsSource = gameStatus;
            PART_GameStatusCompletionist.ItemsSource = gameStatus;
        }


        #region Tag
        private void ButtonAddTag_Click(object sender, RoutedEventArgs e)
        {
            HowLongToBeat.PluginDatabase.AddTagAllGame();
        }

        private void ButtonRemoveTag_Click(object sender, RoutedEventArgs e)
        {
            HowLongToBeat.PluginDatabase.RemoveTagAllGame();
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
                    PART_ExportFolder.Text = selected;
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
                var folder = PART_ExportFolder.Text?.Trim();
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
                var folder = PART_ExportFolder.Text?.Trim();
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

                            long? installSize = null;
                            try
                            {
                                var prop = typeof(Game).GetProperty("InstallSize");
                                if (prop != null)
                                {
                                    var value = prop.GetValue(game);
                                    if (value is long l)
                                    {
                                        installSize = l;
                                    }
                                    else if (value is long?)
                                    {
                                        installSize = (long?)value;
                                    }
                                }
                            }
                            catch { }

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
                                game.LastActivity
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


        private static string EscapeCsv(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            bool needsQuotes = input.Contains(";") || input.Contains("\"") || input.Contains("\n") || input.Contains("\r");
            string escaped = input.Replace("\"", "\"\"");
            return needsQuotes ? $"\"{escaped}\"" : escaped;
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
            HowLongToBeat.PluginDatabase.GetSelectData();
        }

        private void BtRemoveData_Click(object sender, RoutedEventArgs e)
        {
            HowLongToBeat.PluginDatabase.ClearDatabase();
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
                    PART_SelectorColorPicker.IsSimpleColor = true;

                    Color color = sBrush.Color;
                    PART_SelectorColorPicker.SetColors(color);
                }
                if (tbControl.Background is LinearGradientBrush lBrush)
                {
                    PART_SelectorColorPicker.IsSimpleColor = false;

                    LinearGradientBrush linearGradientBrush = lBrush;
                    PART_SelectorColorPicker.SetColors(linearGradientBrush);
                }

                PART_SelectorColor.Visibility = Visibility.Visible;
                spSettings.Visibility = Visibility.Collapsed;
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
                            tbThumb.Background = (LinearGradientBrush)ResourceProvider.GetResource("NormalBrush");
                            ThumbSolidColorBrush = null;
                            ThumbLinearGradient = ThemeLinearGradient.ToThemeLinearGradient((LinearGradientBrush)ResourceProvider.GetResource("NormalBrush"));
                        }
                        else
                        {
                            tbThumb.Background = (SolidColorBrush)ResourceProvider.GetResource("NormalBrush");
                            ThumbSolidColorBrush = (SolidColorBrush)ResourceProvider.GetResource("NormalBrush");
                            ThumbLinearGradient = null;
                        }

                        break;

                    case "1":
                        tbControl.Background = Brushes.DarkCyan;
                        FirstColorBrush = new SolidColorBrush(Brushes.DarkCyan.Color);
                        FirstLinearGradient = null;
                        break;

                    case "2":
                        tbControl.Background = Brushes.RoyalBlue;
                        SecondColorBrush = new SolidColorBrush(Brushes.RoyalBlue.Color);
                        SecondLinearGradient = null;
                        break;

                    case "3":
                        tbControl.Background = Brushes.ForestGreen;
                        ThirdColorBrush = new SolidColorBrush(Brushes.ForestGreen.Color);
                        ThirdLinearGradient = null;
                        break;

                    case "4":
                        tbControl.Background = Brushes.DarkCyan;
                        FirstMultiColorBrush = new SolidColorBrush(Brushes.DarkCyan.Color);
                        FirstMultiLinearGradient = null;
                        break;

                    case "5":
                        tbControl.Background = Brushes.RoyalBlue;
                        SecondMultiColorBrush = new SolidColorBrush(Brushes.RoyalBlue.Color);
                        SecondMultiLinearGradient = null;
                        break;

                    case "6":
                        tbControl.Background = Brushes.ForestGreen;
                        ThirdMultiColorBrush = new SolidColorBrush(Brushes.ForestGreen.Color);
                        ThirdMultiLinearGradient = null;
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
                if (PART_SelectorColorPicker.IsSimpleColor)
                {
                    color = PART_SelectorColorPicker.SimpleColor;
                    tbControl.Background = new SolidColorBrush(color);

                    switch ((string)tbControl.Tag)
                    {
                        case "0":
                            ThumbSolidColorBrush = new SolidColorBrush(color);
                            ThumbLinearGradient = null;
                            break;

                        case "1":
                            FirstColorBrush = new SolidColorBrush(color);
                            FirstLinearGradient = null;
                            break;

                        case "2":
                            SecondColorBrush = new SolidColorBrush(color);
                            SecondLinearGradient = null;
                            break;

                        case "3":
                            ThirdColorBrush = new SolidColorBrush(color);
                            ThirdLinearGradient = null;
                            break;

                        case "4":
                            FirstMultiColorBrush = new SolidColorBrush(color);
                            FirstMultiLinearGradient = null;
                            break;

                        case "5":
                            SecondMultiColorBrush = new SolidColorBrush(color);
                            SecondMultiLinearGradient = null;
                            break;

                        case "6":
                            ThirdMultiColorBrush = new SolidColorBrush(color);
                            ThirdMultiLinearGradient = null;
                            break;

                        default:
                            break;
                    }
                }
                else
                {
                    tbControl.Background = PART_SelectorColorPicker.GetLinearGradientBrush();

                    switch ((string)tbControl.Tag)
                    {
                        case "0":
                            ThumbSolidColorBrush = null;
                            ThumbLinearGradient = ThemeLinearGradient.ToThemeLinearGradient(PART_SelectorColorPicker.GetLinearGradientBrush());
                            break;

                        case "1":
                            FirstColorBrush = null;
                            FirstLinearGradient = ThemeLinearGradient.ToThemeLinearGradient(PART_SelectorColorPicker.GetLinearGradientBrush());
                            break;

                        case "2":
                            SecondColorBrush = null;
                            SecondLinearGradient = ThemeLinearGradient.ToThemeLinearGradient(PART_SelectorColorPicker.GetLinearGradientBrush());
                            break;

                        case "3":
                            ThirdColorBrush = null;
                            ThirdLinearGradient = ThemeLinearGradient.ToThemeLinearGradient(PART_SelectorColorPicker.GetLinearGradientBrush());
                            break;

                        case "4":
                            FirstMultiColorBrush = null;
                            FirstMultiLinearGradient = ThemeLinearGradient.ToThemeLinearGradient(PART_SelectorColorPicker.GetLinearGradientBrush());
                            break;

                        case "5":
                            SecondMultiColorBrush = null;
                            SecondMultiLinearGradient = ThemeLinearGradient.ToThemeLinearGradient(PART_SelectorColorPicker.GetLinearGradientBrush());
                            break;

                        case "6":
                            ThirdMultiColorBrush = null;
                            ThirdMultiLinearGradient = ThemeLinearGradient.ToThemeLinearGradient(PART_SelectorColorPicker.GetLinearGradientBrush());
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

            PART_SelectorColor.Visibility = Visibility.Collapsed;
            spSettings.Visibility = Visibility.Visible;
        }

        private void PART_TM_ColorCancel_Click(object sender, RoutedEventArgs e)
        {
            PART_SelectorColor.Visibility = Visibility.Collapsed;
            spSettings.Visibility = Visibility.Visible;
        }
        #endregion


        #region Authenticate
        private void CheckAuthenticate()
        {
            PART_LbUserLogin.Visibility = Visibility.Collapsed;
            PART_LbAuthenticate.Content = ResourceProvider.GetString("LOCCommonLoginChecking");

            var task = Task.Run(() => PluginDatabase.HowLongToBeatApi.GetIsUserLoggedIn());
        }

        private void PART_BtAuthenticate_Click(object sender, RoutedEventArgs e)
        {
            PART_LbUserLogin.Visibility = Visibility.Collapsed;
            var task = Task.Run(() =>
            {
                PluginDatabase.HowLongToBeatApi.Login();
            });
        }


        protected void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                try
                {
                    var api = PluginDatabase?.HowLongToBeatApi;
                    if (api != null && (bool?)(api.IsConnected) == true)
                    {
                        PART_LbAuthenticate.Content = ResourceProvider.GetString("LOCCommonLoggedIn");
                        PART_LbUserLogin.Visibility = Visibility.Visible;

                        string UserLogin = api.UserLogin;
                        if (UserLogin.IsNullOrEmpty())
                        {
                            UserLogin = PluginDatabase?.Database?.UserHltbData?.Login ?? string.Empty;
                        }

                        PART_LbUserLogin.Content = ResourceProvider.GetString("LOCCommonAccountName") + " " + UserLogin;
                    }
                    else
                    {
                        PART_LbAuthenticate.Content = ResourceProvider.GetString("LOCCommonNotLoggedIn");
                    }
                }
                catch { }
            }));
        }
        #endregion

        private void HowLongToBeatSettingsView_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                API.Instance.Database.Platforms.ItemCollectionChanged -= Platforms_ItemCollectionChanged;
            }
            catch { }
            try
            {
                API.Instance.Database.Platforms.ItemUpdated -= Platforms_ItemUpdated;
            }
            catch { }
            try
            {
                if (PluginDatabase?.HowLongToBeatApi != null)
                {
                    PluginDatabase.HowLongToBeatApi.PropertyChanged -= OnPropertyChanged;
                }
            }
            catch { }
            try { this.Unloaded -= HowLongToBeatSettingsView_Unloaded; } catch { }
        }

        private void Platforms_ItemUpdated(object sender, ItemUpdatedEventArgs<Platform> e)
        {
            try
            {
                Application.Current?.Dispatcher?.BeginInvoke(new Action(() => SetPlatforms(_settingsRef)));
            }
            catch { }
        }

        private void Platforms_ItemCollectionChanged(object sender, ItemCollectionChangedEventArgs<Platform> e)
        {
            try
            {
                Application.Current?.Dispatcher?.BeginInvoke(new Action(() => SetPlatforms(_settingsRef)));
            }
            catch { }
        }

        private void SetPlatforms(HowLongToBeatSettings settings)
        {
            List<Platform> platforms = API.Instance.Database
                    .Platforms.Distinct().OrderBy(x => x.Name).ToList();

            // Remove from settings game platforms that were deleted
            _ = settings.Platforms.RemoveAll(m => !platforms.Contains(m.Platform));
            // Add an empty match for game platforms not in the settings
            platforms.Where(p => !settings.Platforms.Exists(m => p.Equals(m.Platform)))
                    .ForEach(p => settings.Platforms.Add(new HltbPlatformMatch { Platform = p }));
            // Replace game platform in settings where GUID matches to actualize names
            platforms.ForEach(p => settings.Platforms.Where(m => p.Equals(m.Platform))
                    .ForEach(m => m.Platform = p));

            settings.Platforms.Sort();
            PART_GridPlatformsList.ItemsSource = settings.Platforms;
        }

        private void HltB_IntegrationProgressBarShowTime_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                PluginDatabase.PluginSettings.Settings.ProgressBarShowTime = true;
            }
            catch { }
        }

        private void HltB_IntegrationProgressBarShowTime_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                PluginDatabase.PluginSettings.Settings.ProgressBarShowTime = false;
            }
            catch { }
        }

        private void HltB_ProgressBarTimeAbove_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                PluginDatabase.PluginSettings.Settings.ProgressBarShowTimeAbove = true;
                PluginDatabase.PluginSettings.Settings.ProgressBarShowTimeInterior = false;
                PluginDatabase.PluginSettings.Settings.ProgressBarShowTimeBelow = false;
            }
            catch { }
        }

        private void HltB_ProgressBarTimeInterior_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                PluginDatabase.PluginSettings.Settings.ProgressBarShowTimeAbove = false;
                PluginDatabase.PluginSettings.Settings.ProgressBarShowTimeInterior = true;
                PluginDatabase.PluginSettings.Settings.ProgressBarShowTimeBelow = false;
            }
            catch { }
        }

        private void HltB_ProgressBarTimeBelow_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                PluginDatabase.PluginSettings.Settings.ProgressBarShowTimeAbove = false;
                PluginDatabase.PluginSettings.Settings.ProgressBarShowTimeInterior = false;
                PluginDatabase.PluginSettings.Settings.ProgressBarShowTimeBelow = true;
            }
            catch { }
        }

        private void CbDefaultSorting_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string index = ((ComboBoxItem)cbDefaultSorting.SelectedItem).Tag.ToString();
            switch (index)
            {
                case "0":
                    PluginDatabase.PluginSettings.Settings.TitleListSort = TitleListSort.GameName;
                    break;

                case "1":
                    PluginDatabase.PluginSettings.Settings.TitleListSort = TitleListSort.Platform;
                    break;

                case "2":
                    PluginDatabase.PluginSettings.Settings.TitleListSort = TitleListSort.Completion;
                    break;

                case "3":
                    PluginDatabase.PluginSettings.Settings.TitleListSort = TitleListSort.CurrentTime;
                    break;

                case "4":
                    PluginDatabase.PluginSettings.Settings.TitleListSort = TitleListSort.LastUpdate;
                    break;

                default:
                    break;
            }
        }
    }

}
