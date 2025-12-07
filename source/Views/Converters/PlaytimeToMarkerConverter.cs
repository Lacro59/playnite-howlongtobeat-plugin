using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HowLongToBeat.Views.Converters
{
    // MultiValueConverter: values[0]=playtime, values[1]=totalCompletionist, values[2]=containerWidth
    public class PlaytimeToMarkerConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (values == null || values.Length < 3) return new Thickness(0);

                double playtime = ToDouble(values[0]);
                double total = ToDouble(values[1]);
                double totalWidth = ToDouble(values[2]);

                if (total <= 0) return new Thickness(0, 0, 0, 0);
                if (totalWidth <= 0) return new Thickness(0, 0, 0, 0);

                double fraction = playtime / total;
                if (double.IsNaN(fraction) || double.IsInfinity(fraction)) fraction = 0;
                fraction = Math.Max(0.0, Math.Min(1.0, fraction));
                double markerHalf = 7.0;
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

        private double ToDouble(object o)
        {
            if (o == null) return 0.0;
            if (o is double d) return d;
            if (o is float f) return f;
            if (o is int i) return i;
            if (o is long l) return l;
            if (double.TryParse(o.ToString(), out double parsed)) return parsed;
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
