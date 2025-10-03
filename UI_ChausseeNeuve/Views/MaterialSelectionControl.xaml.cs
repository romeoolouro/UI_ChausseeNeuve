using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using UI_ChausseeNeuve.ViewModels;

namespace UI_ChausseeNeuve.Views
{
    public partial class MaterialSelectionControl : UserControl
    {
        public MaterialSelectionControl()
        {
            InitializeComponent();
        }

        // Dependency Properties pour le binding MVVM
        public static readonly DependencyProperty CurrentMaterialViewModelProperty =
            DependencyProperty.Register(
                "CurrentMaterialViewModel",
                typeof(object),
                typeof(MaterialSelectionControl),
                new PropertyMetadata(null));

        public object CurrentMaterialViewModel
        {
            get => GetValue(CurrentMaterialViewModelProperty);
            set => SetValue(CurrentMaterialViewModelProperty, value);
        }

        // Gestionnaire d'événement pour la sélection de matériau
        private void MaterialSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem != null && listBox.SelectedItem is MaterialItem materialItem)
            {
                // Notification via ToastService si nécessaire
                var parent = VisualTreeHelper.GetParent(this) as FrameworkElement;
                while (parent != null && !(parent.DataContext is BibliothequeViewModel))
                {
                    parent = VisualTreeHelper.GetParent(parent) as FrameworkElement;
                }
                if (parent?.DataContext is BibliothequeViewModel bibVM)
                {
                    bibVM.OnMaterialSelected(materialItem);
                }
            }
        }

        // Helper to get the current MaterialViewModel from parent BibliothequeViewModel
        private MaterialViewModelBase? GetCurrentMaterialVM()
        {
            var parent = VisualTreeHelper.GetParent(this) as FrameworkElement;
            while (parent != null && !(parent.DataContext is BibliothequeViewModel))
            {
                parent = VisualTreeHelper.GetParent(parent) as FrameworkElement;
            }
            if (parent?.DataContext is BibliothequeViewModel bibVM)
            {
                return bibVM.CurrentMaterialViewModel as MaterialViewModelBase;
            }
            return null;
        }

        // Ajout des handlers pour les flèches température (SEULEMENT pour MB)
        private void OnTemperatureDown(object sender, RoutedEventArgs e)
        {
            var vm = GetCurrentMaterialVM();
            if (vm == null) return;

            int min = vm.TemperatureOptions != null && vm.TemperatureOptions.Length > 0 ? vm.TemperatureOptions.Min() : -10;
            if (vm.SelectedTemperature > min)
            {
                vm.SelectedTemperature -= 1;
            }
        }
        
        private void OnTemperatureUp(object sender, RoutedEventArgs e)
        {
            var vm = GetCurrentMaterialVM();
            if (vm == null) return;

            int max = vm.TemperatureOptions != null && vm.TemperatureOptions.Length > 0 ? vm.TemperatureOptions.Max() : 40;
            if (vm.SelectedTemperature < max)
            {
                vm.SelectedTemperature += 1;
            }
        }
        
        // Ajout des handlers pour les flèches fréquence (SEULEMENT pour MB)
        private void OnFrequenceDown(object sender, RoutedEventArgs e)
        {
            var vm = GetCurrentMaterialVM();
            if (vm == null) return;

            int min = vm.FrequenceOptions != null && vm.FrequenceOptions.Length > 0 ? vm.FrequenceOptions.Min() : 5;
            if (vm.SelectedFrequence > min)
            {
                vm.SelectedFrequence -= 1;
            }
        }
        
        private void OnFrequenceUp(object sender, RoutedEventArgs e)
        {
            var vm = GetCurrentMaterialVM();
            if (vm == null) return;

            int max = vm.FrequenceOptions != null && vm.FrequenceOptions.Length > 0 ? vm.FrequenceOptions.Max() : 20;
            if (vm.SelectedFrequence < max)
            {
                vm.SelectedFrequence += 1;
            }
        }

        /// <summary>
        /// Gestionnaire pour le double-clic sur les cellules Sh
        /// Ouvre la fenêtre d'explication de la dispersion d'épaisseur comme Alizé
        /// </summary>
        private void OnShCellDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && sender is FrameworkElement element && element.DataContext is MaterialItem material)
            {
                if (material.ShStatus == "standard")
                {
                    // Ouvrir la fenêtre d'explication comme dans Alizé
                    var mainWindow = Window.GetWindow(this);
                    bool applied = Windows.ShDispersionExplanationWindow.ShowExplanation(material, mainWindow);
                    
                    if (applied)
                    {
                        // Optionnel : notification toast de succès
                        Services.ToastService.ShowToast(
                            $"Valeur Sh appliquée pour {material.Name}: {material.Sh:F3}m",
                            ChausseeNeuve.Domain.Models.ToastType.Success);
                    }
                }
                else
                {
                    // Si déjà rempli, proposer de réinitialiser ou de modifier
                    var result = MessageBox.Show(
                        $"La valeur Sh pour {material.Name} est déjà définie ({material.Sh:F3}m).\n\n" +
                        "Voulez-vous ouvrir à nouveau l'explication de la dispersion d'épaisseur ?",
                        "Valeur Sh déjà définie",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        var mainWindow = Window.GetWindow(this);
                        Windows.ShDispersionExplanationWindow.ShowExplanation(material, mainWindow);
                    }
                }
            }
        }
    }
}
