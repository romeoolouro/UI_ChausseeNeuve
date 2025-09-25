using System.Windows;

namespace UI_ChausseeNeuve.Windows
{
    public partial class SaisieKsTableWindow : Window
    {
        public double SelectedKs { get; private set; }

        public SaisieKsTableWindow(double currentKs)
        {
            InitializeComponent();
            // Sélectionne le bouton radio correspondant à la valeur actuelle
            if (currentKs >= 0.82 && currentKs < 0.88) RadioKs1.IsChecked = true; // 1/1,2
            else if (currentKs >= 0.88 && currentKs < 0.93) RadioKs2.IsChecked = true; // 1/1,1
            else if (currentKs >= 0.93 && currentKs < 0.99) RadioKs3.IsChecked = true; // 1/1,065
            else if (currentKs >= 0.99) RadioKs4.IsChecked = true; // 1
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (RadioKs1.IsChecked == true) SelectedKs = 1.0 / 1.2;
            else if (RadioKs2.IsChecked == true) SelectedKs = 1.0 / 1.1;
            else if (RadioKs3.IsChecked == true) SelectedKs = 1.0 / 1.065;
            else if (RadioKs4.IsChecked == true) SelectedKs = 1.0;
            else { MessageBox.Show("Veuillez sélectionner une valeur de ks."); return; }
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
