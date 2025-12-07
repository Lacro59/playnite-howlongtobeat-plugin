using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HowLongToBeat.Views.Converters
{
    public class PlaytimeToMarkerConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (values == null || values.Length < 3) return new Thickness(0);

                double playtime = ConverterHelpers.ToDouble(values[0]);
                double total = ConverterHelpers.ToDouble(values[1]);
                double totalWidth = ConverterHelpers.ToDouble(values[2]);

                if (total <= 0) return new Thickness(0, 0, 0, 0);
                if (totalWidth <= 0) return new Thickness(0, 0, 0, 0);

                double fraction = playtime / total;
                if (double.IsNaN(fraction) || double.IsInfinity(fraction)) fraction = 0;
                fraction = Math.Max(0.0, Math.Min(1.0, fraction));
                const double markerHalf = 7.0;
                double left = fraction * totalWidth - markerHalf;
                if (left < 0) left = 0;
                if (left > totalWidth - 2 * markerHalf) left = totalWidth - 2 * markerHalf;

                return new Thickness(left, 0, 0, 0);
            }
            catch
            {
                return new Thickness(0);
            }
        }


        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
