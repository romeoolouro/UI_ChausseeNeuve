using System.Windows;
using ChausseeNeuve.Domain.Models;

namespace UI_ChausseeNeuve.Windows
{
    public partial class ProjetInfosWindow : Window
    {
        public ProjetInfosWindow()
        {
            InitializeComponent();

            // Title according to selected mode
            var mode = AppState.CurrentProject.Mode;
            TitleBlock.Text = mode == DimensionnementMode.Expert
                ? "Informations du projet - Mode expert"
                : "Informations du projet - Mode automatique";

            DataContext = AppState.CurrentProject;
        }

        private void Browse_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // TODO: Replace with WPF FolderBrowserDialog or modern file picker
            // using var dialog = new System.Windows.Forms.FolderBrowserDialog();
            // dialog.Description = "Choisir l'emplacement du projet";
            // dialog.UseDescriptionForTitle = true;
            // var result = dialog.ShowDialog();
            // if (result == System.Windows.Forms.DialogResult.OK)
            // {
            //     AppState.CurrentProject.Location = dialog.SelectedPath;
            //     LocationBox.Text = dialog.SelectedPath;
            // }
        }

        private void Back_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var w = new ModeSelectionWindow();
            w.Show();
            System.Windows.Application.Current.MainWindow = w;
            this.Close();
        }

        private void Create_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(AppState.CurrentProject.Name))
            {
                System.Windows.MessageBox.Show("Veuillez saisir le titre du projet.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var w = new AccueilWindow();
                w.Show();
                System.Windows.Application.Current.MainWindow = w;
                this.Hide();
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString(), "Erreur d'ouverture", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}