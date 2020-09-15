using HowLongToBeat.Models;
using HowLongToBeat.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginCommon;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace HowLongToBeat.Views
{
    public partial class HowLongToBeatSettingsView : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

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

        private void ButtonAddTag_Click(object sender, RoutedEventArgs e)
        {
            tbDataLoad.Text = resources.GetString("LOCHowLongToBeatProgressBarTag");
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
                    HowLongToBeatData.AddAllTag(PlayniteApi, game, PluginUserDataPath);

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

        private void ButtonRemoveTag_Click(object sender, RoutedEventArgs e)
        {
            tbDataLoad.Text = resources.GetString("LOCHowLongToBeatProgressBarTag");
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
                    HowLongToBeatData.RemoveAllTag(PlayniteApi, game);

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

        private void BtAddData_Click(object sender, RoutedEventArgs e)
        {
            bool AutoAccept = (bool)cbAutoAccept.IsChecked;
            bool ShowWhenMismatch = (bool)cbShowWhenMismatch.IsChecked;
            bool EnableTag = (bool)cbEnableTag.IsChecked;

            pbDataLoad.IsIndeterminate = false;
            pbDataLoad.Minimum = 0;
            pbDataLoad.Value = 0;
            pbDataLoad.Maximum = PlayniteApi.Database.Games.Count;

            DataLoad.Visibility = Visibility.Visible;
            spSettings.Visibility = Visibility.Hidden;

            tokenSource = new CancellationTokenSource();
            ct = tokenSource.Token;

            int TotalAdded = 0;
            int TotalAlready = 0;
            int TotalMultiFind = 0;
            int TotlaNotFind = 0;

            var taskSystem = Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                foreach (Game game in PlayniteApi.Database.Games)
                {
                    try
                    {
                        if (!HowLongToBeatData.HaveData(game.Id, PluginUserDataPath))
                        {
                            List<HltbData> dataSearch = new HowLongToBeatClient().Search(game.Name);

                            if (dataSearch.Count == 1 && AutoAccept)
                            {
                                HowLongToBeatData.SaveData(game.Id, dataSearch[0], PluginUserDataPath);

                                if (EnableTag)
                                {
                                    HowLongToBeatData.AddAllTag(PlayniteApi, game, PluginUserDataPath);
                                }

                                TotalAdded += 1;
                            }
                            else
                            {
                                TotalMultiFind += 1;
                                if (dataSearch.Count > 0 && ShowWhenMismatch)
                                {
                                    Application.Current.Dispatcher.Invoke(new Action(() =>
                                    {
                                        string FileGameData = PluginUserDataPath + "\\howlongtobeat\\" + game.Id.ToString() + ".json";
                                        new HowLongToBeatSelect(dataSearch, FileGameData, game.Name).ShowDialog();

                                        if (EnableTag)
                                        {
                                            HowLongToBeatData.AddAllTag(PlayniteApi, game, PluginUserDataPath);
                                        }
                                    }));
                                }
                                else
                                {
                                    TotlaNotFind += 1;
                                }
                            }
                        }
                        else
                        {
                            TotalAlready += 1;
                            logger.Debug($"HowLongToBeat - {game.Name}");
                        }
                        Application.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            tbDataLoad.Text = string.Format(resources.GetString("LOCHowLongToBeatProgressBar"), TotalAdded, TotalAlready, TotlaNotFind);
                            pbDataLoad.Value += 1;
                        }));
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, "HowLongToBeat", $"Error on BtAddData_Click()");
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

        private void BtRemoveData_Click(object sender, RoutedEventArgs e)
        {
            tbDataLoad.Text = resources.GetString("LOCHowLongToBeatProgressBarTag");
            pbDataLoad.IsIndeterminate = true;

            DataLoad.Visibility = Visibility.Visible;
            spSettings.Visibility = Visibility.Hidden;

            tokenSource = new CancellationTokenSource();
            ct = tokenSource.Token;

            var taskSystem = Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                HowLongToBeatData.ClearAllData(PluginUserDataPath, PlayniteApi);

                foreach (Game game in PlayniteApi.Database.Games)
                {
                    HowLongToBeatData.RemoveAllTag(PlayniteApi, game);

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