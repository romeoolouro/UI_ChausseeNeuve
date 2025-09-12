using System.Windows;

namespace UI_ChausseeNeuve.Windows
{
    public partial class RigidStructuresInfoWindow : Window
    {
        public RigidStructuresInfoWindow()
        {
            InitializeComponent();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}