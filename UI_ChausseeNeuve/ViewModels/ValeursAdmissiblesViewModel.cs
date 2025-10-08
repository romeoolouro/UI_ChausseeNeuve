using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Linq;
using ChausseeNeuve.Domain.Models;
using UI_ChausseeNeuve; // AppState
using System.Collections.Generic;

namespace UI_ChausseeNeuve.ViewModels
{
    /// <summary>
    /// ViewModel pour la gestion des valeurs admissibles et paramètres de calcul
    /// VERSION ULTRA-SÉCURISÉE pour éviter les plantages
    /// </summary>
    public class ValeursAdmissiblesViewModel : INotifyPropertyChanged, IDisposable
    {
        // Constantes par défaut pour εz adm
        private const double DefaultExponentEpsiZ = -0.222; // -1/b affiché dans le tableau
        private const double DefaultA_LowNE = 16000.0;       // si NE <= 250000
        private const double DefaultA_HighNE = 12000.0;      // si NE > 250000
        private const double ThresholdNE = 250000.0;
        // Constante c (impact d’une variation d’épaisseur) en m^-1
        private const double CThicknessSensitivity = 2.0;    // par défaut = 2 m^-1
        private const double BitumeMatchTolerance = 0.15; // 15% d'écart relatif max pour considérer un matériau identique
        private static readonly string[] BitumeLibraries = { "NFP98_086_2019", "CatalogueFrancais1998", "MateriauxBenin", "CatalogueSenegalais" };

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

                // Synchroniser immédiatement avec la structure
                SyncFromStructure();

                // Calculer le trafic cumulé et initialiser les valeurs dès l'ouverture
                CalculerTraficCumule();
                CalculerValeursAdmissibles();

                // S'abonner aux changements de structure
                AppState.StructureChanged += OnStructureChanged;

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

        private void OnStructureChanged()
        {
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(new Action(SyncFromStructure));
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

        #region Auto-CAM NF P98-086 (péri-urbain non autoroutier)
        private enum NFPTrafficBand { T5, T4, T3Minus, T3Plus, T2T1T0 }
        private enum NFPCamMaterialGroup { Bitumineux, TraitesLHBetons, SolsTraites, PlateformeGNT }
        private static NFPTrafficBand GetNfpTrafficBand(double mja)
        {
            if (mja < 25) return NFPTrafficBand.T5;
            if (mja < 50) return NFPTrafficBand.T4;
            if (mja < 85) return NFPTrafficBand.T3Minus;
            if (mja < 150) return NFPTrafficBand.T3Plus;
            return NFPTrafficBand.T2T1T0;
        }
        private static NFPCamMaterialGroup ClassifyLayerGroup(Layer layer)
        {
            return layer.Family switch
            {
                MaterialFamily.BetonBitumineux => NFPCamMaterialGroup.Bitumineux,
                MaterialFamily.MTLH => NFPCamMaterialGroup.TraitesLHBetons,
                MaterialFamily.BetonCiment => NFPCamMaterialGroup.TraitesLHBetons,
                MaterialFamily.GNT => NFPCamMaterialGroup.PlateformeGNT,
                _ => ClassifyByModulus(layer.Modulus_MPa)
            };
        }
        private static NFPCamMaterialGroup ClassifyByModulus(double e)
        {
            if (e >= 12000) return NFPCamMaterialGroup.TraitesLHBetons;
            if (e >= 2500) return NFPCamMaterialGroup.Bitumineux;
            if (e >= 500) return NFPCamMaterialGroup.PlateformeGNT;
            return NFPCamMaterialGroup.SolsTraites;
        }
        private static readonly Dictionary<(NFPCamMaterialGroup, NFPTrafficBand), double> _autoCamTable = new()
        {
            {(NFPCamMaterialGroup.Bitumineux, NFPTrafficBand.T5),0.3}, {(NFPCamMaterialGroup.Bitumineux, NFPTrafficBand.T4),0.3}, {(NFPCamMaterialGroup.Bitumineux, NFPTrafficBand.T3Minus),0.4}, {(NFPCamMaterialGroup.Bitumineux, NFPTrafficBand.T3Plus),0.5}, {(NFPCamMaterialGroup.Bitumineux, NFPTrafficBand.T2T1T0),0.5},
            {(NFPCamMaterialGroup.TraitesLHBetons, NFPTrafficBand.T5),0.4}, {(NFPCamMaterialGroup.TraitesLHBetons, NFPTrafficBand.T4),0.5}, {(NFPCamMaterialGroup.TraitesLHBetons, NFPTrafficBand.T3Minus),0.6}, {(NFPCamMaterialGroup.TraitesLHBetons, NFPTrafficBand.T3Plus),0.6}, {(NFPCamMaterialGroup.TraitesLHBetons, NFPTrafficBand.T2T1T0),0.8},
            {(NFPCamMaterialGroup.SolsTraites, NFPTrafficBand.T5),0.4}, {(NFPCamMaterialGroup.SolsTraites, NFPTrafficBand.T4),0.5}, {(NFPCamMaterialGroup.SolsTraites, NFPTrafficBand.T3Minus),0.7}, {(NFPCamMaterialGroup.SolsTraites, NFPTrafficBand.T3Plus),0.7}, {(NFPCamMaterialGroup.SolsTraites, NFPTrafficBand.T2T1T0),0.8},
            {(NFPCamMaterialGroup.PlateformeGNT, NFPTrafficBand.T5),0.4}, {(NFPCamMaterialGroup.PlateformeGNT, NFPTrafficBand.T4),0.5}, {(NFPCamMaterialGroup.PlateformeGNT, NFPTrafficBand.T3Minus),0.6}, {(NFPCamMaterialGroup.PlateformeGNT, NFPTrafficBand.T3Plus),0.75}, {(NFPCamMaterialGroup.PlateformeGNT, NFPTrafficBand.T2T1T0),1.0}
        };
        private static double ComputeAutoCam(Layer layer, double mja)
        {
            var key = (ClassifyLayerGroup(layer), GetNfpTrafficBand(mja));
            return _autoCamTable.TryGetValue(key, out var v) ? v : 0.5;
        }
        private void RecomputeAutoCamForAll()
        {
            if (!IsAutomaticMode || ValeursAdmissibles == null) return;
            foreach (var c in ValeursAdmissibles)
            {
                if (c.SourceLayer == null) continue;
                // Toujours recalculer en mode automatique (ignorer CamUserSet)
                var auto = ComputeAutoCam(c.SourceLayer, _traficMJA);
                c.SetCamAuto(auto);
                c.Ne = Math.Round(TraficCumule * c.Cam, 0);
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
                // Persist immediately in project for report
                try { if (AppState.CurrentProject != null) AppState.CurrentProject.TraficMJA = _traficMJA; } catch { }
                SafePropertyChanged();
                CalculerTraficCumuleCommand?.RaiseCanExecuteChanged();
                // Recalcul trafic cumulé puis CAM auto (CAM dépend classe de trafic)
                CalculerTraficCumule();
                RecomputeAutoCamForAll();
            }
        }

        public double TauxAccroissement
        {
            get => _tauxAccroissement;
            set
            {
                _tauxAccroissement = value;
                try { if (AppState.CurrentProject != null) AppState.CurrentProject.TauxAccroissement = _tauxAccroissement; } catch { }
                SafePropertyChanged();
                CalculerTraficCumuleCommand?.RaiseCanExecuteChanged();
                // Calcul automatique
                CalculerTraficCumule();
            }
        }

        public int DureeService
        {
            get => _dureeService;
            set
            {
                _dureeService = value;
                try { if (AppState.CurrentProject != null) AppState.CurrentProject.DureeService = _dureeService; } catch { }
                SafePropertyChanged();
                CalculerTraficCumuleCommand?.RaiseCanExecuteChanged();
                // Calcul automatique
                CalculerTraficCumule();
            }
        }

        public string TypeTauxAccroissement
        {
            get => _typeTauxAccroissement;
            set
            {
                _typeTauxAccroissement = value ?? "géométrique (%)";
                try { if (AppState.CurrentProject != null) AppState.CurrentProject.TypeTauxAccroissement = _typeTauxAccroissement; } catch { }
                SafePropertyChanged();
                CalculerTraficCumuleCommand?.RaiseCanExecuteChanged();
                // Calcul automatique
                CalculerTraficCumule();
            }
        }

        public double TraficCumule
        {
            get => _traficCumule;
            set
            {
                _traficCumule = value;
                try { if (AppState.CurrentProject != null) AppState.CurrentProject.TraficCumuleNPL = _traficCumule; } catch { }
                SafePropertyChanged();
                SafePropertyChanged(nameof(TraficCumuleFormatted));
                UpdateNeForAllCouches();
                CalculerValeursAdmissiblesCommand?.RaiseCanExecuteChanged();
            }
        }

        public string TraficCumuleFormatted 
        {
            get
            {
                try { return TraficCumule.ToString("N0"); } catch { return "0"; }
            }
        }

        public ObservableCollection<ValeurAdmissibleCouche> ValeursAdmissibles
        {
            get => _valeursAdmissibles;
            set
            {
                _valeursAdmissibles = value ?? new ObservableCollection<ValeurAdmissibleCouche>();
                SafePropertyChanged();
                SaveToProject();
            }
        }

        private void SaveToProject()
        {
            if (AppState.CurrentProject != null)
            {
                AppState.CurrentProject.ValeursAdmissibles = new ObservableCollection<ValeurAdmissibleCoucheDto>(
                    _valeursAdmissibles.Select(ToDto)
                );
            }
        }

        private void LoadFromProject()
        {
            if (AppState.CurrentProject?.ValeursAdmissibles != null && AppState.CurrentProject.ValeursAdmissibles.Count > 0)
            {
                _valeursAdmissibles = new ObservableCollection<ValeurAdmissibleCouche>(
                    AppState.CurrentProject.ValeursAdmissibles.Select(FromDto)
                );
                foreach (var c in _valeursAdmissibles)
                    HookCoucheForAutoUpdate(c);
                SafePropertyChanged(nameof(ValeursAdmissibles));
            }
        }

        private static ValeurAdmissibleCoucheDto ToDto(ValeurAdmissibleCouche c) => new ValeurAdmissibleCoucheDto
        {
            Materiau = c.Materiau,
            Niveau = c.Niveau,
            Critere = c.Critere,
            Sn = c.Sn,
            Sh = c.Sh,
            B = c.B,
            Kc = c.Kc,
            Kr = c.Kr,
            Ks = c.Ks,
            Ktheta = c.Ktheta,
            Kd = c.Kd,
            Risque = c.Risque,
            Ne = c.Ne,
            Epsilon6 = c.Epsilon6,
            ValeurAdmissible = c.ValeurAdmissible,
            AmplitudeValue = c.AmplitudeValue,
            Sigma6 = c.Sigma6,
            Cam = c.Cam,
            E10C10Hz = c.E10C10Hz,
            Eteq10Hz = c.Eteq10Hz,
            KthetaAuto = c.KthetaAuto
        };

        private static ValeurAdmissibleCouche FromDto(ValeurAdmissibleCoucheDto d) => new ValeurAdmissibleCouche
        {
            Materiau = d.Materiau,
            Niveau = d.Niveau,
            Critere = d.Critere,
            Sn = d.Sn,
            Sh = d.Sh,
            B = d.B,
            Kc = d.Kc,
            Kr = d.Kr,
            Ks = d.Ks,
            Ktheta = d.Ktheta,
            Kd = d.Kd,
            Risque = d.Risque,
            Ne = d.Ne,
            Epsilon6 = d.Epsilon6,
            ValeurAdmissible = d.ValeurAdmissible,
            AmplitudeValue = d.AmplitudeValue,
            Sigma6 = d.Sigma6,
            Cam = d.Cam,
            E10C10Hz = d.E10C10Hz,
            Eteq10Hz = d.Eteq10Hz,
            KthetaAuto = d.KthetaAuto
        };

        public bool IsCalculating
        {
            get => _isCalculating;
            set { _isCalculating = value; SafePropertyChanged(); }
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
                // MAJ contexte global
                AppState.TraficCumuleGlobal = TraficCumule;
                AppState.TypeAccroissementGlobal = TypeTauxAccroissement;
                // Persist full traffic parameter set in Project for report
                try
                {
                    if (AppState.CurrentProject != null)
                    {
                        AppState.CurrentProject.TraficMJA = _traficMJA;
                        AppState.CurrentProject.TauxAccroissement = _tauxAccroissement;
                        AppState.CurrentProject.DureeService = _dureeService;
                        AppState.CurrentProject.TypeTauxAccroissement = _typeTauxAccroissement;
                        AppState.CurrentProject.TraficCumuleNPL = _traficCumule; // already updated by TraficCumule setter but ensure consistency
                    }
                }
                catch { }
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
                            // NE = Trafic cumulé (NPL) * CAM
                            couche.Ne = Math.Round(TraficCumule * couche.Cam, 0);

                            // Calcul automatique kr
                            UpdateKr(couche);

                            // Si critère EpsiZ: calcul εz adm = A * NE^(B)
                            if (string.Equals(couche.Critere, "EpsiZ", StringComparison.OrdinalIgnoreCase))
                            {
                                EnsureDefaultsForEpsiZ(couche);
                                // Ne plus écrire dans Epsilon6 (réservé à la traction). Calcul direct au moment du besoin.
                            }
                            
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
                // Sauvegarder les nouvelles valeurs dans le projet (DTO) avant notification
                SaveToProject();
                // Notifier structure (pour cas où la liste change)
                AppState.OnStructureChanged();
                // Nouvel évènement spécifique pour mise à jour directe des valeurs dans ResultatView
                AppState.RaiseValeursAdmissiblesUpdated();
            }
        }

        private double CalculerValeurAdmissibleSimple(ValeurAdmissibleCouche couche)
        {
            try
            {
                if (couche == null) return 0;
                if (couche.Ne <= 0) return 0;
                if (IsExpertMode)
                {
                    switch (couche.Critere)
                    {
                        case "EpsiZ":
                            if (couche.AmplitudeValue <= 0 || Math.Abs(couche.B) < 1e-6) return 0;
                            break;
                        case "EpsiT":
                            if (couche.Epsilon6 <= 0 || Math.Abs(couche.B) < 1e-6) return 0;
                            break;
                        case "SigmaT":
                            if (couche.Sigma6 <= 0 || Math.Abs(couche.B) < 1e-6) return 0;
                            break;
                    }
                }
                switch (couche.Critere)
                {
                    case "EpsiZ":
                        // Calcul direct de εz admissible sans utiliser la propriété Epsilon6
                        return ComputeEpsiZ(couche.AmplitudeValue, couche.Ne, couche.B);
                    case "SigmaT":
                    {
                        double baseSigma6 = couche.Sigma6;
                        double B = couche.B;
                        if (Math.Abs(B) < 1e-9) B = B < 0 ? -1e-9 : 1e-9;
                        double b = -1.0 / B;
                        double factor = Math.Pow(couche.Ne / 1e6, b);
                        return baseSigma6 * factor * couche.Kc * couche.Kr * couche.Ks * couche.Kd;
                    }
                    case "EpsiT":
                    {
                        double baseEpsi6 = couche.Epsilon6;
                        double B = couche.B;
                        if (Math.Abs(B) < 1e-9) B = B < 0 ? -1e-9 : 1e-9;
                        double b = -1.0 / B;
                        double factor = Math.Pow(couche.Ne / 1e6, b);
                        return baseEpsi6 * factor * couche.Kc * couche.Kr * couche.Ks * couche.Ktheta;
                    }
                }
                double amplitude = couche.Sn;
                double facteurFatigue = Math.Pow(Math.Max(TraficCumule, 1.0) / 1e6, 1.0 / Math.Max(Math.Abs(couche.B), 1e-9));
                return amplitude * facteurFatigue * couche.Kc * couche.Kr * couche.Ks * couche.Ktheta * couche.Kd;
            }
            catch { return 0; }
        }

        private void AjouterCouche()
        {
            try
            {
                var nouvelleCouche = new ValeurAdmissibleCouche
                {
                    Materiau = "Nouveau matériau",
                    Niveau = ValeursAdmissibles.Count + 1,
                    Critere = "EpsiT"
                };
                if (IsAutomaticMode)
                {
                    nouvelleCouche.AmplitudeValue = 100;
                    nouvelleCouche.Sigma6 = 1.0;
                    nouvelleCouche.Risque = 10;
                    nouvelleCouche.B = -0.20;
                    nouvelleCouche.Sn = 100; nouvelleCouche.Sh = 120;
                    nouvelleCouche.Kc = 1.0; nouvelleCouche.Kr = 1.0; nouvelleCouche.Ks = 1.0; nouvelleCouche.Ktheta = 1.0; nouvelleCouche.Kd = 1.0;
                }
                ValeursAdmissibles.Add(nouvelleCouche);
                HookCoucheForAutoUpdate(nouvelleCouche);
                nouvelleCouche.Ne = Math.Round(TraficCumule * nouvelleCouche.Cam, 0);
                UpdateKr(nouvelleCouche);
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
                    // désabonner pour sécurité
                    couche.PropertyChanged -= Couche_PropertyChanged;
                    // détacher la source layer si présente
                    try
                    {
                        if (couche.SourceLayer != null && couche.SourceLayerHandler != null)
                        {
                            try { couche.SourceLayer.PropertyChanged -= couche.SourceLayerHandler; } catch { }
                        }
                    }
                    catch { }
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

        private void SyncFromStructure()
        {
            try
            {
                var structure = AppState.CurrentProject?.PavementStructure;
                // Toujours synchroniser la collection avec la structure (jamais avec ValeursAdmissibles du projet)
                if (structure?.Layers == null || structure.Layers.Count == 0)
                {
                    foreach (var c in ValeursAdmissibles)
                    {
                        c.PropertyChanged -= Couche_PropertyChanged;
                        // détacher source layer handlers
                        try
                        {
                            if (c.SourceLayer != null && c.SourceLayerHandler != null)
                            {
                                try { c.SourceLayer.PropertyChanged -= c.SourceLayerHandler; } catch { }
                            }
                        }
                        catch { }
                    }
                    ValeursAdmissibles.Clear();
                    return;
                }

                var ordered = structure.Layers.OrderBy(l => l.Order).ToList();

                // désabonner les anciennes couches
                foreach (var c in ValeursAdmissibles)
                {
                    c.PropertyChanged -= Couche_PropertyChanged;
                    try
                    {
                        if (c.SourceLayer != null && c.SourceLayerHandler != null)
                        {
                            try { c.SourceLayer.PropertyChanged -= c.SourceLayerHandler; } catch { }
                        }
                    }
                    catch { }
                }

                ValeursAdmissibles.Clear();
                int niveau = 1;
                foreach (var layer in ordered)
                {
                    var ligne = new ValeurAdmissibleCouche();
                    ligne.Materiau = layer.MaterialName ?? string.Empty;
                    ligne.Niveau = niveau++;
                    ligne.Critere = GetDefaultCritereFor(layer);

                    if (IsAutomaticMode)
                    {
                        switch (layer.Family)
                        {
                            case MaterialFamily.BetonBitumineux:
                            case MaterialFamily.Bibliotheque:
                                // Valeurs par défaut avant surcharge bibliothèque
                                double defaultInverseB = 5.0; // fallback -1/b
                                double defaultSn = 0.25;      // fallback Sn
                                double defaultKc = 1.1;       // fallback Kc (si non trouvé)
                                double eps = 100.0;           // fallback epsilon6
                                double inverseBFromLib = defaultInverseB;
                                double snFromLib = defaultSn;
                                double shFromLib = 0.25;      // fallback Sh (m) dispersion
                                double kcFromLib = defaultKc;

                                // Recherche systématique dans les bibliothèques pour récupérer tous les paramètres (même si LibraryEpsilon6 déjà présent)
                                try
                                {
                                    var dataService = new UI_ChausseeNeuve.Services.MaterialDataService();
                                    string[] libs = { "NFP98_086_2019", "CatalogueFrancais1998", "MateriauxBenin", "CatalogueSenegalais" };
                                    foreach (var lib in libs)
                                    {
                                        var mats = dataService.LoadMaterialsAsync(lib).GetAwaiter().GetResult();
                                        var found = mats.FirstOrDefault(m => string.Equals(m.Name, layer.MaterialName, StringComparison.OrdinalIgnoreCase));
                                        if (found != null)
                                        {
                                            if (found.Epsi0_10C.HasValue && found.Epsi0_10C.Value > 0)
                                            {
                                                eps = found.Epsi0_10C.Value;
                                                layer.LibraryEpsilon6 = eps; // mémoriser
                                            }
                                            if (found.InverseB.HasValue && found.InverseB.Value > 0)
                                                inverseBFromLib = found.InverseB.Value;
                                            if (found.SN.HasValue && found.SN.Value > 0)
                                                snFromLib = found.SN.Value;
                                            if (found.Sh.HasValue && found.Sh.Value > 0)
                                            {
                                                shFromLib = found.Sh.Value;
                                                layer.LibrarySh = shFromLib;
                                            }
                                            if (found.Kc.HasValue && found.Kc.Value > 0)
                                                kcFromLib = found.Kc.Value;
                                            break;
                                        }
                                    }
                                }
                                catch { }

                                // Si aucune valeur trouvée pour epsilon mais la layer a déjà sa valeur en cache, l'utiliser
                                if (layer.LibraryEpsilon6.HasValue && layer.LibraryEpsilon6.Value > 0)
                                    eps = layer.LibraryEpsilon6.Value;
                                if (layer.LibrarySh.HasValue && layer.LibrarySh.Value > 0)
                                    shFromLib = layer.LibrarySh.Value;

                                if (string.Equals(ligne.Critere, "EpsiT", StringComparison.OrdinalIgnoreCase))
                                    ligne.Epsilon6 = eps;
                                ligne.Sn = snFromLib;
                                ligne.Sh = ComputeShNormatifBitume(layer.Thickness_m); // calcul dynamique basé sur épaisseur
                                ligne.B = inverseBFromLib;
                                ligne.Kc = kcFromLib; ligne.Ks = 1.0; ligne.Kd = 1.0;
                                // Calcul automatique Ktheta
                                ComputeAutoKtheta(ligne);
                                break;
                            case MaterialFamily.MTLH:
                            case MaterialFamily.BetonCiment:
                                ligne.Sn = 0.8; ligne.Sh = 1.0; ligne.B = -0.12; ligne.Ks = 1.3; ligne.Kc = 1.0; ligne.Kd = 1.0; break;
                            case MaterialFamily.GNT:
                                {
                                    bool isEpsiZ = string.Equals(ligne.Critere, "EpsiZ", StringComparison.OrdinalIgnoreCase);
                                    double defaultInvB_GNT = 1.0 / 0.222; // ≈4.5045 (fallback)
                                    bool canDeriveFromLibrary = isEpsiZ && (Math.Abs(ligne.B) < 1e-6 || Math.Abs(ligne.B - defaultInvB_GNT) < 1e-3);
                                    double invB = ligne.B;
                                    if (canDeriveFromLibrary)
                                    {
                                        invB = defaultInvB_GNT;
                                        try
                                        {
                                            var dataService = new UI_ChausseeNeuve.Services.MaterialDataService();
                                            string[] libs = { "NFP98_086_2019", "CatalogueFrancais1998", "MateriauxBenin", "CatalogueSenegalais" };
                                            foreach (var lib in libs)
                                            {
                                                var mats = dataService.LoadMaterialsAsync(lib).GetAwaiter().GetResult();
                                                var found = mats.FirstOrDefault(m => string.Equals(m.Name, layer.MaterialName, StringComparison.OrdinalIgnoreCase));
                                                if (found != null && found.AdditionalProperties != null && found.AdditionalProperties.TryGetValue("pente_b", out var pbObj))
                                                {
                                                    if (pbObj is double dval && Math.Abs(dval) > 1e-6)
                                                        invB = 1.0 / Math.Abs(dval);
                                                    else if (double.TryParse(pbObj?.ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed) && Math.Abs(parsed) > 1e-6)
                                                        invB = 1.0 / Math.Abs(parsed);
                                                    break;
                                                }
                                            }
                                        }
                                        catch { }
                                    }
                                    if (Math.Abs(ligne.Sn) < 1e-9) ligne.Sn = 500;
                                    if (Math.Abs(ligne.Sh) < 1e-9) ligne.Sh = 600;
                                    if (canDeriveFromLibrary) ligne.B = invB;
                                    ligne.Kd = ligne.Kd == 0 ? 3.0 : ligne.Kd;
                                    if (ligne.Ks == 0) ligne.Ks = 1.0;
                                    if (ligne.Kc == 0) ligne.Kc = 1.0;
                                }
                                break;
                            default:
                                ligne.Sn = 0; ligne.Sh = 0; ligne.B = -0.20; break;
                        }
                        if (string.Equals(ligne.Critere, "EpsiZ", StringComparison.OrdinalIgnoreCase))
                        {
                            ligne.B = DefaultExponentEpsiZ;
                            ligne.AUserDefined = false;
                            EnsureDefaultsForEpsiZ(ligne);
                        }
                        // Auto ks après initialisation valeurs
                        ligne.Ks = ComputeAutoKsFor(ligne);
                        // Ktheta auto bitume
                        ComputeAutoKtheta(ligne);
                    }
                    else
                    {
                        // Mode Expert: laisser tout à 0 sans marquer comme saisi
                        ligne.Sn = 0; ligne.Sh = 0; ligne.B = 0; ligne.Kc = 0; ligne.Kr = 0; ligne.Ks = 0; ligne.Ktheta = 0; ligne.Kd = 0; ligne.Cam = 0; ligne.Risque = 0; ligne.Epsilon6 = 0; ligne.Sigma6 = 0; ligne.AmplitudeValue = 0;
                    }

                    // Brancher la mise à jour auto et calculer NE initial
                    HookCoucheForAutoUpdate(ligne);
                    // ATTACH: synchronisation dynamique du nom du matériau avec la layer
                    AttachLayerSync(ligne, layer);

                    // NE initial uniquement si CAM déjà défini (mode Automatique)
                    if (IsAutomaticMode)
                    {
                        // Affectation CAM initiale via SetCamAuto pour ne pas marquer CamUserSet
                        ligne.SetCamAuto(ComputeAutoCam(layer, _traficMJA));
                        ligne.Ne = Math.Round(TraficCumule * ligne.Cam, 0);
                        EnforceRisqueSoupleBitumineux(ligne);
                        UpdateKr(ligne);
                        if (string.Equals(ligne.Critere, "EpsiZ", StringComparison.OrdinalIgnoreCase)) { }
                        // Recalcule ks (cas où l'ordre dépend de la couche sous-jacente déjà ajoutée)
                        ligne.Ks = ComputeAutoKsFor(ligne);
                        // Ktheta auto bitume
                        ComputeAutoKtheta(ligne);
                    }
                    else
                    {
                        // En mode Expert valeur admissible laissée à 0 jusqu'à saisie utilisateur
                        ligne.ValeurAdmissible = 0;
                    }

                    ValeursAdmissibles.Add(ligne);
                }

                // Persist the synchronized values into the project pour visibilité globale
                SaveToProject();

                System.Diagnostics.Debug.WriteLine($"VA Sync: {ValeursAdmissibles.Count} lignes (Mode={(IsAutomaticMode ? "Automatique" : "Expert")})");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur SyncFromStructure: {ex.Message}");
            }
        }

        private void HookCoucheForAutoUpdate(ValeurAdmissibleCouche couche)
        {
            try
            {
                couche.PropertyChanged -= Couche_PropertyChanged;
                couche.PropertyChanged += Couche_PropertyChanged;
            }
            catch { }
        }

        // Attache un handler sur la Layer source pour synchroniser dynamiquement le nom du matériau
        private void AttachLayerSync(ValeurAdmissibleCouche ligne, Layer layer)
        {
            try
            {
                if (ligne.SourceLayer != null && ligne.SourceLayerHandler != null)
                {
                    try { ligne.SourceLayer.PropertyChanged -= ligne.SourceLayerHandler; } catch { }
                    ligne.SourceLayerHandler = null;
                    ligne.SourceLayer = null;
                }

                PropertyChangedEventHandler handler = (s, e) =>
                {
                    try
                    {
                        if (e.PropertyName == nameof(Layer.MaterialName))
                        {
                            ligne.Materiau = layer.MaterialName ?? string.Empty;
                            SaveToProject();
                            AppState.OnStructureChanged();
                        }
                        if (IsAutomaticMode && (e.PropertyName == nameof(Layer.Modulus_MPa) || e.PropertyName == nameof(Layer.Family)))
                        {
                            ligne.SetCamAuto(ComputeAutoCam(layer, _traficMJA));
                            ligne.Ne = Math.Round(TraficCumule * ligne.Cam, 0);
                            EnforceRisqueSoupleBitumineux(ligne);
                            // Recalcul ks car le module sous-jacent peut avoir changé (ou la famille)
                            ligne.Ks = ComputeAutoKsFor(ligne);
                            // Recalcul ktheta si bitume
                            ComputeAutoKtheta(ligne);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"AttachLayerSync handler erreur: {ex.Message}");
                    }
                };

                layer.PropertyChanged += handler;
                ligne.SourceLayer = layer;
                ligne.SourceLayerHandler = handler;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AttachLayerSync erreur: {ex.Message}");
            }
        }

         private void Couche_PropertyChanged(object? sender, PropertyChangedEventArgs e)
         {
             try
             {
                 if (sender is ValeurAdmissibleCouche couche)
                 {
                     if (e.PropertyName == nameof(ValeurAdmissibleCouche.Cam))
                     {
                         // NE = TraficCumule (NPL) * CAM
                         couche.Ne = Math.Round(TraficCumule * couche.Cam, 0);
                     
                    }

                    // Marqueur interne: A saisi par l’utilisateur (pour EpsiZ)
                    if (e.PropertyName == nameof(ValeurAdmissibleCouche.AmplitudeValue))
                    {
                        couche.AUserDefined = true;
                    }

                    // Calcul automatique pour EpsiZ
                    if (string.Equals(couche.Critere, "EpsiZ", StringComparison.OrdinalIgnoreCase))
                    {
                        if (e.PropertyName == nameof(ValeurAdmissibleCouche.Ne) ||
                            e.PropertyName == nameof(ValeurAdmissibleCouche.B) ||
                            e.PropertyName == nameof(ValeurAdmissibleCouche.AmplitudeValue) ||
                            e.PropertyName == nameof(ValeurAdmissibleCouche.Critere))
                        {
                            EnsureDefaultsForEpsiZ(couche);
                            // Pas d'affectation Epsilon6 ici
                        }
                    }

                    // Calcul kr si Risque, Sn, Sh, B, Critere changent
                    if (e.PropertyName == nameof(ValeurAdmissibleCouche.Risque) ||
                        e.PropertyName == nameof(ValeurAdmissibleCouche.Sn) ||
                        e.PropertyName == nameof(ValeurAdmissibleCouche.Sh) ||
                        e.PropertyName == nameof(ValeurAdmissibleCouche.B) ||
                        e.PropertyName == nameof(ValeurAdmissibleCouche.Critere))
                    {
                        // Empêcher modification du risque pour bitumineux en structure souple
                        if (e.PropertyName == nameof(ValeurAdmissibleCouche.Risque))
                            EnforceRisqueSoupleBitumineux(couche);
                        UpdateKr(couche);
                    }

                    // Recalculer la valeur admissible à chaque changement pertinent
                    if (e.PropertyName == nameof(ValeurAdmissibleCouche.Ne) ||
                        e.PropertyName == nameof(ValeurAdmissibleCouche.B) ||
                        e.PropertyName == nameof(ValeurAdmissibleCouche.AmplitudeValue) ||
                        e.PropertyName == nameof(ValeurAdmissibleCouche.Kc) ||
                        e.PropertyName == nameof(ValeurAdmissibleCouche.Kr) ||
                        e.PropertyName == nameof(ValeurAdmissibleCouche.Ks) ||
                        e.PropertyName == nameof(ValeurAdmissibleCouche.Ktheta) ||
                        e.PropertyName == nameof(ValeurAdmissibleCouche.Kd) ||
                        e.PropertyName == nameof(ValeurAdmissibleCouche.Critere) ||
                        e.PropertyName == nameof(ValeurAdmissibleCouche.Sn) ||
                        e.PropertyName == nameof(ValeurAdmissibleCouche.Sh) ||
                        e.PropertyName == nameof(ValeurAdmissibleCouche.Sigma6) ||
                        e.PropertyName == nameof(ValeurAdmissibleCouche.Epsilon6))
                    {
                        couche.ValeurAdmissible = CalculerValeurAdmissibleSimple(couche);
                        // Sauvegarder immédiatement les changements pour que les résultats peuvent lire la nouvelle valeur
                        SaveToProject();
                        // mise à jour ciblée pour les résultats
                        AppState.RaiseValeursAdmissiblesUpdated();
                    }
                 }
             }
             catch (Exception ex)
             {
                 System.Diagnostics.Debug.WriteLine($"Erreur Couche_PropertyChanged: {ex.Message}");
             }
         }

         private void UpdateNeForAllCouches()
         {
             try
             {
                 if (ValeursAdmissibles == null) return;
                 foreach (var c in ValeursAdmissibles)
                 {
                     c.Ne = Math.Round(TraficCumule * c.Cam, 0);
                     // maj kr
                     UpdateKr(c);
                     if (string.Equals(c.Critere, "EpsiZ", StringComparison.OrdinalIgnoreCase))
                     {
                         EnsureDefaultsForEpsiZ(c);
                         // Pas d'affectation Epsilon6 lors du recalcul NE global
                     }
                 }
             }
             catch (Exception ex)
             {
                 System.Diagnostics.Debug.WriteLine($"Erreur UpdateNeForAllCouches: {ex.Message}");
             }
         }

         private string GetDefaultCritereFor(Layer layer)
         {
             if (layer.Role == LayerRole.Plateforme || layer.Family == MaterialFamily.GNT)
                 return "EpsiZ"; // Déformation verticale
             if (layer.Family == MaterialFamily.MTLH || layer.Family == MaterialFamily.BetonCiment)
                 return "SigmaT"; // Contrainte traction
             return "EpsiT"; // Déformation traction (BB)
         }

         private void LoadSampleDataSafe()
         {
             // Désormais, synchronisation automatique avec la structure
             SyncFromStructure();
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

         // Helpers εz adm
         private static double ComputeEpsiZ(double A, double NE, double displayedMinus1Overb)
         {
             // Nouvelle spécification : εz = A * NE^(-1 / B) où B est la valeur affichée (colonne "-1/b").
            // Rappel : B = -1 / b (b étant le paramètre fatigue classique). Donc -1/B = b.
            // Cette formulation revient à εz = A * NE^b avec b = -1/B.
            if (NE <= 0) return 0;
            if (Math.Abs(displayedMinus1Overb) < 1e-12) return 0; // évite division par zéro
            try
            {
                double exponent = -1.0 / displayedMinus1Overb; // b réel
                return A * Math.Pow(NE, exponent);
            }
            catch { return 0; }
         }

         // Calcul kr: 10^(-b * u0 * delta) avec b = -1/B et delta = sqrt(SN^2 + (c * Sh / b)^2)
         private void UpdateKr(ValeurAdmissibleCouche c)
         {
             try
             {
                 // En mode Expert: ne calcule Kr que si l'utilisateur a fourni des valeurs significatives
                 if (IsExpertMode)
                 {
                     if (Math.Abs(c.B) < 1e-6) return; // b inconnu
                     if (c.Sn <= 0 && c.Sh <= 0) return; // rien fourni
                     if (c.Risque <= 0) return; // risque non défini
                 }

                 double B = c.B; // B est la valeur affichée dans la colonne -1/b
                 if (Math.Abs(B) < 1e-9)
                 {
                     B = B < 0 ? -1e-9 : 1e-9;
                 }
                 // Correction : b = -1.0 / B
                 double b = -1.0 / B;
                 double p = Math.Clamp(c.Risque / 100.0, 0.0001, 0.9999);
                 double u0 = NormalQuantile(p); // Valeur de la loi normale centrée réduite
                 double sn = c.Sn;
                 double sh = c.Sh;
                 double delta = Math.Sqrt(sn * sn + Math.Pow((CThicknessSensitivity * sh) / b, 2));
                 c.Kr = Math.Pow(10.0, -b * u0 * delta);
             }
             catch (Exception ex)
             {
                 System.Diagnostics.Debug.WriteLine($"Erreur UpdateKr: {ex.Message}");
             }
         }

         private void EnsureDefaultsForEpsiZ(ValeurAdmissibleCouche c)
         {
             // Normalisation signe : la colonne affiche -1/b (positif). Si valeur négative (ex -0.222) on convertit en 1.0/|val| ≈ 4.5
             if (c.Critere != null && string.Equals(c.Critere, "EpsiZ", StringComparison.OrdinalIgnoreCase) && c.B < 0)
             {
                 double neg = c.B;
                 if (Math.Abs(neg) > 1e-9) c.B = 1.0 / Math.Abs(neg);
             }

             // Structure souple : amplitude TOUJOURS fixée (NE<=250000 =>16000, sinon 12000) quelle que soit la saisie
             bool structureSouple = false;
             try { structureSouple = string.Equals(AppState.CurrentProject?.PavementStructure?.StructureType, "Souple", StringComparison.OrdinalIgnoreCase); } catch { }

             if (structureSouple)
             {
                 if (Math.Abs(c.B) < 0.001) c.B = 1.0 / Math.Abs(DefaultExponentEpsiZ); // fallback normalisé
                 var targetA = c.Ne > ThresholdNE ? DefaultA_HighNE : DefaultA_LowNE;
                 if (Math.Abs(c.AmplitudeValue - targetA) > 0.001)
                 {
                     c.AUserDefined = false; // on considère que c'est une valeur normative
                     c.AmplitudeValue = targetA;
                 }
                 return; // ne pas appliquer la logique Expert/Automatique ci-dessous
             }

             // En mode Expert: ne rien forcer (hors structure souple)
             if (IsExpertMode) return;

             // forcer -1/b par défaut si non défini (proche de 0)
             if (Math.Abs(c.B) < 0.001) c.B = 1.0 / Math.Abs(DefaultExponentEpsiZ);

             // si A n’a pas été saisi par l’utilisateur, l’ajuster selon NE
             if (!c.AUserDefined)
             {
                 if (c.Ne > ThresholdNE) c.AmplitudeValue = DefaultA_HighNE; else c.AmplitudeValue = DefaultA_LowNE;
             }
         }

         // Calcul Sh normatif bitume (h en m, retour en m)
         private static double ComputeShNormatifBitume(double h)
         {
             if (h <= 0.10) return 0.010;
             if (h >= 0.15) return 0.025;
             // interpolation linéaire entre 0.10 et 0.15: 0.010 + 0.3*(h-0.10)
             return 0.010 + 0.3 * (h - 0.10);
         }

         private double ComputeAutoKsFor(ValeurAdmissibleCouche c)
        {
            try
            {
                if (c?.SourceLayer == null) return c.Ks;
                var structure = AppState.CurrentProject?.PavementStructure;
                if (structure?.Layers == null) return c.Ks;
                var ordered = structure.Layers.OrderBy(l => l.Order).ToList();
                int idx = ordered.FindIndex(l => ReferenceEquals(l, c.SourceLayer));
                if (idx < 0) return c.Ks;

                // Chercher en descendant la PREMIERE couche non liée (GNT / sol) définie par: Family == GNT OR Modulus < 1000 MPa
                Layer? referenceUnbound = null;
                for (int j = idx + 1; j < ordered.Count; j++)
                {
                    var candidate = ordered[j];
                    if (candidate.Family == MaterialFamily.GNT || candidate.Modulus_MPa < 1000)
                    {
                        referenceUnbound = candidate;
                        break;
                    }
                }
                if (referenceUnbound == null) return c.Ks; // pas trouvé => on conserve
                double e = referenceUnbound.Modulus_MPa;
                if (e < 50) return Math.Round(1.0 / 1.2, 3);       // ≈0.833
                if (e < 80) return Math.Round(1.0 / 1.1, 3);       // ≈0.909
                if (e < 120) return Math.Round(1.0 / 1.065, 3);    // ≈0.939
                return 1.0;                                        // ≥120 MPa
            }
            catch { return c.Ks; }
        }
        #endregion

        #region IDisposable
         
         public void Dispose()
         {
             try
             {
                 AppState.StructureChanged -= OnStructureChanged;
                 // désabonner
                 foreach (var c in ValeursAdmissibles)
                 {
                     c.PropertyChanged -= Couche_PropertyChanged;
                     try
                     {
                         if (c.SourceLayer != null && c.SourceLayerHandler != null)
                             c.SourceLayer.PropertyChanged -= c.SourceLayerHandler;
                     }
                     catch { }
                 }
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

         public void EnsureSyncedWithStructure()
         {
             if (ValeursAdmissibles == null || ValeursAdmissibles.Count == 0)
                 SyncFromStructure();
         }

        private bool IsAutomaticMode => AppState.CurrentProject?.Mode == DimensionnementMode.Automatique;
        private bool IsExpertMode => AppState.CurrentProject?.Mode == DimensionnementMode.Expert;

        private static double NormalQuantile(double p)
        {
            const double a1 = -39.6968302866538;
            const double a2 = 220.946098424521;
            const double a3 = -275.928510446969;
            const double a4 = 138.357751867269;
            const double a5 = -30.6647980661472;
            const double a6 = 2.50662827745924;
            const double b1 = -54.4760987982241;
            const double b2 = 161.585836858041;
            const double b3 = -155.698979859887;
            const double b4 = 66.8013118877197;
            const double b5 = -13.2806815528857;
            const double c1 = -0.00778489400243029;
            const double c2 = -0.322396458041136;
            const double c3 = -2.40075827716184;
            const double c4 = -2.54973253934373;
            const double c5 = 4.37466414146497;
            const double c6 = 2.93816398269878;
            const double d1 = 0.00778469570904146;
            const double d2 = 0.32246712907004;
            const double d3 = 2.445134137143;
            const double d4 = 3.75440866190742;
            const double plow = 0.02425;
            const double phigh = 1 - plow;
            if (p <= 0) return double.NegativeInfinity;
            if (p >= 1) return double.PositiveInfinity;
            if (p < plow)
            {
                double q = Math.Sqrt(-2 * Math.Log(p));
                return (((((c1 * q + c2) * q + c3) * q + c4) * q + c5) * q + c6) /
                       ((((d1 * q + d2) * q + d3) * q + d4) * q + 1);
            }
            if (p > phigh)
            {
                double q = Math.Sqrt(-2 * Math.Log(1 - p));
                return -(((((c1 * q + c2) * q + c3) * q + c4) * q + c5) * q + c6) /
                        ((((d1 * q + d2) * q + d3) * q + d4) * q + 1);
            }
            else
            {
                double q = p - 0.5; double r = q * q;
                return (((((a1 * r + a2) * r + a3) * r + a4) * r + a5) * r + a6) * q /
                       (((((b1 * r + b2) * r + b3) * r + b4) * r + b5) * r + 1);
            }
        }

        private bool IsStructureSouple()
        {
            try { return string.Equals(AppState.CurrentProject?.PavementStructure?.StructureType, "Souple", StringComparison.OrdinalIgnoreCase); } catch { return false; }
        }
        private void EnforceRisqueSoupleBitumineux(ValeurAdmissibleCouche c)
        {
            try
            {
                if (c?.SourceLayer == null) return;
                if (!IsStructureSouple()) return;
                if (c.SourceLayer.Family == MaterialFamily.BetonBitumineux)
                {
                    if (Math.Abs(c.Risque - 25.0) > 0.0001)
                        c.Risque = 25.0; // force
                }
            }
            catch { }
        }

        private (MaterialItem? material, double e34, double e10, double relError) IdentifyBitumeMaterialForLayer(Layer layer)
        {
            try
            {
                if (layer == null) return (null, 0, 0, 1);
                if (layer.Family != MaterialFamily.BetonBitumineux && layer.Family != MaterialFamily.Bibliotheque) return (null, 0, 0, 1);
                double targetE34 = layer.Modulus_MPa; // Interprétation: module saisi = E(34°C,20Hz)
                if (targetE34 <= 0) return (null, 0, 0, 1);

                var dataService = new UI_ChausseeNeuve.Services.MaterialDataService();
                MaterialItem? best = null; double bestRel = double.MaxValue; double bestE34 = 0; double bestE10 = 0;

                foreach (var lib in BitumeLibraries)
                {
                    List<MaterialItem> mats;
                    try { mats = dataService.LoadMaterialsAsync(lib).GetAwaiter().GetResult(); } catch { continue; }
                    foreach (var m in mats.Where(m => string.Equals(m.Category, "MB", StringComparison.OrdinalIgnoreCase)))
                    {
                        try
                        {
                            // Calculer E(34°C,20Hz) et E(10°C,10Hz) via modèle de MaterialItem
                            double e34 = m.GetModulusAt(34, 20); // extrapolation fréquencielle admise
                            if (e34 <= 0) continue;
                            double rel = Math.Abs(e34 - targetE34) / e34;
                            if (rel < bestRel)
                            {
                                bestRel = rel; best = m; bestE34 = e34; bestE10 = m.GetModulusAt(10, 10);
                            }
                        }
                        catch { }
                    }
                }
                if (best != null)
                {
                    return (best, bestE34, bestE10, bestRel);
                }
                return (null, 0, 0, 1);
            }
            catch { return (null, 0, 0, 1); }
        }

        private void ComputeAutoKtheta(ValeurAdmissibleCouche ligne)
        {
            try
            {
                if (ligne == null || ligne.SourceLayer == null) return;
                if (!(IsAutomaticMode)) return;
                if (ligne.SourceLayer.Family != MaterialFamily.BetonBitumineux && ligne.SourceLayer.Family != MaterialFamily.Bibliotheque) return;

                var (mat, e34Lib, e10Lib, rel) = IdentifyBitumeMaterialForLayer(ligne.SourceLayer);
                double e34 = ligne.SourceLayer.Modulus_MPa; // par défaut on utilise le module saisi
                double e10 = e34; // fallback ratio 1
                if (mat != null && rel <= BitumeMatchTolerance && e34Lib > 0 && e10Lib > 0)
                {
                    // Utiliser les valeurs normatives identifiées
                    e34 = e34Lib;
                    e10 = e10Lib;
                }
                else if (mat != null && e34Lib > 0 && e10Lib > 0)
                {
                    // Matériau trouvé mais écart > tolérance : on conserve le module utilisateur comme e34 mais ratio basé sur matériau (approximation)
                    e10 = e10Lib * (e34 / e34Lib); // ajuste E10 proportionnellement
                }
                // Stocker dans les champs dédiés (utilisés par Ktheta)
                ligne.E10C10Hz = e10;
                ligne.Eteq10Hz = e34; // denominator
                ligne.KthetaAuto = true; // active calcul automatique
                // Forcer notification Ktheta
                ligne.Ktheta = ligne.Ktheta; // déclenche recalcul via getter
            }
            catch { }
        }
    }

    /// <summary>
    /// Modèle de données pour une valeur admissible par couche
    /// VERSION ULTRA-SÉCURISÉE
    /// </summary>
    public class ValeurAdmissibleCouche : INotifyPropertyChanged
    {
        internal Layer? SourceLayer { get; set; }
        internal PropertyChangedEventHandler? SourceLayerHandler { get; set; }
        internal bool AUserDefined { get; set; } = false;
        private string _materiau = "";
        private int _niveau;
        private string _critere = "EpsiT";
        private double _sn = 0;
        private double _sh = 0;
        private double _b = 0;
        private double _kc = 0;
        private double _kr = 0;
        private double _ks = 0;
        private double _ktheta = 0;
        private double _kd = 0;
        private double _risque = 0;
        private double _ne = 0.0;
        private double _epsilon6 = 0.0;
        private double _valeurAdmissible;
        private double _amplitudeValue = 0;
        private double _sigma6 = 0.0;
        private double _cam;
        private double _e10C10Hz = 0.0;
        private double _eteq10Hz = 0.0;
        private bool _kthetaAuto = false;
        public bool CamUserSet { get; private set; }

        // Champs ajoutés pour calcul inverse (NEmax, réserve, etc.) conservés pour compatibilité ENC
        private double _nEmax; // NE maximum admissible inverse
        private double _nEReserve; // NEmax - NE
        private double _tauxUtilInverse; // NE / NEmax
        public double NEmax { get => _nEmax; set { _nEmax = value; SafePropertyChanged(); } }
        public double NEReserve { get => _nEReserve; set { _nEReserve = value; SafePropertyChanged(); } }
        public double TauxUtilInverse { get => _tauxUtilInverse; set { _tauxUtilInverse = value; SafePropertyChanged(); } }

        public string Materiau { get => _materiau; set { _materiau = value ?? ""; SafePropertyChanged(); } }
        public int Niveau { get => _niveau; set { _niveau = value; SafePropertyChanged(); } }
        public string Critere { get => _critere; set { _critere = value ?? "EpsiT"; SafePropertyChanged(); SafePropertyChanged(nameof(AmplitudeLabel)); SafePropertyChanged(nameof(IsEpsiZ)); SafePropertyChanged(nameof(IsEpsiT)); SafePropertyChanged(nameof(IsSigmaT)); SafePropertyChanged(nameof(CanEditSn)); SafePropertyChanged(nameof(CanEditSh)); SafePropertyChanged(nameof(CanEditKc)); SafePropertyChanged(nameof(CanEditKr)); SafePropertyChanged(nameof(CanEditKs)); SafePropertyChanged(nameof(CanEditKtheta)); SafePropertyChanged(nameof(CanEditKd)); SafePropertyChanged(nameof(CanEditRisque)); SafePropertyChanged(nameof(CanEditEpsilon6)); SafePropertyChanged(nameof(CanEditAmplitude)); } }
        public bool IsEpsiZ => string.Equals(_critere, "EpsiZ", StringComparison.OrdinalIgnoreCase);
        public bool IsEpsiT => string.Equals(_critere, "EpsiT", StringComparison.OrdinalIgnoreCase);
        public bool IsSigmaT => string.Equals(_critere, "SigmaT", StringComparison.OrdinalIgnoreCase);
        private bool IsStructureSouple => string.Equals(AppState.CurrentProject?.PavementStructure?.StructureType, "Souple", StringComparison.OrdinalIgnoreCase);
        public bool CanEditAmplitude => IsEpsiZ; // Amplitude (A) toujours editable pour EpsiZ
        // Contrôles d'édition spécifiques aux critères
        public bool CanEditSn => !IsEpsiZ;              // Pas utilisé pour EpsiZ
        public bool CanEditSh => !IsEpsiZ;              // Pas utilisé pour EpsiZ
        public bool CanEditKc => !IsEpsiZ;              // Pas utilisé pour EpsiZ
        public bool CanEditKr => !IsEpsiZ;              // Pas utilisé pour EpsiZ
        public bool CanEditKs => !IsEpsiZ && IsEpsiT;   // Ks uniquement pertinent pour EpsiT (fatigue en flexion)
        public bool CanEditKtheta => IsEpsiT;           // kθ seulement pour EpsiT
        public bool CanEditKd => !IsEpsiZ && (IsEpsiT || IsSigmaT); // Kd pas pour EpsiZ
        public bool CanEditRisque => !IsEpsiZ;          // Risque pas pour EpsiZ
        public bool CanEditEpsilon6 => IsEpsiT;         // ε6 uniquement pour EpsiT (SigmaT utilise σ6, EpsiZ utilise A)
        public double Sn { get => _sn; set { _sn = value; SafePropertyChanged(); } }
        public double Sh { get => _sh; set { _sh = value; SafePropertyChanged(); } }
        public double B { get => _b; set { _b = value; SafePropertyChanged(); } }
        public double Kc { get => _kc; set { _kc = value; SafePropertyChanged(); } }
        public double Kr { get => _kr; set { _kr = value; SafePropertyChanged(); } }
        public double Ks { get => _ks; set { _ks = value; SafePropertyChanged(); } }
        public double Ktheta { get => _kthetaAuto && E10C10Hz > 0 && Eteq10Hz > 0 ? Math.Sqrt(E10C10Hz / Eteq10Hz) : _ktheta; set { _ktheta = value; SafePropertyChanged(); } }
        public double Kd { get => _kd; set { _kd = value; SafePropertyChanged(); } }
        public double Risque { get => _risque; set { _risque = value; SafePropertyChanged(); } }
        public double Ne { get => _ne; set { _ne = value; SafePropertyChanged(); } }
        public double Epsilon6 { get => _epsilon6; set { _epsilon6 = value; SafePropertyChanged(); } }
        public double ValeurAdmissible { get => _valeurAdmissible; set { _valeurAdmissible = value; SafePropertyChanged(); } }
        public double AmplitudeValue { get => _amplitudeValue; set { _amplitudeValue = value; SafePropertyChanged(); } }
        public double Sigma6 { get => _sigma6; set { _sigma6 = value; SafePropertyChanged(); } }
        public string AmplitudeLabel => _critere switch { "EpsiZ" => "A", "SigmaT" => "ε6", _ => "ε6" };
        public double Cam { get => _cam; set { _cam = value; CamUserSet = true; SafePropertyChanged(); SafePropertyChanged(nameof(CamUserSet)); } }
        internal void SetCamAuto(double value)
        {
            _cam = value; // ne marque pas CamUserSet
            SafePropertyChanged(nameof(Cam));
        }
        public double E10C10Hz { get => _e10C10Hz; set { _e10C10Hz = value; SafePropertyChanged(); if (KthetaAuto) UpdateKtheta(); } }
        public double Eteq10Hz { get => _eteq10Hz; set { _eteq10Hz = value; SafePropertyChanged(); if (KthetaAuto) UpdateKtheta(); } }
        public bool KthetaAuto { get => _kthetaAuto; set { _kthetaAuto = value; SafePropertyChanged(); UpdateKtheta(); } }
        private void UpdateKtheta() { if (KthetaAuto && E10C10Hz > 0 && Eteq10Hz > 0) Ktheta = Math.Sqrt(E10C10Hz / Eteq10Hz); SafePropertyChanged(nameof(Ktheta)); }
        public ObservableCollection<string> CriteresDisponibles { get; } = new ObservableCollection<string> { "EpsiT", "SigmaT", "EpsiZ" };
        public event PropertyChangedEventHandler? PropertyChanged;
        private void SafePropertyChanged([CallerMemberName] string? propertyName = null) { try { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); } catch { } }
    }
}
