using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace HowLongToBeat.Views
{
    /// <summary>
    /// Root HLTB settings host: tab shell and master-detail navigation only.
    /// Section-specific UI logic lives in the corresponding Settings section controls.
    /// </summary>
    public partial class HltbSettingsView : UserControl
    {
        private readonly HowLongToBeatSettingsViewModel _viewModel;
        private readonly HowLongToBeatSettings _settingsRef;

        private HltbGeneralSettingsSection _generalSection;
        private HltbSyncSettingsSection _syncIgnoredGamesSection;
        private HltbSyncStatusSettingsSection _syncStatusSection;
        private HltbDataSettingsSection _dataAliasesSection;
        private HltbDataExportSettingsSection _dataExportSection;
        private HltbDataDatabaseSettingsSection _dataDatabaseSection;
        private HltbDisplayProgressBarSettingsSection _displayProgressBarSection;
        private HltbHelpSettingsSection _helpSection;
        private HltbSettingsMasterDetailControl _syncMasterDetail;
        private HltbSettingsMasterDetailControl _dataMasterDetail;
        private HltbSettingsMasterDetailControl _displayMasterDetail;

        /// <summary>
        /// Initializes a new instance of the <see cref="HltbSettingsView"/> class.
        /// </summary>
        /// <param name="viewModel">Settings view model provided by the plugin.</param>
        public HltbSettingsView(HowLongToBeatSettingsViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _settingsRef = viewModel.Settings;
            DataContext = viewModel;

            InitializeComponent();
            InitializeSectionContent();

            Unloaded += HltbSettingsView_Unloaded;
        }

        private void HltbSettingsView_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _dataAliasesSection?.Detach();
            }
            catch
            {
            }

            try
            {
                Unloaded -= HltbSettingsView_Unloaded;
            }
            catch
            {
            }
        }

        /// <summary>
        /// Ensures ignore-sync pending edits are tracked when the Sync tab is visited
        /// (same timing as before: Sync tab focus, not only Ignored games nav item).
        /// </summary>
        private void TabIgnoreSync_GotFocus(object sender, RoutedEventArgs e)
        {
            CreateSyncIgnoredGamesSection();
        }

        private void InitializeSectionContent()
        {
            _helpSection = new HltbHelpSettingsSection();
            PART_TabHelp.Content = _helpSection;

            _syncMasterDetail = CreateMasterDetailHost();
            PART_TabSync.Content = _syncMasterDetail;
            _dataMasterDetail = CreateMasterDetailHost();
            PART_TabData.Content = _dataMasterDetail;
            _displayMasterDetail = CreateMasterDetailHost();
            PART_TabDisplay.Content = _displayMasterDetail;
            PART_TabMapping.Content = new HltbMappingSettingsSection(_settingsRef);

            ConfigureSyncNavigation();
            CreateGeneralSection();
            ConfigureDataNavigation();
            ConfigureDisplayNavigation();

            // Eager init: aliases hydrated for EndEdit; progress colors match editing settings.
            CreateDataAliasesSection();
            CreateDisplayProgressBarSection();
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

            ConfigureMasterDetailNavigation(_syncMasterDetail, items);
        }

        private UserControl CreateGeneralSection()
        {
            if (_generalSection != null)
            {
                return _generalSection;
            }

            _generalSection = new HltbGeneralSettingsSection();
            return _generalSection;
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
                    viewFactory: () => new HltbDataTagsSettingsSection()),
                new HltbSettingsNavigationItem(
                    "data-aliases",
                    GetLoc("LOCHowLongToBeatAliases"),
                    viewFactory: CreateDataAliasesSection),
            };

            ConfigureMasterDetailNavigation(_dataMasterDetail, items);
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

            ConfigureMasterDetailNavigation(_displayMasterDetail, items);
        }

        private UserControl CreateSyncStatusSection()
        {
            if (_syncStatusSection != null)
            {
                return _syncStatusSection;
            }

            _syncStatusSection = new HltbSyncStatusSettingsSection();
            return _syncStatusSection;
        }

        private UserControl CreateSyncIgnoredGamesSection()
        {
            if (_syncIgnoredGamesSection != null)
            {
                _syncIgnoredGamesSection.EnsureInitialized();
                return _syncIgnoredGamesSection;
            }

            _syncIgnoredGamesSection = new HltbSyncSettingsSection();
            _syncIgnoredGamesSection.EnsureInitialized();
            return _syncIgnoredGamesSection;
        }

        private UserControl CreateDataDatabaseSection()
        {
            if (_dataDatabaseSection != null)
            {
                return _dataDatabaseSection;
            }

            _dataDatabaseSection = new HltbDataDatabaseSettingsSection();
            return _dataDatabaseSection;
        }

        private UserControl CreateDataExportSection()
        {
            if (_dataExportSection != null)
            {
                return _dataExportSection;
            }

            _dataExportSection = new HltbDataExportSettingsSection();
            return _dataExportSection;
        }

        private UserControl CreateDataAliasesSection()
        {
            if (_dataAliasesSection != null)
            {
                return _dataAliasesSection;
            }

            _dataAliasesSection = new HltbDataSettingsSection();
            _dataAliasesSection.Initialize(_viewModel);
            return _dataAliasesSection;
        }

        private UserControl CreateDisplayProgressBarSection()
        {
            if (_displayProgressBarSection != null)
            {
                return _displayProgressBarSection;
            }

            _displayProgressBarSection = new HltbDisplayProgressBarSettingsSection();
            _displayProgressBarSection.Initialize(_settingsRef, _viewModel);
            return _displayProgressBarSection;
        }
    }
}
