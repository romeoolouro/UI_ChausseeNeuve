using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using UI_ChausseeNeuve.ViewModels;

namespace UI_ChausseeNeuve.Windows
{
    /// <summary>
    /// Fenetre pour selectionner precisement quels materiaux doivent recevoir une valeur (CAM ou RISQUE)
    /// </summary>
    public partial class MaterialSelectionWindow : Window
    {
        /// <summary>
        /// Liste des indices des materiaux selectionnes
        /// </summary>
        public List<int> SelectedMaterialIndices { get; private set; } = new List<int>();

        /// <summary>
        /// Valeur a appliquer (CAM ou RISQUE)
        /// </summary>
        public double SelectedValue { get; }

        /// <summary>
        /// Type/Contexte de selection (ex: "Bitumineux", "Risque usuelles")
        /// </summary>
        public string MaterialType { get; }

        /// <summary>
        /// Type de valeur: "CAM" (defaut) ou "RISQUE"
        /// </summary>
        public string ValueKind { get; } = "CAM";

        /// <summary>
        /// Liste des checkboxes pour chaque materiau
        /// </summary>
        private List<CheckBox> MaterialCheckBoxes { get; set; } = new List<CheckBox>();

        public MaterialSelectionWindow(double value, string materialType, string valueKind = "CAM")
        {
            InitializeComponent();
            SelectedValue = value;
            MaterialType = materialType;
            ValueKind = string.IsNullOrWhiteSpace(valueKind) ? "CAM" : valueKind.ToUpperInvariant();

            // En-tetes dynamiques
            HeaderTitleText.Text = ValueKind == "RISQUE" ? "Selection des materiaux (Risque)" : "Selection des materiaux (CAM)";
            SelectedKindLabel.Text = ValueKind == "RISQUE" ? "Valeur de risque selectionnee :" : "Valeur selectionnee :";
            CamValueText.Text = ValueKind == "RISQUE" ? $"RISQUE = {value:F1}%" : $"CAM = {value:F1}";
            MaterialTypeText.Text = $"Type : {materialType}";
            InstructionText.Text = ValueKind == "RISQUE"
                ? "Cochez les materiaux auxquels vous souhaitez appliquer ce risque (%)."
                : "Cochez les materiaux auxquels vous souhaitez appliquer cette valeur (CAM).";

            // Charger les materiaux disponibles
            LoadAvailableMaterials();
        }

        /// <summary>
        /// Charge les materiaux disponibles depuis le ViewModel des valeurs admissibles
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

                UpdateSelectionCount(null, null);
            }
            else
            {
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
        /// Cree une checkbox pour un materiau
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
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) }); // valeur actuelle

            var checkBox = new CheckBox
            {
                Tag = index,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0)
            };
            checkBox.Checked += UpdateSelectionCount;
            checkBox.Unchecked += UpdateSelectionCount;
            Grid.SetColumn(checkBox, 0);
            MaterialCheckBoxes.Add(checkBox);

            var niveauText = new TextBlock
            {
                Text = couche.Niveau.ToString(),
                FontWeight = FontWeights.Bold,
                Foreground = (System.Windows.Media.Brush)FindResource("AccentBlue"),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetColumn(niveauText, 1);

            var materiauText = new TextBlock
            {
                Text = couche.Materiau,
                Style = (Style)FindResource("InfoTextStyle"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetColumn(materiauText, 2);

            // Valeur actuelle (CAM ou Risque)
            string valueLabel = ValueKind == "RISQUE" ? $"Risque: {couche.Risque:F1}%" : $"CAM: {couche.Cam:F1}";
            var valueText = new TextBlock
            {
                Text = valueLabel,
                Style = (Style)FindResource("InfoTextStyle"),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetColumn(valueText, 3);

            grid.Children.Add(checkBox);
            grid.Children.Add(niveauText);
            grid.Children.Add(materiauText);
            grid.Children.Add(valueText);

            border.Child = grid;
            MaterialsStackPanel.Children.Add(border);
        }

        private ValeursAdmissiblesViewModel? FindValeursAdmissiblesViewModel()
        {
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow is UI_ChausseeNeuve.Windows.AccueilWindow accueilWindow)
            {
                var valeursAdmissiblesView = FindVisualChild<UI_ChausseeNeuve.Views.ValeursAdmissiblesView>(accueilWindow);
                return valeursAdmissiblesView?.ViewModel;
            }
            return null;
        }

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

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var checkBox in MaterialCheckBoxes)
                checkBox.IsChecked = true;
            UpdateSelectionCount(null, null);
        }

        private void DeselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var checkBox in MaterialCheckBoxes)
                checkBox.IsChecked = false;
            UpdateSelectionCount(null, null);
        }

        private void Validate_Click(object sender, RoutedEventArgs e)
        {
            SelectedMaterialIndices.Clear();
            foreach (var checkBox in MaterialCheckBoxes)
            {
                if (checkBox.IsChecked == true && checkBox.Tag is int index)
                    SelectedMaterialIndices.Add(index);
            }

            if (SelectedMaterialIndices.Count > 0)
            {
                var selectedMaterials = new List<string>();
                var vm = FindValeursAdmissiblesViewModel();
                if (vm != null)
                {
                    foreach (var index in SelectedMaterialIndices)
                    {
                        if (index >= 0 && index < vm.ValeursAdmissibles.Count)
                        {
                            var couche = vm.ValeursAdmissibles[index];
                            string current = ValueKind == "RISQUE" ? $"R actuel: {couche.Risque:F1}%" : $"CAM actuel: {couche.Cam:F1}";
                            selectedMaterials.Add($"- Couche {couche.Niveau}: {couche.Materiau} ({current})");
                        }
                    }
                }

                var materialsText = string.Join("\n", selectedMaterials);
                string valueText = ValueKind == "RISQUE" ? $"{SelectedValue:F1}%" : $"{SelectedValue:F1}";
                var confirmationMessage = $"Confirmer l'application de la valeur {ValueKind} {valueText} aux materiaux suivants ?\n\n{materialsText}\n\nType: {MaterialType}";

                var result = MessageBox.Show(confirmationMessage, "Confirmation de validation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    ApplyValues();
                    DialogResult = true;
                    Close();
                }
            }
            else
            {
                MessageBox.Show("Veuillez selectionner au moins un materiau dans la liste.", "Selection requise", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Applique la valeur selectionnee (CAM ou RISQUE) aux materiaux selectionnes
        /// </summary>
        private void ApplyValues()
        {
            var vm = FindValeursAdmissiblesViewModel();
            if (vm != null)
            {
                var updatedMaterials = new List<string>();
                foreach (var index in SelectedMaterialIndices)
                {
                    if (index >= 0 && index < vm.ValeursAdmissibles.Count)
                    {
                        var couche = vm.ValeursAdmissibles[index];
                        if (ValueKind == "RISQUE")
                        {
                            System.Diagnostics.Debug.WriteLine($"AVANT: Couche {couche.Niveau} '{couche.Materiau}' R = {couche.Risque:F1}%");
                            couche.Risque = SelectedValue;
                            System.Diagnostics.Debug.WriteLine($"APRES: Couche {couche.Niveau} '{couche.Materiau}' R = {couche.Risque:F1}%");
                            updatedMaterials.Add($"- Couche {couche.Niveau}: {couche.Materiau} -> R = {SelectedValue:F1}%");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"AVANT: Couche {couche.Niveau} '{couche.Materiau}' CAM = {couche.Cam:F1}");
                            couche.Cam = SelectedValue;
                            System.Diagnostics.Debug.WriteLine($"APRES: Couche {couche.Niveau} '{couche.Materiau}' CAM = {couche.Cam:F1}");
                            updatedMaterials.Add($"- Couche {couche.Niveau}: {couche.Materiau} -> CAM = {SelectedValue:F1}");
                        }
                    }
                }

                var materialsText = string.Join("\n", updatedMaterials);
                string header = ValueKind == "RISQUE" ? "Valeurs de risque appliquees avec succes:" : "Valeurs CAM appliquees avec succes :";
                MessageBox.Show($"{header}\n\n{materialsText}\n\nNote : Vous pouvez toujours modifier ces valeurs manuellement dans le tableau principal.",
                                "Application reussie", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            SelectedMaterialIndices.Clear();
            DialogResult = false;
            Close();
        }

        private void UpdateSelectionCount(object? sender, RoutedEventArgs? e)
        {
            var selectedCount = MaterialCheckBoxes.Count(cb => cb.IsChecked == true);
            if (SelectionCountText != null)
                SelectionCountText.Text = $"{selectedCount} materiau(x) selectionne(s)";
            if (SelectionSummary != null)
                SelectionSummary.Visibility = selectedCount > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}