using Playnite.SDK;
using Playnite.SDK.Models;
using System.Windows.Controls;

namespace HowLongToBeat.Views
{
    /// <summary>
    /// Settings section mapping Playnite completion statuses to HLTB sync statuses.
    /// HowLongToBeat→Playnite uses 1:1 ComboBoxes; Playnite→HowLongToBeat uses multi-select checklists on the view model.
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

            PART_FromGameStatusPlaying.ItemsSource = gameStatus;
            PART_FromGameStatusCompleted.ItemsSource = gameStatus;
            PART_FromGameStatusCompletionist.ItemsSource = gameStatus;
            PART_FromGameStatusBacklog.ItemsSource = gameStatus;
            PART_FromGameStatusReplays.ItemsSource = gameStatus;
            PART_FromGameStatusRetired.ItemsSource = gameStatus;
        }
    }
}
