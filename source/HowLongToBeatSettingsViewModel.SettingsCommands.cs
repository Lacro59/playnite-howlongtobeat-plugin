using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using HowLongToBeat.Models;
using HowLongToBeat.Services;
using Playnite.SDK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace HowLongToBeat
{
    /// <summary>
    /// Settings view-model commands and auth UI state (export, aliases, login).
    /// </summary>
    public partial class HowLongToBeatSettingsViewModel
    {
        private static HowLongToBeatDatabase PluginDatabase => HowLongToBeat.PluginDatabase;

        private string _authStatusText = string.Empty;
        private string _userLoginText = string.Empty;
        private bool _isUserLoginVisible;
        private string _exportFolder = string.Empty;
        private GameNameAliasEntry _selectedAliasEntry;
        private bool _authEventsSubscribed;

        /// <summary>Status label next to the authenticate button.</summary>
        public string AuthStatusText
        {
            get => _authStatusText;
            set => SetValue(ref _authStatusText, value);
        }

        /// <summary>Displayed account name when logged in.</summary>
        public string UserLoginText
        {
            get => _userLoginText;
            set => SetValue(ref _userLoginText, value);
        }

        /// <summary>Whether the account name should be visible.</summary>
        public bool IsUserLoginVisible
        {
            get => _isUserLoginVisible;
            set => SetValue(ref _isUserLoginVisible, value);
        }

        /// <summary>Folder path used by settings library export.</summary>
        public string ExportFolder
        {
            get => _exportFolder;
            set => SetValue(ref _exportFolder, value);
        }

        /// <summary>Currently selected alias row (scroll / focus target after add).</summary>
        public GameNameAliasEntry SelectedAliasEntry
        {
            get => _selectedAliasEntry;
            set => SetValue(ref _selectedAliasEntry, value);
        }

        /// <summary>Opens the HLTB login dialog.</summary>
        public RelayCommand AuthenticateCommand { get; private set; }

        /// <summary>Picks the export destination folder.</summary>
        public RelayCommand BrowseExportFolderCommand { get; private set; }

        /// <summary>Exports library times as CSV with comma delimiter.</summary>
        public RelayCommand ExportCsvCommaCommand { get; private set; }

        /// <summary>Exports library times as CSV with semicolon delimiter.</summary>
        public RelayCommand ExportCsvSemicolonCommand { get; private set; }

        /// <summary>Exports library times as JSON.</summary>
        public RelayCommand ExportJsonCommand { get; private set; }

        /// <summary>Adds an empty alias row.</summary>
        public RelayCommand AliasAddCommand { get; private set; }

        /// <summary>Removes selected alias rows. Parameter: <see cref="IList"/> of selected items.</summary>
        public RelayCommand<object> AliasRemoveCommand { get; private set; }

        /// <summary>Resets aliases to the built-in Pokémon defaults.</summary>
        public RelayCommand AliasResetCommand { get; private set; }

        /// <summary>Imports aliases from the plugin user-data file.</summary>
        public RelayCommand AliasImportCommand { get; private set; }

        /// <summary>Exports aliases to the plugin user-data file.</summary>
        public RelayCommand AliasExportCommand { get; private set; }

        /// <summary>Opens the aliases JSON file in the default editor.</summary>
        public RelayCommand AliasOpenFileCommand { get; private set; }

        private void InitializeSettingsCommands()
        {
            AuthenticateCommand = new RelayCommand(ExecuteAuthenticate);
            BrowseExportFolderCommand = new RelayCommand(ExecuteBrowseExportFolder);
            ExportCsvCommaCommand = new RelayCommand(() => HltbSettingsLibraryExport.TryExportCsv(ExportFolder?.Trim(), ','));
            ExportCsvSemicolonCommand = new RelayCommand(() => HltbSettingsLibraryExport.TryExportCsv(ExportFolder?.Trim(), ';'));
            ExportJsonCommand = new RelayCommand(() => HltbSettingsLibraryExport.TryExportJson(ExportFolder?.Trim()));
            AliasAddCommand = new RelayCommand(ExecuteAliasAdd);
            AliasRemoveCommand = new RelayCommand<object>(ExecuteAliasRemove);
            AliasResetCommand = new RelayCommand(ExecuteAliasReset);
            AliasImportCommand = new RelayCommand(ExecuteAliasImport);
            AliasExportCommand = new RelayCommand(ExecuteAliasExport);
            AliasOpenFileCommand = new RelayCommand(ExecuteAliasOpenFile);
        }

        private void SubscribeAuthEvents()
        {
            if (_authEventsSubscribed)
            {
                return;
            }

            try
            {
                HowLongToBeatApi api = PluginDatabase?.HowLongToBeatApi;
                if (api == null)
                {
                    return;
                }

                api.LoginCompleted += HowLongToBeatApi_LoginCompleted;
                api.PropertyChanged += HowLongToBeatApi_PropertyChanged;
                _authEventsSubscribed = true;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase?.PluginName);
            }
        }

        private void HowLongToBeatApi_LoginCompleted(object sender, EventArgs e)
        {
            try
            {
                Logger.Info("HLTB Auth UI: login dialog closed, refreshing auth state");
                TaskHelpers.FireAndForget(RefreshAuthStateAsync(), "SettingsViewModel-CheckAuthenticateAfterLogin", Logger);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase?.PluginName);
            }
        }

        private void HowLongToBeatApi_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    HowLongToBeatApi api = PluginDatabase?.HowLongToBeatApi;
                    bool isLoggedIn = api != null && api.IsConnected == true;
                    UpdateAuthUi(isLoggedIn);
                });
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase?.PluginName);
            }
        }

        /// <summary>
        /// Refreshes login status labels asynchronously (safe to call from UI or background).
        /// </summary>
        public async Task RefreshAuthStateAsync()
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                IsUserLoginVisible = false;
                AuthStatusText = ResourceProvider.GetString("LOCCommonLoginChecking");
            });

            try
            {
                Logger.Info("HLTB Auth UI: CheckAuthenticate start");
            }
            catch
            {
            }

            bool isLoggedIn = false;
            try
            {
                HowLongToBeatApi api = PluginDatabase?.HowLongToBeatApi;
                if (api != null)
                {
                    isLoggedIn = await api.GetIsUserLoggedInAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                try
                {
                    Logger.Warn(ex, "HLTB Auth UI: CheckAuthenticate failed");
                }
                catch
                {
                }

                isLoggedIn = false;
            }

            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() => UpdateAuthUi(isLoggedIn));
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase?.PluginName);
            }
        }

        private void UpdateAuthUi(bool isLoggedIn)
        {
            try
            {
                HowLongToBeatApi api = PluginDatabase?.HowLongToBeatApi;
                if (isLoggedIn || (api != null && api.IsConnected == true))
                {
                    AuthStatusText = ResourceProvider.GetString("LOCCommonLoggedIn");
                    IsUserLoginVisible = true;

                    string userLogin = api?.UserLogin;
                    if (userLogin.IsNullOrEmpty())
                    {
                        userLogin = PluginDatabase?.UserHltbData?.Login ?? string.Empty;
                    }

                    UserLoginText = ResourceProvider.GetString("LOCCommonAccountName") + " " + userLogin;
                }
                else
                {
                    AuthStatusText = ResourceProvider.GetString("LOCCommonNotLoggedIn");
                    IsUserLoginVisible = false;
                }

                try
                {
                    Logger.Info($"HLTB Auth UI: CheckAuthenticate done isLoggedIn={isLoggedIn} api.IsConnected={(api?.IsConnected?.ToString() ?? "<null>")}");
                }
                catch
                {
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase?.PluginName);
            }
        }

        private void ExecuteAuthenticate()
        {
            IsUserLoginVisible = false;
            try
            {
                Logger.Info("HLTB Auth UI: Login button clicked");
            }
            catch
            {
            }

            try
            {
                PluginDatabase.HowLongToBeatApi.Login();
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private void ExecuteBrowseExportFolder()
        {
            try
            {
                string selected = API.Instance.Dialogs.SelectFolder();
                if (!selected.IsNullOrEmpty())
                {
                    ExportFolder = selected;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private void ExecuteAliasAdd()
        {
            try
            {
                if (Settings?.GameNameAliasesList == null)
                {
                    return;
                }

                GameNameAliasEntry entry = new GameNameAliasEntry(string.Empty, string.Empty);
                Settings.GameNameAliasesList.Add(entry);
                SelectedAliasEntry = entry;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private void ExecuteAliasRemove(object parameter)
        {
            try
            {
                if (Settings?.GameNameAliasesList == null)
                {
                    return;
                }

                IList selected = parameter as IList;
                if (selected == null || selected.Count == 0)
                {
                    return;
                }

                List<GameNameAliasEntry> toRemove = selected.Cast<object>()
                    .Select(o => o as GameNameAliasEntry)
                    .Where(a => a != null)
                    .ToList();

                foreach (GameNameAliasEntry alias in toRemove)
                {
                    Settings.GameNameAliasesList.Remove(alias);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private void ExecuteAliasReset()
        {
            try
            {
                if (Settings == null)
                {
                    return;
                }

                MessageBoxResult confirm = API.Instance.Dialogs.ShowMessage(
                    ResourceProvider.GetString("LOCHowLongToBeatAliasesResetConfirm"),
                    PluginDatabase.PluginName,
                    MessageBoxButton.YesNo);

                if (confirm != MessageBoxResult.Yes)
                {
                    return;
                }

                Dictionary<string, string> defaults = GameNameAliases.GetDefaultPokemonAliases();
                Settings.GameNameAliases = defaults ?? new Dictionary<string, string>();
                Settings.SyncAliasesListFromDictionary();
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private void ExecuteAliasImport()
        {
            try
            {
                if (Settings == null)
                {
                    return;
                }

                string userDataPath = PluginDatabase?.Paths?.PluginUserDataPath;
                if (string.IsNullOrEmpty(userDataPath))
                {
                    API.Instance.Dialogs.ShowMessage(ResourceProvider.GetString("LOCHowLongToBeatAliasesFilePathUnavailable"));
                    return;
                }

                if (!GameNameAliases.TryImportAliasesFromFile(userDataPath, out Dictionary<string, string> aliases, out string filePath, out Exception error))
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

                MessageBoxResult confirm = API.Instance.Dialogs.ShowMessage(
                    string.Format(ResourceProvider.GetString("LOCHowLongToBeatAliasesImportConfirm"), filePath),
                    PluginDatabase.PluginName,
                    MessageBoxButton.YesNo);

                if (confirm != MessageBoxResult.Yes)
                {
                    return;
                }

                Settings.GameNameAliases = aliases ?? new Dictionary<string, string>();
                Settings.SyncAliasesListFromDictionary();
                API.Instance.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCHowLongToBeatAliasesImportOk"), filePath));
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private void ExecuteAliasExport()
        {
            try
            {
                if (Settings == null)
                {
                    return;
                }

                string userDataPath = PluginDatabase?.Paths?.PluginUserDataPath;
                if (string.IsNullOrEmpty(userDataPath))
                {
                    API.Instance.Dialogs.ShowMessage(ResourceProvider.GetString("LOCHowLongToBeatAliasesFilePathUnavailable"));
                    return;
                }

                try
                {
                    Settings.SyncAliasesDictionaryFromList();
                }
                catch
                {
                }

                if (!GameNameAliases.TryExportAliasesToFile(userDataPath, Settings.GameNameAliases, out string filePath, out Exception error))
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

        private void ExecuteAliasOpenFile()
        {
            try
            {
                string userDataPath = PluginDatabase?.Paths?.PluginUserDataPath;
                string file = GameNameAliases.GetAliasFilePath(userDataPath);
                if (string.IsNullOrEmpty(file) || !File.Exists(file))
                {
                    API.Instance.Dialogs.ShowMessage(ResourceProvider.GetString("LOCHowLongToBeatAliasesFilePathUnavailable"));
                    return;
                }

                Process.Start(new ProcessStartInfo(file) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }
    }
}
