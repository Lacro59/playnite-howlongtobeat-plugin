using HowLongToBeat.Models;
using HowLongToBeat.Services;
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
        }

        /*
        public void SetHltbData(long Playtime, HowLongToBeatData data, HowLongToBeatSettings settings)
        {
            hltbProgressBar.SetHltbData(Playtime, data, settings);
            PART_HltbProgressBar.UpdateLayout();
        }
        */
    }
}
