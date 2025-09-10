using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using UI_ChausseeNeuve.ViewModels;

namespace UI_ChausseeNeuve.Windows
{
    /// <summary>
    /// Fenêtre pour sélectionner précisément quels matériaux doivent recevoir une valeur CAM
    /// </summary>
    public partial class MaterialSelectionWindow : Window
    {
        /// <summary>
        /// Liste des indices des matériaux sélectionnés
        /// </summary>
        public List<int> SelectedMaterialIndices { get; private set; } = new List<int>();

        /// <summary>
        /// Valeur CAM à appliquer
        /// </summary>
        public double CamValue { get; }

        /// <summary>
        /// Type de matériau de la valeur CAM
        /// </summary>
        public string MaterialType { get; }

        /// <summary>
        /// Liste des checkboxes pour chaque matériau
        /// </summary>
        private List<CheckBox> MaterialCheckBoxes { get; set; } = new List<CheckBox>();

        public MaterialSelectionWindow(double camValue, string materialType)
        {
            InitializeComponent();
            
            CamValue = camValue;
            MaterialType = materialType;
            
            // Afficher les informations sur la valeur sélectionnée
            CamValueText.Text = $"CAM = {camValue:F1}";
            MaterialTypeText.Text = $"Type : {materialType}";
            
            // Charger les matériaux disponibles
            LoadAvailableMaterials();
        }

        /// <summary>
        /// Charge les matériaux disponibles depuis le ViewModel des valeurs admissibles
        /// </summary>
        private void LoadAvailableMaterials()
        {
            var valeursAdmissiblesViewModel = FindValeursAdmissiblesViewModel();
            
            if (valeursAdmissiblesViewModel?.ValeursAdmissibles != null)
            {
                MaterialsStackPanel.Children.Clear();
                MaterialCheckBoxes.Clear();
                
                for (int i = 0; i < valeursAdmissiblesViewModel.ValeursAdmissibles.Count; i++)
                {
                    var couche = valeursAdmissiblesViewModel.ValeursAdmissibles[i];
                    CreateMaterialCheckBox(i, couche);
                }
                
                // Initialiser le compteur de sélection
                UpdateSelectionCount(null, null);
            }
            else
            {
                // Message si aucun matériau n'est disponible
                var noDataText = new TextBlock
                {
                    Text = "Aucun materiau disponible dans le tableau des valeurs admissibles",
                    Style = (Style)FindResource("InfoTextStyle"),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(10)
                };
                MaterialsStackPanel.Children.Add(noDataText);
            }
        }

        /// <summary>
        /// Crée une checkbox pour un matériau
        /// </summary>
        private void CreateMaterialCheckBox(int index, ValeurAdmissibleCouche couche)
        {
            var border = new Border
            {
                Background = (System.Windows.Media.Brush)FindResource("CardBg"),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 2, 0, 2),
                BorderBrush = (System.Windows.Media.Brush)FindResource("CardBorder"),
                BorderThickness = new Thickness(1)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) }); // Checkbox
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) }); // Niveau
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Matériau
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) }); // CAM actuel

            // Checkbox
            var checkBox = new CheckBox
            {
                Tag = index,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0)
            };
            
            // Événement pour mettre à jour le compteur de sélection
            checkBox.Checked += UpdateSelectionCount;
            checkBox.Unchecked += UpdateSelectionCount;
            
            Grid.SetColumn(checkBox, 0);
            MaterialCheckBoxes.Add(checkBox);

            // Niveau de la couche
            var niveauText = new TextBlock
            {
                Text = couche.Niveau.ToString(),
                FontWeight = FontWeights.Bold,
                Foreground = (System.Windows.Media.Brush)FindResource("AccentBlue"),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetColumn(niveauText, 1);

            // Nom du matériau
            var materiauText = new TextBlock
            {
                Text = couche.Materiau,
                Style = (Style)FindResource("InfoTextStyle"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetColumn(materiauText, 2);

            // Valeur CAM actuelle
            var camActuelText = new TextBlock
            {
                Text = $"CAM: {couche.Cam:F1}",
                Style = (Style)FindResource("InfoTextStyle"),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetColumn(camActuelText, 3);

            grid.Children.Add(checkBox);
            grid.Children.Add(niveauText);
            grid.Children.Add(materiauText);
            grid.Children.Add(camActuelText);

            border.Child = grid;
            MaterialsStackPanel.Children.Add(border);
        }

        /// <summary>
        /// Trouve le ViewModel des valeurs admissibles
        /// </summary>
        private ValeursAdmissiblesViewModel? FindValeursAdmissiblesViewModel()
        {
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow is UI_ChausseeNeuve.Windows.AccueilWindow accueilWindow)
            {
                // Rechercher le contrôle ValeursAdmissiblesView dans l'arbre visuel
                var valeursAdmissiblesView = FindVisualChild<UI_ChausseeNeuve.Views.ValeursAdmissiblesView>(accueilWindow);
                return valeursAdmissiblesView?.ViewModel;
            }
            
            return null;
        }

        /// <summary>
        /// Méthode helper pour trouver un contrôle enfant dans l'arbre visuel
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
        /// Gestionnaire pour cocher tous les matériaux
        /// </summary>
        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var checkBox in MaterialCheckBoxes)
            {
                checkBox.IsChecked = true;
            }
            UpdateSelectionCount(null, null);
        }

        /// <summary>
        /// Gestionnaire pour décocher tous les matériaux
        /// </summary>
        private void DeselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var checkBox in MaterialCheckBoxes)
            {
                checkBox.IsChecked = false;
            }
            UpdateSelectionCount(null, null);
        }

        /// <summary>
        /// Gestionnaire pour le bouton Valider et Appliquer
        /// </summary>
        private void Validate_Click(object sender, RoutedEventArgs e)
        {
            // Récupérer les indices des matériaux cochés
            SelectedMaterialIndices.Clear();
            
            foreach (var checkBox in MaterialCheckBoxes)
            {
                if (checkBox.IsChecked == true && checkBox.Tag is int index)
                {
                    SelectedMaterialIndices.Add(index);
                }
            }

            if (SelectedMaterialIndices.Count > 0)
            {
                // Afficher une boîte de confirmation avant d'appliquer
                var selectedMaterials = new List<string>();
                var valeursAdmissiblesViewModel = FindValeursAdmissiblesViewModel();
                
                if (valeursAdmissiblesViewModel != null)
                {
                    foreach (var index in SelectedMaterialIndices)
                    {
                        if (index >= 0 && index < valeursAdmissiblesViewModel.ValeursAdmissibles.Count)
                        {
                            var couche = valeursAdmissiblesViewModel.ValeursAdmissibles[index];
                            selectedMaterials.Add($"• Couche {couche.Niveau}: {couche.Materiau} (CAM actuel: {couche.Cam:F1})");
                        }
                    }
                }

                var materialsText = string.Join("\n", selectedMaterials);
                var confirmationMessage = $"Confirmer l'application de la valeur CAM {CamValue:F1} aux materiaux suivants ?\n\n{materialsText}\n\nType: {MaterialType}";
                
                var result = MessageBox.Show(
                    confirmationMessage,
                    "Confirmation de validation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Appliquer immédiatement les valeurs CAM
                    ApplyCamValues();
                    
                    DialogResult = true;
                    Close();
                }
            }
            else
            {
                MessageBox.Show(
                    "Veuillez selectionner au moins un materiau dans la liste.",
                    "Selection requise",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Applique les valeurs CAM aux matériaux sélectionnés
        /// </summary>
        private void ApplyCamValues()
        {
            var valeursAdmissiblesViewModel = FindValeursAdmissiblesViewModel();
            
            if (valeursAdmissiblesViewModel != null)
            {
                var updatedMaterials = new List<string>();
                
                foreach (var index in SelectedMaterialIndices)
                {
                    if (index >= 0 && index < valeursAdmissiblesViewModel.ValeursAdmissibles.Count)
                    {
                        var couche = valeursAdmissiblesViewModel.ValeursAdmissibles[index];
                        
                        // Debug : afficher la valeur avant modification
                        System.Diagnostics.Debug.WriteLine($"AVANT: Couche {couche.Niveau} '{couche.Materiau}' CAM = {couche.Cam:F1}");
                        
                        // Appliquer la nouvelle valeur CAM
                        couche.Cam = CamValue;
                        
                        // Debug : afficher la valeur après modification
                        System.Diagnostics.Debug.WriteLine($"APRES: Couche {couche.Niveau} '{couche.Materiau}' CAM = {couche.Cam:F1}");
                        
                        updatedMaterials.Add($"• Couche {couche.Niveau}: {couche.Materiau} ? CAM = {CamValue:F1}");
                    }
                }
                
                // Message de confirmation final
                var materialsText = string.Join("\n", updatedMaterials);
                MessageBox.Show(
                    $"Valeurs CAM appliquees avec succes :\n\n{materialsText}\n\n?? Note : Vous pouvez toujours modifier ces valeurs manuellement dans le tableau principal.",
                    "Application reussie",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Gestionnaire pour le bouton Annuler
        /// </summary>
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            SelectedMaterialIndices.Clear();
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// Met à jour le compteur de sélection
        /// </summary>
        private void UpdateSelectionCount(object? sender, RoutedEventArgs? e)
        {
            var selectedCount = MaterialCheckBoxes.Count(cb => cb.IsChecked == true);
            
            if (SelectionCountText != null)
            {
                SelectionCountText.Text = $"{selectedCount} materiau(x) selectionne(s)";
            }
            
            if (SelectionSummary != null)
            {
                SelectionSummary.Visibility = selectedCount > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }
}