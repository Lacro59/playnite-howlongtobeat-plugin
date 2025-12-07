using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HowLongToBeat.Views.Converters
{
    // MultiValueConverter: values[0]=main, values[1]=main+extra, values[2]=completionist, values[3]=containerWidth
    // ConverterParameter: "width0"/"width1"/"width2" or "left0"/"left1"/"left2"
    public class HoursToSegmentConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (values == null || values.Length < 4) return 0.0;

                double main = ToDouble(values[0]);
                double mainExtra = ToDouble(values[1]);
                double comp = ToDouble(values[2]);
                double totalWidth = ToDouble(values[3]);

                double total = Math.Max(0.0, Math.Max(1.0, Math.Max(main, 0) + Math.Max(mainExtra - main, 0) + Math.Max(comp - mainExtra, 0)));

                // Normalize values to segments: seg0 = main, seg1 = mainExtra - main, seg2 = comp - mainExtra
                double seg0 = Math.Max(0, main);
                double seg1 = Math.Max(0, mainExtra - main);
                double seg2 = Math.Max(0, comp - mainExtra);

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
