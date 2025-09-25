using System;
using System.Windows;

namespace UI_ChausseeNeuve.Windows
{
    public partial class SaisieKthetaWindow : Window
    {
        public double? KthetaValue { get; private set; }
        public double? E10C10Hz { get; private set; }
        public double? Eteq10Hz { get; private set; }
        public string Mode { get; private set; } = "Manuel";

        public SaisieKthetaWindow(double? ktheta = null, double? e10 = null, double? eteq = null, string mode = "Manuel")
        {
            InitializeComponent();
            if (mode == "Calculé")
            {
                RadioAuto.IsChecked = true;
                E10Box.Text = e10?.ToString() ?? "";
                EteqBox.Text = eteq?.ToString() ?? "";
                UpdateAutoResult();
            }
            else
            {
                RadioManuel.IsChecked = true;
                KthetaManuelBox.Text = ktheta?.ToString() ?? "";
            }
            UpdatePanelsVisibility();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (RadioManuel.IsChecked == true)
            {
                if (double.TryParse(KthetaManuelBox.Text, out var val))
                {
                    KthetaValue = val;
                    Mode = "Manuel";
                    DialogResult = true;
                }
                else
                {
                    MessageBox.Show("Veuillez saisir une valeur numérique pour kθ.");
                }
            }
            else if (RadioAuto.IsChecked == true)
            {
                if (double.TryParse(E10Box.Text, out var e10) && double.TryParse(EteqBox.Text, out var eteq) && eteq > 0)
                {
                    E10C10Hz = e10;
                    Eteq10Hz = eteq;
                    KthetaValue = Math.Sqrt(e10 / eteq);
                    Mode = "Calculé";
                    DialogResult = true;
                }
                else
                {
                    MessageBox.Show("Veuillez saisir des valeurs valides pour E(10°C,10Hz) et Etéq(10Hz) (>0).");
                }
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void Radio_Checked(object sender, RoutedEventArgs e)
        {
            UpdatePanelsVisibility();
        }

        private void UpdatePanelsVisibility()
        {
            if (PanelManuel != null && PanelAuto != null)
            {
                PanelManuel.Visibility = RadioManuel.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
                PanelAuto.Visibility = RadioAuto.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void UpdateAutoResult()
        {
            if (double.TryParse(E10Box.Text, out var e10) && double.TryParse(EteqBox.Text, out var eteq) && eteq > 0)
            {
                var ktheta = Math.Sqrt(e10 / eteq);
                KthetaAutoResult.Text = $"kθ = √({e10} / {eteq}) = {ktheta:F3}";
            }
            else
            {
                KthetaAutoResult.Text = "";
            }
        }

        private void E10Box_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => UpdateAutoResult();
        private void EteqBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => UpdateAutoResult();
    }
}
