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
            // CORRECTION: Synchronisation immédiate au lieu de BeginInvoke pour éviter race conditions
            SyncFromStructure();
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
                // Calcul automatique
                CalculerTraficCumule();
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
                SafePropertyChanged();
                CalculerTraficCumuleCommand?.RaiseCanExecuteChanged();
                // Calcul automatique
                CalculerTraficCumule();
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
                // Mettre à jour NE sur toutes les couches quand le trafic cumulé change
                UpdateNeForAllCouches();
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

        private static ValeurAdmissibleCoucheDto ToDto(ValeurAdmissibleCouche c)
        {
            return new ValeurAdmissibleCoucheDto
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
        }

        private static ValeurAdmissibleCouche FromDto(ValeurAdmissibleCoucheDto d)
        {
            return new ValeurAdmissibleCouche
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
                            // NE = Trafic cumulé (NPL) * CAM
                            couche.Ne = Math.Round(TraficCumule * couche.Cam, 0);

                            // Calcul automatique kr
                            UpdateKr(couche);

                            // Si critère EpsiZ: calcul εz adm = A * NE^(B)
                            if (string.Equals(couche.Critere, "EpsiZ", StringComparison.OrdinalIgnoreCase))
                            {
                                EnsureDefaultsForEpsiZ(couche);
                                couche.Epsilon6 = ComputeEpsiZ(couche.AmplitudeValue, couche.Ne, couche.B);
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
                // Ajout : Notifier la structure pour forcer la synchro des résultats
                AppState.OnStructureChanged();
            }
        }

        private double CalculerValeurAdmissibleSimple(ValeurAdmissibleCouche couche)
        {
            try
            {
                if (couche == null) return 0;

                // NE requis
                if (couche.Ne <= 0) return 0;

                // Appliquer les formules selon le critère
                switch (couche.Critere)
                {
                    case "EpsiZ":
                        // εz adm déjà calculé dans Epsilon6 via A * NE^b (sans autres coefficients)
                        return couche.Epsilon6;

                    case "SigmaT":
                        // σ_t adm = σ_6 × (NE/10^6 )^b × k_c × k_r × k_s × k_d
                        // avec b = -1/B où B est la valeur affichée dans la colonne -1/b
                        {
                            double baseSigma6 = couche.Sigma6;
                            double B = couche.B;
                            if (Math.Abs(B) < 1e-9) B = B < 0 ? -1e-9 : 1e-9;
                            double b = -1.0 / B;
                            double factor = Math.Pow(couche.Ne / 1e6, b);
                            return baseSigma6 * factor * couche.Kc * couche.Kr * couche.Ks * couche.Kd;
                        }

                    case "EpsiT":
                        // εt adm = ε6(10°C,25Hz) × (NE/1e6)^b × kc × kr × ks × kθ avec b = -1/B
                        {
                            double baseEpsi6 = couche.Epsilon6;
                            double B = couche.B;
                            if (Math.Abs(B) < 1e-9) B = B < 0 ? -1e-9 : 1e-9;
                            double b = -1.0 / B;
                            double factor = Math.Pow(couche.Ne / 1e6, b);
                            return baseEpsi6 * factor * couche.Kc * couche.Kr * couche.Ks * couche.Ktheta;
                        }
                }

                // Fallback: ancienne logique (rare)
                double amplitude = couche.Sn;
                double facteurFatigue = Math.Pow(Math.Max(TraficCumule, 1.0) / 1e6, 1.0 / Math.Max(Math.Abs(couche.B), 1e-9));
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
                    Sigma6 = 1.0,
                    Cam = 0,
                    Risque = 10,
                    Epsilon6 = 0,
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
                HookCoucheForAutoUpdate(nouvelleCouche);
                // Calcul NE immédiat pour la nouvelle ligne
                nouvelleCouche.Ne = Math.Round(TraficCumule * nouvelleCouche.Cam, 0);
                // Calcul kr initial
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
                            couche.SourceLayer.PropertyChanged -= couche.SourceLayerHandler;
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
                                c.SourceLayer.PropertyChanged -= c.SourceLayerHandler;
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
                            c.SourceLayer.PropertyChanged -= c.SourceLayerHandler;
                    }
                    catch { }
                }

                ValeursAdmissibles.Clear();
                int niveau = 1;
                foreach (var layer in ordered)
                {
                    var ligne = new ValeurAdmissibleCouche
                    {
                        Materiau = layer.MaterialName ?? string.Empty, // Toujours le nom exact de la structure
                        Niveau = niveau++,
                        Critere = GetDefaultCritereFor(layer)
                    };

                    switch (layer.Family)
                    {
                        case MaterialFamily.BetonBitumineux:
                            ligne.Sn = 90; ligne.Sh = 100; ligne.B = -0.20; ligne.Kc = 1.3; break;
                        case MaterialFamily.MTLH:
                        case MaterialFamily.BetonCiment:
                            ligne.Sn = 0.8; ligne.Sh = 1.0; ligne.B = -0.12; ligne.Ks = 1.3; break;
                        case MaterialFamily.GNT:
                            ligne.Sn = 500; ligne.Sh = 600; ligne.B = DefaultExponentEpsiZ; ligne.Kd = 3.0; break;
                    }

                    // Si le critère est EpsiZ, s'assurer des défauts A/B
                    if (string.Equals(ligne.Critere, "EpsiZ", StringComparison.OrdinalIgnoreCase))
                    {
                        ligne.B = DefaultExponentEpsiZ;
                        ligne.AUserDefined = false; // on laisse l'auto initialiser A
                        EnsureDefaultsForEpsiZ(ligne);
                    }

                    // Brancher la mise à jour auto et calculer NE initial
                    HookCoucheForAutoUpdate(ligne);
                    // ATTACH: synchronisation dynamique du nom du matériau avec la layer
                    AttachLayerSync(ligne, layer);

                    ligne.Ne = Math.Round(TraficCumule * ligne.Cam, 0);

                    // kr initial
                    UpdateKr(ligne);

                    // Calcul initial εz si applicable
                    if (string.Equals(ligne.Critere, "EpsiZ", StringComparison.OrdinalIgnoreCase))
                    {
                        ligne.Epsilon6 = ComputeEpsiZ(ligne.AmplitudeValue, ligne.Ne, ligne.B);
                    }

                    // Calcul initial de la valeur admissible
                    ligne.ValeurAdmissible = CalculerValeurAdmissibleSimple(ligne);

                    ValeursAdmissibles.Add(ligne);
                }

                // Persist the synchronized values into the project so other VMs (Results) see updates
                SaveToProject();

                System.Diagnostics.Debug.WriteLine($"VA Sync: {ValeursAdmissibles.Count} lignes");
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
                // éviter abonnements multiples
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
                // détacher l'ancien si présent
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
                            // Mettre à jour le champ Materiau de la ligne (provoque notification UI)
                            ligne.Materiau = layer.MaterialName ?? string.Empty;
                            // Sauvegarder la collection mise à jour dans le projet (DTO)
                            SaveToProject();
                            // Propager la modification pour forcer la mise à jour des résultats
                            AppState.OnStructureChanged();
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
                            couche.Epsilon6 = ComputeEpsiZ(couche.AmplitudeValue, couche.Ne, couche.B);
                        }
                    }

                    // Calcul kr si Risque, Sn, Sh, B, Critere changent
                    if (e.PropertyName == nameof(ValeurAdmissibleCouche.Risque) ||
                        e.PropertyName == nameof(ValeurAdmissibleCouche.Sn) ||
                        e.PropertyName == nameof(ValeurAdmissibleCouche.Sh) ||
                        e.PropertyName == nameof(ValeurAdmissibleCouche.B) ||
                        e.PropertyName == nameof(ValeurAdmissibleCouche.Critere))
                    {
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
                        c.Epsilon6 = ComputeEpsiZ(c.AmplitudeValue, c.Ne, c.B);
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
        private static double ComputeEpsiZ(double A, double NE, double exponentB)
        {
            if (NE <= 0) return 0;
            try
            {
                return A * Math.Pow(NE, exponentB);
            }
            catch { return 0; }
        }

        // Calcul kr: 10^(-b * u0 * delta) avec b = -1/B et delta = sqrt(SN^2 + (c * Sh / b)^2)
        private void UpdateKr(ValeurAdmissibleCouche c)
        {
            try
            {
                double B = c.B; // B est la valeur affichée dans la colonne -1/b
                if (Math.Abs(B) < 1e-9)
                {
                    B = B < 0 ? -1e-9 : 1e-9;
                }
                // Correction : b = -1/B
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

        // Approximation du quantile de la loi normale centrée réduite (Acklam-like)
        private static double NormalQuantile(double p)
        {
            // Coefficients pour l’approximation rationnelle
            // Source: coefficients génériques reconnus pour approximation de Phi^-1
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

            // Points de coupure
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
            else if (p > phigh)
            {
                double q = Math.Sqrt(-2 * Math.Log(1 - p));
                return -(((((c1 * q + c2) * q + c3) * q + c4) * q + c5) * q + c6) /
                        ((((d1 * q + d2) * q + d3) * q + d4) * q + 1);
            }
            else
            {
                double q = p - 0.5;
                double r = q * q;
                return (((((a1 * r + a2) * r + a3) * r + a4) * r + a5) * r + a6) * q /
                       (((((b1 * r + b2) * r + b3) * r + b4) * r + b5) * r + 1);
            }
        }

        private void EnsureDefaultsForEpsiZ(ValeurAdmissibleCouche c)
        {
            // forcer -1/b par défaut si non défini (proche de 0)
            if (Math.Abs(c.B) < 0.001) c.B = DefaultExponentEpsiZ;

            // si A n’a pas été saisi par l’utilisateur, l’ajuster selon NE
            if (!c.AUserDefined)
            {
                if (c.Ne > ThresholdNE) c.AmplitudeValue = DefaultA_HighNE; else c.AmplitudeValue = DefaultA_LowNE;
            }
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
        private double _ne = 0.0;
        private double _epsilon6 = 0.0;
        private double _valeurAdmissible;
        private double _amplitudeValue = 100;
        private double _sigma6 = 1.0; // nouvelle colonne σ6
        private double _cam;
        private double _e10C10Hz = 0.0;
        private double _eteq10Hz = 0.0;
        private bool _kthetaAuto = false;

        // Marqueur interne: A saisi par l’utilisateur (pour EpsiZ)
        internal bool AUserDefined { get; set; } = false;

        // Référence à la Layer source pour synchronisation dynamique
        internal Layer? SourceLayer { get; set; }
        internal PropertyChangedEventHandler? SourceLayerHandler { get; set; }

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
            set
            {
                _critere = value ?? "EpsiT";
                SafePropertyChanged();
                SafePropertyChanged(nameof(AmplitudeLabel));
                // Notifier états d’édition dépendants
                SafePropertyChanged(nameof(IsEpsiZ));
                SafePropertyChanged(nameof(IsEpsiT));
                SafePropertyChanged(nameof(IsSigmaT));
                SafePropertyChanged(nameof(CanEditEpsilon6));
                SafePropertyChanged(nameof(CanEditSn));
                SafePropertyChanged(nameof(CanEditSh));
                SafePropertyChanged(nameof(CanEditKc));
                SafePropertyChanged(nameof(CanEditKr));
                SafePropertyChanged(nameof(CanEditKs));
                SafePropertyChanged(nameof(CanEditKtheta));
                SafePropertyChanged(nameof(CanEditKd));
            }
        }

        // Etats d’édition/visibilité selon le critère
        public bool IsEpsiZ => string.Equals(_critere, "EpsiZ", StringComparison.OrdinalIgnoreCase);
        public bool IsEpsiT => string.Equals(_critere, "EpsiT", StringComparison.OrdinalIgnoreCase);
        public bool IsSigmaT => string.Equals(_critere, "SigmaT", StringComparison.OrdinalIgnoreCase);

        // ε6 (colonne dédiée): éditable uniquement pour EpsiT; auto pour EpsiZ; inutilisé pour SigmaT
        public bool CanEditEpsilon6 => IsEpsiT;
        // Sn/Sh désactivés quand Kr n'est pas éditable
        public bool CanEditSn => CanEditKr;
        public bool CanEditSh => CanEditKr;
        // Coefficients par critère
        public bool CanEditKc => IsEpsiT || IsSigmaT;
        public bool CanEditKr => IsEpsiT || IsSigmaT;
        public bool CanEditKs => IsEpsiT || IsSigmaT;
        public bool CanEditKtheta => IsEpsiT; // kθ éditable uniquement pour EpsiT
        public bool CanEditKd => IsSigmaT;    // seulement pour σt

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
            get => _kthetaAuto && E10C10Hz > 0 && Eteq10Hz > 0 ? Math.Sqrt(E10C10Hz / Eteq10Hz) : _ktheta;
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

        // Nouvelle colonne: NE (nombre d'essieux équivalents ou autre indicateur)
        public double Ne
        {
            get => _ne;
            set { _ne = value; SafePropertyChanged(); }
        }

        // Nouvelle colonne: Epsilon 6 (ε6)
        public double Epsilon6
        {
            get => _epsilon6;
            set { _epsilon6 = value; SafePropertyChanged(); }
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

        public double Sigma6
        {
            get => _sigma6;
            set { _sigma6 = value; SafePropertyChanged(); }
        }

        public string AmplitudeLabel => _critere switch
        {
            "EpsiZ" => "A",
            "SigmaT" => "ε6",
            _ => "ε6"
        };

        public double Cam
        {
            get => _cam;
            set { _cam = value; SafePropertyChanged(); }
        }

        public double E10C10Hz
        {
            get => _e10C10Hz;
            set { _e10C10Hz = value; SafePropertyChanged(); if (KthetaAuto) UpdateKtheta(); }
        }
        public double Eteq10Hz
        {
            get => _eteq10Hz;
            set { _eteq10Hz = value; SafePropertyChanged(); if (KthetaAuto) UpdateKtheta(); }
        }
        public bool KthetaAuto
        {
            get => _kthetaAuto;
            set { _kthetaAuto = value; SafePropertyChanged(); UpdateKtheta(); }
        }

        private void UpdateKtheta()
        {
            if (KthetaAuto && E10C10Hz > 0 && Eteq10Hz > 0)
                Ktheta = Math.Sqrt(E10C10Hz / Eteq10Hz);
            SafePropertyChanged(nameof(Ktheta));
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
