using System;

namespace HowLongToBeat.Views.Converters
{
    internal static class ConverterHelpers
    {
        public static double ToDouble(object o)
        {
            if (o == null) return 0.0;
            if (o is double d) return d;
            if (o is float f) return f;
            if (o is int i) return i;
            if (o is long l) return l;
            if (o is ulong ul)
            {
                return ul;
            }
            if (double.TryParse(o.ToString(), out double parsed)) return parsed;
            return 0.0;
        }
    }
}
