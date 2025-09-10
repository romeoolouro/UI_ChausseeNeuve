using System;
using System.Windows;
using System.Windows.Controls;

namespace UI_ChausseeNeuve.Views
{
    /// <summary>
    /// Logique d'interaction pour ValeursAdmissiblesView.xaml
    /// </summary>
    public partial class ValeursAdmissiblesView : UserControl
    {
        /// <summary>
        /// Propriété ViewModel pour accéder au ViewModel depuis l'extérieur (pour les fenêtres modales)
        /// </summary>
        public ViewModels.ValeursAdmissiblesViewModel ViewModel => DataContext as ViewModels.ValeursAdmissiblesViewModel;

        public ValeursAdmissiblesView()
        {
            InitializeComponent();
        }

        private void DocumentationButton_Click(object sender, RoutedEventArgs e)
        {
            // Ouvrir fenêtre de documentation
            var documentationWindow = new Windows.DocumentationWindow();
            documentationWindow.ShowDialog();
        }

        private void LcpcSetraButton_Click(object sender, RoutedEventArgs e)
        {
            // Ouvrir fenêtre LCPC-SETRA
            var lcpcSetraWindow = new Windows.LcpcSetraWindow();
            lcpcSetraWindow.ShowDialog();
        }

        private void Catalogue1998Button_Click(object sender, RoutedEventArgs e)
        {
            // Ouvrir fenêtre Catalogue 1998 (similaire à LCPC-SETRA)
            MessageBox.Show("Fonctionnalité Catalogue 1998 en cours de développement", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void NormeNFP98Button_Click(object sender, RoutedEventArgs e)
        {
            // Ouvrir fenêtre NF P98-086
            MessageBox.Show("Fonctionnalité Norme NF P98-086 en cours de développement", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
