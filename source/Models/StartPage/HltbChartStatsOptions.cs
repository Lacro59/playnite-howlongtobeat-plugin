using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HowLongToBeat.Models.StartPage
{
    public enum ChartStatsType
    {
        month, year
    }

    public class HltbChartStatsOptions : ObservableObject
    {
        private double _Margin = 10;
        public double Margin { get => _Margin; set => SetValue(ref _Margin, value); }


        private ChartStatsType _StatsType = ChartStatsType.year;
        public ChartStatsType StatsType { get => _StatsType; set => SetValue(ref _StatsType, value); }

        private bool _ChartTitle = false;
        public bool ChartTitle { get => _ChartTitle; set => SetValue(ref _ChartTitle, value); }

        private bool _ChartLabels = true;
        public bool ChartLabels { get => _ChartLabels; set => SetValue(ref _ChartLabels, value); }

        private bool _ChartLabelsOrdinates = false;
        public bool ChartLabelsOrdinates { get => _ChartLabelsOrdinates; set => SetValue(ref _ChartLabelsOrdinates, value); }

        private double _DataNumber = 5;
        public double DataNumber { get => _DataNumber; set => SetValue(ref _DataNumber, value); }
    }
}
