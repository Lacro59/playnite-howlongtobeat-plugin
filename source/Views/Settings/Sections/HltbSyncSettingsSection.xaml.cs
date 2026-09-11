using CommonPluginsShared;
using HowLongToBeat.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace HowLongToBeat.Views
{
    /// <summary>
    /// Settings section for games ignored from playtime sync. Owns list editing and
    /// pending ids published to <see cref="HowLongToBeatSettingsView"/> for EndEdit.
    /// </summary>
    public partial class HltbSyncSettingsSection : UserControl
    {
        private static HowLongToBeatDatabase PluginDatabase => HowLongToBeat.PluginDatabase;

        private ObservableCollection<Game> _ignoreSyncGames;
        private bool _ignoreSyncListInitialized;
        private bool _wired;

        /// <summary>
        /// Initializes a new instance of the <see cref="HltbSyncSettingsSection"/> class.
        /// </summary>
        public HltbSyncSettingsSection()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Wires handlers and loads the ignored-games list once (idempotent).
        /// </summary>
        public void EnsureInitialized()
        {
            WireHandlers();
            EnsureIgnoreSyncListInitialized();
        }

        private void WireHandlers()
        {
            if (_wired)
            {
                return;
            }

            PART_BtnIgnoreSyncAddGame.Click += ButtonIgnoreSyncAddGame_Click;
            AddHandler(Button.ClickEvent, new RoutedEventHandler(SyncSection_ButtonClick), true);
            _wired = true;
        }

        private void EnsureIgnoreSyncListInitialized()
        {
            try
            {
                if (_ignoreSyncListInitialized || PluginDatabase == null)
                {
                    return;
                }

                _ignoreSyncGames = new ObservableCollection<Game>(PluginDatabase.GetGamesIgnoredForPlaytimeSync());
                PART_IgnoreSyncList.ItemsSource = _ignoreSyncGames;
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
            HowLongToBeatSettingsView.EditingIgnoreSyncGameIds = _ignoreSyncGames?.Select(g => g.Id).ToList() ?? new List<Guid>();
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

        private void SyncSection_ButtonClick(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is Button button && button.Name == "PART_RemoveButton")
            {
                ButtonIgnoreSyncRemoveItem_Click(button, e);
            }
        }
    }
}
