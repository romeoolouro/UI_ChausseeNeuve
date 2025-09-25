using System.Collections.Generic;
using System.Windows;

namespace UI_ChausseeNeuve.Windows
{
    public partial class SelectTargetLayerWindow : Window
    {
        public int SelectedIndex { get; private set; } = -1;

        public SelectTargetLayerWindow(List<string> items)
        {
            InitializeComponent();
            LayersList.ItemsSource = items;
        }

        private void Validate_Click(object sender, RoutedEventArgs e)
        {
            SelectedIndex = LayersList.SelectedIndex;
            if (SelectedIndex < 0)
            {
                MessageBox.Show("Veuillez sélectionner une ligne.");
                return;
            }
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}