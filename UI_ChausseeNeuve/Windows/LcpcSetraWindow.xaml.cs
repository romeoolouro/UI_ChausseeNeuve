using System;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Collections.Generic;
using UI_ChausseeNeuve.ViewModels;

namespace UI_ChausseeNeuve.Windows
{
    /// <summary>
    /// Fenêtre pour la configuration des valeurs CAM du Guide LCPC-SETRA 1994
    /// Reproduction du style Alizé en mode sombre avec sélection interactive
    /// </summary>
    public partial class LcpcSetraWindow : Window
    {
        /// <summary>
        /// Indique si l'utilisateur a validé ses choix
        /// </summary>
        public bool IsAccepted { get; private set; }

        /// <summary>
        /// ViewModel pour accéder aux données CAM
        /// </summary>
        public LcpcSetraViewModel? ViewModel => DataContext as LcpcSetraViewModel;

        public string Mode { get; } = "CAM"; // CAM ou RISQUE

        public LcpcSetraWindow() : this("CAM") { }

        public LcpcSetraWindow(string mode)
        {
            InitializeComponent();

            Mode = string.IsNullOrWhiteSpace(mode) ? "CAM" : mode.ToUpperInvariant();

            // Afficher uniquement la section en fonction du mode
            if (Mode == "RISQUE")
            {
                if (CamBlockMoyensForts != null) CamBlockMoyensForts.Visibility = Visibility.Collapsed;
                if (CamBlockFaibles != null) CamBlockFaibles.Visibility = Visibility.Collapsed;
                if (RiskBlock != null) RiskBlock.Visibility = Visibility.Visible;
                this.Title = "Beniroute - Risque R, Guide Lcpc-Setra 1994";
            }
            else
            {
                if (CamBlockMoyensForts != null) CamBlockMoyensForts.Visibility = Visibility.Visible;
                if (CamBlockFaibles != null) CamBlockFaibles.Visibility = Visibility.Visible;
                if (RiskBlock != null) RiskBlock.Visibility = Visibility.Collapsed;
                this.Title = "Beniroute - CAM, valeurs du Guide Lcpc-Setra 1994";
            }

            // S'abonner à l'événement de sélection de valeur CAM
            if (ViewModel != null)
            {
                ViewModel.OnCamValueSelected += OnCamValueSelected;
                ViewModel.OnRiskValueSelected += OnRiskValueSelected;
            }
        }

        /// <summary>
        /// Gestionnaire pour la sélection d'une valeur CAM
        /// </summary>
        private void OnCamValueSelected(double camValue, string materialType)
        {
            // Ouvrir la fenêtre de sélection précise des matériaux en mode CAM
            var materialSelectionWindow = new MaterialSelectionWindow(camValue, materialType, valueKind: "CAM");
            materialSelectionWindow.Owner = this;
            
            var result = materialSelectionWindow.ShowDialog();
            if (result == true && materialSelectionWindow.SelectedMaterialIndices.Count > 0)
            {
                // Les valeurs CAM ont déjà été appliquées dans MaterialSelectionWindow
                // Plus besoin de les appliquer à nouveau ici
                System.Diagnostics.Debug.WriteLine($"Selection CAM terminee: {materialSelectionWindow.SelectedMaterialIndices.Count} materiaux modifies");
            }
        }

        /// <summary>
        /// Gestionnaire pour la sélection d'une valeur de risque
        /// </summary>
        private void OnRiskValueSelected(double riskPercent, string label)
        {
            // Ouvrir la fenêtre de sélection précise des matériaux en mode RISQUE
            var selectWin = new MaterialSelectionWindow(riskPercent, label, valueKind: "RISQUE");
            selectWin.Owner = this;
            
            var result = selectWin.ShowDialog();
            if (result == true && selectWin.SelectedMaterialIndices.Count > 0)
            {
                // Les valeurs de risque ont déjà été appliquées dans MaterialSelectionWindow
                // Plus besoin de les appliquer à nouveau ici
                System.Diagnostics.Debug.WriteLine($"Selection RISQUE terminee: {selectWin.SelectedMaterialIndices.Count} materiaux modifies");
            }
        }

        /// <summary>
        /// Vérifie si un matériau correspond au type sélectionné
        /// </summary>
        private bool MaterialMatchesType(string materiau, string materialType)
        {
            string materiauLower = materiau.ToLowerInvariant();
            
            return materialType switch
            {
                "bitumineux" or "bitumineux_mince" or "bitumineux_structures" => 
                    materiauLower.Contains("enrob") || materiauLower.Contains("bbsg") || materiauLower.Contains("bitum"),
                "traites_hydrauliques" => 
                    materiauLower.Contains("mtlh") || materiauLower.Contains("grave ciment") || materiauLower.Contains("sable ciment"),
                "beton" => 
                    materiauLower.Contains("beton"),
                "granulaires" => 
                    materiauLower.Contains("gnt") || materiauLower.Contains("plateforme") || materiauLower.Contains("granulaire"),
                _ => false
            };
        }

        /// <summary>
        /// Trouve le ViewModel des valeurs admissibles dans l'application
        /// </summary>
        private ValeursAdmissiblesViewModel? FindValeursAdmissiblesViewModel()
        {
            // Cette méthode doit être adaptée selon votre architecture
            // Pour l'instant, on utilise AppState pour accéder aux données
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow is UI_ChausseeNeuve.Windows.AccueilWindow accueilWindow)
            {
                // Parcourir les contrôles pour trouver ValeursAdmissiblesView
                var valeursAdmissiblesView = FindVisualChild<UI_ChausseeNeuve.Views.ValeursAdmissiblesView>(accueilWindow);
                return valeursAdmissiblesView?.ViewModel;
            }
            
            return null;
        }

        /// <summary>
        /// Méthode helper pour trouver un contrôle enfant
        /// </summary>
        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;
                
                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }

        /// <summary>
        /// Gestionnaire pour le bouton OK - Valide et ferme la fenêtre
        /// </summary>
        private void OK_Click(object sender, RoutedEventArgs e)
        {
            IsAccepted = true;
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// Gestionnaire pour le bouton Annuler - Ferme sans valider
        /// </summary>
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            IsAccepted = false;
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// Nettoyage des événements
        /// </summary>
        protected override void OnClosed(EventArgs e)
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