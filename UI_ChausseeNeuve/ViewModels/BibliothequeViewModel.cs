using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using ChausseeNeuve.Domain.Models;
using UI_ChausseeNeuve.Services;

namespace UI_ChausseeNeuve.ViewModels
{
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
            // Initialiser les commandes
            SelectLibraryCommand = new RelayCommand<string>(SelectLibrary);
            SelectCategoryCommand = new RelayCommand<string>(SelectCategory);
            ValidateSelectionCommand = new RelayCommand(ValidateSelection, () => CanValidateSelection);
            CloseCommand = new RelayCommand(Close);

            // État initial : écran d'accueil visible
            UpdateButtonStates();
        }

        #endregion

        #region Propriétés

        // Propriété pour le contrôle générique
        public object? CurrentMaterialViewModel
        {
            get => _currentMaterialViewModel;
            set
            {
                _currentMaterialViewModel = value;
                OnPropertyChanged();
            }
        }

        public MaterialItem? SelectedMaterial
        {
            get => _selectedMaterial;
            set
            {
                _selectedMaterial = value;
                OnPropertyChanged();
                // Safely notify command that can-execute may have changed
                ValidateSelectionCommand?.RaiseCanExecuteChanged();
            }
        }

        public Visibility AccueilVisibility
        {
            get => _accueilVisibility;
            set { _accueilVisibility = value; OnPropertyChanged(); }
        }

        public Visibility MaterialSelectionVisibility
        {
            get => _materialSelectionVisibility;
            set { _materialSelectionVisibility = value; OnPropertyChanged(); }
        }

        // État des boutons de bibliothèque
        public bool IsMateriauxBeninEnabled
        {
            get => _isMateriauxBeninEnabled;
            set { _isMateriauxBeninEnabled = value; OnPropertyChanged(); }
        }

        public bool IsCatalogueSenegalaisEnabled
        {
            get => _isCatalogueSenegalaisEnabled;
            set { _isCatalogueSenegalaisEnabled = value; OnPropertyChanged(); }
        }

        public bool IsCatalogueFrancaisEnabled
        {
            get => _isCatalogueFrancaisEnabled;
            set { _isCatalogueFrancaisEnabled = value; OnPropertyChanged(); }
        }

        public bool IsNFP98086Enabled
        {
            get => _isNFP98086Enabled;
            set { _isNFP98086Enabled = value; OnPropertyChanged(); }
        }

        public bool IsMateriauxUserEnabled
        {
            get => _isMateriauxUserEnabled;
            set { _isMateriauxUserEnabled = value; OnPropertyChanged(); }
        }

        // Propriétés de sélection des bibliothèques
        public bool IsMateriauxBeninSelected
        {
            get => _isMateriauxBeninSelected;
            set { _isMateriauxBeninSelected = value; OnPropertyChanged(); }
        }

        public bool IsCatalogueSenegalaisSelected
        {
            get => _isCatalogueSenegalaisSelected;
            set { _isCatalogueSenegalaisSelected = value; OnPropertyChanged(); }
        }

        public bool IsCatalogueFrancaisSelected
        {
            get => _isCatalogueFrancaisSelected;
            set { _isCatalogueFrancaisSelected = value; OnPropertyChanged(); }
        }

        public bool IsNFP98086Selected
        {
            get => _isNFP98086Selected;
            set { _isNFP98086Selected = value; OnPropertyChanged(); }
        }

        public bool IsMateriauxUserSelected
        {
            get => _isMateriauxUserSelected;
            set { _isMateriauxUserSelected = value; OnPropertyChanged(); }
        }

        // État des boutons de catégorie
        public bool IsMBEnabled
        {
            get => _isMBEnabled;
            set { _isMBEnabled = value; OnPropertyChanged(); }
        }

        public bool IsMTLHEnabled
        {
            get => _isMTLHEnabled;
            set { _isMTLHEnabled = value; OnPropertyChanged(); }
        }

        public bool IsBetonEnabled
        {
            get => _isBetonEnabled;
            set { _isBetonEnabled = value; OnPropertyChanged(); }
        }

        public bool IsSolGNTEnabled
        {
            get => _isSolGNTEnabled;
            set { _isSolGNTEnabled = value; OnPropertyChanged(); }
        }

        // Propriétés de sélection des catégories
        public bool IsMBSelected
        {
            get => _isMBSelected;
            set { _isMBSelected = value; OnPropertyChanged(); }
        }

        public bool IsMTLHSelected
        {
            get => _isMTLHSelected;
            set { _isMTLHSelected = value; OnPropertyChanged(); }
        }

        public bool IsBetonSelected
        {
            get => _isBetonSelected;
            set { _isBetonSelected = value; OnPropertyChanged(); }
        }

        public bool IsSolGNTSelected
        {
            get => _isSolGNTSelected;
            set { _isSolGNTSelected = value; OnPropertyChanged(); }
        }

        // Propriété pour le fil d'Ariane
        public string BreadcrumbText
        {
            get
            {
                if (string.IsNullOrEmpty(SelectedLibrary))
                    return "";

                var libraryDisplayName = SelectedLibrary switch
                {
                    "MateriauxBenin" => "MATÉRIAUX DU BÉNIN",
                    "CatalogueSenegalais" => "CATALOGUE SÉNÉGALAIS",
                    "CatalogueFrancais1998" => "CATALOGUE FRANÇAIS 1998",
                    "NFP98_086_2019" => "NF P 98-086 2019",
                    "MateriauxUser" => "MATÉRIAUX UTILISATEUR",
                    _ => SelectedLibrary
                };

                var categoryDisplayName = SelectedCategory switch
                {
                    "MB" => "MB",
                    "MTLH" => "MTLH",
                    "Beton" => "BÉTON",
                    "Sol_GNT" => "SOL & GNT",
                    _ => SelectedCategory
                };

                if (string.IsNullOrEmpty(SelectedCategory))
                    return $"Bibliothèque : {libraryDisplayName}";

                return $"Bibliothèque : {libraryDisplayName} • Catégorie : {categoryDisplayName}";
            }
        }

        public bool ShowBreadcrumb => !string.IsNullOrEmpty(SelectedLibrary);

        public string SelectedLibrary
        {
            get => _selectedLibrary;
            set { _selectedLibrary = value; OnPropertyChanged(); }
        }

        public string SelectedCategory
        {
            get => _selectedCategory;
            set { _selectedCategory = value; OnPropertyChanged(); }
        }

        public bool CanValidateSelection
        {
            get => _canValidateSelection;
            set { _canValidateSelection = value; OnPropertyChanged(); }
        }

        #endregion

        #region Commandes

        public RelayCommand<string>? SelectLibraryCommand { get; private set; }
        public RelayCommand<string>? SelectCategoryCommand { get; private set; }
        public RelayCommand? ValidateSelectionCommand { get; private set; }
        public RelayCommand? CloseCommand { get; private set; }

        #endregion

        #region Méthodes de commande

        private void SelectLibrary(string? libraryName)
        {
            if (string.IsNullOrEmpty(libraryName)) return;

            // Reset toutes les sélections de bibliothèque
            IsMateriauxBeninSelected = false;
            IsCatalogueSenegalaisSelected = false;
            IsCatalogueFrancaisSelected = false;
            IsNFP98086Selected = false;
            IsMateriauxUserSelected = false;

            // Activer la bibliothèque sélectionnée
            switch (libraryName)
            {
                case "MateriauxBenin":
                    IsMateriauxBeninSelected = true;
                    break;
                case "CatalogueSenegalais":
                    IsCatalogueSenegalaisSelected = true;
                    break;
                case "CatalogueFrancais1998":
                    IsCatalogueFrancaisSelected = true;
                    break;
                case "NFP98_086_2019":
                    IsNFP98086Selected = true;
                    break;
                case "MateriauxUser":
                    IsMateriauxUserSelected = true;
                    break;
            }

            SelectedLibrary = libraryName;
            SelectedCategory = ""; // Reset category
            SelectedMaterial = null;

            // Reset toutes les sélections de catégorie
            IsMBSelected = false;
            IsMTLHSelected = false;
            IsBetonSelected = false;
            IsSolGNTSelected = false;

            // Masquer l'accueil et afficher la sélection de matériaux
            AccueilVisibility = Visibility.Collapsed;
            MaterialSelectionVisibility = Visibility.Visible;

            // Créer le ViewModel approprié selon la bibliothèque sélectionnée
            CreateMaterialViewModel(libraryName);

            // Activer les boutons de catégorie
            UpdateCategoryButtons(true);

            UpdateButtonStates();

            // Notifier les changements pour le fil d'Ariane
            OnPropertyChanged(nameof(BreadcrumbText));
            OnPropertyChanged(nameof(ShowBreadcrumb));

            ToastRequested?.Invoke($"Bibliothèque {libraryName} sélectionnée", ToastType.Info);
        }

        private void SelectCategory(string? categoryName)
        {
            if (string.IsNullOrEmpty(categoryName)) return;

            // Reset toutes les sélections de catégorie
            IsMBSelected = false;
            IsMTLHSelected = false;
            IsBetonSelected = false;
            IsSolGNTSelected = false;

            // Activer la catégorie sélectionnée
            switch (categoryName)
            {
                case "MB":
                    IsMBSelected = true;
                    break;
                case "MTLH":
                    IsMTLHSelected = true;
                    break;
                case "Beton":
                    IsBetonSelected = true;
                    break;
                case "Sol_GNT":
                    IsSolGNTSelected = true;
                    break;
            }

            SelectedCategory = categoryName;

            // Mettre à jour le ViewModel actuel avec la nouvelle catégorie
            if (CurrentMaterialViewModel is MaterialViewModelBase viewModel)
            {
                viewModel.SetCategory(categoryName);
            }

            UpdateButtonStates();

            // Notifier les changements pour le fil d'Ariane
            OnPropertyChanged(nameof(BreadcrumbText));
            OnPropertyChanged(nameof(ShowBreadcrumb));

            ToastRequested?.Invoke($"Catégorie {categoryName} sélectionnée", ToastType.Info);

            UpdateButtonStates();
            ToastRequested?.Invoke($"Catégorie {categoryName} sélectionnée", ToastType.Info);
        }

        private void ValidateSelection()
        {
            if (SelectedMaterial == null)
            {
                ToastRequested?.Invoke("Veuillez sélectionner un matériau", ToastType.Warning);
                return;
            }

            // Créer un Layer à partir du matériau sélectionné
            var layer = SelectedMaterial.ToLayer(LayerRole.Roulement); // Par défaut, rôle roulement

            // Ici, intégrer avec le système de structure existant
            // Par exemple : AppState.CurrentProject.Structure.Layers.Add(layer);

            ToastRequested?.Invoke($"Matériau validé : {SelectedMaterial.Name} (E={SelectedMaterial.Modulus_MPa} MPa, ν={SelectedMaterial.PoissonRatio})", ToastType.Success);

            // TODO: Fermer la fenêtre ou retourner à l'écran précédent
        }

        private void Close()
        {
            // Logique de fermeture
            ToastRequested?.Invoke("Fermeture de la bibliothèque", ToastType.Info);
        }

        #endregion

        #region Méthodes privées

        private void CreateMaterialViewModel(string libraryName)
        {
            CurrentMaterialViewModel = libraryName switch
            {
                "MateriauxBenin" => new MateriauxBeninViewModel(),
                "CatalogueSenegalais" => new CatalogueSenegalaisViewModel(),
                "CatalogueFrancais1998" => new CatalogueFrancaisViewModel(),
                "NFP98_086_2019" => new NFP98ViewModel(),
                "MateriauxUser" => new MateriauxUserViewModel(),
                _ => null
            };
        }

        private void UpdateMaterialViewModel()
        {
            if (CurrentMaterialViewModel is MaterialViewModelBase viewModel)
            {
                // Cette méthode n'est plus nécessaire car SetCategory fait déjà le travail
                viewModel.SetCategory(SelectedCategory);
            }
        }

        private void UpdateCategoryButtons(bool enabled)
        {
            IsMBEnabled = enabled;
            IsMTLHEnabled = enabled;
            IsBetonEnabled = enabled;
            IsSolGNTEnabled = enabled;
        }

        private void UpdateButtonStates()
        {
            // Désactiver le bouton de la bibliothèque sélectionnée
            IsMateriauxBeninEnabled = SelectedLibrary != "MateriauxBenin";
            IsCatalogueSenegalaisEnabled = SelectedLibrary != "CatalogueSenegalais";
            IsCatalogueFrancaisEnabled = SelectedLibrary != "CatalogueFrancais1998";
            IsNFP98086Enabled = SelectedLibrary != "NFP98_086_2019";
            IsMateriauxUserEnabled = SelectedLibrary != "MateriauxUser";

            // Validation possible seulement si bibliothèque ET catégorie sélectionnées
            CanValidateSelection = !string.IsNullOrEmpty(SelectedLibrary) &&
                                 !string.IsNullOrEmpty(SelectedCategory) &&
                                 SelectedMaterial != null;
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Méthodes publiques

        public void OnMaterialSelected(object material)
        {
            if (material is MaterialItem materialItem)
            {
                SelectedMaterial = materialItem;
                ToastRequested?.Invoke($"Matériau sélectionné : {materialItem.Name}", ToastType.Info);
            }
        }

        #endregion
    }
}
