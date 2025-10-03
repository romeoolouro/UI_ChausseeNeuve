using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using ChausseeNeuve.Domain.Models;
using UI_ChausseeNeuve.Services;
using UI_ChausseeNeuve.Windows;
using UI_ChausseeNeuve.Views;

namespace UI_ChausseeNeuve.ViewModels
{
    public class ChargesViewModel : INotifyPropertyChanged
    {
        // Événement pour les notifications toast
        public event Action<string, ToastType>? ToastRequested;

        private ChargeReference _chargeReference = new ChargeReference();
        private ChargeType _selectedChargeType;

        public ChargesViewModel()
        {
            // Charger les données depuis AppState
            LoadFromAppState();

            // Initialiser les commandes
            SelectChargeTypeCommand = new RelayCommand<ChargeType>(SelectChargeType);
            NavigateToValeursAdmissiblesCommand = new RelayCommand(NavigateToValeursAdmissibles);
        }

        #region Propriétés

        public ChargeReference ChargeReference
        {
            get => _chargeReference;
            set
            {
                if (_chargeReference != value)
                {
                    if (_chargeReference != null)
                    {
                        _chargeReference.PropertyChanged -= ChargeReference_PropertyChanged;
                    }

                    _chargeReference = value;

                    if (_chargeReference != null)
                    {
                        _chargeReference.PropertyChanged += ChargeReference_PropertyChanged;
                    }

                    OnPropertyChanged();
                    UpdateSelectedChargeType();
                }
            }
        }

        public ChargeType SelectedChargeType
        {
            get => _selectedChargeType;
            set
            {
                if (_selectedChargeType != value)
                {
                    _selectedChargeType = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsJumelageFrancaisSelected));
                    OnPropertyChanged(nameof(IsAutreJumelageSelected));
                    OnPropertyChanged(nameof(IsRoueIsoleeSelected));
                }
            }
        }

        // Propriétés pour les bindings des RadioButtons/CheckBoxes
        public bool IsJumelageFrancaisSelected => SelectedChargeType == ChargeType.JumelageFrancais;
        public bool IsAutreJumelageSelected => SelectedChargeType == ChargeType.AutreJumelage;
        public bool IsRoueIsoleeSelected => SelectedChargeType == ChargeType.RoueIsolee;

        // Propriétés pour contrôler l'état des TextBox
        public bool IsRayonEnabled => true;
        public bool IsPressionEnabled => true;
        public bool IsPoidsEnabled => true;
        public bool IsDistanceRouesEnabled => SelectedChargeType != ChargeType.RoueIsolee;
        public bool IsPositionXEnabled => true;
        public bool IsPositionYEnabled => true;

        #endregion

        #region Commandes

        public RelayCommand<ChargeType> SelectChargeTypeCommand { get; }
        public RelayCommand NavigateToValeursAdmissiblesCommand { get; }

        #endregion

        #region Méthodes privées

        private void NavigateToValeursAdmissibles()
        {
            try
            {
                // Rechercher AccueilWindow dans les fenêtres ouvertes
                var mainWindow = Application.Current.Windows.OfType<AccueilWindow>().FirstOrDefault();
                if (mainWindow != null)
                {
                    var navBar = mainWindow.FindName("NavBar") as HoverNavBar;
                    if (navBar != null)
                    {
                        // Trouver et cocher le RadioButton de la section valeurs
                        var grid = navBar.FindName("Root") as Grid;
                        if (grid != null)
                        {
                            foreach (var child in grid.Children.OfType<RadioButton>())
                            {
                                if (child.Tag as string == "valeurs")
                                {
                                    child.IsChecked = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ToastRequested?.Invoke($"Erreur de navigation : {ex.Message}", ToastType.Error);
            }
        }

        private void LoadFromAppState()
        {
            var pavementStructure = AppState.CurrentProject.PavementStructure;

            // Charger ou initialiser la charge de référence
            if (pavementStructure.ChargeReference == null)
            {
                pavementStructure.ChargeReference = new ChargeReference();
            }

            ChargeReference = pavementStructure.ChargeReference;
            SelectedChargeType = ChargeReference.Type;
        }

        private void SelectChargeType(ChargeType chargeType)
        {
            if (ChargeReference.Type != chargeType)
            {
                ChargeReference.Type = chargeType;
                SelectedChargeType = chargeType;

                // Mettre à jour AppState
                UpdateAppState();

                // Notification
                ToastRequested?.Invoke($"Type de charge changé: {GetChargeTypeDescription(chargeType)}", ToastType.Info);
            }
        }

        private void ChargeReference_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Mettre à jour AppState quand une propriété change
            UpdateAppState();

            // Notifications pour les propriétés calculées
            if (e.PropertyName == nameof(ChargeReference.Type))
            {
                OnPropertyChanged(nameof(IsDistanceRouesEnabled));
            }
        }

        private void UpdateAppState()
        {
            var pavementStructure = AppState.CurrentProject.PavementStructure;
            pavementStructure.ChargeReference = ChargeReference;
        }

        private void UpdateSelectedChargeType()
        {
            SelectedChargeType = ChargeReference.Type;
        }

        private string GetChargeTypeDescription(ChargeType type)
        {
            return type switch
            {
                ChargeType.JumelageFrancais => "Jumelage français",
                ChargeType.AutreJumelage => "Autre jumelage",
                ChargeType.RoueIsolee => "Roue isolée",
                _ => "Type inconnu"
            };
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
