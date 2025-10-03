using System;
using System.Globalization;
using System.Windows.Data;
using ChausseeNeuve.Domain.Models;
using UI_ChausseeNeuve;

namespace UI_ChausseeNeuve.Converters
{
    /// <summary>
    /// Multi value converter: values[0] = double value, values[1] = bool userSet flag.
    /// Returns empty string in Expert mode when value == 0 AND userSet flag is false.
    /// Otherwise returns formatted value (ConverterParameter format e.g. F2).
    /// ConvertBack parses first value only; userSet flag is left unchanged (it is set in the property setter).
    /// </summary>
    public class EmptyIfZeroUnlessUserSetMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2) return string.Empty;
            double d = 0;
            bool hasValue = false;
            if (values[0] is double vd) { d = vd; hasValue = true; }
            else if (values[0] != null && double.TryParse(values[0].ToString()?.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)) { d = parsed; hasValue = true; }
            bool userSet = values[1] is bool b && b;
            var mode = AppState.CurrentProject?.Mode;
            if (mode == DimensionnementMode.Expert && hasValue && Math.Abs(d) < 1e-12 && !userSet)
                return string.Empty;
            string fmt = parameter as string ?? "G";
            try { return d.ToString(fmt, culture); } catch { return d.ToString(culture); }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            double d = 0;
            if (value != null)
            {
                var s = value.ToString()?.Trim();
                if (!string.IsNullOrEmpty(s))
                {
                    s = s.Replace(',', '.');
                    if (!double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out d))
                        d = 0;
                }
            }
            // Only update the first binding source; second (userSet flag) is managed by property setter.
            var res = new object[targetTypes.Length];
            if (res.Length > 0) res[0] = d;
            for (int i = 1; i < res.Length; i++) res[i] = Binding.DoNothing;
            return res;
        }
    }
}
