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
            // Ouvrir SETRA en mode CAM ou RISQUE selon la sélection
            var mode = ValeursRisqueRadio.IsChecked == true ? "RISQUE" : "CAM";
            var lcpcSetraWindow = new Windows.LcpcSetraWindow(mode);
            lcpcSetraWindow.Owner = Window.GetWindow(this);
            lcpcSetraWindow.ShowDialog();
        }

        private void Catalogue1998Button_Click(object sender, RoutedEventArgs e)
        {
            // Ouvrir Catalogue 1998 en mode RISQUE quand le radio “Valeurs des risques R” est actif
            var mode = ValeursRisqueRadio.IsChecked == true ? "RISQUE" : "CAM";
            var win = new Windows.Catalogue1998Window(mode);
            win.Owner = Window.GetWindow(this);
            win.ShowDialog();
        }

        private void NormeNFP98Button_Click(object sender, RoutedEventArgs e)
        {
            // Ouvrir NFP en mode CAM ou RISQUE selon le radio ‘Valeurs des risques R’
            var mode = ValeursRisqueRadio.IsChecked == true ? "RISQUE" : "CAM";
            var win = new Windows.NFP98086Window(mode);
            win.Owner = Window.GetWindow(this);
            win.ShowDialog();
        }

        private void RigidInfo_Click(object sender, RoutedEventArgs e)
        {
            // Ouvrir la fenêtre d'information Kd/Sh
            var win = new Windows.RigidStructuresInfoWindow();
            win.Owner = Window.GetWindow(this);
            win.ShowDialog();
        }
    }
}
