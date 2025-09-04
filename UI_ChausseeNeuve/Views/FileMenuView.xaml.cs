using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using UI_ChausseeNeuve.Services;

namespace UI_ChausseeNeuve.Views
{
    public partial class FileMenuView : System.Windows.Controls.UserControl
    {
        public FileMenuView()
        {
            InitializeComponent();
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Ouvrir un projet BENIROUTE",
                Filter = "Projet BENIROUTE (*.bnrproj)|*.bnrproj|JSON (*.json)|*.json|Tous les fichiers (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    ProjectStorage.Load(dlg.FileName);
                    System.Windows.MessageBox.Show("Projet chargé.", "Ouverture", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (System.Exception ex)
                {
                    System.Windows.MessageBox.Show("Échec de l'ouverture : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var path = AppState.CurrentFilePath;
            if (string.IsNullOrWhiteSpace(path))
            {
                SaveAs_Click(sender, e);
                return;
            }
            try
            {
                ProjectStorage.Save(AppState.CurrentProject, path!);
                System.Windows.MessageBox.Show("Projet enregistré.", "Enregistrer", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show("Échec de l'enregistrement : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Enregistrer le projet",
                Filter = "Projet BENIROUTE (*.bnrproj)|*.bnrproj|JSON (*.json)|*.json", DefaultExt = ".bnrproj",
                FileName = string.IsNullOrWhiteSpace(AppState.CurrentProject.Name) ? "Projet" : AppState.CurrentProject.Name
            };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    ProjectStorage.Save(AppState.CurrentProject, dlg.FileName);
                    System.Windows.MessageBox.Show("Projet enregistré.", "Enregistrer sous", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (System.Exception ex)
                {
                    System.Windows.MessageBox.Show("Échec de l'enregistrement : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Recent_Click(object sender, RoutedEventArgs e)
        {
            var items = RecentFiles.Get().ToList();
            RecentPanel.Visibility = items.Any() ? Visibility.Visible : Visibility.Collapsed;
            RecentList.ItemsSource = items;
            if (!items.Any())
            {
                System.Windows.MessageBox.Show("Aucun fichier récent.", "Récents", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OpenRecent_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button b && b.Tag is string path && File.Exists(path))
            {
                try
                {
                    ProjectStorage.Load(path);
                    System.Windows.MessageBox.Show("Projet chargé.", "Ouverture", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (System.Exception ex)
                {
                    System.Windows.MessageBox.Show("Échec de l'ouverture : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Le fichier n'existe plus.", "Récents", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Quit_Click(object sender, RoutedEventArgs e)
        {
            // Quitter le module = revenir à la sélection de mode
            var w = new UI_ChausseeNeuve.Windows.ModeSelectionWindow();
            w.Show();
            System.Windows.Application.Current.MainWindow = w;
            Window.GetWindow(this)?.Close();
        }
    }
}
