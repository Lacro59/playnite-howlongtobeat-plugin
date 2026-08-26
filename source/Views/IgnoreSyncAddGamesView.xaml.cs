using HowLongToBeat.Services;
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
    /// Dialog to pick library games to add to the pending ignore-sync list in plugin settings.
    /// Selection is returned to the caller; tags are applied only when settings are saved.
    /// </summary>
    public partial class IgnoreSyncAddGamesView : UserControl
    {
        private readonly HashSet<Guid> _excludedGameIds;
        private List<Game> _availableGames = new List<Game>();
        private bool _confirmed;

        /// <summary>
        /// Creates the add-games dialog, excluding games already in the pending ignore list.
        /// </summary>
        /// <param name="database">Plugin database (unused for tagging; kept for API consistency).</param>
        /// <param name="excludedGameIds">Game ids already listed as ignored in the settings editor.</param>
        public IgnoreSyncAddGamesView(HowLongToBeatDatabase database, IEnumerable<Guid> excludedGameIds)
        {
            _ = database;
            _excludedGameIds = excludedGameIds != null
                ? new HashSet<Guid>(excludedGameIds)
                : new HashSet<Guid>();

            InitializeComponent();
            PART_SearchBox.TextChanged += SearchBox_TextChanged;
            ReloadGames();
        }

        /// <summary>
        /// Gets whether the user confirmed the dialog with at least one selection.
        /// </summary>
        public bool Confirmed => _confirmed;

        /// <summary>
        /// Returns the games selected by the user.
        /// </summary>
        /// <returns>Selected games (empty when cancelled).</returns>
        public List<Game> GetSelectedGames()
        {
            if (!_confirmed)
            {
                return new List<Game>();
            }

            return PART_GamesList.SelectedItems.Cast<Game>().ToList();
        }

        private void ReloadGames()
        {
            if (API.Instance?.Database?.Games == null)
            {
                _availableGames = new List<Game>();
            }
            else
            {
                _availableGames = API.Instance.Database.Games
                    .Where(g => g != null && !g.Hidden && !_excludedGameIds.Contains(g.Id))
                    .OrderBy(g => g.Name)
                    .ToList();
            }

            ApplyFilter(PART_SearchBox?.Text);
        }

        private void ApplyFilter(string filter)
        {
            IEnumerable<Game> source = _availableGames;
            if (!string.IsNullOrWhiteSpace(filter))
            {
                string term = filter.Trim();
                source = source.Where(g => g.Name != null && g.Name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            PART_GamesList.ItemsSource = source.ToList();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter(PART_SearchBox.Text);
        }

        private void PART_BtClose_Click(object sender, RoutedEventArgs e)
        {
            _confirmed = false;
            ((Window)Parent)?.Close();
        }

        private void PART_BtAdd_Click(object sender, RoutedEventArgs e)
        {
            if (PART_GamesList.SelectedItems.Count == 0)
            {
                return;
            }

            _confirmed = true;
            ((Window)Parent)?.Close();
        }
    }
}
