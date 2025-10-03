using System;
using System.Globalization;
using System.Windows.Data;

namespace UI_ChausseeNeuve.Converters
{
    /// <summary>
    /// Converter "brut": n'impose aucun formatage visuel (pas de virgule auto) et accepte les séparateurs '.' ou ','.
    /// Utilisé avec UpdateSourceTrigger=LostFocus pour laisser l'utilisateur taper librement (ex: 0.25, 0,25, .35, 0.).
    /// Incomplet ou vide => retourne 0.
    /// </summary>
    public class DecimalDirectConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                // Ne jamais forcer de virgule française: retourner la représentation InvariantCulture sans groupement.
                // Laisser 0 s'afficher comme "0" pour permettre de continuer la saisie (0.25 etc.).
                return d.ToString("0.############", CultureInfo.InvariantCulture);
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return 0d;
            var s = value.ToString()?.Trim();
            if (string.IsNullOrEmpty(s)) return 0d;

            // Autoriser saisie commençant par '.' (".25")
            if (s.StartsWith('.')) s = "0" + s;
            if (s.StartsWith(',')) s = "0" + s;

            // Remplacer virgule par point
            s = s.Replace(',', '.');

            // Si l'utilisateur termine par un point ("0."), on ne force pas encore la conversion stricte -> considérer sans le point
            if (s.EndsWith('.') && s.Count(c => c=='.')==1)
            {
                var temp = s.TrimEnd('.');
                if (double.TryParse(temp, NumberStyles.Float, CultureInfo.InvariantCulture, out var partial))
                    return partial; // garde la valeur partielle
            }

            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                return d;

            return 0d; // fallback silencieux
        }
    }
}
