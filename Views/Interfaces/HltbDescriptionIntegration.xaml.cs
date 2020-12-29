using CommonShared;
using HowLongToBeat.Models;
using HowLongToBeat.Services;
using Newtonsoft.Json;
using Playnite.SDK;
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
                if (e.PropertyName == "GameSelectedData" || e.PropertyName == "PluginSettings")
                { 
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                    {
                        // No data
                        if (!PluginDatabase.GameSelectedData.HasData)
                        {
                            this.Visibility = Visibility.Collapsed;
                            return;
                        }
                        else
                        {
                            this.Visibility = Visibility.Visible;
                        }


                        if (PluginDatabase.PluginSettings.IntegrationShowTitle)
                        {
                            PART_HltbProgressBar.Margin = new Thickness(0, 5, 0, 5);
                        }
                        else
                        {
                        }

                        this.DataContext = new
                        {
                            IntegrationShowTitle = PluginDatabase.PluginSettings.IntegrationShowTitle
                        };
                    }));
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "HowLongToBeat");
            }
        }
    }
}
