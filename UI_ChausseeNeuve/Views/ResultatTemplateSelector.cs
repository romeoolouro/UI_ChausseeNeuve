using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System;
using System.Globalization;
using UI_ChausseeNeuve.ViewModels;

namespace UI_ChausseeNeuve.Views
{
    /// <summary>
    /// S�lecteur de template pour diff�rencier l'affichage des couches et des interfaces
    /// Permet d'afficher les interfaces ENTRE les couches comme dans Aliz�
    /// </summary>
    public class ResultatTemplateSelector : DataTemplateSelector
    {
        /// <summary>
        /// Template pour afficher une couche de chauss�e
        /// </summary>
        public DataTemplate? CoucheTemplate { get; set; }

        /// <summary>
        /// Template pour afficher une interface entre deux couches
        /// </summary>
        public DataTemplate? InterfaceTemplate { get; set; }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            return item switch
            {
                ResultatCouche => CoucheTemplate,
                ResultatInterface => InterfaceTemplate,
                _ => base.SelectTemplate(item, container)
            };
        }
    }

    /// <summary>
    /// Convertit un bool�en en couleur pour les lignes du tableau
    /// </summary>
    public class SimpleBooleanToRowColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isValid)
            {
                return isValid ? "#ffffff" : "#ffe6e6"; // Blanc ou rouge clair
            }
            return "#ffffff";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit un bool�en en symbole de validation - CORRIG�
    /// </summary>
    public class SimpleBooleanToCheckConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isValid)
            {
                // Utilisation de caract�res simples qui fonctionnent dans WPF
                return isValid ? "\u2713" : "\u2717"; // Unicode pour ? et ?
            }
            return "\u2014"; // Tiret long pour valeur ind�termin�e
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit un bool�en en couleur pour le statut
    /// </summary>
    public class SimpleBooleanToStatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isValid)
            {
                return new SolidColorBrush(isValid ? Colors.Green : Colors.Red);
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit un count en visibilit� (visible si count = 0)
    /// </summary>
    public class SimpleCountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                return count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}