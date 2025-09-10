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
    /// ViewModel pour la gestion des valeurs admissibles et paramètres de calcul
    /// VERSION ULTRA-SÉCURISÉE pour éviter les plantages
    /// </summary>
    public class ValeursAdmissiblesViewModel : INotifyPropertyChanged, IDisposable
    {
        #region Champs privés
        private bool _isCalculationManual = true;
        private double _traficMJA = 450;
        private double _tauxAccroissement = 2.5;
        private int _dureeService = 20;
        private string _typeTauxAccroissement = "géométrique (%)";
        private double _traficCumule;
        private ObservableCollection<ValeurAdmissibleCouche> _valeursAdmissibles;
        private bool _isCalculating;
        private bool _isInitialized = false;
        #endregion

        #region Constructeur
        public ValeursAdmissiblesViewModel()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("ValeursAdmissiblesViewModel: Début initialisation SÉCURISÉE");
                
                // Initialisation sécurisée de la collection
                _valeursAdmissibles = new ObservableCollection<ValeurAdmissibleCouche>();

                // Initialisation sécurisée des commandes
                InitializeCommands();

                // Chargement des données d'exemple (sécurisé)
                LoadSampleDataSafe();

                // Marquer comme initialisé
                _isInitialized = true;

                System.Diagnostics.Debug.WriteLine("ValeursAdmissiblesViewModel: Initialisation terminée avec succès");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ValeursAdmissiblesViewModel: ERREUR CRITIQUE: {ex}");
                
                // Fallback ultra-sécurisé
                InitializeMinimal();
            }
        }

        private void InitializeCommands()
        {
            try
            {
                CalculerTraficCumuleCommand = new RelayCommand(
                    execute: () => SafeExecute(CalculerTraficCumule),
                    canExecute: () => SafeCanExecute(CanCalculerTraficCumule)
                );

                CalculerValeursAdmissiblesCommand = new RelayCommand(
                    execute: () => SafeExecute(CalculerValeursAdmissibles),
                    canExecute: () => SafeCanExecute(CanCalculerValeursAdmissibles)
                );

                AjouterCoucheCommand = new RelayCommand(() => SafeExecute(AjouterCouche));
                SupprimerCoucheCommand = new RelayCommand<ValeurAdmissibleCouche>(couche => SafeExecute(() => SupprimerCouche(couche)));

                System.Diagnostics.Debug.WriteLine("ValeursAdmissiblesViewModel: Commandes initialisées avec succès");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ValeursAdmissiblesViewModel: Erreur initialisation commandes: {ex.Message}");
                
                // Commandes de fallback
                CalculerTraficCumuleCommand = new RelayCommand(() => { }, () => false);
                CalculerValeursAdmissiblesCommand = new RelayCommand(() => { }, () => false);
                AjouterCoucheCommand = new RelayCommand(() => { });
                SupprimerCoucheCommand = new RelayCommand<ValeurAdmissibleCouche>(_ => { });
            }
        }

        private void InitializeMinimal()
        {
            try
            {
                _valeursAdmissibles = new ObservableCollection<ValeurAdmissibleCouche>();
                CalculerTraficCumuleCommand = new RelayCommand(() => { }, () => false);
                CalculerValeursAdmissiblesCommand = new RelayCommand(() => { }, () => false);
                AjouterCoucheCommand = new RelayCommand(() => { });
                SupprimerCoucheCommand = new RelayCommand<ValeurAdmissibleCouche>(_ => { });
                _isInitialized = true;
                
                System.Diagnostics.Debug.WriteLine("ValeursAdmissiblesViewModel: Initialisation minimale terminée");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ValeursAdmissiblesViewModel: Erreur initialisation minimale: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"ValeursAdmissiblesViewModel: Erreur exécution sécurisée: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"ValeursAdmissiblesViewModel: Erreur CanExecute sécurisé: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Propriétés publiques

        public bool IsCalculationManual
        {
            get => _isCalculationManual;
            set
            {
                _isCalculationManual = value;
                SafePropertyChanged();
                SafePropertyChanged(nameof(IsDirectValue));
            }
        }

        public bool IsDirectValue
        {
            get => !_isCalculationManual;
            set
            {
                _isCalculationManual = !value;
                SafePropertyChanged();
                SafePropertyChanged(nameof(IsCalculationManual));
            }
        }

        public double TraficMJA
        {
            get => _traficMJA;
            set
            {
                _traficMJA = value;
                SafePropertyChanged();
                CalculerTraficCumuleCommand?.RaiseCanExecuteChanged();
            }
        }

        public double TauxAccroissement
        {
            get => _tauxAccroissement;
            set
            {
                _tauxAccroissement = value;
                SafePropertyChanged();
                CalculerTraficCumuleCommand?.RaiseCanExecuteChanged();
            }
        }

        public int DureeService
        {
            get => _dureeService;
            set
            {
                _dureeService = value;
                SafePropertyChanged();
                CalculerTraficCumuleCommand?.RaiseCanExecuteChanged();
            }
        }

        public string TypeTauxAccroissement
        {
            get => _typeTauxAccroissement;
            set
            {
                _typeTauxAccroissement = value ?? "géométrique (%)";
                SafePropertyChanged();
                CalculerTraficCumuleCommand?.RaiseCanExecuteChanged();
            }
        }

        public ObservableCollection<string> TypesTauxAccroissement { get; } = new ObservableCollection<string>
        {
            "arithmétique (%)",
            "géométrique (%)"
        };

        public double TraficCumule
        {
            get => _traficCumule;
            set
            {
                _traficCumule = value;
                SafePropertyChanged();
                SafePropertyChanged(nameof(TraficCumuleFormatted));
                CalculerValeursAdmissiblesCommand?.RaiseCanExecuteChanged();
            }
        }

        public string TraficCumuleFormatted 
        {
            get
            {
                try
                {
                    return TraficCumule.ToString("N0");
                }
                catch
                {
                    return "0";
                }
            }
        }

        public ObservableCollection<ValeurAdmissibleCouche> ValeursAdmissibles
        {
            get => _valeursAdmissibles;
            set
            {
                _valeursAdmissibles = value ?? new ObservableCollection<ValeurAdmissibleCouche>();
                SafePropertyChanged();
            }
        }

        public bool IsCalculating
        {
            get => _isCalculating;
            set
            {
                _isCalculating = value;
                SafePropertyChanged();
            }
        }

        #endregion

        #region Commandes

        public RelayCommand CalculerTraficCumuleCommand { get; private set; }
        public RelayCommand CalculerValeursAdmissiblesCommand { get; private set; }
        public RelayCommand AjouterCoucheCommand { get; private set; }
        public RelayCommand<ValeurAdmissibleCouche> SupprimerCoucheCommand { get; private set; }

        #endregion

        #region Méthodes des commandes

        private bool CanCalculerTraficCumule()
        {
            try
            {
                return TraficMJA > 0 && TauxAccroissement >= 0 && DureeService > 0 && !string.IsNullOrEmpty(TypeTauxAccroissement);
            }
            catch
            {
                return false;
            }
        }

        private void CalculerTraficCumule()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Calcul TCPL: MJA={TraficMJA}, Taux={TauxAccroissement}, Durée={DureeService}, Type={TypeTauxAccroissement}");

                if (TypeTauxAccroissement == "arithmétique (%)")
                {
                    TraficCumule = Math.Round(365 * TraficMJA * DureeService *
                        (1 + (DureeService - 1) * (TauxAccroissement * 0.01) / 2), 2);
                }
                else if (TypeTauxAccroissement == "géométrique (%)")
                {
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

                System.Diagnostics.Debug.WriteLine($"Résultat TCPL: {TraficCumule}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur calcul TCPL: {ex.Message}");
                TraficCumule = 0;
            }
        }

        private bool CanCalculerValeursAdmissibles()
        {
            try
            {
                return TraficCumule > 0 && ValeursAdmissibles?.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        private void CalculerValeursAdmissibles()
        {
            try
            {
                IsCalculating = true;
                System.Diagnostics.Debug.WriteLine("Début calcul valeurs admissibles");

                if (ValeursAdmissibles != null)
                {
                    foreach (var couche in ValeursAdmissibles)
                    {
                        try
                        {
                            couche.ValeurAdmissible = CalculerValeurAdmissibleSimple(couche);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Erreur calcul couche {couche.Materiau}: {ex.Message}");
                            couche.ValeurAdmissible = 0;
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine("Fin calcul valeurs admissibles");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur générale calcul valeurs admissibles: {ex.Message}");
            }
            finally
            {
                IsCalculating = false;
            }
        }

        private double CalculerValeurAdmissibleSimple(ValeurAdmissibleCouche couche)
        {
            try
            {
                if (TraficCumule <= 0 || Math.Abs(couche.B) < 0.001) return 0;
                
                double amplitude = couche.Sn;
                if (couche.Critere == "EpsiT" || couche.Critere == "EpsiZ")
                {
                    amplitude *= 1e-6; // Conversion en microdef
                }
                
                double facteurFatigue = Math.Pow(TraficCumule / 1e6, 1.0 / Math.Abs(couche.B));
                return amplitude * facteurFatigue * couche.Kc * couche.Kr * couche.Ks * couche.Ktheta * couche.Kd;
            }
            catch
            {
                return 0;
            }
        }

        private void AjouterCouche()
        {
            try
            {
                var nouvelleCouche = new ValeurAdmissibleCouche
                {
                    Materiau = "Nouveau matériau",
                    Niveau = ValeursAdmissibles.Count + 1,
                    Critere = "EpsiT",
                    AmplitudeValue = 100,
                    Cam = 0,
                    Risque = 10,
                    B = -0.20,
                    Sn = 100,
                    Sh = 120,
                    Kc = 1.0,
                    Kr = 1.0,
                    Ks = 1.0,
                    Ktheta = 1.0,
                    Kd = 1.0
                };
                ValeursAdmissibles.Add(nouvelleCouche);
                System.Diagnostics.Debug.WriteLine($"Couche ajoutée: {nouvelleCouche.Materiau}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur ajout couche: {ex.Message}");
            }
        }

        private void SupprimerCouche(ValeurAdmissibleCouche? couche)
        {
            try
            {
                if (couche != null && ValeursAdmissibles.Contains(couche))
                {
                    ValeursAdmissibles.Remove(couche);
                    System.Diagnostics.Debug.WriteLine($"Couche supprimée: {couche.Materiau}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur suppression couche: {ex.Message}");
            }
        }

        #endregion

        #region Méthodes privées

        private void LoadSampleDataSafe()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Chargement des données d'exemple sécurisées");
                
                ValeursAdmissibles.Clear();
                
                ValeursAdmissibles.Add(new ValeurAdmissibleCouche
                {
                    Materiau = "EB-BBSG 0/10",
                    Niveau = 1,
                    Critere = "EpsiT",
                    AmplitudeValue = 90,
                    Cam = 0,
                    Risque = 10,
                    B = -0.20,
                    Sn = 90,
                    Sh = 100,
                    Kc = 1.3,
                    Kr = 1.0,
                    Ks = 1.0,
                    Ktheta = 1.0,
                    Kd = 1.0
                });

                ValeursAdmissibles.Add(new ValeurAdmissibleCouche
                {
                    Materiau = "MTLH Base",
                    Niveau = 2,
                    Critere = "SigmaT",
                    AmplitudeValue = 0.8,
                    Cam = 0,
                    Risque = 5,
                    B = -0.12,
                    Sn = 0.8,
                    Sh = 1.0,
                    Kc = 1.0,
                    Kr = 1.0,
                    Ks = 1.3,
                    Ktheta = 1.0,
                    Kd = 1.0
                });

                ValeursAdmissibles.Add(new ValeurAdmissibleCouche
                {
                    Materiau = "Plateforme",
                    Niveau = 3,
                    Critere = "EpsiZ",
                    AmplitudeValue = 500,
                    Cam = 0,
                    Risque = 20,
                    B = -0.30,
                    Sn = 500,
                    Sh = 600,
                    Kc = 1.0,
                    Kr = 1.0,
                    Ks = 1.0,
                    Ktheta = 1.0,
                    Kd = 3.0
                });

                System.Diagnostics.Debug.WriteLine($"Données d'exemple chargées: {ValeursAdmissibles.Count} couches");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur chargement données d'exemple: {ex.Message}");
                
                // Fallback ultime
                if (ValeursAdmissibles == null)
                    ValeursAdmissibles = new ObservableCollection<ValeurAdmissibleCouche>();
            }
        }

        private void SafePropertyChanged([CallerMemberName] string? propertyName = null)
        {
            try
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur PropertyChanged pour {propertyName}: {ex.Message}");
            }
        }

        #endregion

        #region IDisposable
        
        public void Dispose()
        {
            try
            {
                // Aucun abonnement à AppState dans cette version sécurisée
                _isInitialized = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur Dispose: {ex.Message}");
            }
        }
        
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        #endregion
    }

    /// <summary>
    /// Modèle de données pour une valeur admissible par couche
    /// VERSION ULTRA-SÉCURISÉE
    /// </summary>
    public class ValeurAdmissibleCouche : INotifyPropertyChanged
    {
        private string _materiau = "";
        private int _niveau;
        private string _critere = "EpsiT";
        private double _sn = 100;
        private double _sh = 120;
        private double _b = -0.20;
        private double _kc = 1.0;
        private double _kr = 1.0;
        private double _ks = 1.0;
        private double _ktheta = 1.0;
        private double _kd = 1.0;
        private double _risque = 10;
        private double _valeurAdmissible;
        private double _amplitudeValue = 100;
        private double _cam;

        public string Materiau
        {
            get => _materiau;
            set { _materiau = value ?? ""; SafePropertyChanged(); }
        }

        public int Niveau
        {
            get => _niveau;
            set { _niveau = value; SafePropertyChanged(); }
        }

        public string Critere
        {
            get => _critere;
            set { _critere = value ?? "EpsiT"; SafePropertyChanged(); SafePropertyChanged(nameof(AmplitudeLabel)); }
        }

        public double Sn
        {
            get => _sn;
            set { _sn = value; SafePropertyChanged(); }
        }

        public double Sh
        {
            get => _sh;
            set { _sh = value; SafePropertyChanged(); }
        }

        public double B
        {
            get => _b;
            set { _b = value; SafePropertyChanged(); }
        }

        public double Kc
        {
            get => _kc;
            set { _kc = value; SafePropertyChanged(); }
        }

        public double Kr
        {
            get => _kr;
            set { _kr = value; SafePropertyChanged(); }
        }

        public double Ks
        {
            get => _ks;
            set { _ks = value; SafePropertyChanged(); }
        }

        public double Ktheta
        {
            get => _ktheta;
            set { _ktheta = value; SafePropertyChanged(); }
        }

        public double Kd
        {
            get => _kd;
            set { _kd = value; SafePropertyChanged(); }
        }

        public double Risque
        {
            get => _risque;
            set { _risque = value; SafePropertyChanged(); }
        }

        public double ValeurAdmissible
        {
            get => _valeurAdmissible;
            set { _valeurAdmissible = value; SafePropertyChanged(); }
        }

        public double AmplitudeValue
        {
            get => _amplitudeValue;
            set { _amplitudeValue = value; SafePropertyChanged(); }
        }

        public string AmplitudeLabel => _critere switch
        {
            "EpsiZ" => "A",
            "SigmaT" => "σ6",
            _ => "ε6"
        };

        public double Cam
        {
            get => _cam;
            set { _cam = value; SafePropertyChanged(); }
        }

        public ObservableCollection<string> CriteresDisponibles { get; } = new ObservableCollection<string>
        {
            "EpsiT", "SigmaT", "EpsiZ"
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        
        private void SafePropertyChanged([CallerMemberName] string? propertyName = null)
        {
            try
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur PropertyChanged pour {propertyName}: {ex.Message}");
            }
        }
    }
}
