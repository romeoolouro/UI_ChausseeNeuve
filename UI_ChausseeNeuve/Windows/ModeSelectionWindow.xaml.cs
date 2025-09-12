using System.Windows;
using System.Windows.Media;
using ChausseeNeuve.Domain.Models;

namespace UI_ChausseeNeuve.Windows
{
    public partial class ModeSelectionWindow : Window
    {
        private bool isExpertSelected = false;
        private bool isAutoSelected = false;

        public ModeSelectionWindow()
        {
            InitializeComponent();
        }

        private void ExpertCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SelectExpertMode();
        }

        private void AutoCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SelectAutoMode();
        }

        private void SelectExpertMode()
        {
            // Reset selection
            isExpertSelected = true;
            isAutoSelected = false;

            // Update UI
            ExpertCard.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0078D7"));
            ExpertCard.BorderThickness = new Thickness(2);
            AutoCard.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF404040"));
            AutoCard.BorderThickness = new Thickness(1);

            // Enable/disable buttons
            ExpertSelectButton.IsEnabled = true;
            AutoSelectButton.IsEnabled = false;
        }

        private void SelectAutoMode()
        {
            // Reset selection
            isExpertSelected = false;
            isAutoSelected = true;

            // Update UI
            AutoCard.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0078D7"));
            AutoCard.BorderThickness = new Thickness(2);
            ExpertCard.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF404040"));
            ExpertCard.BorderThickness = new Thickness(1);

            // Enable/disable buttons
            AutoSelectButton.IsEnabled = true;
            ExpertSelectButton.IsEnabled = false;
        }

        private void Expert_Click(object sender, RoutedEventArgs e)
        {
            if (!isExpertSelected) return;

            AppState.CurrentProject = new Project { Mode = DimensionnementMode.Expert };
            try
            {
                var w = new ProjetInfosWindow();
                w.Show();
                System.Windows.Application.Current.MainWindow = w;
                this.Hide();
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString(), "Erreur d'ouverture", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Auto_Click(object sender, RoutedEventArgs e)
        {
            if (!isAutoSelected) return;

            AppState.CurrentProject = new Project { Mode = DimensionnementMode.Automatique };
            try
            {
                var w = new ProjetInfosWindow();
                w.Show();
                System.Windows.Application.Current.MainWindow = w;
                this.Hide();
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString(), "Erreur d'ouverture", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
    }
}
