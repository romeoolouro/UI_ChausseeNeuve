using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using UI_ChausseeNeuve.ViewModels;
using UI_ChausseeNeuve.Services;

namespace UI_ChausseeNeuve.Views
{
    /// <summary>
    /// Vue pour l'affichage des résultats de calcul de la chaussée
    /// Intégrée avec le service de calcul basé sur votre code C++
    /// </summary>
    public partial class ResultatView : UserControl
    {
        public ResultatView()
        {
            InitializeComponent();

            // Créer le ViewModel et connecter les toasts
            var viewModel = new ResultatViewModel();
            viewModel.ToastRequested += (message, type) =>
            {
                ToastService.ShowToast(message, type);
            };

            DataContext = viewModel;
        }

        /// <summary>
        /// Accès au ViewModel depuis le code-behind si nécessaire
        /// </summary>
        public ResultatViewModel? ViewModel => DataContext as ResultatViewModel;
    }

    // Converter pour afficher le texte du tooltip des détails
    public class BooleanToDetailInfoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool showDetails)
            {
                return showDetails 
                    ? "Masquer les détails de calcul pour économiser l'espace" 
                    : "Afficher les détails de calcul (structure, charge, etc.)";
            }
            return "Basculer l'affichage des détails";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
