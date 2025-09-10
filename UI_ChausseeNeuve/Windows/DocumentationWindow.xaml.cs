using System.Windows;

namespace UI_ChausseeNeuve.Windows
{
    /// <summary>
    /// Fen�tre modale pour la documentation des valeurs admissibles
    /// </summary>
    public partial class DocumentationWindow : Window
    {
        public DocumentationWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Ferme la fen�tre de documentation
        /// </summary>
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}