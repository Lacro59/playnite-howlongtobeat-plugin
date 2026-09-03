using CommonPluginsShared;
using HowLongToBeat.Models;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace HowLongToBeat.Views
{
    /// <summary>
    /// Self-contained mapping settings host for the Correspondences tab. Merges platform and
    /// storefront mapping under a master-detail layout (pattern similar to
    /// <c>StoresSettingsView</c>).
    /// </summary>
    public partial class HltbMappingSettingsSection : UserControl
    {
        private readonly HowLongToBeatSettings _settings;
        private readonly HltbSettingsMasterDetailControl _masterDetail;
        private HltbPlatformsSettingsSection _platformsSection;
        private HltbStorefrontSettingsSection _storefrontSection;
        private bool _platformEventsAttached;

        /// <summary>
        /// Initializes a new instance of the <see cref="HltbMappingSettingsSection"/> class.
        /// </summary>
        /// <param name="settings">Plugin settings instance bound to the settings view model.</param>
        public HltbMappingSettingsSection(HowLongToBeatSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            InitializeComponent();

            _masterDetail = new HltbSettingsMasterDetailControl
            {
                ShowSearch = false
            };
            Content = _masterDetail;

            ConfigureNavigation();
            SyncPlatforms();

            Loaded += MappingSettingsSection_Loaded;
            Unloaded += MappingSettingsSection_Unloaded;
        }

        private void MappingSettingsSection_Loaded(object sender, RoutedEventArgs e)
        {
            AttachPlatformEvents();
        }

        private void MappingSettingsSection_Unloaded(object sender, RoutedEventArgs e)
        {
            DetachPlatformEvents();
            Loaded -= MappingSettingsSection_Loaded;
            Unloaded -= MappingSettingsSection_Unloaded;
        }

        private void ConfigureNavigation()
        {
            var items = new List<HltbSettingsNavigationItem>
            {
                new HltbSettingsNavigationItem(
                    "mapping-platforms",
                    GetLoc("LOCHltbSettingsNavMappingPlatforms"),
                    viewFactory: CreatePlatformsSection,
                    subtitle: GetLoc("LOCHltbSettingsNavMappingPlatformsSubtitle")),
                new HltbSettingsNavigationItem(
                    "mapping-storefront",
                    GetLoc("LOCHowLongToBeatStorefront"),
                    viewFactory: CreateStorefrontSection,
                    subtitle: GetLoc("LOCHltbSettingsNavMappingStorefrontSubtitle")),
            };

            _masterDetail.ItemsSource = items;
            if (items.Count > 0)
            {
                _masterDetail.SelectedItem = items[0];
            }
        }

        private UserControl CreatePlatformsSection()
        {
            if (_platformsSection != null)
            {
                return _platformsSection;
            }

            _platformsSection = new HltbPlatformsSettingsSection();
            _platformsSection.PART_GridPlatformsList.ItemsSource = _settings.Platforms;
            return _platformsSection;
        }

        private UserControl CreateStorefrontSection()
        {
            if (_storefrontSection != null)
            {
                return _storefrontSection;
            }

            _storefrontSection = new HltbStorefrontSettingsSection();
            return _storefrontSection;
        }

        private void AttachPlatformEvents()
        {
            if (_platformEventsAttached)
            {
                return;
            }

            try
            {
                API.Instance.Database.Platforms.ItemCollectionChanged += Platforms_ItemCollectionChanged;
                API.Instance.Database.Platforms.ItemUpdated += Platforms_ItemUpdated;
                _platformEventsAttached = true;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, HowLongToBeat.PluginDatabase?.PluginName);
            }
        }

        private void DetachPlatformEvents()
        {
            if (!_platformEventsAttached)
            {
                return;
            }

            try
            {
                API.Instance.Database.Platforms.ItemCollectionChanged -= Platforms_ItemCollectionChanged;
            }
            catch
            {
            }

            try
            {
                API.Instance.Database.Platforms.ItemUpdated -= Platforms_ItemUpdated;
            }
            catch
            {
            }

            _platformEventsAttached = false;
        }

        private void Platforms_ItemUpdated(object sender, ItemUpdatedEventArgs<Platform> e)
        {
            try
            {
                Application.Current?.Dispatcher?.BeginInvoke(new Action(SyncPlatforms));
            }
            catch
            {
            }
        }

        private void Platforms_ItemCollectionChanged(object sender, ItemCollectionChangedEventArgs<Platform> e)
        {
            try
            {
                Application.Current?.Dispatcher?.BeginInvoke(new Action(SyncPlatforms));
            }
            catch
            {
            }
        }

        private void SyncPlatforms()
        {
            try
            {
                List<Platform> platforms = API.Instance.Database.Platforms
                    .Distinct()
                    .OrderBy(x => x.Name)
                    .ToList();

                _settings.Platforms.RemoveAll(m => !platforms.Contains(m.Platform));
                platforms.Where(p => !_settings.Platforms.Exists(m => p.Equals(m.Platform)))
                    .ToList()
                    .ForEach(p => _settings.Platforms.Add(new HltbPlatformMatch { Platform = p }));
                platforms.ForEach(p => _settings.Platforms
                    .Where(m => p.Equals(m.Platform))
                    .ToList()
                    .ForEach(m => m.Platform = p));

                _settings.Platforms.Sort();

                if (_platformsSection != null)
                {
                    _platformsSection.PART_GridPlatformsList.ItemsSource = _settings.Platforms;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, HowLongToBeat.PluginDatabase?.PluginName);
            }
        }

        private static string GetLoc(string key)
        {
            return ResourceProvider.GetString(key);
        }
    }
}
