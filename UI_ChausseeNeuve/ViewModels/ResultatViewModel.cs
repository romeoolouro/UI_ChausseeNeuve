using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Linq;
using ChausseeNeuve.Domain.Models;

namespace UI_ChausseeNeuve.ViewModels
{
    /// <summary>
    /// ViewModel pour l'affichage des résultats de calcul de la chaussée
    /// </summary>
    public class ResultatViewModel : INotifyPropertyChanged
    {
        #region Champs privés
        private bool _isCalculationInProgress;
        private string _calculationDuration = "0 sec";
        private bool _isStructureValid;
        private ObservableCollection<ResultatCouche> _resultats;
        #endregion

        #region Constructeur
        public ResultatViewModel()
        {
            _resultats = new ObservableCollection<ResultatCouche>();

            // Initialiser les commandes
            CalculateStructureCommand = new RelayCommand(CalculateStructure, () => !IsCalculationInProgress);

            LoadSampleData(); // Pour démonstration
        }
        #endregion

        #region Propriétés publiques

        /// <summary>
        /// Indique si un calcul est en cours
        /// </summary>
        public bool IsCalculationInProgress
        {
            get => _isCalculationInProgress;
            set
            {
                _isCalculationInProgress = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Durée du dernier calcul
        /// </summary>
        public string CalculationDuration
        {
            get => _calculationDuration;
            set
            {
                _calculationDuration = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Indique si la structure calculée est valide
        /// </summary>
        public bool IsStructureValid
        {
            get => _isStructureValid;
            set
            {
                _isStructureValid = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ValidationMessage));
                OnPropertyChanged(nameof(ValidationColor));
            }
        }

        /// <summary>
        /// Message de validation affiché à l'utilisateur
        /// </summary>
        public string ValidationMessage => IsStructureValid
            ? "✓ Structure validée - Tous les critères sont respectés"
            : "⚠ Structure non validée - Certains critères ne sont pas respectés";

        /// <summary>
        /// Couleur du message de validation
        /// </summary>
        public string ValidationColor => IsStructureValid ? "#28a745" : "#dc3545";

        /// <summary>
        /// Collection des résultats par couche
        /// </summary>
        public ObservableCollection<ResultatCouche> Resultats
        {
            get => _resultats;
            set
            {
                _resultats = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Commandes

        /// <summary>
        /// Commande pour lancer le calcul de la structure
        /// </summary>
        public RelayCommand CalculateStructureCommand { get; }

        #endregion

        #region Méthodes publiques

        /// <summary>
        /// Lance le calcul de la structure
        /// PLACEHOLDER: Cette méthode devra être implémentée avec la vraie logique de calcul
        /// </summary>
        public void CalculateStructure()
        {
            // TODO: Implémenter la vraie logique de calcul basée sur:
            // - La structure de chaussée définie (AppState.CurrentProject.PavementStructure)
            // - Les charges appliquées (AppState.CurrentProject.ChargeReference)
            // - Les matériaux sélectionnés dans la bibliothèque

            IsCalculationInProgress = true;

            // Simulation d'un calcul asynchrone
            var startTime = DateTime.Now;

            // PLACEHOLDER: Remplacer par le vrai calcul
            SimulateCalculation();

            var duration = DateTime.Now - startTime;
            CalculationDuration = FormatDuration(duration.TotalSeconds);

            IsCalculationInProgress = false;
        }

        #endregion

        #region Méthodes privées

        /// <summary>
        /// Charge des données d'exemple pour la démonstration
        /// </summary>
        private void LoadSampleData()
        {
            // Données d'exemple pour démonstration
            Resultats.Add(new ResultatCouche
            {
                Interface = "Surface",
                Materiau = "BBSG",
                NiveauSup = 0,
                NiveauInf = 6,
                Module = 5400,
                CoefficientPoisson = 0.35,
                SigmaTSup = 0.15,
                SigmaTInf = 0.12,
                EpsilonTSup = 28.5,
                EpsilonTInf = 22.1,
                SigmaZ = -0.85,
                EpsilonZ = -157.4,
                ValeurAdmissible = 145.2,
                EstValide = true
            });

            Resultats.Add(new ResultatCouche
            {
                Interface = "Fondation",
                Materiau = "GNT",
                NiveauSup = 6,
                NiveauInf = 30,
                Module = 280,
                CoefficientPoisson = 0.35,
                SigmaTSup = 0.08,
                SigmaTInf = 0.06,
                EpsilonTSup = 18.2,
                EpsilonTInf = 15.8,
                SigmaZ = -0.62,
                EpsilonZ = -221.4,
                ValeurAdmissible = 98.7,
                EstValide = true
            });

            // Définir la validité globale
            IsStructureValid = Resultats.All(r => r.EstValide);
        }

        /// <summary>
        /// Simule un calcul (PLACEHOLDER)
        /// </summary>
        private void SimulateCalculation()
        {
            // PLACEHOLDER: Cette méthode simule un calcul
            // À remplacer par la vraie logique de calcul qui devra:
            // 1. Récupérer les données de structure depuis AppState
            // 2. Appliquer les formules de mécanique des chaussées
            // 3. Calculer les contraintes et déformations
            // 4. Comparer avec les valeurs admissibles
            // 5. Déterminer la validité de chaque couche

            System.Threading.Thread.Sleep(500); // Simulation d'un calcul qui prend du temps

            // Mise à jour des résultats avec des valeurs calculées
            foreach (var resultat in Resultats)
            {
                // PLACEHOLDER: Calculs réels à implémenter
                resultat.SigmaTSup *= 1.1; // Exemple de modification
                resultat.EstValide = resultat.SigmaTSup < resultat.ValeurAdmissible;
            }

            IsStructureValid = Resultats.All(r => r.EstValide);
        }

        /// <summary>
        /// Formate la durée d'un calcul
        /// </summary>
        private string FormatDuration(double seconds)
        {
            if (seconds < 60)
                return $"Durée : {seconds:F2} sec";

            int minutes = (int)(seconds / 60);
            seconds %= 60;
            return $"Durée : {minutes} min {seconds:F2} sec";
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
    /// Modèle de données pour un résultat de calcul par couche
    /// </summary>
    public class ResultatCouche : INotifyPropertyChanged
    {
        private string _interface = "";
        private string _materiau = "";
        private double _niveauSup;
        private double _niveauInf;
        private double _module;
        private double _coefficientPoisson;
        private double _sigmaTSup;
        private double _sigmaTInf;
        private double _epsilonTSup;
        private double _epsilonTInf;
        private double _sigmaZ;
        private double _epsilonZ;
        private double _valeurAdmissible;
        private bool _estValide;

        /// <summary>Interface de la couche (Surface, Fondation, etc.)</summary>
        public string Interface
        {
            get => _interface;
            set { _interface = value; OnPropertyChanged(); }
        }

        /// <summary>Type de matériau (BBSG, GNT, etc.)</summary>
        public string Materiau
        {
            get => _materiau;
            set { _materiau = value; OnPropertyChanged(); }
        }

        /// <summary>Niveau supérieur en cm</summary>
        public double NiveauSup
        {
            get => _niveauSup;
            set { _niveauSup = value; OnPropertyChanged(); }
        }

        /// <summary>Niveau inférieur en cm</summary>
        public double NiveauInf
        {
            get => _niveauInf;
            set { _niveauInf = value; OnPropertyChanged(); }
        }

        /// <summary>Module d'élasticité en MPa</summary>
        public double Module
        {
            get => _module;
            set { _module = value; OnPropertyChanged(); }
        }

        /// <summary>Coefficient de Poisson</summary>
        public double CoefficientPoisson
        {
            get => _coefficientPoisson;
            set { _coefficientPoisson = value; OnPropertyChanged(); }
        }

        /// <summary>Contrainte horizontale supérieure en MPa</summary>
        public double SigmaTSup
        {
            get => _sigmaTSup;
            set { _sigmaTSup = value; OnPropertyChanged(); }
        }

        /// <summary>Contrainte horizontale inférieure en MPa</summary>
        public double SigmaTInf
        {
            get => _sigmaTInf;
            set { _sigmaTInf = value; OnPropertyChanged(); }
        }

        /// <summary>Déformation horizontale supérieure en micro-déformation</summary>
        public double EpsilonTSup
        {
            get => _epsilonTSup;
            set { _epsilonTSup = value; OnPropertyChanged(); }
        }

        /// <summary>Déformation horizontale inférieure en micro-déformation</summary>
        public double EpsilonTInf
        {
            get => _epsilonTInf;
            set { _epsilonTInf = value; OnPropertyChanged(); }
        }

        /// <summary>Contrainte verticale en MPa</summary>
        public double SigmaZ
        {
            get => _sigmaZ;
            set { _sigmaZ = value; OnPropertyChanged(); }
        }

        /// <summary>Déformation verticale en micro-déformation</summary>
        public double EpsilonZ
        {
            get => _epsilonZ;
            set { _epsilonZ = value; OnPropertyChanged(); }
        }

        /// <summary>Valeur admissible pour le critère sélectionné</summary>
        public double ValeurAdmissible
        {
            get => _valeurAdmissible;
            set { _valeurAdmissible = value; OnPropertyChanged(); }
        }

        /// <summary>Indique si cette couche respecte les critères</summary>
        public bool EstValide
        {
            get => _estValide;
            set { _estValide = value; OnPropertyChanged(); OnPropertyChanged(nameof(CouleurValidation)); }
        }

        /// <summary>Couleur à utiliser pour afficher le statut de validation</summary>
        public string CouleurValidation => EstValide ? "#d4edda" : "#f8d7da";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
