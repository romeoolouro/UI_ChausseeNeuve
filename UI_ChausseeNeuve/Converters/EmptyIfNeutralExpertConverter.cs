using System;
using System.Globalization;
using System.Windows.Data;
using ChausseeNeuve.Domain.Models;
using UI_ChausseeNeuve;

namespace UI_ChausseeNeuve.Converters
{
    /// <summary>
    /// Affiche une chaîne vide en mode Expert si la valeur correspond à un neutre (0 ou valeur cible indiquée).
    /// ConverterParameter format:
    ///   "FORMAT"                   -> cache uniquement 0
    ///   "FORMAT|NEUTRE"            -> cache 0 et NEUTRE (double)
    /// Exemple: ConverterParameter="F2|1" pour cacher 0 et 1.00.
    /// </summary>
    public class EmptyIfNeutralExpertConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;
            if (value is not double d) return value.ToString() ?? string.Empty;

            var mode = AppState.CurrentProject?.Mode;
            string? fmt = null; double? neutral = null;
            if (parameter is string p && !string.IsNullOrWhiteSpace(p))
            {
                var parts = p.Split('|');
                if (parts.Length > 0) fmt = parts[0];
                if (parts.Length > 1 && double.TryParse(parts[1].Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var n))
                    neutral = n;
            }

            if (mode == DimensionnementMode.Expert)
            {
                if (Math.Abs(d) < 1e-12) return string.Empty; // 0
                if (neutral.HasValue && Math.Abs(d - neutral.Value) < 1e-12) return string.Empty; // neutre
            }

            if (!string.IsNullOrWhiteSpace(fmt))
            {
                try { return d.ToString(fmt, culture); } catch { }
            }
            return d.ToString(culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return 0d;
            var s = value.ToString()?.Trim();
            if (string.IsNullOrEmpty(s)) return 0d;
            s = s.Replace(',', '.');
            return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0d;
        }
    }
}
