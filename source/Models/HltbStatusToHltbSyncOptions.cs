namespace HowLongToBeat.Models
{
    /// <summary>
    /// Per HowLongToBeat target status: whether other lists are cleared when syncing to this status.
    /// </summary>
    public class HltbStatusToHltbSyncOptions
    {
        /// <summary>
        /// When true, other HowLongToBeat list flags are cleared before applying this target list.
        /// </summary>
        public bool ClearOtherLists { get; set; } = true;
    }

    /// <summary>
    /// Global HowLongToBeat list flags to preserve on the profile when clearing other lists during status sync.
    /// </summary>
    public class HltbListAlwaysKeepOptions
    {
        /// <summary>
        /// Keeps Playing when it was already set, even when syncing to another status with clear-other-lists enabled.
        /// </summary>
        public bool AlwaysKeepPlayingIfPresent { get; set; }

        /// <summary>
        /// Keeps Backlog when it was already set, even when syncing to another status with clear-other-lists enabled.
        /// </summary>
        public bool AlwaysKeepBacklogIfPresent { get; set; }

        /// <summary>
        /// Keeps Replay when it was already set, even when syncing to another status with clear-other-lists enabled.
        /// </summary>
        public bool AlwaysKeepReplayIfPresent { get; set; }

        /// <summary>
        /// Keeps Completed when it was already set, even when syncing to another status with clear-other-lists enabled.
        /// </summary>
        public bool AlwaysKeepCompletedIfPresent { get; set; }

        /// <summary>
        /// Keeps Retired when it was already set, even when syncing to another status with clear-other-lists enabled.
        /// </summary>
        public bool AlwaysKeepRetiredIfPresent { get; set; }
    }
}
