using System.Windows;
using System.Windows.Controls;
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
            if (sender is ListBox listBox && listBox.SelectedItem != null)
            {
                // Notification via ToastService si nécessaire
                var viewModel = DataContext as BibliothequeViewModel;
                viewModel?.OnMaterialSelected(listBox.SelectedItem);
            }
        }

        // Ajout des handlers pour les flèches température
        private void OnTemperatureDown(object sender, RoutedEventArgs e)
        {
            var vm = (this.DataContext as BibliothequeViewModel)?.CurrentMaterialViewModel as dynamic;
            if (vm?.TemperatureOptions != null && vm?.SelectedTemperature != null)
            {
                int idx = vm.TemperatureOptions.ToList().IndexOf(vm.SelectedTemperature);
                if (idx > 0)
                    vm.SelectedTemperature = vm.TemperatureOptions[idx - 1];
            }
        }
        private void OnTemperatureUp(object sender, RoutedEventArgs e)
        {
            var vm = (this.DataContext as BibliothequeViewModel)?.CurrentMaterialViewModel as dynamic;
            if (vm?.TemperatureOptions != null && vm?.SelectedTemperature != null)
            {
                int idx = vm.TemperatureOptions.ToList().IndexOf(vm.SelectedTemperature);
                if (idx < vm.TemperatureOptions.Length - 1)
                    vm.SelectedTemperature = vm.TemperatureOptions[idx + 1];
            }
        }
        // Ajout des handlers pour les flèches fréquence
        private void OnFrequenceDown(object sender, RoutedEventArgs e)
        {
            var vm = (this.DataContext as BibliothequeViewModel)?.CurrentMaterialViewModel as dynamic;
            if (vm?.FrequenceOptions != null && vm?.SelectedFrequence != null)
            {
                int idx = vm.FrequenceOptions.ToList().IndexOf(vm.SelectedFrequence);
                if (idx > 0)
                    vm.SelectedFrequence = vm.FrequenceOptions[idx - 1];
            }
        }
        private void OnFrequenceUp(object sender, RoutedEventArgs e)
        {
            var vm = (this.DataContext as BibliothequeViewModel)?.CurrentMaterialViewModel as dynamic;
            if (vm?.FrequenceOptions != null && vm?.SelectedFrequence != null)
            {
                int idx = vm.FrequenceOptions.ToList().IndexOf(vm.SelectedFrequence);
                if (idx < vm.FrequenceOptions.Length - 1)
                    vm.SelectedFrequence = vm.FrequenceOptions[idx + 1];
            }
        }
    }
}
