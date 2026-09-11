using Playnite.SDK;
using System;
using System.Windows.Controls;

namespace HowLongToBeat.Views
{
    /// <summary>
    /// Left-navigation entry in an HLTB settings master-detail tab. Holds display metadata and
    /// creates its detail view lazily on first selection.
    /// </summary>
    public class HltbSettingsNavigationItem : System.Collections.Generic.ObservableObject
    {
        private readonly Func<UserControl> _viewFactory;
        private UserControl _view;

        /// <summary>
        /// Initializes a new instance of the <see cref="HltbSettingsNavigationItem"/> class.
        /// </summary>
        /// <param name="key">Stable item key used for redirects and search.</param>
        /// <param name="displayName">Localized label shown in the navigation list.</param>
        /// <param name="groupName">Optional group header for grouped navigation.</param>
        /// <param name="viewFactory">Factory that creates the detail view on first selection.</param>
        /// <param name="redirectKey">Optional key of another item to forward selection to.</param>
        /// <param name="subtitle">Optional secondary line or tooltip text.</param>
        /// <param name="iconGlyph">Optional Segoe MDL2 Assets glyph for the list icon.</param>
        public HltbSettingsNavigationItem(
            string key,
            string displayName,
            string groupName = null,
            Func<UserControl> viewFactory = null,
            string redirectKey = null,
            string subtitle = null,
            string iconGlyph = null)
        {
            Key = key;
            DisplayName = displayName;
            GroupName = groupName;
            _viewFactory = viewFactory;
            RedirectKey = redirectKey;
            Subtitle = subtitle;
            IconGlyph = iconGlyph;
        }

        /// <summary>Stable item key.</summary>
        public string Key { get; }

        /// <summary>Localized navigation label.</summary>
        public string DisplayName { get; }

        /// <summary>Optional navigation group name.</summary>
        public string GroupName { get; }

        /// <summary>Optional secondary line shown under the display name.</summary>
        public string Subtitle { get; }

        /// <summary>Whether <see cref="Subtitle"/> should be shown.</summary>
        public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);

        /// <summary>Optional Segoe MDL2 Assets glyph.</summary>
        public string IconGlyph { get; }

        /// <summary>Whether <see cref="IconGlyph"/> should be shown.</summary>
        public bool HasIconGlyph => !string.IsNullOrWhiteSpace(IconGlyph);

        /// <summary>Key of another item this entry forwards selection to.</summary>
        public string RedirectKey { get; }

        /// <summary>Whether this entry redirects to <see cref="RedirectKey"/>.</summary>
        public bool IsRedirect => !string.IsNullOrWhiteSpace(RedirectKey);

        /// <summary>Whether the navigation entry is enabled.</summary>
        public virtual bool IsEnabled => true;

        /// <summary>Lazily created detail view, or null until first selection.</summary>
        public UserControl View => _view;

        /// <summary>
        /// Creates the detail view on first selection. Factory failures are logged and yield null
        /// so a single broken section does not crash the settings host.
        /// </summary>
        /// <returns>The created view, or null when creation fails.</returns>
        public UserControl EnsureView()
        {
            if (IsRedirect || _view != null)
            {
                return _view;
            }

            UserControl view;
            try
            {
                view = _viewFactory?.Invoke();
            }
            catch (Exception ex)
            {
                LogManager.GetLogger()?.Error(ex, $"Failed creating the HLTB settings view for '{Key}'.");
                return null;
            }

            if (view != null)
            {
                _view = view;
                OnPropertyChanged(nameof(View));
            }

            return _view;
        }
    }
}
