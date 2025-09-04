using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace UI_ChausseeNeuve.ViewModels
{
    /// <summary>
    /// ViewModel pour la gestion des valeurs admissibles et paramètres de calcul
    /// </summary>
    public class ValeursAdmissiblesViewModel : INotifyPropertyChanged
    {
        #region Champs privés
        private bool _isCalculationManual = true;
        private double _traficMJA;
        private double _tauxAccroissement;
        private int _dureeService = 20;
        private string _typeTauxAccroissement = "arithmétique (%)";
        private double _traficCumule;
        private ObservableCollection<ValeurAdmissibleCouche> _valeursAdmissibles;
        private bool _isCalculating;
        #endregion

        #region Constructeur
        public ValeursAdmissiblesViewModel()
        {
            _valeursAdmissibles = new ObservableCollection<ValeurAdmissibleCouche>();

            // Initialiser les commandes d'abord (obligatoire)
            CalculerTraficCumuleCommand = new RelayCommand(CalculerTraficCumule, CanCalculerTraficCumule);
            CalculerValeursAdmissiblesCommand = new RelayCommand(CalculerValeursAdmissibles, CanCalculerValeursAdmissibles);
            AjouterCoucheCommand = new RelayCommand(AjouterCouche);
            SupprimerCoucheCommand = new RelayCommand<ValeurAdmissibleCouche>(SupprimerCouche);

            try
            {
                LoadSampleData(); // Pour démonstration
            }
            catch (Exception ex)
            {
                // Log l'erreur pour debugging
                System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement des données dans ValeursAdmissiblesViewModel: {ex.Message}");
            }
        }
        #endregion

        #region Propriétés publiques

        /// <summary>
        /// Mode de calcul manuel (true) ou valeur directe (false)
        /// </summary>
        public bool IsCalculationManual
        {
            get => _isCalculationManual;
            set
            {
                _isCalculationManual = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsDirectValue));
            }
        }

        /// <summary>
        /// Mode valeur directe (inverse du mode manuel)
        /// </summary>
        public bool IsDirectValue
        {
            get => !_isCalculationManual;
            set
            {
                _isCalculationManual = !value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsCalculationManual));
            }
        }

        /// <summary>
        /// Trafic moyen journalier annuel (poids lourds/jour)
        /// </summary>
        public double TraficMJA
        {
            get => _traficMJA;
            set
            {
                _traficMJA = value;
                OnPropertyChanged();
                CalculerTraficCumuleCommand?.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Taux d'accroissement du trafic (%)
        /// </summary>
        public double TauxAccroissement
        {
            get => _tauxAccroissement;
            set
            {
                _tauxAccroissement = value;
                OnPropertyChanged();
                CalculerTraficCumuleCommand?.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Durée de service en années
        /// </summary>
        public int DureeService
        {
            get => _dureeService;
            set
            {
                _dureeService = value;
                OnPropertyChanged();
                CalculerTraficCumuleCommand?.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Type de taux d'accroissement (arithmétique ou géométrique)
        /// </summary>
        public string TypeTauxAccroissement
        {
            get => _typeTauxAccroissement;
            set
            {
                _typeTauxAccroissement = value;
                OnPropertyChanged();
                CalculerTraficCumuleCommand?.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Collection des types de taux d'accroissement disponibles
        /// </summary>
        public ObservableCollection<string> TypesTauxAccroissement { get; } = new ObservableCollection<string>
        {
            "arithmétique (%)",
            "géométrique (%)"
        };

        /// <summary>
        /// Trafic cumulé calculé (poids lourds sur la durée de service)
        /// </summary>
        public double TraficCumule
        {
            get => _traficCumule;
            set
            {
                _traficCumule = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TraficCumuleFormatted));
                CalculerValeursAdmissiblesCommand?.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Trafic cumulé formaté pour l'affichage
        /// </summary>
        public string TraficCumuleFormatted => TraficCumule.ToString("N0");

        /// <summary>
        /// Collection des valeurs admissibles par couche
        /// </summary>
        public ObservableCollection<ValeurAdmissibleCouche> ValeursAdmissibles
        {
            get => _valeursAdmissibles;
            set
            {
                _valeursAdmissibles = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Indique si un calcul est en cours
        /// </summary>
        public bool IsCalculating
        {
            get => _isCalculating;
            set
            {
                _isCalculating = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Commandes

        public RelayCommand CalculerTraficCumuleCommand { get; }
        public RelayCommand CalculerValeursAdmissiblesCommand { get; }
        public RelayCommand AjouterCoucheCommand { get; }
        public RelayCommand<ValeurAdmissibleCouche> SupprimerCoucheCommand { get; }

        #endregion

        #region Méthodes des commandes

        private bool CanCalculerTraficCumule()
        {
            return TraficMJA > 0 && TauxAccroissement >= 0 && DureeService > 0 && !string.IsNullOrEmpty(TypeTauxAccroissement);
        }

        private void CalculerTraficCumule()
        {
            // PLACEHOLDER: Calcul du trafic cumulé selon les formules de dimensionnement des chaussées
            // Formules réelles utilisées dans l'ancienne application:

            if (TypeTauxAccroissement == "arithmétique (%)")
            {
                // Formule arithmétique: TCPL = 365 * MJA * DS * (1 + (DS-1) * TA/200)
                TraficCumule = Math.Round(365 * TraficMJA * DureeService *
                    (1 + (DureeService - 1) * (TauxAccroissement * 0.01) / 2), 2);
            }
            else if (TypeTauxAccroissement == "géométrique (%)")
            {
                // Formule géométrique: TCPL = 365 * MJA * ((1+TA/100)^DS - 1) / (TA/100)
                if (TauxAccroissement > 0)
                {
                    TraficCumule = Math.Round(365 * TraficMJA *
                        (Math.Pow(1 + (TauxAccroissement * 0.01), DureeService) - 1) /
                        (TauxAccroissement * 0.01), 2);
                }
                else
                {
                    TraficCumule = Math.Round(365 * TraficMJA * DureeService, 2);
                }
            }
        }

        private bool CanCalculerValeursAdmissibles()
        {
            return TraficCumule > 0 && ValeursAdmissibles.Count > 0;
        }

        private void CalculerValeursAdmissibles()
        {
            // PLACEHOLDER: Calcul des valeurs admissibles selon les critères de fatigue
            // Cette méthode devra implémenter les formules de:
            // - Fatigue en traction (EpsiT, SigmaT)
            // - Fatigue en compression (EpsiZ)
            // - Prise en compte des coefficients de risque

            IsCalculating = true;

            try
            {
                foreach (var couche in ValeursAdmissibles)
                {
                    // PLACEHOLDER: Calculs réels à implémenter selon le critère choisi
                    switch (couche.Critere)
                    {
                        case "EpsiT":
                            // Calcul fatigue en déformation horizontale
                            couche.ValeurAdmissible = CalculerEpsilonTAdmissible(couche);
                            break;
                        case "SigmaT":
                            // Calcul fatigue en contrainte horizontale
                            couche.ValeurAdmissible = CalculerSigmaTAdmissible(couche);
                            break;
                        case "EpsiZ":
                            // Calcul fatigue en déformation verticale
                            couche.ValeurAdmissible = CalculerEpsilonZAdmissible(couche);
                            break;
                    }
                }
            }
            finally
            {
                IsCalculating = false;
            }
        }

        private void AjouterCouche()
        {
            var nouvelleCouche = new ValeurAdmissibleCouche
            {
                Materiau = "Nouveau matériau",
                Niveau = ValeursAdmissibles.Count + 1,
                Critere = "EpsiT"
            };
            ValeursAdmissibles.Add(nouvelleCouche);
        }

        private void SupprimerCouche(ValeurAdmissibleCouche? couche)
        {
            if (couche != null && ValeursAdmissibles.Contains(couche))
            {
                ValeursAdmissibles.Remove(couche);
            }
        }

        #endregion

        #region Méthodes privées

        /// <summary>
        /// Charge des données d'exemple pour la démonstration
        /// </summary>
        private void LoadSampleData()
        {
            // Paramètres de trafic d'exemple
            TraficMJA = 450;
            TauxAccroissement = 2.5;
            DureeService = 20;
            TypeTauxAccroissement = "géométrique (%)";

            // Couches d'exemple
            ValeursAdmissibles.Add(new ValeurAdmissibleCouche
            {
                Materiau = "BBSG",
                Niveau = 1,
                Critere = "EpsiT",
                Sn = 40,
                Sh = 45,
                B = -0.02,
                Kc = 1.3,
                Kr = 1.5,
                Risque = 10
            });

            ValeursAdmissibles.Add(new ValeurAdmissibleCouche
            {
                Materiau = "GNT",
                Niveau = 2,
                Critere = "EpsiZ",
                Sn = 65,
                Sh = 70,
                B = -0.22,
                Kc = 1.0,
                Kr = 1.2,
                Risque = 5
            });
        }

        /// <summary>
        /// PLACEHOLDER: Calcule la déformation horizontale admissible
        /// </summary>
        private double CalculerEpsilonTAdmissible(ValeurAdmissibleCouche couche)
        {
            // PLACEHOLDER: Formule réelle à implémenter
            // Formule générale: εt,adm = εt,6 * (N/10^6)^(-1/b) * kc * kr
            // où εt,6 est la déformation admissible à 10^6 cycles

            double epsilon6 = couche.Sn * 1e-6; // Conversion en déformation
            double facteurFatigue = Math.Pow(TraficCumule / 1e6, 1.0 / Math.Abs(couche.B));
            return epsilon6 * facteurFatigue * couche.Kc * couche.Kr;
        }

        /// <summary>
        /// PLACEHOLDER: Calcule la contrainte horizontale admissible
        /// </summary>
        private double CalculerSigmaTAdmissible(ValeurAdmissibleCouche couche)
        {
            // PLACEHOLDER: Formule réelle à implémenter
            double sigma6 = couche.Sn; // Contrainte admissible à 10^6 cycles
            double facteurFatigue = Math.Pow(TraficCumule / 1e6, 1.0 / Math.Abs(couche.B));
            return sigma6 * facteurFatigue * couche.Kc * couche.Kr;
        }

        /// <summary>
        /// PLACEHOLDER: Calcule la déformation verticale admissible
        /// </summary>
        private double CalculerEpsilonZAdmissible(ValeurAdmissibleCouche couche)
        {
            // PLACEHOLDER: Formule réelle à implémenter pour l'orniérage
            double epsilonZ6 = couche.Sn * 1e-6;
            double facteurOrnierage = Math.Pow(TraficCumule / 1e6, 1.0 / Math.Abs(couche.B));
            return epsilonZ6 * facteurOrnierage * couche.Kc * couche.Kr;
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

    /// <summary>
    /// Modèle de données pour une valeur admissible par couche
    /// </summary>
    public class ValeurAdmissibleCouche : INotifyPropertyChanged
    {
        private string _materiau = "";
        private int _niveau;
        private string _critere = "EpsiT";
        private double _sn;
        private double _sh;
        private double _b;
        private double _kc = 1.0;
        private double _kr = 1.0;
        private double _risque = 10;
        private double _valeurAdmissible;

        /// <summary>Nom du matériau</summary>
        public string Materiau
        {
            get => _materiau;
            set { _materiau = value; OnPropertyChanged(); }
        }

        /// <summary>Niveau de la couche</summary>
        public int Niveau
        {
            get => _niveau;
            set { _niveau = value; OnPropertyChanged(); }
        }

        /// <summary>Critère de dimensionnement (EpsiT, SigmaT, EpsiZ)</summary>
        public string Critere
        {
            get => _critere;
            set { _critere = value; OnPropertyChanged(); }
        }

        /// <summary>Valeur de référence Sn</summary>
        public double Sn
        {
            get => _sn;
            set { _sn = value; OnPropertyChanged(); }
        }

        /// <summary>Valeur de référence Sh</summary>
        public double Sh
        {
            get => _sh;
            set { _sh = value; OnPropertyChanged(); }
        }

        /// <summary>Pente de la droite de fatigue (b)</summary>
        public double B
        {
            get => _b;
            set { _b = value; OnPropertyChanged(); }
        }

        /// <summary>Coefficient de calage (kc)</summary>
        public double Kc
        {
            get => _kc;
            set { _kc = value; OnPropertyChanged(); }
        }

        /// <summary>Coefficient de risque (kr)</summary>
        public double Kr
        {
            get => _kr;
            set { _kr = value; OnPropertyChanged(); }
        }

        /// <summary>Risque de ruine (%)</summary>
        public double Risque
        {
            get => _risque;
            set { _risque = value; OnPropertyChanged(); }
        }

        /// <summary>Valeur admissible calculée</summary>
        public double ValeurAdmissible
        {
            get => _valeurAdmissible;
            set { _valeurAdmissible = value; OnPropertyChanged(); }
        }

        /// <summary>Critères disponibles</summary>
        public static ObservableCollection<string> CriteresDisponibles { get; } = new ObservableCollection<string>
        {
            "EpsiT", "SigmaT", "EpsiZ"
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
