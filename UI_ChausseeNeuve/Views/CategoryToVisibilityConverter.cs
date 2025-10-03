using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace UI_ChausseeNeuve.Views
{
    /// <summary>
    /// Permet d'afficher un controle uniquement si la categorie courante correspond au parametre.
    /// (Commentaires sans accents pour eviter tout probleme d'encodage dans certains parseurs.)
    /// </summary>
    public class CategoryToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;
            string currentCategory = value.ToString() ?? string.Empty;
            string expectedCategory = parameter.ToString() ?? string.Empty;
            return string.Equals(currentCategory, expectedCategory, StringComparison.OrdinalIgnoreCase)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
