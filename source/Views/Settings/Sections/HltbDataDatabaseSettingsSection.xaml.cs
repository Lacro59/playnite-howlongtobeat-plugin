using HowLongToBeat.Services;
using System.Windows;
using System.Windows.Controls;

namespace HowLongToBeat.Views
{
    /// <summary>
    /// Settings section for shared data options: default provider, matching, and local database actions.
    /// </summary>
    public partial class HltbDataDatabaseSettingsSection : UserControl
    {
        private static HowLongToBeatDatabase PluginDatabase => HowLongToBeat.PluginDatabase;

        /// <summary>
        /// Initializes a new instance of the <see cref="HltbDataDatabaseSettingsSection"/> class.
        /// </summary>
        public HltbDataDatabaseSettingsSection()
        {
            InitializeComponent();
            btAddData.Click += BtAddData_Click;
            btRemoveData.Click += BtRemoveData_Click;
        }

        private void BtAddData_Click(object sender, RoutedEventArgs e)
        {
            PluginDatabase.GetSelectData();
        }

        private void BtRemoveData_Click(object sender, RoutedEventArgs e)
        {
            PluginDatabase.ClearDatabase();
        }
    }
}
