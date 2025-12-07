using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HowLongToBeat.Views.Converters
{
    public class HoursToSegmentConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (values == null || values.Length < 4) return 0.0;

                double main = ConverterHelpers.ToDouble(values[0]);
                double mainExtra = ConverterHelpers.ToDouble(values[1]);
                double comp = ConverterHelpers.ToDouble(values[2]);
                double totalWidth = ConverterHelpers.ToDouble(values[3]);

                double seg0 = Math.Max(0, main);
                double seg1 = Math.Max(0, mainExtra - main);
                double seg2 = Math.Max(0, comp - mainExtra);

                double total = Math.Max(1.0, seg0 + seg1 + seg2);

                string param = parameter as string ?? string.Empty;

                if (param.StartsWith("width", StringComparison.OrdinalIgnoreCase))
                {
                    if (!int.TryParse(param.Substring(5), out int idx)) idx = 0;
                    double fraction = 0;
                    switch (idx)
                    {
                        case 0: fraction = seg0 / total; break;
                        case 1: fraction = seg1 / total; break;
                        case 2: fraction = seg2 / total; break;
                        default: fraction = 0; break;
                    }
                    return fraction * totalWidth;
                }

                if (param.StartsWith("left", StringComparison.OrdinalIgnoreCase))
                {
                    if (!int.TryParse(param.Substring(4), out int idx)) idx = 0;
                    double left = 0;
                    double w0 = seg0 / total * totalWidth;
                    double w1 = seg1 / total * totalWidth;
                    switch (idx)
                    {
                        case 0: left = 0; break;
                        case 1: left = w0; break;
                        case 2: left = w0 + w1; break;
                        default: left = 0; break;
                    }
                    return new Thickness(left, 0, 0, 0);
                }

                return 0.0;
            }
            catch
            {
                return 0.0;
            }
        }


        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
