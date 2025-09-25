using System.Windows;

namespace UI_ChausseeNeuve.Windows
{
    public partial class BibliothequeWindow : Window
    {
        public ViewModels.BibliothequeViewModel? ViewModel => BibView.DataContext as ViewModels.BibliothequeViewModel;

        public BibliothequeWindow()
        {
            InitializeComponent();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel?.SelectedMaterial != null)
            {
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Veuillez s�lectionner un mat�riau dans la biblioth�que.", "Validation requise", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}