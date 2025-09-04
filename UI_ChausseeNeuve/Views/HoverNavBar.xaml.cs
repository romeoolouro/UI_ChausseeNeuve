using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace UI_ChausseeNeuve.Views
{
    public partial class HoverNavBar : System.Windows.Controls.UserControl
    {
        // Emits a section key like "fichier", or "" when nothing selected
        public event System.Action<string>? SectionSelected;

        public HoverNavBar()
        {
            InitializeComponent();
        }

        private void OnChecked(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.RadioButton rb && rb.Tag is string key)
            {
                SectionSelected?.Invoke(key);
            }
        }

        private void OnUnchecked(object sender, RoutedEventArgs e)
        {
            // Back to home (no section)
            SectionSelected?.Invoke(string.Empty);
        }

        private void OnToggleSelected(object sender, MouseButtonEventArgs e)
        {
            var rb = sender as System.Windows.Controls.RadioButton;
            if (rb != null && rb.IsChecked == true)
            {
                rb.IsChecked = false;
                e.Handled = true;
            }
        }
    }
}