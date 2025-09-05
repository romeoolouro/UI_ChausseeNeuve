using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ChausseeNeuve.Domain.Models;
using UI_ChausseeNeuve.Services;
using System.Linq;

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
        private int _selectedTemperature = 15;
        public int SelectedTemperature
        {
            get => _selectedTemperature;
            set { _selectedTemperature = value; OnPropertyChanged(); }
        }
        private int _selectedFrequence = 10;
        public int SelectedFrequence
        {
            get => _selectedFrequence;
            set { _selectedFrequence = value; OnPropertyChanged(); }
        }
        public int[] TemperatureOptions { get; } = new int[] { -10, -9, -8, -7, -6, -5, -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40 };
        public int[] FrequenceOptions { get; } = new int[] { 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };

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
                    AvailableMaterials = new ObservableCollection<MaterialItem>(
                        _dataService.FilterByCategory(allMaterials, CurrentCategory));
                }
                else
                {
                    AvailableMaterials = new ObservableCollection<MaterialItem>(allMaterials);
                }
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
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur chargement matériaux utilisateur: {ex.Message}");
                AvailableMaterials = new ObservableCollection<MaterialItem>();
            }
        }
    }
}
