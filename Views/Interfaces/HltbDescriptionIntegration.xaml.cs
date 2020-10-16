using HowLongToBeat.Models;
using HowLongToBeat.Services;
using System;
using System.Windows;
using System.Windows.Controls;

namespace HowLongToBeat.Views.Interfaces
{
    /// <summary>
    /// Logique d'interaction pour HltbDescriptionIntegration.xaml
    /// </summary>
    public partial class HltbDescriptionIntegration : StackPanel
    {
        private HltbProgressBar hltbProgressBar = new HltbProgressBar();

        public HltbDescriptionIntegration(bool IntegrationShowTitle)
        {
            InitializeComponent();

            if (!IntegrationShowTitle)
            {
                PART_tbHltb.Visibility = Visibility.Collapsed;
                PART_hltbsep.Visibility = Visibility.Collapsed;
            }

            PART_HltbProgressBar.Children.Add(hltbProgressBar);
        }

        public void SetHltbData(long Playtime, HowLongToBeatData data, HowLongToBeatSettings settings)
        {
            hltbProgressBar.SetHltbData(Playtime, data, settings);
            PART_HltbProgressBar.UpdateLayout();
        }
    }
}
