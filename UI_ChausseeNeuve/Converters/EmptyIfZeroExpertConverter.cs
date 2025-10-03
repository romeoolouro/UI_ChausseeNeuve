using System;
using System.Globalization;
using System.Windows.Data;
using ChausseeNeuve.Domain.Models;
using UI_ChausseeNeuve;

namespace UI_ChausseeNeuve.Converters
{
    /// <summary>
    /// Affiche une case vide ("") pour les valeurs numériques égales à 0 en mode Expert.
    /// Permet de ne pas remplir visuellement les cellules avec des 0 par défaut.
    /// ConverterParameter peut contenir un format standard (ex: F2, 0.###, etc.).
    /// </summary>
    public class EmptyIfZeroExpertConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;
            if (value is double d)
            {
                var mode = AppState.CurrentProject?.Mode;
                if (mode == DimensionnementMode.Expert && Math.Abs(d) < 1e-12)
                    return string.Empty;
                string? fmt = parameter as string;
                if (!string.IsNullOrWhiteSpace(fmt))
                {
                    try { return d.ToString(fmt, culture); } catch { return d.ToString(culture); }
                }
                return d.ToString(culture);
            }
            return value.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Entrée utilisateur vide => 0
            if (value == null) return 0d;
            var s = value.ToString()?.Trim();
            if (string.IsNullOrEmpty(s)) return 0d;
            s = s.Replace(',', '.');
            if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                return d;
            return 0d; // fallback
        }
    }
}
