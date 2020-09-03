using HowLongToBeat.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;


namespace HowLongToBeat.Views
{
    public partial class HowLongToBeatSettingsView : UserControl
    {
        private IPlayniteAPI PlayniteApi;
        private string PluginUserDataPath;

        private CancellationTokenSource tokenSource;
        private CancellationToken ct;

        public HowLongToBeatSettingsView(IPlayniteAPI PlayniteApi, string PluginUserDataPath)
        {
            this.PlayniteApi = PlayniteApi;
            this.PluginUserDataPath = PluginUserDataPath;
            InitializeComponent();

            DataLoad.Visibility = Visibility.Collapsed;
            spSettings.Visibility = Visibility.Visible;
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

        private void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            pbDataLoad.IsIndeterminate = true;

            DataLoad.Visibility = Visibility.Visible;
            spSettings.Visibility = Visibility.Hidden;

            tokenSource = new CancellationTokenSource();
            ct = tokenSource.Token;

            var taskSystem = Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                foreach (Game game in PlayniteApi.Database.Games)
                {
                    try
                    {
                        HowLongToBeatData data = new HowLongToBeatData(game, PluginUserDataPath, PlayniteApi, true, false);
                    }
                    catch (Exception ex)
                    {
                    }

                    if (ct.IsCancellationRequested)
                    {
                        ct.ThrowIfCancellationRequested();
                    }
                }
            }, tokenSource.Token)
            .ContinueWith(antecedent =>
            {
                Application.Current.Dispatcher.Invoke(new Action(() => {
                    DataLoad.Visibility = Visibility.Collapsed;
                    spSettings.Visibility = Visibility.Visible;
                }));
            });
        }

        private void ButtonRemove_Click(object sender, RoutedEventArgs e)
        {
            pbDataLoad.IsIndeterminate = true;

            DataLoad.Visibility = Visibility.Visible;
            spSettings.Visibility = Visibility.Hidden;

            tokenSource = new CancellationTokenSource();
            ct = tokenSource.Token;

            var taskSystem = Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                foreach (Game game in PlayniteApi.Database.Games)
                {
                    try
                    {
                        HowLongToBeatData data = new HowLongToBeatData(game, PluginUserDataPath, PlayniteApi, false, false);
                    }
                    catch (Exception ex)
                    {
                    }

                    if (ct.IsCancellationRequested)
                    {
                        ct.ThrowIfCancellationRequested();
                    }
                }
            }, tokenSource.Token)
            .ContinueWith(antecedent =>
            {
                Application.Current.Dispatcher.Invoke(new Action(() => {
                    DataLoad.Visibility = Visibility.Collapsed;
                    spSettings.Visibility = Visibility.Visible;
                }));
            });
        }

        private void ButtonCancelTask_Click(object sender, RoutedEventArgs e)
        {
            tokenSource.Cancel();
        }
    }
}