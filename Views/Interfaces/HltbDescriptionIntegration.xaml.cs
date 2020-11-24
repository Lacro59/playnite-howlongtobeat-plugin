using HowLongToBeat.Models;
using HowLongToBeat.Services;
using Newtonsoft.Json;
using Playnite.SDK;
using PluginCommon;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace HowLongToBeat.Views.Interfaces
{
    /// <summary>
    /// Logique d'interaction pour HltbDescriptionIntegration.xaml
    /// </summary>
    public partial class HltbDescriptionIntegration : StackPanel
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private HowLongToBeatDatabase PluginDatabase = HowLongToBeat.PluginDatabase;
        private HltbProgressBar hltbProgressBar;


        public HltbDescriptionIntegration()
        {
            InitializeComponent();

            hltbProgressBar = new HltbProgressBar();
            PART_HltbProgressBar.Children.Add(hltbProgressBar);

            PluginDatabase.PropertyChanged += OnPropertyChanged;
        }

        protected void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
#if DEBUG
                logger.Debug($"HltbDescriptionIntegration.OnPropertyChanged({e.PropertyName}): {JsonConvert.SerializeObject(PluginDatabase.GameSelectedData)}");
#endif
                if (e.PropertyName == "GameSelectedData" || e.PropertyName == "PluginSettings")
                { 
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                    {
                        if (!PluginDatabase.GameSelectedData.HasData)
                        {
                            this.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            this.Visibility = Visibility.Visible;

                            if (PluginDatabase.PluginSettings.IntegrationShowTitle && !PluginDatabase.PluginSettings.EnableIntegrationInCustomTheme)
                            {
                                PART_Title.Visibility = Visibility.Visible;
                                PART_Separator.Visibility = Visibility.Visible;
                                PART_HltbProgressBar.Margin = new Thickness(0, 5, 0, 0);
                            }
                            else
                            {
                                PART_Title.Visibility = Visibility.Collapsed;
                                PART_Separator.Visibility = Visibility.Collapsed;
                                PART_HltbProgressBar.Margin = new Thickness(0, 0, 0, 0);

                                if (!PluginDatabase.PluginSettings.IntegrationTopGameDetails)
                                {
                                    PART_HltbProgressBar.Margin = new Thickness(0, 15, 0, 0);
                                }
                            }
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "GameActivity");
            }
        }
    }
}
