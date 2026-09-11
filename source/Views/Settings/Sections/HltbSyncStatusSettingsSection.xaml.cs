using Playnite.SDK;
using Playnite.SDK.Models;
using System.Windows.Controls;

namespace HowLongToBeat.Views
{
    /// <summary>
    /// Settings section mapping Playnite completion statuses to HLTB sync statuses.
    /// </summary>
    public partial class HltbSyncStatusSettingsSection : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HltbSyncStatusSettingsSection"/> class.
        /// </summary>
        public HltbSyncStatusSettingsSection()
        {
            InitializeComponent();
            IItemCollection<CompletionStatus> gameStatus = API.Instance.Database.CompletionStatuses;
            PART_GameStatusPlaying.ItemsSource = gameStatus;
            PART_GameStatusCompleted.ItemsSource = gameStatus;
            PART_GameStatusCompletionist.ItemsSource = gameStatus;
            PART_GameStatusBacklog.ItemsSource = gameStatus;
            PART_GameStatusReplays.ItemsSource = gameStatus;
            PART_GameStatusRetired.ItemsSource = gameStatus;
        }
    }
}
