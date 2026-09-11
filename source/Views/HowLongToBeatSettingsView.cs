using CommonPluginsShared;
using CommonPluginsShared.Models;
using HowLongToBeat.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HowLongToBeat.Views
{
    /// <summary>
    /// Static helpers shared between settings sections and the settings view model
    /// (ignore-sync editing state only — progress colors live on <see cref="HowLongToBeatSettings"/>).
    /// </summary>
    public static class HowLongToBeatSettingsView
    {
        private static HowLongToBeatDatabase PluginDatabase => global::HowLongToBeat.HowLongToBeat.PluginDatabase;

        /// <summary>
        /// Pending ignore-sync game ids edited in the settings UI.
        /// Null when the Ignored games tab was never opened; EndEdit then leaves tags unchanged.
        /// </summary>
        public static List<Guid> EditingIgnoreSyncGameIds { get; set; }

        /// <summary>
        /// Applies pending ignore-sync list edits to Playnite tags when settings are saved.
        /// No-op when the Ignored games tab was never opened.
        /// </summary>
        public static void ApplyEditingIgnoreSyncChanges()
        {
            if (EditingIgnoreSyncGameIds == null || PluginDatabase == null)
            {
                return;
            }

            try
            {
                HashSet<Guid> pendingIds = new HashSet<Guid>(EditingIgnoreSyncGameIds);
                HashSet<Guid> currentIds = new HashSet<Guid>(
                    PluginDatabase.GetGamesIgnoredForPlaytimeSync().Select(g => g.Id));

                foreach (Guid gameId in pendingIds.Where(id => !currentIds.Contains(id)))
                {
                    Game game = API.Instance?.Database?.Games?.Get(gameId);
                    if (game != null)
                    {
                        PluginDatabase.AddIgnoreSyncTag(game);
                    }
                }

                foreach (Guid gameId in currentIds.Where(id => !pendingIds.Contains(id)))
                {
                    Game game = API.Instance?.Database?.Games?.Get(gameId);
                    if (game != null)
                    {
                        PluginDatabase.RemoveIgnoreSyncTag(game);
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
            finally
            {
                EditingIgnoreSyncGameIds = null;
            }
        }

        /// <summary>
        /// Discards pending ignore-sync list edits when settings are cancelled.
        /// </summary>
        public static void CancelEditingIgnoreSyncChanges()
        {
            EditingIgnoreSyncGameIds = null;
        }
    }
}
