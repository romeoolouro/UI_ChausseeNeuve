using System.Windows;
using UI_ChausseeNeuve.ViewModels;

namespace UI_ChausseeNeuve.Windows
{
    public partial class Catalogue1998Window : Window
    {
        public Catalogue1998ViewModel? ViewModel => DataContext as Catalogue1998ViewModel;
        private readonly string _mode;

        public Catalogue1998Window() : this("CAM") { }

        public Catalogue1998Window(string mode)
        {
            InitializeComponent();
            _mode = string.IsNullOrWhiteSpace(mode) ? "CAM" : mode.ToUpperInvariant();

            if (ViewModel != null)
            {
                ViewModel.OnCamValueSelected += OnCamValueSelected;
                ViewModel.OnRiskValueSelected += OnRiskValueSelected;
            }

            // Toggle UI according to mode
            if (_mode == "RISQUE")
            {
                this.Title = "Catalogue francais 1998 - R";
                if (CamStructuresBlock != null) CamStructuresBlock.Visibility = Visibility.Collapsed;
                if (CamPlateformeBlock != null) CamPlateformeBlock.Visibility = Visibility.Collapsed;
                if (RiskBlock != null) RiskBlock.Visibility = Visibility.Visible;
            }
            else
            {
                this.Title = "Catalogue francais 1998 - CAM";
                if (CamStructuresBlock != null) CamStructuresBlock.Visibility = Visibility.Visible;
                if (CamPlateformeBlock != null) CamPlateformeBlock.Visibility = Visibility.Visible;
                if (RiskBlock != null) RiskBlock.Visibility = Visibility.Collapsed;
            }
        }

        private void OnCamValueSelected(double value, string label)
        {
            var selectWindow = new MaterialSelectionWindow(value, label, valueKind: "CAM");
            selectWindow.Owner = this;
            selectWindow.ShowDialog();
        }

        private void OnRiskValueSelected(double value, string label)
        {
            var selectWindow = new MaterialSelectionWindow(value, label, valueKind: "RISQUE");
            selectWindow.Owner = this;
            selectWindow.ShowDialog();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosed(System.EventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.OnCamValueSelected -= OnCamValueSelected;
                ViewModel.OnRiskValueSelected -= OnRiskValueSelected;
            }
            base.OnClosed(e);
        }
    }
}
