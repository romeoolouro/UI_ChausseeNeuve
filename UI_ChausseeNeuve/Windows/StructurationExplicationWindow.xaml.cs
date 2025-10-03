using System.Collections.Generic;
using System.Windows;

namespace UI_ChausseeNeuve.Windows
{
    public class SousCoucheInfo
    {
        public string Name { get; set; }
        public double Thickness { get; set; }
        public double Module { get; set; }
    }

    public partial class StructurationExplicationWindow : Window
    {
        public bool IsValidated { get; private set; }

        public StructurationExplicationWindow(List<SousCoucheInfo> sousCouches, string parametersText)
        {
            InitializeComponent();
            
            // Afficher les sous-couches
            SousCouchesList.ItemsSource = sousCouches;
            
            // Afficher les paramètres
            ParamsText.Text = parametersText;
        }

        private void ValidateButton_Click(object sender, RoutedEventArgs e)
        {
            IsValidated = true;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            IsValidated = false;
            DialogResult = false;
            Close();
        }
    }
}