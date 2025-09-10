using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace UI_ChausseeNeuve.Converters
{
    /// <summary>
    /// Converter pour inverser une valeur bool�enne
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;
            return false;
        }
    }

    /// <summary>
    /// Converter pour convertir une cha�ne de couleur en brush
    /// </summary>
    public class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string colorString)
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(colorString);
                    return new SolidColorBrush(color);
                }
                catch
                {
                    return Brushes.Gray;
                }
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    /// <summary>
    /// Converter pour afficher un �l�ment si le compte est z�ro
    /// </summary>
    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
                return count == 0 ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    /// <summary>
    /// Converter pour convertir un bool�en en couleur de validation
    /// </summary>
    public class BooleanToValidationColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isValid)
                return new SolidColorBrush(isValid ? Colors.Green : Colors.Red);
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    /// <summary>
    /// Converter pour convertir un bool�en en symbole de validation
    /// </summary>
    public class BooleanToCheckmarkConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isValid)
                return isValid ? "?" : "?";
            return "�"; // Tiret au lieu de point d'interrogation
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    /// <summary>
    /// Convertit un bool�en en couleur RGB pour les lignes du tableau
    /// </summary>
    public class BooleanToRowColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isValid)
            {
                return isValid ? Color.FromRgb(240, 253, 244) : Color.FromRgb(254, 242, 242);
            }
            return Color.FromRgb(255, 255, 255);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit un bool�en en couleur RGB pour le statut
    /// </summary>
    public class BooleanToStatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isValid)
            {
                return isValid ? Color.FromRgb(56, 178, 172) : Color.FromRgb(245, 101, 101);
            }
            return Color.FromRgb(160, 174, 192);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit un bool�en en ic�ne de check
    /// </summary>
    public class BooleanToCheckConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isValid)
            {
                return isValid ? "?" : "?";
            }
            return "�"; // Tiret au lieu de point d'interrogation
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit un bool�an en ic�ne de statut
    /// </summary>
    public class BooleanToStatusIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isValid)
            {
                return isValid ? "?" : "??";
            }
            return "?";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit une interface en ic�ne appropri�e
    /// </summary>
    public class InterfaceToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string interfaceType)
            {
                return interfaceType switch
                {
                    "Surface" => "???",
                    "Base" => "???",
                    "Fondation" => "???",
                    "Plateforme" => "??",
                    _ => "??"
                };
            }
            return "??";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit une valeur num�rique en couleur selon le signe
    /// </summary>
    public class ValueToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                if (doubleValue > 0)
                    return new SolidColorBrush(Color.FromRgb(229, 62, 62)); // Rouge pour positif
                else if (doubleValue < 0)
                    return new SolidColorBrush(Color.FromRgb(56, 178, 172)); // Teal pour n�gatif
                else
                    return new SolidColorBrush(Color.FromRgb(74, 85, 104)); // Gris pour z�ro
            }
            return new SolidColorBrush(Color.FromRgb(74, 85, 104));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit un type d'interface en couleur appropri�e selon NF P98-086
    /// </summary>
    public class InterfaceTypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string interfaceType)
            {
                return interfaceType switch
                {
                    "Coll�e" => new SolidColorBrush(Color.FromRgb(40, 167, 69)),      // Vert pour interface coll�e
                    "Semi-coll�e" => new SolidColorBrush(Color.FromRgb(255, 193, 7)), // Jaune pour interface semi-coll�e
                    "D�coll�e" => new SolidColorBrush(Color.FromRgb(220, 53, 69)),    // Rouge pour interface d�coll�e
                    _ => new SolidColorBrush(Color.FromRgb(108, 117, 125))            // Gris pour interface inconnue
                };
            }
            return new SolidColorBrush(Color.FromRgb(108, 117, 125));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit un type d'interface en symbole visuel
    /// </summary>
    public class InterfaceTypeToSymbolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string interfaceType)
            {
                return interfaceType switch
                {
                    "Coll�e" => "?",       // Cercle plein pour interface coll�e
                    "Semi-coll�e" => "?",  // Demi-cercle pour interface semi-coll�e
                    "D�coll�e" => "?",     // Cercle vide pour interface d�coll�e
                    _ => "�"               // Tiret au lieu de point d'interrogation pour interface inconnue
                };
            }
            return "�"; // Tiret au lieu de point d'interrogation
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
