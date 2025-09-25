using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace UI_ChausseeNeuve.Windows
{
    public partial class SaisieKdTableWindow : Window
    {
        public double SelectedKd { get; private set; }

        public SaisieKdTableWindow(double currentKd)
        {
            InitializeComponent();
            // Sélectionne le bouton radio correspondant à la valeur actuelle
            if (Math.Abs(currentKd - 1.0 / 1.47) < 0.01) RadioKd1.IsChecked = true;
            else if (Math.Abs(currentKd - 1.0 / 1.37) < 0.01) RadioKd2.IsChecked = true;
            else if (Math.Abs(currentKd - 1.0 / 1.07) < 0.01) RadioKd3.IsChecked = true;
            else if (Math.Abs(currentKd - 1.0 / 1.7) < 0.01) RadioKd4.IsChecked = true;
            else if (Math.Abs(currentKd - 1.0) < 0.01) RadioKd5.IsChecked = true;
            else ManualKdBox.Text = currentKd.ToString("F3", CultureInfo.InvariantCulture);
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            // Priorité à la saisie manuelle si le champ n'est pas vide et contient une valeur valide
            if (!string.IsNullOrWhiteSpace(ManualKdBox.Text) &&
                double.TryParse(ManualKdBox.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
            {
                SelectedKd = val;
            }
            else if (RadioKd1.IsChecked == true) SelectedKd = 1.0 / 1.47;
            else if (RadioKd2.IsChecked == true) SelectedKd = 1.0 / 1.37;
            else if (RadioKd3.IsChecked == true) SelectedKd = 1.0 / 1.07;
            else if (RadioKd4.IsChecked == true) SelectedKd = 1.0 / 1.7;
            else if (RadioKd5.IsChecked == true) SelectedKd = 1.0;
            else
            {
                MessageBox.Show("Veuillez sélectionner ou saisir une valeur de kd valide.");
                return;
            }
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ManualKdBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(ManualKdBox.Text))
            {
                RadioKd1.IsChecked = false;
                RadioKd2.IsChecked = false;
                RadioKd3.IsChecked = false;
                RadioKd4.IsChecked = false;
                RadioKd5.IsChecked = false;
            }
        }
    }
}
