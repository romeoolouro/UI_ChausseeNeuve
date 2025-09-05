using System;
using System.Windows;
using System.Windows.Controls;
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
                // Since DataContext is the material view model, we need to access the parent view model
                // The CurrentMaterialViewModel property contains the same instance, so we can use that
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

        // Ajout des handlers pour les flèches température
        private void OnTemperatureDown(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as dynamic;
            if (vm?.TemperatureOptions != null && vm?.SelectedTemperature != null)
            {
                var options = (int[])vm.TemperatureOptions;
                int idx = Array.IndexOf(options, vm.SelectedTemperature);
                if (idx > 0)
                    vm.SelectedTemperature = options[idx - 1];
            }
        }
        private void OnTemperatureUp(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as dynamic;
            if (vm?.TemperatureOptions != null && vm?.SelectedTemperature != null)
            {
                var options = (int[])vm.TemperatureOptions;
                int idx = Array.IndexOf(options, vm.SelectedTemperature);
                if (idx < options.Length - 1)
                    vm.SelectedTemperature = options[idx + 1];
            }
        }
        // Ajout des handlers pour les flèches fréquence
        private void OnFrequenceDown(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as dynamic;
            if (vm?.FrequenceOptions != null && vm?.SelectedFrequence != null)
            {
                var options = (int[])vm.FrequenceOptions;
                int idx = Array.IndexOf(options, vm.SelectedFrequence);
                if (idx > 0)
                    vm.SelectedFrequence = options[idx - 1];
            }
        }
        private void OnFrequenceUp(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as dynamic;
            if (vm?.FrequenceOptions != null && vm?.SelectedFrequence != null)
            {
                var options = (int[])vm.FrequenceOptions;
                int idx = Array.IndexOf(options, vm.SelectedFrequence);
                if (idx < options.Length - 1)
                    vm.SelectedFrequence = options[idx + 1];
            }
        }
    }
}
