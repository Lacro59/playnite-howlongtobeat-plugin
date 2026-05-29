using LiveCharts;
using LiveCharts.Wpf;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace HowLongToBeat.Controls
{
    /// <summary>
    /// Tooltip for pie charts using <see cref="PieSeries"/> with numeric values and series titles.
    /// </summary>
    public partial class HltbPieChartTooltip : IChartTooltip
    {
        public TooltipSelectionMode? SelectionMode { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

        private TooltipData _data;
        public TooltipData Data
        {
            get { return _data; }
            set
            {
                _data = value;
                OnPropertyChanged(nameof(Data));
            }
        }

        public FontFamily TextFontFamily
        {
            get { return (FontFamily)GetValue(TextFontFamilyProperty); }
            set { SetValue(TextFontFamilyProperty, value); }
        }

        public static readonly DependencyProperty TextFontFamilyProperty = DependencyProperty.Register(
            nameof(TextFontFamily),
            typeof(FontFamily),
            typeof(HltbPieChartTooltip),
            new FrameworkPropertyMetadata(null));

        public HltbPieChartTooltip()
        {
            InitializeComponent();
            DataContext = this;
        }

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
