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
                if (values == null || values.Length < 2) return Visibility.Collapsed;
                double playtime = ConverterHelpers.ToDouble(values[0]);
                double total = ConverterHelpers.ToDouble(values[1]);

                if (playtime <= 0) return Visibility.Collapsed;
                if (total <= 0) return Visibility.Collapsed;

                return Visibility.Visible;
            }
            catch
            {
                return Visibility.Collapsed;
            }
        }


        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
