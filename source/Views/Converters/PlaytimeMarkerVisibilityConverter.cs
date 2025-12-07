using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HowLongToBeat.Views.Converters
{
    public class PlaytimeMarkerVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (values == null || values.Length < 3) return Visibility.Collapsed;
                double playtime = ToDouble(values[0]);
                double total = ToDouble(values[1]);
                double totalWidth = ToDouble(values[2]);

                if (playtime <= 0) return Visibility.Collapsed;
                if (total <= 0) return Visibility.Collapsed;

                return Visibility.Visible;
            }
            catch
            {
                return Visibility.Collapsed;
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
