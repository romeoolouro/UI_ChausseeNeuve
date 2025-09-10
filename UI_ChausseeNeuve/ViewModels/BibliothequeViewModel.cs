using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using ChausseeNeuve.Domain.Models;
using UI_ChausseeNeuve.Services;

namespace UI_ChausseeNeuve.ViewModels
{
    /// <summary>
    /// ViewModel pour la gestion de la bibliothèque des matériaux
    /// VERSION ULTRA-SÉCURISÉE pour éviter les plantages
    /// </summary>
    public class BibliothequeViewModel : INotifyPropertyChanged
    {
        // Événement pour les notifications toast
        public event Action<string, ToastType>? ToastRequested;

        #region Champs privés

        private string _selectedLibrary = "";
        private string _selectedCategory = "";
        private bool _canValidateSelection = false;
        private object? _currentMaterialViewModel;
        private MaterialItem? _selectedMaterial;
        private bool _isInitialized = false;

        // Visibilité des conteneurs (simplifié)
        private Visibility _accueilVisibility = Visibility.Visible;
        private Visibility _materialSelectionVisibility = Visibility.Collapsed;

        // État des boutons de bibliothèque
        private bool _isMateriauxBeninEnabled = true;
        private bool _isCatalogueSenegalaisEnabled = true;
        private bool _isCatalogueFrancaisEnabled = true;
        private bool _isNFP98086Enabled = true;
        private bool _isMateriauxUserEnabled = true;

        // État de sélection des bibliothèques
        private bool _isMateriauxBeninSelected = false;
        private bool _isCatalogueSenegalaisSelected = false;
        private bool _isCatalogueFrancaisSelected = false;
        private bool _isNFP98086Selected = false;
        private bool _isMateriauxUserSelected = false;

        // État des boutons de catégorie
        private bool _isMBEnabled = false;
        private bool _isMTLHEnabled = false;
        private bool _isBetonEnabled = false;
        private bool _isSolGNTEnabled = false;

        // État de sélection des catégories
        private bool _isMBSelected = false;
        private bool _isMTLHSelected = false;
        private bool _isBetonSelected = false;
        private bool _isSolGNTSelected = false;

        #endregion

        #region Constructeur

        public BibliothequeViewModel()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("BibliothequeViewModel: Début initialisation SÉCURISÉE");

                // Initialiser les commandes de manière sécurisée
                InitializeCommands();

                // État initial : écran d'accueil visible
                UpdateButtonStates();

                // Marquer comme initialisé
                _isInitialized = true;

                System.Diagnostics.Debug.WriteLine("BibliothequeViewModel: Initialisation terminée avec succès");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"BibliothequeViewModel: ERREUR CRITIQUE: {ex}");
                
                // Fallback ultra-sécurisé
                InitializeMinimal();
            }
        }

        private void InitializeCommands()
        {
            try
            {
                SelectLibraryCommand = new RelayCommand<string>(library => SafeExecute(() => SelectLibrary(library)));
                SelectCategoryCommand = new RelayCommand<string>(category => SafeExecute(() => SelectCategory(category)));
                ValidateSelectionCommand = new RelayCommand(() => SafeExecute(ValidateSelection), () => SafeCanExecute(() => CanValidateSelection));
                CloseCommand = new RelayCommand(() => SafeExecute(Close));

                System.Diagnostics.Debug.WriteLine("BibliothequeViewModel: Commandes initialisées avec succès");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"BibliothequeViewModel: Erreur initialisation commandes: {ex.Message}");
                
                // Commandes de fallback
                SelectLibraryCommand = new RelayCommand<string>(_ => { });
                SelectCategoryCommand = new RelayCommand<string>(_ => { });
                ValidateSelectionCommand = new RelayCommand(() => { }, () => false);
                CloseCommand = new RelayCommand(() => { });
            }
        }

        private void InitializeMinimal()
        {
            try
            {
                SelectLibraryCommand = new RelayCommand<string>(_ => { });
                SelectCategoryCommand = new RelayCommand<string>(_ => { });
                ValidateSelectionCommand = new RelayCommand(() => { }, () => false);
                CloseCommand = new RelayCommand(() => { });
                _isInitialized = true;
                
                System.Diagnostics.Debug.WriteLine("BibliothequeViewModel: Initialisation minimale terminée");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"BibliothequeViewModel: Erreur initialisation minimale: {ex.Message}");
            }
        }

        private void SafeExecute(Action action)
        {
            try
            {
                if (!_isInitialized) return;
                action?.Invoke();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"BibliothequeViewModel: Erreur exécution sécurisée: {ex.Message}");
            }
        }

        private bool SafeCanExecute(Func<bool> canExecuteFunc)
        {
            try
            {
                if (!_isInitialized) return false;
                return canExecuteFunc?.Invoke() ?? false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"BibliothequeViewModel: Erreur CanExecute sécurisé: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Propriétés

        // Exposé pour les bindings (MaterialSelectionControl)
        public string SelectedCategory
        {
            get => _selectedCategory;
            private set
            {
                if (_selectedCategory != value)
                {
                    _selectedCategory = value;
                    SafePropertyChanged();
                    SafePropertyChanged(nameof(BreadcrumbText));
                    SafePropertyChanged(nameof(ShowBreadcrumb));
                }
            }
        }

        public string SelectedLibrary
        {
            get => _selectedLibrary;
            private set
            {
                if (_selectedLibrary != value)
                {
                    _selectedLibrary = value;
                    SafePropertyChanged();
                    SafePropertyChanged(nameof(BreadcrumbText));
                    SafePropertyChanged(nameof(ShowBreadcrumb));
                }
            }
        }

        // Propriété pour le contrôle générique
        public object? CurrentMaterialViewModel
        {
            get => _currentMaterialViewModel;
            set
            {
                _currentMaterialViewModel = value;
                SafePropertyChanged();
            }
        }

        public MaterialItem? SelectedMaterial
        {
            get => _selectedMaterial;
            set
            {
                _selectedMaterial = value;
                CanValidateSelection = value != null;
                SafePropertyChanged();
            }
        }

        public bool CanValidateSelection
        {
            get => _canValidateSelection;
            set
            {
                _canValidateSelection = value;
                SafePropertyChanged();
                ValidateSelectionCommand?.RaiseCanExecuteChanged();
            }
        }

        public string BreadcrumbText
        {
            get
            {
                try
                {
                    if (string.IsNullOrEmpty(_selectedLibrary))
                        return "";

                    string result = _selectedLibrary;
                    if (!string.IsNullOrEmpty(_selectedCategory))
                        result += $" > {_selectedCategory}";

                    return result;
                }
                catch
                {
                    return "";
                }
            }
        }

        public bool ShowBreadcrumb => !string.IsNullOrEmpty(BreadcrumbText);

        // Visibilité des conteneurs
        public Visibility AccueilVisibility
        {
            get => _accueilVisibility;
            set { _accueilVisibility = value; SafePropertyChanged(); }
        }

        public Visibility MaterialSelectionVisibility
        {
            get => _materialSelectionVisibility;
            set { _materialSelectionVisibility = value; SafePropertyChanged(); }
        }

        // État des boutons de bibliothèque
        public bool IsMateriauxBeninEnabled
        {
            get => _isMateriauxBeninEnabled;
            set { _isMateriauxBeninEnabled = value; SafePropertyChanged(); }
        }

        public bool IsCatalogueSenegalaisEnabled
        {
            get => _isCatalogueSenegalaisEnabled;
            set { _isCatalogueSenegalaisEnabled = value; SafePropertyChanged(); }
        }

        public bool IsCatalogueFrancaisEnabled
        {
            get => _isCatalogueFrancaisEnabled;
            set { _isCatalogueFrancaisEnabled = value; SafePropertyChanged(); }
        }

        public bool IsNFP98086Enabled
        {
            get => _isNFP98086Enabled;
            set { _isNFP98086Enabled = value; SafePropertyChanged(); }
        }

        public bool IsMateriauxUserEnabled
        {
            get => _isMateriauxUserEnabled;
            set { _isMateriauxUserEnabled = value; SafePropertyChanged(); }
        }

        // État de sélection des bibliothèques
        public bool IsMateriauxBeninSelected
        {
            get => _isMateriauxBeninSelected;
            set { _isMateriauxBeninSelected = value; SafePropertyChanged(); }
        }

        public bool IsCatalogueSenegalaisSelected
        {
            get => _isCatalogueSenegalaisSelected;
            set { _isCatalogueSenegalaisSelected = value; SafePropertyChanged(); }
        }

        public bool IsCatalogueFrancaisSelected
        {
            get => _isCatalogueFrancaisSelected;
            set { _isCatalogueFrancaisSelected = value; SafePropertyChanged(); }
        }

        public bool IsNFP98086Selected
        {
            get => _isNFP98086Selected;
            set { _isNFP98086Selected = value; SafePropertyChanged(); }
        }

        public bool IsMateriauxUserSelected
        {
            get => _isMateriauxUserSelected;
            set { _isMateriauxUserSelected = value; SafePropertyChanged(); }
        }

        // État des boutons de catégorie
        public bool IsMBEnabled
        {
            get => _isMBEnabled;
            set { _isMBEnabled = value; SafePropertyChanged(); }
        }

        public bool IsMTLHEnabled
        {
            get => _isMTLHEnabled;
            set { _isMTLHEnabled = value; SafePropertyChanged(); }
        }

        public bool IsBetonEnabled
        {
            get => _isBetonEnabled;
            set { _isBetonEnabled = value; SafePropertyChanged(); }
        }

        public bool IsSolGNTEnabled
        {
            get => _isSolGNTEnabled;
            set { _isSolGNTEnabled = value; SafePropertyChanged(); }
        }

        // État de sélection des catégories
        public bool IsMBSelected
        {
            get => _isMBSelected;
            set { _isMBSelected = value; SafePropertyChanged(); }
        }

        public bool IsMTLHSelected
        {
            get => _isMTLHSelected;
            set { _isMTLHSelected = value; SafePropertyChanged(); }
        }

        public bool IsBetonSelected
        {
            get => _isBetonSelected;
            set { _isBetonSelected = value; SafePropertyChanged(); }
        }

        public bool IsSolGNTSelected
        {
            get => _isSolGNTSelected;
            set { _isSolGNTSelected = value; SafePropertyChanged(); }
        }

        #endregion

        #region Commandes

        public RelayCommand<string> SelectLibraryCommand { get; private set; }
        public RelayCommand<string> SelectCategoryCommand { get; private set; }
        public RelayCommand ValidateSelectionCommand { get; private set; }
        public RelayCommand CloseCommand { get; private set; }

        #endregion

        #region Méthodes des commandes

        private void SelectLibrary(string? libraryName)
        {
            try
            {
                if (string.IsNullOrEmpty(libraryName)) return;

                System.Diagnostics.Debug.WriteLine($"BibliothequeViewModel: Sélection bibliothèque {libraryName}");

                SelectedLibrary = libraryName;

                // Réinitialiser les sélections
                ResetLibrarySelections();
                ResetCategorySelections();

                // Marquer la bibliothèque comme sélectionnée
                SetLibrarySelected(libraryName, true);

                // Activer les catégories pour cette bibliothèque
                EnableCategoriesForLibrary(libraryName);

                // Créer le VM pour cette bibliothèque (sans catégorie pour l'instant)
                CurrentMaterialViewModel = CreateMaterialVM(libraryName);

                // Mettre à jour l'interface
                UpdateBreadcrumb();
                UpdateButtonStates();

                System.Diagnostics.Debug.WriteLine($"BibliothequeViewModel: Bibliothèque {libraryName} sélectionnée avec succès");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"BibliothequeViewModel: Erreur sélection bibliothèque: {ex.Message}");
            }
        }

        private void SelectCategory(string? categoryName)
        {
            try
            {
                if (string.IsNullOrEmpty(categoryName)) return;

                System.Diagnostics.Debug.WriteLine($"BibliothequeViewModel: Sélection catégorie {categoryName}");

                SelectedCategory = categoryName;

                // Réinitialiser les sélections de catégorie
                ResetCategorySelections();

                // Marquer la catégorie comme sélectionnée
                SetCategorySelected(categoryName, true);

                // Charger réellement les matériaux
                LoadMaterialsForCategory(categoryName);

                // Mettre à jour l'interface
                UpdateBreadcrumb();
                UpdateVisibility();

                System.Diagnostics.Debug.WriteLine($"BibliothequeViewModel: Catégorie {categoryName} sélectionnée avec succès");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"BibliothequeViewModel: Erreur sélection catégorie: {ex.Message}");
            }
        }

        private void ValidateSelection()
        {
            try
            {
                if (SelectedMaterial == null) return;

                System.Diagnostics.Debug.WriteLine($"BibliothequeViewModel: Validation du matériau {SelectedMaterial.Name}");

                // TODO: Implémenter la logique de validation réelle
                ToastRequested?.Invoke($"Matériau {SelectedMaterial.Name} sélectionné", ToastType.Success);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"BibliothequeViewModel: Erreur validation: {ex.Message}");
                ToastRequested?.Invoke("Erreur lors de la validation", ToastType.Error);
            }
        }

        private void Close()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("BibliothequeViewModel: Fermeture de la bibliothèque");
                
                // TODO: Implémenter la logique de fermeture
                var window = Application.Current?.MainWindow;
                // Fermer ou masquer selon le contexte
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"BibliothequeViewModel: Erreur fermeture: {ex.Message}");
            }
        }

        #endregion

        #region Méthodes privées

        private void ResetLibrarySelections()
        {
            IsMateriauxBeninSelected = false;
            IsCatalogueSenegalaisSelected = false;
            IsCatalogueFrancaisSelected = false;
            IsNFP98086Selected = false;
            IsMateriauxUserSelected = false;
        }

        private void ResetCategorySelections()
        {
            IsMBSelected = false;
            IsMTLHSelected = false;
            IsBetonSelected = false;
            IsSolGNTSelected = false;
        }

        private void SetLibrarySelected(string libraryName, bool selected)
        {
            switch (libraryName)
            {
                case "MateriauxBenin":
                    IsMateriauxBeninSelected = selected;
                    break;
                case "CatalogueSenegalais":
                    IsCatalogueSenegalaisSelected = selected;
                    break;
                case "CatalogueFrancais1998":
                    IsCatalogueFrancaisSelected = selected;
                    break;
                case "NFP98_086_2019":
                    IsNFP98086Selected = selected;
                    break;
                case "MateriauxUser":
                    IsMateriauxUserSelected = selected;
                    break;
            }
        }

        private void SetCategorySelected(string categoryName, bool selected)
        {
            switch (categoryName)
            {
                case "MB":
                    IsMBSelected = selected;
                    break;
                case "MTLH":
                    IsMTLHSelected = selected;
                    break;
                case "Beton":
                    IsBetonSelected = selected;
                    break;
                case "Sol_GNT":
                    IsSolGNTSelected = selected;
                    break;
            }
        }

        private void EnableCategoriesForLibrary(string libraryName)
        {
            // Réinitialiser toutes les catégories
            IsMBEnabled = false;
            IsMTLHEnabled = false;
            IsBetonEnabled = false;
            IsSolGNTEnabled = false;

            // Activer selon la bibliothèque (logique simplifiée pour test)
            switch (libraryName)
            {
                case "MateriauxBenin":
                case "CatalogueSenegalais":
                case "CatalogueFrancais1998":
                case "NFP98_086_2019":
                    IsMBEnabled = true;
                    IsMTLHEnabled = true;
                    IsBetonEnabled = true;
                    IsSolGNTEnabled = true;
                    break;
                case "MateriauxUser":
                    IsMBEnabled = true;
                    IsMTLHEnabled = true;
                    break;
            }
        }

        private MaterialViewModelBase CreateMaterialVM(string libraryName)
        {
            return libraryName switch
            {
                "MateriauxBenin" => new MateriauxBeninViewModel(),
                "CatalogueSenegalais" => new CatalogueSenegalaisViewModel(),
                "CatalogueFrancais1998" => new CatalogueFrancaisViewModel(),
                "NFP98_086_2019" => new NFP98ViewModel(),
                "MateriauxUser" => new MateriauxUserViewModel(),
                _ => new MateriauxUserViewModel()
            };
        }

        private void LoadMaterialsForCategory(string categoryName)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"BibliothequeViewModel: Chargement des matériaux pour {categoryName}");

                // S'assurer qu'un VM existe pour la bibliothèque courante
                if (CurrentMaterialViewModel is not MaterialViewModelBase vm)
                {
                    vm = CreateMaterialVM(_selectedLibrary);
                    CurrentMaterialViewModel = vm;
                }

                // Définir la catégorie -> déclenche le chargement via setter
                vm.SetCategory(categoryName);

                // Afficher la zone sélection
                AccueilVisibility = Visibility.Collapsed;
                MaterialSelectionVisibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"BibliothequeViewModel: Erreur chargement matériaux: {ex.Message}");
            }
        }

        private void UpdateBreadcrumb()
        {
            SafePropertyChanged(nameof(BreadcrumbText));
            SafePropertyChanged(nameof(ShowBreadcrumb));
        }

        private void UpdateVisibility()
        {
            if (!string.IsNullOrEmpty(_selectedLibrary) && !string.IsNullOrEmpty(_selectedCategory))
            {
                AccueilVisibility = Visibility.Collapsed;
                MaterialSelectionVisibility = Visibility.Visible;
            }
            else
            {
                AccueilVisibility = Visibility.Visible;
                MaterialSelectionVisibility = Visibility.Collapsed;
            }
        }

        private void UpdateButtonStates()
        {
            // Placeholder pour logique future
        }

        private void SafePropertyChanged([CallerMemberName] string? propertyName = null)
        {
            try
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"BibliothequeViewModel: Erreur PropertyChanged pour {propertyName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Méthode appelée quand un matériau est sélectionné
        /// </summary>
        public void OnMaterialSelected(MaterialItem? materialItem)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"BibliothequeViewModel: Matériau sélectionné: {materialItem?.Name}");
                
                SelectedMaterial = materialItem;
                CanValidateSelection = materialItem != null;
                
                if (materialItem != null)
                {
                    ToastRequested?.Invoke($"Matériau sélectionné: {materialItem.Name}", ToastType.Info);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"BibliothequeViewModel: Erreur OnMaterialSelected: {ex.Message}");
            }
        }

        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        #endregion
    }
}
