using System.Windows;
using System.Windows.Controls;


namespace HowLongToBeat.Views
{
    public partial class HowLongToBeatSettingsView : UserControl
    {
        public HowLongToBeatSettingsView()
        {
            InitializeComponent();
        }

        private void Checkbox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox cb = (CheckBox)sender;

            if ((cb.Name == "HltB_IntegrationInDescription") && (bool)cb.IsChecked)
            {
                HltB_IntegrationInCustomTheme.IsChecked = false;
            }

            if ((cb.Name == "HltB_IntegrationInCustomTheme") && (bool)cb.IsChecked)
            {
                HltB_IntegrationInDescription.IsChecked = false;
            }
        }
    }
}