using CommonPluginsShared.Commands;
using HowLongToBeat.Services;
using System.Windows;
using System.Windows.Controls;

namespace HowLongToBeat.Views
{
    /// <summary>
    /// Settings section for HLTB completion tags (add / remove via plugin commands).
    /// </summary>
    public partial class HltbDataTagsSettingsSection : UserControl
    {
        private static HowLongToBeatDatabase PluginDatabase => HowLongToBeat.PluginDatabase;

        /// <summary>
        /// Initializes a new instance of the <see cref="HltbDataTagsSettingsSection"/> class.
        /// </summary>
        public HltbDataTagsSettingsSection()
        {
            InitializeComponent();
            PART_BtnAddTag.Click += ButtonAddTag_Click;
            PART_BtnRemoveTag.Click += ButtonRemoveTag_Click;
        }

        private void ButtonAddTag_Click(object sender, RoutedEventArgs e)
        {
            CommandsPlugin commandsPlugin = new CommandsPlugin(PluginDatabase.PluginName, PluginDatabase);
            commandsPlugin.CmdAddTag.Execute(null);
        }

        private void ButtonRemoveTag_Click(object sender, RoutedEventArgs e)
        {
            CommandsPlugin commandsPlugin = new CommandsPlugin(PluginDatabase.PluginName, PluginDatabase);
            commandsPlugin.CmdRemoveTag.Execute(null);
        }
    }
}
