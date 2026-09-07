using HowLongToBeat.Services;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Controls;

namespace HowLongToBeat.Views
{
    /// <summary>
    /// Help / QuickSearch settings section (static help content + TimeToBeat image).
    /// </summary>
    public partial class HltbHelpSettingsSection : UserControl
    {
        private static HowLongToBeatDatabase PluginDatabase => HowLongToBeat.PluginDatabase;

        /// <summary>
        /// Initializes a new instance of the <see cref="HltbHelpSettingsSection"/> class.
        /// </summary>
        public HltbHelpSettingsSection()
        {
            InitializeComponent();
            try
            {
                PART_TTB.Source = BitmapExtensions.BitmapFromFile(
                    Path.Combine(PluginDatabase.Paths.PluginPath, "Resources", "ttb.png"));
            }
            catch
            {
            }
        }
    }
}
