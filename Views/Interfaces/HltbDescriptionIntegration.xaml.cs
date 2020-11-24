using HowLongToBeat.Models;
using HowLongToBeat.Services;
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
        private HltbProgressBar hltbProgressBar;


        public HltbDescriptionIntegration(HowLongToBeatSettings settings, bool IntegrationShowTitle)
        {
            hltbProgressBar = new HltbProgressBar(settings);

            InitializeComponent();

            if (!IntegrationShowTitle)
            {
                PART_Title.Visibility = Visibility.Collapsed;
                PART_Separator.Visibility = Visibility.Collapsed;
            }

            PART_HltbProgressBar.Children.Add(hltbProgressBar);

            //HowLongToBeat.PluginDatabase.PropertyChanged += OnPropertyChanged;
        }

        /*
        protected void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
                this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
                {
                    if (!HowLongToBeat.PluginDatabase.GameSelectedData.HasData)
                    {
                        this.Visibility = Visibility.Collapsed;
                        return;
                    }
                }));
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "GameActivity");
            }
        }
        */

        /*
        private void PART_HltbProgressBar_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                this.Visibility = PART_Title.Visibility;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "GameActivity");
            }
        }
        */
    }
}
