using HowLongToBeat.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HowLongToBeat.Views.InterfacesFS
{
    /// <summary>
    /// Logique d'interaction pour HLTBInfoBarFS.xaml
    /// </summary>
    public partial class HLTBInfoBarFS : StackPanel
    {
        public HLTBInfoBarFS()
        {
            InitializeComponent();
        }


        public void SetData(GameHowLongToBeat GameSelectedData)
        {
            if (GameSelectedData.HasData)
            {
                this.Visibility = Visibility.Visible;
            }
            else
            {
                this.Visibility = Visibility.Collapsed;
                return;
            }

            string TimeToBeat = string.Empty;

            if (GameSelectedData.GetData().GameHltbData.MainStory != 0)
            {
                TimeToBeat = GameSelectedData.GetData().GameHltbData.MainStoryFormat;
            } 
            else if (GameSelectedData.GetData().GameHltbData.MainExtra != 0)
            {
                TimeToBeat = GameSelectedData.GetData().GameHltbData.MainExtraFormat;
            }
            else if (GameSelectedData.GetData().GameHltbData.Completionist != 0)
            {
                TimeToBeat = GameSelectedData.GetData().GameHltbData.CompletionistFormat;
            }
            else if (GameSelectedData.GetData().GameHltbData.Solo != 0)
            {
                TimeToBeat = GameSelectedData.GetData().GameHltbData.SoloFormat;
            }
            else if (GameSelectedData.GetData().GameHltbData.CoOp != 0)
            {
                TimeToBeat = GameSelectedData.GetData().GameHltbData.CoOpFormat;
            }
            else if (GameSelectedData.GetData().GameHltbData.Vs != 0)
            {
                TimeToBeat = GameSelectedData.GetData().GameHltbData.VsFormat;
            }

            if (TimeToBeat.IsNullOrEmpty())
            {
                this.Visibility = Visibility.Collapsed;
            }
            else
            {
                PART_HltbTime.Text = TimeToBeat;
            }
        }
    }
}
