using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UI_ChausseeNeuve.Windows;

namespace UI_ChausseeNeuve.Views
{
    /// <summary>
    /// Logique d'interaction pour ValeursAdmissiblesView.xaml
    /// </summary>
    public partial class ValeursAdmissiblesView : UserControl
    {
        /// <summary>
        /// Propriété ViewModel pour accéder au ViewModel depuis l'extérieur (pour les fenêtres modales)
        /// </summary>
        public ViewModels.ValeursAdmissiblesViewModel ViewModel => DataContext as ViewModels.ValeursAdmissiblesViewModel;

        public ValeursAdmissiblesView()
        {
            InitializeComponent();
        }

        private void DocumentationButton_Click(object sender, RoutedEventArgs e)
        {
            // Ouvrir fenêtre de documentation
            var documentationWindow = new Windows.DocumentationWindow();
            documentationWindow.ShowDialog();
        }

        private void LcpcSetraButton_Click(object sender, RoutedEventArgs e)
        {
            // Ouvrir SETRA en mode CAM ou RISQUE selon la sélection
            var mode = ValeursRisqueRadio.IsChecked == true ? "RISQUE" : "CAM";
            var lcpcSetraWindow = new Windows.LcpcSetraWindow(mode);
            lcpcSetraWindow.Owner = Window.GetWindow(this);
            lcpcSetraWindow.ShowDialog();
        }

        private void Catalogue1998Button_Click(object sender, RoutedEventArgs e)
        {
            // Ouvrir Catalogue 1998 en mode RISQUE quand le radio “Valeurs des risques R” est actif
            var mode = ValeursRisqueRadio.IsChecked == true ? "RISQUE" : "CAM";
            var win = new Windows.Catalogue1998Window(mode);
            win.Owner = Window.GetWindow(this);
            win.ShowDialog();
        }

        private void NormeNFP98Button_Click(object sender, RoutedEventArgs e)
        {
            // Ouvrir NFP en mode CAM ou RISQUE selon le radio ‘Valeurs des risques R’
            var mode = ValeursRisqueRadio.IsChecked == true ? "RISQUE" : "CAM";
            var win = new Windows.NFP98086Window(mode);
            win.Owner = Window.GetWindow(this);
            win.ShowDialog();
        }

        private ViewModels.MaterialItem? _lastValidatedMaterial;

        private void OpenBibliotheque_Click(object sender, RoutedEventArgs e)
        {
            var win = new Windows.BibliothequeWindow();
            win.Owner = Window.GetWindow(this);
            if (win.ShowDialog() == true && win.ViewModel?.SelectedMaterial != null)
            {
                _lastValidatedMaterial = win.ViewModel.SelectedMaterial;
                Services.ToastService.ShowToast($"Matériau validé: {_lastValidatedMaterial.Name}", ChausseeNeuve.Domain.Models.ToastType.Success);
                // Demander la ligne cible immédiatement (comme SETRA/Catalogue/NFP)
                PromptTargetRowAndApply();
            }
        }

        private void PromptTargetRowAndApply()
        {
            if (_lastValidatedMaterial == null) return;
            var vm = ViewModel;
            if (vm == null || vm.ValeursAdmissibles == null || vm.ValeursAdmissibles.Count == 0)
            {
                MessageBox.Show("Aucune ligne disponible pour appliquer le matériau.");
                return;
            }

            var items = vm.ValeursAdmissibles.Select(c => $"Couche {c.Niveau}: {c.Materiau}").ToList();
            var dlg = new Windows.SelectTargetLayerWindow(items);
            dlg.Owner = Window.GetWindow(this);
            if (dlg.ShowDialog() == true && dlg.SelectedIndex >= 0 && dlg.SelectedIndex < vm.ValeursAdmissibles.Count)
            {
                var cible = vm.ValeursAdmissibles[dlg.SelectedIndex];
                ApplyMaterialToRow(cible);
            }
        }

        private void ValidateLine_Click(object sender, RoutedEventArgs e)
        {
            if (_lastValidatedMaterial == null) return;
            if (sender is FrameworkElement fe && fe.DataContext is ViewModels.ValeurAdmissibleCouche couche)
            {
                ApplyMaterialToRow(couche);
            }
        }

        private void ApplyMaterialToRow(ViewModels.ValeurAdmissibleCouche couche)
        {
            if (_lastValidatedMaterial == null) return;
            // Appliquer les propriétés essentielles à la ligne choisie
            couche.Materiau = _lastValidatedMaterial.Name ?? couche.Materiau;

            if (_lastValidatedMaterial.Sh.HasValue) couche.Sh = _lastValidatedMaterial.Sh.Value;
            if (_lastValidatedMaterial.InverseB.HasValue) couche.B = -Math.Abs(_lastValidatedMaterial.InverseB.Value); // -1/b
            if (_lastValidatedMaterial.Kc.HasValue) couche.Kc = _lastValidatedMaterial.Kc.Value;
            if (_lastValidatedMaterial.Kd.HasValue) couche.Kd = _lastValidatedMaterial.Kd.Value;

            if (_lastValidatedMaterial.Epsi0_10C.HasValue)
                couche.Epsilon6 = _lastValidatedMaterial.Epsi0_10C.Value;

            if (_lastValidatedMaterial.SN.HasValue)
                couche.Sn = _lastValidatedMaterial.SN.Value;

            Services.ToastService.ShowToast($"Matériau '{_lastValidatedMaterial.Name}' appliqué à la couche {couche.Niveau}", ChausseeNeuve.Domain.Models.ToastType.Success);
            _lastValidatedMaterial = null;
        }

        private void KthetaCell_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox tb && tb.DataContext is ViewModels.ValeurAdmissibleCouche couche)
            {
                var win = new SaisieKthetaWindow(
                    ktheta: couche.Ktheta,
                    e10: couche.E10C10Hz,
                    eteq: couche.Eteq10Hz,
                    mode: couche.KthetaAuto ? "Calculé" : "Manuel"
                );
                win.Owner = Window.GetWindow(this);
                if (win.ShowDialog() == true)
                {
                    if (win.Mode == "Manuel")
                    {
                        couche.Ktheta = win.KthetaValue ?? couche.Ktheta;
                        couche.KthetaAuto = false;
                    }
                    else if (win.Mode == "Calculé")
                    {
                        couche.E10C10Hz = win.E10C10Hz ?? couche.E10C10Hz;
                        couche.Eteq10Hz = win.Eteq10Hz ?? couche.Eteq10Hz;
                        couche.KthetaAuto = true;
                        couche.Ktheta = win.KthetaValue ?? couche.Ktheta;
                    }
                }
            }
        }

        private void KsCell_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox tb && tb.DataContext is ViewModels.ValeurAdmissibleCouche couche)
            {
                var win = new Windows.SaisieKsTableWindow(couche.Ks);
                win.Owner = Window.GetWindow(this);
                if (win.ShowDialog() == true)
                {
                    couche.Ks = win.SelectedKs;
                }
            }
        }

        private void RigidInfo_Click(object sender, RoutedEventArgs e)
        {
            var win = new Windows.RigidStructuresInfoWindow();
            win.Owner = Window.GetWindow(this);
            win.ShowDialog();
        }

        private void KdCell_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox tb && tb.DataContext is ViewModels.ValeurAdmissibleCouche couche)
            {
                var win = new Windows.SaisieKdTableWindow(couche.Kd);
                win.Owner = Window.GetWindow(this);
                if (win.ShowDialog() == true)
                {
                    couche.Kd = win.SelectedKd;
                }
            }
        }
    }
}
