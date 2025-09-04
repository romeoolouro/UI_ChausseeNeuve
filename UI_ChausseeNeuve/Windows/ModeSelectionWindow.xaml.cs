using System.Windows;
using ChausseeNeuve.Domain.Models;
namespace UI_ChausseeNeuve.Windows
{
    public partial class ModeSelectionWindow : Window
    {
        public ModeSelectionWindow(){ InitializeComponent(); }
        private void Expert_Click(object sender, RoutedEventArgs e)
        {
            AppState.CurrentProject = new Project { Mode = DimensionnementMode.Expert };
            try {
                var w = new ProjetInfosWindow();
                w.Show();
                System.Windows.Application.Current.MainWindow = w;
                this.Hide();
            } catch (System.Exception ex) {
                System.Windows.MessageBox.Show(ex.ToString(), "Erreur d'ouverture", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Auto_Click(object sender, RoutedEventArgs e)
        {
            AppState.CurrentProject = new Project { Mode = DimensionnementMode.Automatique };
            try {
                var w = new ProjetInfosWindow();
                w.Show();
                System.Windows.Application.Current.MainWindow = w;
                this.Hide();
            } catch (System.Exception ex) {
                System.Windows.MessageBox.Show(ex.ToString(), "Erreur d'ouverture", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
    }
}
