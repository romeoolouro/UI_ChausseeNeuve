using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Linq;
using System.Threading.Tasks;
using ChausseeNeuve.Domain.Models;
using UI_ChausseeNeuve.Services;
using UI_ChausseeNeuve.ViewModels;
using System.Collections.Specialized;

namespace UI_ChausseeNeuve.ViewModels
{
    /// <summary>
    /// ViewModel pour l'affichage des résultats de calcul de la chaussée
    /// Intégré avec le service de calcul basé sur votre code C++
    /// </summary>
    public class ResultatViewModel : INotifyPropertyChanged, IDisposable
    {
        #region Champs privés
        private bool _isCalculationInProgress;
        private string _calculationDuration = "0 sec";
        private bool _isStructureValid;
        private ObservableCollection<ResultatItem> _resultats;
        private readonly SolicitationCalculationService _calculationService;
        private string _calculationInfo = "";
        private bool _isHelpVisible;
        private bool _showDetailedInfo = true; // NOUVEAU : contrôle l'affichage des détails
        private ObservableCollection<ChausseeNeuve.Domain.Models.ValeurAdmissibleCoucheDto>? _lastValeursAdmissibles;
        private ObservableCollection<ValeurAdmissibleCouche>? _lastValeursAdmissiblesViewModel;
        #endregion

        #region Événements
        /// <summary>
        /// Événement pour les notifications toast
        /// </summary>
        public event Action<string, ToastType>? ToastRequested;
        #endregion

        #region Constructeur
        public ResultatViewModel()
        {
            _resultats = new ObservableCollection<ResultatItem>();
            _calculationService = new SolicitationCalculationService();

            // Initialiser les commandes
            CalculateStructureCommand = new RelayCommand(async () => await CalculateStructureAsync(), () => !IsCalculationInProgress);
            ShowHelpCommand = new RelayCommand(ShowHelp);
            HideHelpCommand = new RelayCommand(HideHelp);
            ToggleDetailedInfoCommand = new RelayCommand(ToggleDetailedInfo);

            // S'abonner aux changements de structure pour synchronisation automatique
            SubscribeToStructureChanges();

            // Synchronisation automatique des valeurs admissibles
            SubscribeToValeursAdmissiblesChanges();

            // Charger la structure actuelle au démarrage
            LoadCurrentStructure();
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
                CalculateStructureCommand.RaiseCanExecuteChanged();
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
        /// Informations sur le calcul effectué
        /// </summary>
        public string CalculationInfo
        {
            get => _calculationInfo;
            set
            {
                _calculationInfo = value;
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
                OnPropertyChanged(nameof(ValidationSummary));
            }
        }

        /// <summary>
        /// Message de validation affiché à l'utilisateur
        /// </summary>
        public string ValidationMessage => IsStructureValid
            ? "? Structure validée - Tous les critères sont respectés"
            : "? Structure non validée - Certains critères ne sont pas respectés";

        /// <summary>
        /// Couleur du message de validation
        /// </summary>
        public string ValidationColor => IsStructureValid ? "#28a745" : "#dc3545";

        /// <summary>
        /// Résumé détaillé de la validation avec compteurs
        /// </summary>
        public string ValidationSummary
        {
            get
            {
                var couches = Resultats.OfType<ResultatCouche>().ToList();
                if (couches.Count == 0) return "Aucune couche à valider";
                
                var validCount = couches.Count(c => c.EstValide);
                var totalCount = couches.Count;
                
                return $"{validCount}/{totalCount} couches validées";
            }
        }

        /// <summary>
        /// Collection des résultats (couches + interfaces intercalées)
        /// </summary>
        public ObservableCollection<ResultatItem> Resultats
        {
            get => _resultats;
            set
            {
                _resultats = value;
                OnPropertyChanged();
                UpdateValidationStatus();
            }
        }

        /// <summary>
        /// Indique si l'aide est visible
        /// </summary>
        public bool IsHelpVisible
        {
            get => _isHelpVisible;
            set
            {
                _isHelpVisible = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Indique si les informations détaillées de calcul doivent être affichées
        /// Masqué automatiquement après un calcul réussi pour économiser l'espace
        /// </summary>
        public bool ShowDetailedInfo
        {
            get => _showDetailedInfo;
            set
            {
                _showDetailedInfo = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Commandes

        /// <summary>
        /// Commande pour lancer le calcul de la structure
        /// </summary>
        public RelayCommand CalculateStructureCommand { get; }

        /// <summary>
        /// Commande pour afficher l'aide
        /// </summary>
        public RelayCommand ShowHelpCommand { get; }

        /// <summary>
        /// Commande pour masquer l'aide
        /// </summary>
        public RelayCommand HideHelpCommand { get; }

        /// <summary>
        /// Commande pour basculer l'affichage des détails de calcul
        /// </summary>
        public RelayCommand ToggleDetailedInfoCommand { get; }

        #endregion

        #region Méthodes publiques

        /// <summary>
        /// Lance le calcul de la structure de façon asynchrone
        /// Utilise votre code C++ via le service de calcul
        /// </summary>
        public async Task CalculateStructureAsync()
        {
            IsCalculationInProgress = true;
            
            try
            {
                var startTime = DateTime.Now;
                
                // Validation préalable de la structure
                if (!ValidateCurrentStructure())
                {
                    ToastRequested?.Invoke("Structure invalide - Veuillez vérifier la configuration", ToastType.Error);
                    return;
                }

                ToastRequested?.Invoke("Calcul des sollicitations en cours...", ToastType.Info);

                // Calcul via le service basé sur votre code C++
                var calculationResult = await Task.Run(() => 
                    _calculationService.CalculateSolicitations(AppState.CurrentProject.PavementStructure));
                
                if (calculationResult.IsSuccessful)
                {
                    // Affichage des informations de calcul SIMPLIFIEES
                    CalculationInfo = "Calcul termine avec succes";
                    
                    // MASQUER automatiquement les détails après un calcul réussi
                    ShowDetailedInfo = false;
                    
                    // Reconstruction complète avec couches ET interfaces 
                    PopulateResultsWithCalculatedData(calculationResult);

                    var duration = DateTime.Now - startTime;
                    CalculationDuration = FormatDuration(calculationResult.CalculationTimeMs / 1000.0);
                    
                    // Mise à jour finale de la validation
                    UpdateValidationStatus();

                    // Forcer la synchronisation des valeurs admissibles dans les résultats
                    InjectValeursAdmissiblesDansResultats();
                }
                else
                {
                    ToastRequested?.Invoke(calculationResult.Message, ToastType.Error);
                }
            }
            catch (Exception ex)
            {
                ToastRequested?.Invoke($"Erreur inattendue : {ex.Message}", ToastType.Error);
            }
            finally
            {
                IsCalculationInProgress = false;
            }
        }

        /// <summary>
        /// Peuple les résultats avec les données calculées, en incluant les interfaces
        /// </summary>
        private void PopulateResultsWithCalculatedData(SolicitationCalculationResult calculationResult)
        {
            var structure = AppState.CurrentProject.PavementStructure;
            if (structure?.Layers == null) return;

            var orderedLayers = structure.Layers
                .Where(l => l.Role != LayerRole.Plateforme)
                .OrderBy(l => l.Order)
                .ToList();

            var platform = structure.Layers.FirstOrDefault(l => l.Role == LayerRole.Plateforme);

            Resultats.Clear();

            // Créer un dictionnaire pour retrouver facilement les résultats par couche
            var layerResultsDict = calculationResult.LayerResults.ToDictionary(
                lr => lr.Layer.Order,
                lr => lr
            );

            /// Ajouter les couches normales avec leurs interfaces
            for (int i = 0; i < orderedLayers.Count; i++)
            {
                var layer = orderedLayers[i];
                // Ajouter la couche avec les données calculées
                var resultCouche = CreateResultCoucheWithCalculatedData(layer, layerResultsDict);
                resultCouche.Numero = i + 1;
                resultCouche.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(ResultatCouche.EstValide))
                    {
                        UpdateValidationStatus();
                    }
                };
                Resultats.Add(resultCouche);

                // Ajouter l'interface si ce n'est pas la dernière couche
                if (i < orderedLayers.Count - 1 || platform != null)
                {
                    var nextLayer = i < orderedLayers.Count - 1 ? orderedLayers[i + 1] : platform;
                    if (nextLayer != null)
                    {
                        var interfaceResult = CreateResultInterfaceFromLayer(layer, nextLayer);
                        Resultats.Add(interfaceResult);
                    }
                }
            }

            // Ajouter la plateforme si elle existe
            if (platform != null)
            {
                var resultPlateforme = CreateResultCoucheWithCalculatedData(platform, layerResultsDict);
                resultPlateforme.Numero = orderedLayers.Count + 1;
                resultPlateforme.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(ResultatCouche.EstValide))
                    {
                        UpdateValidationStatus();
                    }
                };
                Resultats.Add(resultPlateforme);
            }

            // Injection des valeurs admissibles calculées
            InjectValeursAdmissiblesDansResultats();
        }

        /// <summary>
        /// Crée un ResultatCouche avec les données calculées
        /// </summary>
        private ResultatCouche CreateResultCoucheWithCalculatedData(Layer layer, Dictionary<int, LayerSolicitationResult> layerResultsDict)
        {
            // Récupérer les données calculées si disponibles
            if (layerResultsDict.TryGetValue(layer.Order, out var layerResult))
            {
                // Couche avec données calculées
                return new ResultatCouche
                {
                    Interface = layer.Role.ToString(),
                    Materiau = GetMaterialDisplayName(layer),
                    NiveauSup = GetLayerTopLevel(layer),
                    NiveauInf = GetLayerBottomLevel(layer),
                    Module = layerResult.Module,
                    CoefficientPoisson = layerResult.CoefficientPoisson,
                    SigmaTSup = layerResult.SigmaTTop,
                    SigmaTInf = layerResult.SigmaTBottom,
                    EpsilonTSup = layerResult.EpsilonTTop,
                    EpsilonTInf = layerResult.EpsilonTBottom,
                    SigmaZ = layerResult.SigmaZTop,
                    EpsilonZ = layerResult.EpsilonZTop,
                    SigmaZSup = layerResult.SigmaZTop,
                    SigmaZInf = layerResult.SigmaZBottom,
                    EpsilonZSup = layerResult.EpsilonZTop,
                    EpsilonZInf = layerResult.EpsilonZBottom,
                    DeflexionSup = layerResult.DeflectionTop,
                    DeflexionInf = layerResult.DeflectionBottom,
                    ValeurAdmissible = CalculateAdmissibleValue(layerResult),
                    EstValide = ValidateLayerResult(layerResult)
                };
            }
            else
            {
                // Fallback : couche sans données calculées (ne devrait pas arriver)
                return CreateResultCoucheFromLayer(layer, AppState.CurrentProject.PavementStructure);
            }
        }

        /// <summary>
        /// Méthode publique pour forcer la mise à jour depuis l'extérieur
        /// À appeler depuis StructureEditorViewModel quand la structure change
        /// </summary>
        public void RefreshFromStructure()
        {
            LoadCurrentStructure();
        }

        #endregion

        #region Méthodes privées

        private bool ValidateCurrentStructure()
        {
            var structure = AppState.CurrentProject.PavementStructure;
            
            if (structure.Layers.Count < 2)
            {
                return false;
            }

            var platformCount = structure.Layers.Count(l => l.Role == LayerRole.Plateforme);
            if (platformCount != 1)
            {
                return false;
            }

            return true;
        }

        private double GetLayerTopLevel(Layer layer)
        {
            var layers = AppState.CurrentProject.PavementStructure.Layers
                .Where(l => l.Role != LayerRole.Plateforme)
                .OrderBy(l => l.Order)
                .ToList();
            
            double cumul = 0;
            foreach (var l in layers)
            {
                if (l.Order >= layer.Order) break;
                cumul += l.Thickness_m * 100; // Conversion en cm
            }
            return cumul;
        }

        private double GetLayerBottomLevel(Layer layer)
        {
            if (layer.Role == LayerRole.Plateforme)
            {
                return GetLayerTopLevel(layer); // La plateforme n'a pas de niveau inférieur affiché
            }
            return GetLayerTopLevel(layer) + layer.Thickness_m * 100;
        }

        private double CalculateAdmissibleValue(LayerSolicitationResult layerResult)
        {
            // Calcul des valeurs admissibles selon le type de matériau
            // TODO: Implémenter les formules NF P98-086 complètes
            return layerResult.Layer.Family switch
            {
                MaterialFamily.BetonBitumineux => 100.0, // ?t admissible en ?def
                MaterialFamily.GNT => 80.0, // ?z admissible en ?def  
                MaterialFamily.MTLH => 120.0,
                MaterialFamily.BetonCiment => 150.0,
                _ => 90.0
            };
        }

        private bool ValidateLayerResult(LayerSolicitationResult layerResult)
        {
            // Validation selon les critères NF P98-086
            var admissibleValue = CalculateAdmissibleValue(layerResult);
            
            return layerResult.Layer.Family switch
            {
                MaterialFamily.BetonBitumineux => layerResult.EpsilonTCritical < admissibleValue,
                MaterialFamily.GNT => layerResult.EpsilonZCritical < Math.Abs(admissibleValue),
                MaterialFamily.MTLH => layerResult.EpsilonTCritical < admissibleValue,
                MaterialFamily.BetonCiment => layerResult.SigmaTCritical < (admissibleValue / 10.0), // Conversion MPa
                _ => true
            };
        }

        /// <summary>
        /// Chargement des données d'exemple basées sur votre code C++
        /// Simule l'affichage style Alizé avec interfaces entre les couches
        /// </summary>
        private void LoadSampleData()
        {
            // Données d'exemple qui correspondent à votre main() C++
            // nbrecouche = 4, roue = 2, Poids = 0.662, a = 0.125, d = 0.375
            CalculationInfo = "Donnees d'exemple chargees (4 couches)";
            
            Resultats.Clear();
            
            // Fonction helper pour ajouter une couche avec abonnement automatique
            void AddCoucheWithValidation(ResultatCouche couche)
            {
                couche.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(ResultatCouche.EstValide))
                    {
                        UpdateValidationStatus();
                    }
                };
                Resultats.Add(couche);
            }
            
            // Couche 1 : Surface
            AddCoucheWithValidation(new ResultatCouche
            {
                Interface = "Roulement",
                Materiau = "EB-BBSG 0/10",
                NiveauSup = 0,
                NiveauInf = 6,
                Module = 7000,
                CoefficientPoisson = 0.35,
                SigmaTSup = 0.150,
                SigmaTInf = 0.120,
                EpsilonTSup = 28.5,
                EpsilonTInf = 22.1,
                SigmaZ = -0.662,
                EpsilonZ = -157.4,
                SigmaZSup = -0.662,
                SigmaZInf = -0.850,
                EpsilonZSup = -157.4,
                EpsilonZInf = -180.2,
                DeflexionSup = 0.0,
                DeflexionInf = 0.15,
                ValeurAdmissible = 100.0,
                EstValide = true
            });

            // Interface 1 : Surface/Base
            Resultats.Add(new ResultatInterface
            {
                TypeInterface = "Collée",
                Description = "Interface Surface/Base"
            });

            // Couche 2 : Base
            AddCoucheWithValidation(new ResultatCouche
            {
                Interface = "Base",
                Materiau = "GC (Grave Ciment)",
                NiveauSup = 6,
                NiveauInf = 21,
                Module = 23000,
                CoefficientPoisson = 0.25,
                SigmaTSup = 0.080,
                SigmaTInf = 0.060,
                EpsilonTSup = 18.2,
                EpsilonTInf = 15.8,
                SigmaZ = -0.620,
                EpsilonZ = -221.4,
                SigmaZSup = -0.620,
                SigmaZInf = -0.780,
                EpsilonZSup = -221.4,
                EpsilonZInf = -285.6,
                ValeurAdmissible = 120.0,
                EstValide = true
            });

            // Interface 2 : Base/Fondation
            Resultats.Add(new ResultatInterface
            {
                TypeInterface = "Semi-collée",
                Description = "Interface Base/Fondation"
            });

            // Couche 3 : Fondation
            AddCoucheWithValidation(new ResultatCouche
            {
                Interface = "Fondation",
                Materiau = "SC (Sable Ciment)",
                NiveauSup = 21,
                NiveauInf = 36,
                Module = 23000,
                CoefficientPoisson = 0.25,
                SigmaTSup = 0.040,
                SigmaTInf = 0.020,
                EpsilonTSup = 12.4,
                EpsilonTInf = 8.7,
                SigmaZ = -0.450,
                EpsilonZ = -184.3,
                SigmaZSup = -0.450,
                SigmaZInf = -0.520,
                EpsilonZSup = -184.3,
                EpsilonZInf = -220.8,
                ValeurAdmissible = 120.0,
                EstValide = true
            });

            // Interface 3 : Fondation/Plateforme
            Resultats.Add(new ResultatInterface
            {
                TypeInterface = "Collée",
                Description = "Interface Fondation/Plateforme"
            });

            // Couche 4 : Plateforme (sans interface suivante)
            AddCoucheWithValidation(new ResultatCouche
            {
                Interface = "Plateforme",
                Materiau = "Plateforme PF2",
                NiveauSup = 36,
                NiveauInf = double.PositiveInfinity, // Plateforme semi-infinie
                Module = 120,
                CoefficientPoisson = 0.35,
                SigmaTSup = 0.020,  // Seule valeur de la plateforme
                SigmaTInf = 0,      // Sera remplacé par "-" dans l'affichage
                EpsilonTSup = 5.2,  // Seule valeur de la plateforme  
                EpsilonTInf = 0,    // Sera remplacé par "-" dans l'affichage
                SigmaZ = -0.200,
                EpsilonZ = -85.1,
                SigmaZSup = -0.200, // Seule valeur de la plateforme
                SigmaZInf = 0,      // Sera remplacé par "-" dans l'affichage
                EpsilonZSup = -85.1, // Seule valeur de la plateforme
                EpsilonZInf = 0,     // Sera remplacé par "-" dans l'affichage
                DeflexionSup = 0.0,  // Pas de déflexion pour la plateforme
                DeflexionInf = 0,    // Sera remplacé par "-" dans l'affichage  
                ValeurAdmissible = 200.0,
                EstValide = true
            });

            // Mise à jour de la validation après ajout des données
            UpdateValidationStatus();
        }

        /// <summary>
        /// Met à jour le statut de validation en fonction des résultats actuels
        /// </summary>
        private void UpdateValidationStatus()
        {
            IsStructureValid = Resultats.OfType<ResultatCouche>().All(r => r.EstValide);
        }

        /// <summary>
        /// Affiche l'aide
        /// </summary>
        private void ShowHelp()
        {
            IsHelpVisible = true;
        }

        /// <summary>
        /// Masque l'aide
        /// </summary>
        private void HideHelp()
        {
            IsHelpVisible = false;
        }

        /// <summary>
        /// Bascule l'affichage des détails de calcul
        /// </summary>
        private void ToggleDetailedInfo()
        {
            ShowDetailedInfo = !ShowDetailedInfo;
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

        /// <summary>
        /// S'abonne aux changements de structure pour synchronisation automatique
        /// </summary>
        private void SubscribeToStructureChanges()
        {
            // S'abonner à l'événement global de changement de structure
            AppState.StructureChanged += OnStructureChanged;
        }
        
        /// <summary>
        /// Appelé quand la structure change dans AppState
        /// </summary>
        private void OnStructureChanged()
        {
            // Exécuter sur le thread UI si nécessaire
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                LoadCurrentStructure();
            });
        }

        /// <summary>
        /// Charge la structure actuelle depuis AppState et met à jour l'affichage
        /// </summary>
        private void LoadCurrentStructure()
        {
            try
            {
                if (AppState.CurrentProject?.PavementStructure?.Layers?.Count > 0)
                {
                    UpdateResultsFromCurrentStructure();
                }
                else
                {
                    LoadSampleData(); // Fallback sur les données d'exemple
                }
            }
            catch (Exception ex)
            {
                // En cas d'erreur, charger les données d'exemple
                LoadSampleData();
                System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement de la structure: {ex.Message}");
            }
        }

        /// <summary>
        /// Met à jour les résultats depuis la structure actuelle (sans calcul de sollicitations)
        /// Affiche les propriétés de base : matériaux, niveaux, modules, etc.
        /// </summary>
        private void UpdateResultsFromCurrentStructure()
        {
            var structure = AppState.CurrentProject.PavementStructure;
            if (structure?.Layers == null) return;

            var orderedLayers = structure.Layers
                .Where(l => l.Role != LayerRole.Plateforme)
                .OrderBy(l => l.Order)
                .ToList();

            var platform = structure.Layers.FirstOrDefault(l => l.Role == LayerRole.Plateforme);

            Resultats.Clear();
            CalculationInfo = $"Structure synchronisee : {orderedLayers.Count + (platform != null ? 1 : 0)} couches - Calcul requis";

            // Ajouter les couches normales avec interfaces
            for (int i = 0; i < orderedLayers.Count; i++)
            {
                var layer = orderedLayers[i];
                
                // Ajouter la couche
                var resultCouche = CreateResultCoucheFromLayer(layer, structure);
                resultCouche.Numero = i + 1; // Synchronise le numéro avec l'ordre de la structure
                resultCouche.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(ResultatCouche.EstValide))
                    {
                        UpdateValidationStatus();
                    }
                };
                Resultats.Add(resultCouche);

                // Ajouter l'interface si ce n'est pas la dernière couche
                if (i < orderedLayers.Count - 1 || platform != null)
                {
                    var nextLayer = i < orderedLayers.Count - 1 ? orderedLayers[i + 1] : platform;
                    if (nextLayer != null)
                    {
                        var interfaceResult = CreateResultInterfaceFromLayer(layer, nextLayer);
                        Resultats.Add(interfaceResult);
                    }
                }
            }

            // Ajouter la plateforme si elle existe
            if (platform != null)
            {
                var resultPlateforme = CreateResultCoucheFromLayer(platform, structure);
                resultPlateforme.Numero = orderedLayers.Count + 1; // Numéro plateforme = dernier + 1
                resultPlateforme.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(ResultatCouche.EstValide))
                    {
                        UpdateValidationStatus();
                    }
                };
                Resultats.Add(resultPlateforme);
            }

            // Injection des valeurs admissibles calculées
            InjectValeursAdmissiblesDansResultats();
            UpdateValidationStatus();
        }

        /// <summary>
        /// Crée un ResultatCouche depuis une Layer (sans sollicitations calculées)
        /// </summary>
        private ResultatCouche CreateResultCoucheFromLayer(Layer layer, PavementStructure structure)
        {
            return new ResultatCouche
            {
                Interface = layer.Role.ToString(),
                Materiau = GetMaterialDisplayName(layer),
                NiveauSup = GetLayerTopLevel(layer),
                NiveauInf = GetLayerBottomLevel(layer),
                Module = layer.Modulus_MPa,
                CoefficientPoisson = layer.Poisson,
                
                // Sollicitations initialisées à zéro (à calculer)
                SigmaTSup = 0,
                SigmaTInf = 0,
                EpsilonTSup = 0,
                EpsilonTInf = 0,
                SigmaZ = 0,
                EpsilonZ = 0,
                SigmaZSup = 0,
                SigmaZInf = 0,
                EpsilonZSup = 0,
                EpsilonZInf = 0,
                DeflexionSup = 0,
                DeflexionInf = 0,
                
                ValeurAdmissible = GetDefaultAdmissibleValue(layer.Family),
                EstValide = true // Par défaut, sera mis à jour après calcul
            };
        }

        /// <summary>
        /// Obtient le nom d'affichage du matériau pour synchronisation parfaite avec la structure
        /// </summary>
        private string GetMaterialDisplayName(Layer layer)
        {
            // Toujours utiliser le nom exact du matériau défini dans la structure
            return layer.MaterialName ?? string.Empty;
        }

        /// <summary>
        /// Crée un ResultatInterface depuis deux layers
        /// </summary>
        private ResultatInterface CreateResultInterfaceFromLayer(Layer upperLayer, Layer lowerLayer)
        {
            var interfaceType = GetInterfaceTypeDescription(upperLayer.InterfaceWithBelow);
            return new ResultatInterface
            {
                TypeInterface = interfaceType,
                Description = $"Interface {upperLayer.Role}/{lowerLayer.Role}"
            };
        }

        /// <summary>
        /// Obtient une valeur admissible par défaut selon la famille de matériau
        /// </summary>
        private double GetDefaultAdmissibleValue(MaterialFamily family)
        {
            return family switch
            {
                MaterialFamily.BetonBitumineux => 100.0,
                MaterialFamily.GNT => 80.0,
                MaterialFamily.MTLH => 120.0,
                MaterialFamily.BetonCiment => 150.0,
                _ => 90.0
            };
        }

        /// <summary>
        /// Convertit le type d'interface en description
        /// </summary>
        private string GetInterfaceTypeDescription(InterfaceType? interfaceType)
        {
            return interfaceType switch
            {
                InterfaceType.Collee => "Collée",
                InterfaceType.SemiCollee => "Semi-collée",
                InterfaceType.Decollee => "Décollée",
                _ => "Collée"
            };
        }

        private ObservableCollection<ValeurAdmissibleCouche>? GetValeursAdmissiblesViewModelCollection()
        {
            var dtoCol = AppState.CurrentProject?.ValeursAdmissibles;
            if (dtoCol == null) return null;

            // Always rebuild the VM collection from DTOs to ensure updates (names, values) are reflected
            var vmList = dtoCol.Select(d => new ValeurAdmissibleCouche
            {
                Materiau = (d as dynamic).Materiau,
                Niveau = (int)(d as dynamic).Niveau,
                Critere = (d as dynamic).Critere,
                Sn = (double)(d as dynamic).Sn,
                Sh = (double)(d as dynamic).Sh,
                B = (double)(d as dynamic).B,
                Kc = (double)(d as dynamic).Kc,
                Kr = (double)(d as dynamic).Kr,
                Ks = (double)(d as dynamic).Ks,
                Ktheta = (double)(d as dynamic).Ktheta,
                Kd = (double)(d as dynamic).Kd,
                Risque = (double)(d as dynamic).Risque,
                Ne = (double)(d as dynamic).Ne,
                Epsilon6 = (double)(d as dynamic).Epsilon6,
                ValeurAdmissible = (double)(d as dynamic).ValeurAdmissible,
                AmplitudeValue = (double)(d as dynamic).AmplitudeValue,
                Sigma6 = (double)(d as dynamic).Sigma6,
                Cam = (double)(d as dynamic).Cam,
                E10C10Hz = (double)(d as dynamic).E10C10Hz,
                Eteq10Hz = (double)(d as dynamic).Eteq10Hz,
                KthetaAuto = (bool)(d as dynamic).KthetaAuto
            }).ToList();

            // Replace cached reference and return a fresh ObservableCollection
            _lastValeursAdmissiblesViewModel = new ObservableCollection<ValeurAdmissibleCouche>(vmList);
            return _lastValeursAdmissiblesViewModel;
        }

        private void SubscribeToValeursAdmissiblesChanges()
        {
            // Désabonnement de l'ancienne collection
            if (_lastValeursAdmissiblesViewModel != null)
            {
                _lastValeursAdmissiblesViewModel.CollectionChanged -= ValeursAdmissibles_CollectionChanged;
                foreach (var v in _lastValeursAdmissiblesViewModel)
                    v.PropertyChanged -= ValeurAdmissible_PropertyChanged;
            }

            var valeurs = GetValeursAdmissiblesViewModelCollection();
            if (valeurs == null) return;
            valeurs.CollectionChanged -= ValeursAdmissibles_CollectionChanged;
            valeurs.CollectionChanged += ValeursAdmissibles_CollectionChanged;
            foreach (var v in valeurs)
            {
                v.PropertyChanged -= ValeurAdmissible_PropertyChanged;
                v.PropertyChanged += ValeurAdmissible_PropertyChanged;
            }
            _lastValeursAdmissiblesViewModel = valeurs;
            InjectValeursAdmissiblesDansResultats();
        }

        private void ValeursAdmissibles_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (var v in e.NewItems)
                    if (v is INotifyPropertyChanged npc)
                    {
                        npc.PropertyChanged -= ValeurAdmissible_PropertyChanged;
                        npc.PropertyChanged += ValeurAdmissible_PropertyChanged;
                    }
            if (e.OldItems != null)
                foreach (var v in e.OldItems)
                    if (v is INotifyPropertyChanged npc)
                        npc.PropertyChanged -= ValeurAdmissible_PropertyChanged;
            InjectValeursAdmissiblesDansResultats();
        }

        private void ValeurAdmissible_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ValeurAdmissible" || e.PropertyName == "Materiau" || e.PropertyName == "Niveau")
                InjectValeursAdmissiblesDansResultats();
        }

        // Ajout : synchronisation des valeurs admissibles dans les résultats
        private void InjectValeursAdmissiblesDansResultats()
        {
            // Synchroniser chaque couche résultat avec la valeur admissible correspondante
            var valeursAdmissibles = GetValeursAdmissiblesViewModelCollection();
            if (valeursAdmissibles == null) return;
            foreach (var resultat in Resultats.OfType<ResultatCouche>())
            {
                // Correspondance stricte sur N (Numero) et Materiau
                var coucheAdmissible = valeursAdmissibles.FirstOrDefault(c =>
                    c.Niveau == resultat.Numero &&
                    string.Equals(c.Materiau?.Trim(), resultat.Materiau?.Trim(), StringComparison.InvariantCultureIgnoreCase)
                );
                resultat.ValeurAdmissible = coucheAdmissible?.ValeurAdmissible ?? 0;
            }
            // Forcer la notification de la propriété Resultats
            OnPropertyChanged(nameof(Resultats));
        }

        #endregion

        #region IDisposable
        
        public void Dispose()
        {
            // Se désabonner de l'événement pour éviter les fuites mémoire
            AppState.StructureChanged -= OnStructureChanged;
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
    /// Classe de base pour les éléments de résultat (couches ou interfaces)
    /// </summary>
    public abstract class ResultatItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Résultat pour une couche de chaussée
    /// </summary>
    public class ResultatCouche : ResultatItem
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
        private double _sigmaZSup;
        private double _sigmaZInf;
        private double _epsilonZSup;
        private double _epsilonZInf;
        private double _deflexionSup;
        private double _deflexionInf;
        private double _valeurAdmissible;
        private bool _estValide;
        private int _numero;

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

        /// <summary>Contrainte verticale supérieure en MPa</summary>
        public double SigmaZSup
        {
            get => _sigmaZSup;
            set { _sigmaZSup = value; OnPropertyChanged(); }
        }

        /// <summary>Contrainte verticale inférieure en MPa</summary>
        public double SigmaZInf
        {
            get => _sigmaZInf;
            set { _sigmaZInf = value; OnPropertyChanged(); }
        }

        /// <summary>Déformation verticale supérieure en micro-déformation</summary>
        public double EpsilonZSup
        {
            get => _epsilonZSup;
            set { _epsilonZSup = value; OnPropertyChanged(); }
        }

        /// <summary>Déformation verticale inférieure en micro-déformation</summary>
        public double EpsilonZInf
        {
            get => _epsilonZInf;
            set { _epsilonZInf = value; OnPropertyChanged(); }
        }

        /// <summary>Déflexion supérieure en mm/100</summary>
        public double DeflexionSup
        {
            get => _deflexionSup;
            set { _deflexionSup = value; OnPropertyChanged(); }
        }

        /// <summary>Déflexion inférieure en mm/100</summary>
        public double DeflexionInf
        {
            get => _deflexionInf;
            set { _deflexionInf = value; OnPropertyChanged(); }
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
            set { _estValide = value; OnPropertyChanged(); OnPropertyChanged(nameof(CouleurValidation)); OnPropertyChanged(nameof(StatutValidation)); }
        }

        /// <summary>Couleur à utiliser pour afficher le statut de validation</summary>
        public string CouleurValidation => EstValide ? "#d4edda" : "#f8d7da";

        /// <summary>Symbole de statut de validation (alternative directe sans converter)</summary>
        public string StatutValidation => EstValide ? "\u2713" : "\u2717"; // ? ou ?

        /// <summary>Valeur critique utilisée pour la validation</summary>
        public double ValeurCritique => Math.Max(Math.Abs(EpsilonTSup), Math.Abs(EpsilonTInf));

        /// <summary>Taux d'utilisation en pourcentage</summary>
        public double TauxUtilisation => ValeurAdmissible > 0 ? (ValeurCritique / ValeurAdmissible) * 100 : 0;

        /// <summary>Indique si c'est la plateforme (niveau inférieur infini)</summary>
        public bool EstPlateforme => Interface == "Plateforme" || double.IsPositiveInfinity(NiveauInf);

        /// <summary>Niveau inférieur formaté pour l'affichage (avec tiret pour la plateforme)</summary>
        public string NiveauInfDisplay => EstPlateforme ? "-" : NiveauInf.ToString("F0");

        /// <summary>Numéro de la couche (pour affichage dans la grille des résultats)</summary>
        public int Numero
        {
            get => _numero;
            set { _numero = value; OnPropertyChanged(); OnPropertyChanged(nameof(NiveauDisplay)); }
        }

        /// <summary>Niveau affiché (pour utilisation dans la grille)</summary>
        public string NiveauDisplay => (Numero > 0) ? Numero.ToString() : "";

        /// <summary>Nature de la couche (correspond à l'interface)</summary>
        public string Nature => Interface;

        /// <summary>
        /// Affichage de l'interface sans doublon avec la nature
        /// </summary>
        public string InterfaceAffichee => Interface == Nature ? string.Empty : Interface;
    }

    /// <summary>
    /// Résultat pour une interface entre deux couches
    /// </summary>
    public class ResultatInterface : ResultatItem
    {
        private string _typeInterface = "";
        private string _description = "";

        /// <summary>Type d'interface (Collée, Semi-collée, Décollée)</summary>
        public string TypeInterface
        {
            get => _typeInterface;
            set { _typeInterface = value; OnPropertyChanged(); OnPropertyChanged(nameof(CouleurInterface)); }
        }

        /// <summary>Description de l'interface (ex: "Interface Surface/Base")</summary>
        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        /// <summary>Couleur à utiliser pour afficher le type d'interface</summary>
        public string CouleurInterface => TypeInterface switch
        {
            "Collée" => "#28a745",      // Vert pour interface collée
            "Semi-collée" => "#ffc107", // Jaune pour interface semi-collée
            "Décollée" => "#dc3545",    // Rouge pour interface décollée
            _ => "#6c757d"              // Gris pour interface inconnue
        };
    }
}