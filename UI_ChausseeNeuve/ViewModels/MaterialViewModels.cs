using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ChausseeNeuve.Domain.Models;
using UI_ChausseeNeuve.Services;
using System.Linq;
using System;

namespace UI_ChausseeNeuve.ViewModels
{
    public abstract class MaterialViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected readonly MaterialDataService _dataService;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private ObservableCollection<MaterialItem> _availableMaterials = new();
        public ObservableCollection<MaterialItem> AvailableMaterials
        {
            get => _availableMaterials;
            set
            {
                _availableMaterials = value;
                OnPropertyChanged();
            }
        }

        private MaterialItem? _selectedMaterial;
        public MaterialItem? SelectedMaterial
        {
            get => _selectedMaterial;
            set
            {
                _selectedMaterial = value;
                OnPropertyChanged();
            }
        }

        private string _currentCategory = "";
        public string CurrentCategory
        {
            get => _currentCategory;
            set
            {
                _currentCategory = value;
                OnPropertyChanged();
                LoadMaterials();
            }
        }

        // Ajout propriétés température/fréquence
        private int _selectedTemperature = 15; // Changed default from 10 to 15°C to match Alizé
        public int SelectedTemperature
        {
            get => _selectedTemperature;
            set
            {
                if (_selectedTemperature != value)
                {
                    _selectedTemperature = value;
                    OnPropertyChanged();
                    RecomputeComputedModulusForAll();
                }
            }
        }
        private int _selectedFrequence = 10; // Keep 10Hz as default, matches Alizé
        public int SelectedFrequence
        {
            get => _selectedFrequence;
            set
            {
                if (_selectedFrequence != value)
                {
                    _selectedFrequence = value;
                    OnPropertyChanged();
                    RecomputeComputedModulusForAll();
                }
            }
        }
        // Temperature options now include every integer from -10 to 40
        public int[] TemperatureOptions { get; } = Enumerable.Range(-10, 51).ToArray(); // -10 .. 40
        // Frequency options 1..50 Hz
        public int[] FrequenceOptions { get; } = Enumerable.Range(1, 50).ToArray();

        protected MaterialViewModelBase()
        {
            _dataService = new MaterialDataService();
        }

        internal abstract void LoadMaterials();

        /// <summary>
        /// Définit la catégorie et recharge les matériaux
        /// </summary>
        public void SetCategory(string category)
        {
            CurrentCategory = category;
        }

        protected void RecomputeComputedModulusForAll()
        {
            if (AvailableMaterials == null) return;
            foreach (var m in AvailableMaterials)
            {
                try
                {
                    m.ComputedModulus = m.GetModulusAt(SelectedTemperature, SelectedFrequence);
                    System.Diagnostics.Debug.WriteLine($"Material {m.Name}: T={SelectedTemperature}°C, F={SelectedFrequence}Hz => E={m.ComputedModulus:N0} MPa (calibrated with factor {m.CalibrationFactor:F3})");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur compute modulus pour {m.Name}: {ex.Message}");
                    m.ComputedModulus = m.Modulus_MPa;
                }
            }
        }
    }

    public class MateriauxBeninViewModel : MaterialViewModelBase
    {
        public MateriauxBeninViewModel()
        {
            LoadMaterials();
        }

        internal override async void LoadMaterials()
        {
            try
            {
                var allMaterials = await _dataService.LoadMaterialsAsync("MateriauxBenin");

                // Si une catégorie est sélectionnée, filtrer
                if (!string.IsNullOrEmpty(CurrentCategory))
                {
                    AvailableMaterials = new ObservableCollection<MaterialItem>(
                        _dataService.FilterByCategory(allMaterials, CurrentCategory));
                }
                else
                {
                    AvailableMaterials = new ObservableCollection<MaterialItem>(allMaterials);
                }

                // compute initial computed modulus
                RecomputeComputedModulusForAll();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur chargement matériaux Bénin: {ex.Message}");
                AvailableMaterials = new ObservableCollection<MaterialItem>();
            }
        }
    }

    public class CatalogueSenegalaisViewModel : MaterialViewModelBase
    {
        public CatalogueSenegalaisViewModel()
        {
            LoadMaterials();
        }

        internal override async void LoadMaterials()
        {
            try
            {
                var allMaterials = await _dataService.LoadMaterialsAsync("CatalogueSenegalais");

                if (!string.IsNullOrEmpty(CurrentCategory))
                {
                    AvailableMaterials = new ObservableCollection<MaterialItem>(
                        _dataService.FilterByCategory(allMaterials, CurrentCategory));
                }
                else
                {
                    AvailableMaterials = new ObservableCollection<MaterialItem>(allMaterials);
                }

                RecomputeComputedModulusForAll();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur chargement catalogue sénégalais: {ex.Message}");
                AvailableMaterials = new ObservableCollection<MaterialItem>();
            }
        }
    }

    public class CatalogueFrancaisViewModel : MaterialViewModelBase
    {
        public CatalogueFrancaisViewModel()
        {
            LoadMaterials();
        }

        internal override async void LoadMaterials()
        {
            try
            {
                var allMaterials = await _dataService.LoadMaterialsAsync("CatalogueFrancais1998");

                if (!string.IsNullOrEmpty(CurrentCategory))
                {
                    AvailableMaterials = new ObservableCollection<MaterialItem>(
                        _dataService.FilterByCategory(allMaterials, CurrentCategory));
                }
                else
                {
                    AvailableMaterials = new ObservableCollection<MaterialItem>(allMaterials);
                }

                RecomputeComputedModulusForAll();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur chargement catalogue français: {ex.Message}");
                AvailableMaterials = new ObservableCollection<MaterialItem>();
            }
        }
    }

    public class NFP98ViewModel : MaterialViewModelBase
    {
        public NFP98ViewModel()
        {
            LoadMaterials();
        }

        internal override async void LoadMaterials()
        {
            try
            {
                var allMaterials = await _dataService.LoadMaterialsAsync("NFP98_086_2019");

                if (!string.IsNullOrEmpty(CurrentCategory))
                {
                    var filteredMaterials = _dataService.FilterByCategory(allMaterials, CurrentCategory).ToList();
                    // Calibrate materials at Alizé reference conditions T=15°C, F=10Hz
                    _dataService.CalibrateMaterials(filteredMaterials, referenceTemperature: 15, referenceFrequency: 10);
                    AvailableMaterials = new ObservableCollection<MaterialItem>(filteredMaterials);
                }
                else
                {
                    // Calibrate all materials at Alizé reference conditions T=15°C, F=10Hz
                    _dataService.CalibrateMaterials(allMaterials, referenceTemperature: 15, referenceFrequency: 10);
                    AvailableMaterials = new ObservableCollection<MaterialItem>(allMaterials);
                }

                // Initialize with reference values and compute initial modulus
                SelectedTemperature = 15;  // start at Alizé reference T
                SelectedFrequence = 10;    // start at Alizé reference F
                RecomputeComputedModulusForAll();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur chargement NFP 98-086: {ex.Message}");
                AvailableMaterials = new ObservableCollection<MaterialItem>();
            }
        }
    }

    public class MateriauxUserViewModel : MaterialViewModelBase
    {
        public MateriauxUserViewModel()
        {
            LoadMaterials();
        }

        internal override async void LoadMaterials()
        {
            try
            {
                var allMaterials = await _dataService.LoadMaterialsAsync("MateriauxUser");

                if (!string.IsNullOrEmpty(CurrentCategory))
                {
                    AvailableMaterials = new ObservableCollection<MaterialItem>(
                        _dataService.FilterByCategory(allMaterials, CurrentCategory));
                }
                else
                {
                    AvailableMaterials = new ObservableCollection<MaterialItem>(allMaterials);
                }

                RecomputeComputedModulusForAll();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur chargement matériaux utilisateur: {ex.Message}");
                AvailableMaterials = new ObservableCollection<MaterialItem>();
            }
        }
    }
}
