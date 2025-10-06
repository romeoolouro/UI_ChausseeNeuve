using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Linq;
using System.Threading.Tasks;
using ChausseeNeuve.Domain.Models;
using UI_ChausseeNeuve.Services;
using UI_ChausseeNeuve.Services.PavementCalculation;
using UI_ChausseeNeuve.ViewModels;
using System.Collections.Specialized;

namespace UI_ChausseeNeuve.ViewModels
{
    public enum CritereVerification { EpsiZ, SigmaT, EpsiT }

    public class ResultatViewModel : INotifyPropertyChanged, IDisposable
    {
        #region Champs priv�s
        private bool _isCalculationInProgress;
        private string _calculationDuration = "0 sec";
        private bool _isStructureValid;
        private ObservableCollection<ResultatItem> _resultats;
        private readonly HybridPavementCalculationService _calculationService;
        private string _calculationInfo = "";
        private bool _isHelpVisible;
        private bool _showDetailedInfo = true; // NOUVEAU : contr�le l'affichage des d�tails
        private ObservableCollection<ValeurAdmissibleCouche>? _lastValeursAdmissiblesViewModel;
        private string _performanceInfo = "";
        #endregion

        #region �v�nements
        /// <summary>
        /// �v�nement pour les notifications toast
        /// </summary>
        public event Action<string, ToastType>? ToastRequested;
        #endregion

        #region Constructeur
        public ResultatViewModel()
        {
            Console.WriteLine("=== RESULTAT VIEW MODEL: Constructeur appelé - Navigation vers section structures ===");
            _resultats = new ObservableCollection<ResultatItem>();
            _calculationService = new HybridPavementCalculationService();
            Console.WriteLine($"=== RESULTAT VIEW MODEL: Service de calcul hybride initialisé - Mode: {_calculationService.CurrentMode} ===");

            // Initialiser les commandes
            CalculateStructureCommand = new RelayCommand(async () => await CalculateStructureAsync(), () => !IsCalculationInProgress);
            ShowHelpCommand = new RelayCommand(ShowHelp);
            HideHelpCommand = new RelayCommand(HideHelp);
            ToggleDetailedInfoCommand = new RelayCommand(ToggleDetailedInfo);

            // S'abonner aux changements de structure pour synchronisation automatique
            SubscribeToStructureChanges();

            // Synchronisation automatique des valeurs admissibles
            SubscribeToValeursAdmissiblesChanges();

            // Abonnement au nouvel �v�nement pour mise � jour directe des Val. Adm.
            AppState.ValeursAdmissiblesUpdated += OnValeursAdmissiblesUpdated;

            // Charger la structure actuelle au d�marrage
            LoadCurrentStructure();
        }
        #endregion

        #region Propri�t�s publiques

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
        /// Dur�e du dernier calcul
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
        /// Informations sur le calcul effectu�
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
        /// Indique si la structure calcul�e est valide
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
        /// Message de validation affich� � l'utilisateur
        /// </summary>
        public string ValidationMessage => IsStructureValid
            ? "? Structure valid�e - Tous les crit�res sont respect�s"
            : "? Structure non valid�e - Certains crit�res ne sont pas respect�s";

        /// <summary>
        /// Couleur du message de validation
        /// </summary>
        public string ValidationColor => IsStructureValid ? "#28a745" : "#dc3545";

        /// <summary>
        /// R�sum� d�taill� de la validation avec compteurs
        /// </summary>
        public string ValidationSummary
        {
            get
            {
                var couches = Resultats.OfType<ResultatCouche>().ToList();
                if (couches.Count == 0) return "Aucune couche � valider";
                
                var validCount = couches.Count(c => c.EstValide);
                var totalCount = couches.Count;
                
                return $"{validCount}/{totalCount} couches valid�es";
            }
        }

        /// <summary>
        /// Collection des r�sultats (couches + interfaces intercal�es)
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
        /// Indique si les informations d�taill�es de calcul doivent �tre affich�es
        /// Masqu� automatiquement apr�s un calcul r�ussi pour �conomiser l'espace
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

        /// <summary>
        /// Mode de calcul utilis� par le service hybride
        /// </summary>
        public CalculationMode CurrentCalculationMode => _calculationService.CurrentMode;

        /// <summary>
        /// Informations de performance du dernier calcul
        /// </summary>
        public string PerformanceInfo
        {
            get => _performanceInfo;
            set
            {
                _performanceInfo = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Informations sur le moteur de calcul utilis�
        /// </summary>
        public string EngineInfo => _calculationService.EngineInfo;

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
        /// Commande pour basculer l'affichage des d�tails de calcul
        /// </summary>
        public RelayCommand ToggleDetailedInfoCommand { get; }

        #endregion

        #region M�thodes publiques

        /// <summary>
        /// Lance le calcul de la structure de façon asynchrone
        /// Utilise le service hybride (Native C++ ou Legacy C#)
        /// </summary>
        public async Task CalculateStructureAsync()
        {
            Console.WriteLine($"\n=== RESULTAT VIEW MODEL: CalculateStructureAsync called ===");
            IsCalculationInProgress = true;
            
            try
            {
                var startTime = DateTime.Now;
                
                // Validation préalable de la structure
                if (!ValidateCurrentStructure())
                {
                    Console.WriteLine("Structure validation FAILED");
                    ToastRequested?.Invoke("Structure invalide - Veuillez vérifier la configuration", ToastType.Error);
                    return;
                }

                Console.WriteLine($"Structure validated: {AppState.CurrentProject.PavementStructure.Layers.Count} layers");
                ToastRequested?.Invoke("Calcul des sollicitations en cours...", ToastType.Info);

                // Calcul via le service hybride (Native ou Legacy avec fallback automatique)
                var calculationResult = await Task.Run(() => 
                    _calculationService.CalculateSolicitations(AppState.CurrentProject.PavementStructure));
                
                Console.WriteLine($"Calculation complete: IsSuccessful={calculationResult.IsSuccessful}, Results={calculationResult.LayerResults?.Count ?? 0}");
                
                if (calculationResult.IsSuccessful)
                {
                    // Informations de calcul avec mode utilisé
                    var modeUsed = calculationResult.Message?.Contains("Native") == true ? "Natif (C++)" :
                                   calculationResult.Message?.Contains("Legacy") == true ? "Legacy (C#)" : "Hybride";
                    CalculationInfo = $"Calcul terminé avec succès - Mode: {modeUsed}";
                    
                    Console.WriteLine($"Mode used: {modeUsed}");
                    Console.WriteLine($"Calling PopulateResultsWithCalculatedData with {calculationResult.LayerResults.Count} layer results");
                    
                    // Informations de performance
                    PerformanceInfo = $"Temps: {calculationResult.CalculationTimeMs:F2} ms";
                    CalculationDuration = FormatDuration(calculationResult.CalculationTimeMs / 1000.0);
                    
                    // MASQUER automatiquement les détails après un calcul réussi
                    ShowDetailedInfo = false;
                    
                    // Reconstruction complète avec couches ET interfaces 
                    PopulateResultsWithCalculatedData(calculationResult);
                    
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
        /// Peuple les r�sultats avec les donn�es calcul�es, en incluant les interfaces
        /// </summary>
        private void PopulateResultsWithCalculatedData(SolicitationCalculationResult calculationResult)
        {
            Console.WriteLine($"\n=== PopulateResultsWithCalculatedData START ===");
            var structure = AppState.CurrentProject.PavementStructure;
            if (structure?.Layers == null) return;

            var orderedLayers = structure.Layers
                .Where(l => l.Role != LayerRole.Plateforme)
                .OrderBy(l => l.Order)
                .ToList();

            var platform = structure.Layers.FirstOrDefault(l => l.Role == LayerRole.Plateforme);

            Resultats.Clear();
            Console.WriteLine($"Cleared Resultats collection");

            // Cr�er un dictionnaire pour retrouver facilement les r�sultats par couche
            var layerResultsDict = calculationResult.LayerResults.ToDictionary(
                lr => lr.Layer.Order,
                lr => lr
            );
            
            Console.WriteLine($"Created layer results dictionary with {layerResultsDict.Count} entries:");
            foreach (var kvp in layerResultsDict)
            {
                Console.WriteLine($"  Order={kvp.Key}: σT_top={kvp.Value.SigmaTTop}, εT_top={kvp.Value.EpsilonTTop}");
            }

            /// Ajouter les couches normales avec leurs interfaces
            for (int i = 0; i < orderedLayers.Count; i++)
            {
                var layer = orderedLayers[i];
                // Ajouter la couche avec les donn�es calcul�es
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

                // Ajouter l'interface si ce n'est pas la derni�re couche
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

            // Injection des valeurs admissibles calcul�es
            InjectValeursAdmissiblesDansResultats();
        }

        /// <summary>
        /// Cr�e un ResultatCouche avec les donn�es calcul�es
        /// </summary>
        private ResultatCouche CreateResultCoucheWithCalculatedData(Layer layer, Dictionary<int, LayerSolicitationResult> layerResultsDict)
        {
            Console.WriteLine($"\nCreateResultCouche for layer Order={layer.Order}, Material={layer.MaterialName}");
            
            // R�cup�rer les donn�es calcul�es si disponibles
            if (layerResultsDict.TryGetValue(layer.Order, out var layerResult))
            {
                Console.WriteLine($"  Found calculation results:");
                Console.WriteLine($"    σT_top={layerResult.SigmaTTop}, σT_bottom={layerResult.SigmaTBottom}");
                Console.WriteLine($"    εT_top={layerResult.EpsilonTTop}, εT_bottom={layerResult.EpsilonTBottom}");
                Console.WriteLine($"    σZ_top={layerResult.SigmaZTop}, σZ_bottom={layerResult.SigmaZBottom}");
                
                // Couche avec donn�es calcul�es
                var result = new ResultatCouche
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
                
                Console.WriteLine($"  Created ResultatCouche: σT_sup={result.SigmaTSup}, εT_sup={result.EpsilonTSup}");
                return result;
            }
            else
            {
                Console.WriteLine($"  NO calculation results found for this layer!");
                // Fallback : couche sans donn�es calcul�es (ne devrait pas arriver)
                return CreateResultCoucheFromLayer(layer, AppState.CurrentProject.PavementStructure);
            }
        }

        /// <summary>
        /// M�thode publique pour forcer la mise � jour depuis l'ext�rieur
        /// � appeler depuis StructureEditorViewModel quand la structure change
        /// </summary>
        public void RefreshFromStructure()
        {
            LoadCurrentStructure();
        }

        #endregion

        #region M�thodes priv�es

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
                return GetLayerTopLevel(layer); // La plateforme n'a pas de niveau inf�rieur affich�
            }
            return GetLayerTopLevel(layer) + layer.Thickness_m * 100;
        }

        private double CalculateAdmissibleValue(LayerSolicitationResult layerResult)
        {
            // Calcul des valeurs admissibles selon le type de mat�riau
            // TODO: Impl�menter les formules NF P98-086 compl�tes
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
            // Validation selon les crit�res NF P98-086
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
        /// Chargement des donn�es d'exemple bas�es sur votre code C++
        /// Simule l'affichage style Aliz� avec interfaces entre les couches
        /// </summary>
        private void LoadSampleData()
        {
            // Donn�es d'exemple qui correspondent � votre main() C++
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
                TypeInterface = "Coll�e",
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
                TypeInterface = "Semi-coll�e",
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
                TypeInterface = "Coll�e",
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
                SigmaTInf = 0,      // Sera remplac� par "-" dans l'affichage
                EpsilonTSup = 5.2,  // Seule valeur de la plateforme  
                EpsilonTInf = 0,    // Sera remplac� par "-" dans l'affichage
                SigmaZ = -0.200,
                EpsilonZ = -85.1,
                SigmaZSup = -0.200, // Seule valeur de la plateforme
                SigmaZInf = 0,      // Sera remplac� par "-" dans l'affichage
                EpsilonZSup = -85.1, // Seule valeur de la plateforme
                EpsilonZInf = 0,     // Sera remplac� par "-" dans l'affichage
                DeflexionSup = 0.0,  // Pas de d�flexion pour la plateforme
                DeflexionInf = 0,    // Sera remplac� par "-" dans l'affichage  
                ValeurAdmissible = 200.0,
                EstValide = true
            });

            // Mise � jour de la validation apr�s ajout des donn�es
            UpdateValidationStatus();
        }

        /// <summary>
        /// Met � jour le statut de validation en fonction des r�sultats actuels
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
        /// Bascule l'affichage des d�tails de calcul
        /// </summary>
        private void ToggleDetailedInfo()
        {
            ShowDetailedInfo = !ShowDetailedInfo;
        }

        /// <summary>
        /// Formate la dur�e d'un calcul
        /// </summary>
        private string FormatDuration(double seconds)
        {
            if (seconds < 60)
                return $"Dur�e : {seconds:F2} sec";

            int minutes = (int)(seconds / 60);
            seconds %= 60;
            return $"Dur�e : {minutes} min {seconds:F2} sec";
        }

        /// <summary>
        /// S'abonne aux changements de structure pour synchronisation automatique
        /// </summary>
        private void SubscribeToStructureChanges()
        {
            // S'abonner � l'�v�nement global de changement de structure
            AppState.StructureChanged += OnStructureChanged;
        }
        
        /// <summary>
        /// Appel� quand la structure change dans AppState
        /// </summary>
        private void OnStructureChanged()
        {
            Console.WriteLine("=== RESULTAT VIEW MODEL: Changement de structure détecté ===");
            // Ex�cuter sur le thread UI si n�cessaire
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                LoadCurrentStructure();
            });
        }

        /// <summary>
        /// Charge la structure actuelle depuis AppState et met � jour l'affichage
        /// </summary>
        private void LoadCurrentStructure()
        {
            Console.WriteLine("=== RESULTAT VIEW MODEL: LoadCurrentStructure - Chargement de la structure ===");
            try
            {
                if (AppState.CurrentProject?.PavementStructure?.Layers?.Count > 0)
                {
                    var layerCount = AppState.CurrentProject.PavementStructure.Layers.Count;
                    Console.WriteLine($"=== RESULTAT VIEW MODEL: Structure trouvée avec {layerCount} couches ===");
                    UpdateResultsFromCurrentStructure();
                }
                else
                {
                    Console.WriteLine("=== RESULTAT VIEW MODEL: Aucune structure trouvée, chargement des données d'exemple ===");
                    LoadSampleData(); // Fallback sur les donn�es d'exemple
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== RESULTAT VIEW MODEL: Erreur lors du chargement de la structure: {ex.Message} ===");
                // En cas d'erreur, charger les donn�es d'exemple
                LoadSampleData();
                System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement de la structure: {ex.Message}");
            }
        }

        /// <summary>
        /// Met � jour les r�sultats depuis la structure actuelle (sans calcul de sollicitations)
        /// Affiche les propri�t�s de base : mat�riaux, niveaux, modules, etc.
        /// </summary>
        private void UpdateResultsFromCurrentStructure()
        {
            Console.WriteLine("=== RESULTAT VIEW MODEL: UpdateResultsFromCurrentStructure - Mise à jour des résultats ===");
            var structure = AppState.CurrentProject.PavementStructure;
            if (structure?.Layers == null) 
            {
                Console.WriteLine("=== RESULTAT VIEW MODEL: Aucune couche trouvée dans la structure ===");
                return;
            }

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
                resultCouche.Numero = i + 1; // Synchronise le num�ro avec l'ordre de la structure
                resultCouche.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(ResultatCouche.EstValide))
                    {
                        UpdateValidationStatus();
                    }
                };
                Resultats.Add(resultCouche);

                // Ajouter l'interface si ce n'est pas la derni�re couche
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
                resultPlateforme.Numero = orderedLayers.Count + 1; // Num�ro plateforme = dernier + 1
                resultPlateforme.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(ResultatCouche.EstValide))
                    {
                        UpdateValidationStatus();
                    }
                };
                Resultats.Add(resultPlateforme);
            }

            // Injection des valeurs admissibles calcul�es
            InjectValeursAdmissiblesDansResultats();
            UpdateValidationStatus();
        }

        /// <summary>
        /// Cr�e un ResultatCouche depuis une Layer (sans sollicitations calcul�es)
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
                
                // Sollicitations initialis�es � z�ro (� calculer)
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
                EstValide = true // Par d�faut, sera mis � jour apr�s calcul
            };
        }

        /// <summary>
        /// Obtient le nom d'affichage du mat�riau pour synchronisation parfaite avec la structure
        /// </summary>
        private string GetMaterialDisplayName(Layer layer)
        {
            // Toujours utiliser le nom exact du mat�riau d�fini dans la structure
            return layer.MaterialName ?? string.Empty;
        }

        /// <summary>
        /// Cr�e un ResultatInterface depuis deux layers
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
        /// Obtient une valeur admissible par d�faut selon la famille de mat�riau
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
                InterfaceType.Collee => "Coll�e",
                InterfaceType.SemiCollee => "Semi-coll�e",
                InterfaceType.Decollee => "D�coll�e",
                _ => "Coll�e"
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
            // D�sabonnement de l'ancienne collection
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

        // Ajout : synchronisation des valeurs admissibles dans les r�sultats
        private void InjectValeursAdmissiblesDansResultats()
        {
            var valeursAdmissibles = GetValeursAdmissiblesViewModelCollection();
            if (valeursAdmissibles == null) return;

            foreach (var resultat in Resultats.OfType<ResultatCouche>())
            {
                // D�sabonner ancien handler pour �viter doublons
                resultat.CritereChanged -= Resultat_CritereChanged;

                var coucheAdmissible = valeursAdmissibles.FirstOrDefault(c =>
                    c.Niveau == resultat.Numero &&
                    string.Equals(c.Materiau?.Trim(), resultat.Materiau?.Trim(), StringComparison.InvariantCultureIgnoreCase)
                );

                if (coucheAdmissible != null)
                {
                    // Synchroniser critere (VA prioritaire)
                    if (!string.Equals(resultat.Critere, coucheAdmissible.Critere, StringComparison.OrdinalIgnoreCase))
                        resultat.Critere = coucheAdmissible.Critere;

                    if (coucheAdmissible.ValeurAdmissible > 0)
                    {
                        resultat.ValeurAdmissible = coucheAdmissible.ValeurAdmissible;
                        resultat.HasValeurAdmissible = true;
                    }
                    else
                    {
                        resultat.ValeurAdmissible = 0;
                        resultat.HasValeurAdmissible = false;
                    }
                }
                else
                {
                    resultat.HasValeurAdmissible = false;
                }

                resultat.CritereChanged += Resultat_CritereChanged;
            }
            OnPropertyChanged(nameof(Resultats));
        }

        private void Resultat_CritereChanged(object? sender, EventArgs e)
        {
            if (sender is not ResultatCouche rc) return;
            var valeursAdmissibles = GetValeursAdmissiblesViewModelCollection();
            if (valeursAdmissibles == null) return;
            var coucheAdmissible = valeursAdmissibles.FirstOrDefault(c => c.Niveau == rc.Numero);
            if (coucheAdmissible != null && !string.Equals(coucheAdmissible.Critere, rc.Critere, StringComparison.OrdinalIgnoreCase))
            {
                coucheAdmissible.Critere = rc.Critere; // propage vers VA
                // Recalculer valeur admissible si besoin (logique d�j� dans VA via PropertyChanged)
                AppState.RaiseValeursAdmissiblesUpdated();
            }
        }

        private void OnValeursAdmissiblesUpdated()
        {
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
            {
                InjectValeursAdmissiblesDansResultats();
            }));
        }

        #endregion

        #region IDisposable
        
        public void Dispose()
        {
            AppState.StructureChanged -= OnStructureChanged;
            AppState.ValeursAdmissiblesUpdated -= OnValeursAdmissiblesUpdated;
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
    /// Classe de base pour les �l�ments de r�sultat (couches ou interfaces)
    /// </summary>
    public abstract class ResultatItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// R�sultat pour une couche de chauss�e
    /// </summary>
    public class ResultatCouche : ResultatItem
    {
        // Champs
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
        private bool _hasValeurAdmissible;
        private CritereVerification _selectedCritere = CritereVerification.EpsiT;
        private string _critere = "EpsiT";

        // Propri�t�s de base
        public string Interface { get => _interface; set { _interface = value; OnPropertyChanged(); } }
        public string Materiau { get => _materiau; set { _materiau = value; OnPropertyChanged(); } }
        public double NiveauSup { get => _niveauSup; set { _niveauSup = value; OnPropertyChanged(); } }
        public double NiveauInf { get => _niveauInf; set { _niveauInf = value; OnPropertyChanged(); } }
        public double Module { get => _module; set { _module = value; OnPropertyChanged(); } }
        public double CoefficientPoisson { get => _coefficientPoisson; set { _coefficientPoisson = value; OnPropertyChanged(); } }
        public double SigmaTSup { get => _sigmaTSup; set { _sigmaTSup = value; OnPropertyChanged(); ReevaluateValidation(); } }
        public double SigmaTInf { get => _sigmaTInf; set { _sigmaTInf = value; OnPropertyChanged(); OnPropertyChanged(nameof(SigmaTInfDepasse)); ReevaluateValidation(); } }
        public double EpsilonTSup { get => _epsilonTSup; set { _epsilonTSup = value; OnPropertyChanged(); ReevaluateValidation(); } }
        public double EpsilonTInf { get => _epsilonTInf; set { _epsilonTInf = value; OnPropertyChanged(); OnPropertyChanged(nameof(EpsilonTInfDepasse)); ReevaluateValidation(); } }
        public double SigmaZ { get => _sigmaZ; set { _sigmaZ = value; OnPropertyChanged(); } }
        public double EpsilonZ { get => _epsilonZ; set { _epsilonZ = value; OnPropertyChanged(); } }
        public double SigmaZSup { get => _sigmaZSup; set { _sigmaZSup = value; OnPropertyChanged(); } }
        public double SigmaZInf { get => _sigmaZInf; set { _sigmaZInf = value; OnPropertyChanged(); } }
        public double EpsilonZSup { get => _epsilonZSup; set { _epsilonZSup = value; OnPropertyChanged(); OnPropertyChanged(nameof(EpsilonZSupDisplay)); OnPropertyChanged(nameof(EpsilonZSupDepasse)); ReevaluateValidation(); } }
        public double EpsilonZInf { get => _epsilonZInf; set { _epsilonZInf = value; OnPropertyChanged(); ReevaluateValidation(); } }
        public double DeflexionSup { get => _deflexionSup; set { _deflexionSup = value; OnPropertyChanged(); } }
        public double DeflexionInf { get => _deflexionInf; set { _deflexionInf = value; OnPropertyChanged(); } }
        public double ValeurAdmissible { get => _valeurAdmissible; set { _valeurAdmissible = value; OnPropertyChanged(); OnPropertyChanged(nameof(ValeurAdmissibleDisplay)); OnPropertyChanged(nameof(EpsilonZSupDepasse)); OnPropertyChanged(nameof(SigmaTInfDepasse)); OnPropertyChanged(nameof(EpsilonTInfDepasse)); ReevaluateValidation(); } }
        public bool HasValeurAdmissible { get => _hasValeurAdmissible; set { _hasValeurAdmissible = value; OnPropertyChanged(); OnPropertyChanged(nameof(ValeurAdmissibleDisplay)); } }
        public string ValeurAdmissibleDisplay => HasValeurAdmissible && ValeurAdmissible > 0 ? ValeurAdmissible.ToString("F1") : string.Empty;
        public bool EstValide { get => _estValide; set { _estValide = value; OnPropertyChanged(); OnPropertyChanged(nameof(CouleurValidation)); OnPropertyChanged(nameof(StatutValidation)); } }
        public string CouleurValidation => EstValide ? "#d4edda" : "#f8d7da";
        public string StatutValidation => EstValide ? "\u2713" : "\u2717";
        // Valeur critique : pour SigmaT et EpsiT on utilise uniquement la valeur inf�rieure (absolue)
        public double ValeurCritique => Critere switch
        {
            "EpsiZ" => Math.Abs(EpsilonZSup),
            "SigmaT" => Math.Abs(SigmaTInf),
            _ => Math.Abs(EpsilonTInf)
        };

        public double TauxUtilisation => ValeurAdmissible > 0 ? (ValeurCritique / ValeurAdmissible) * 100 : 0;
        public bool EstPlateforme => Interface == "Plateforme" || double.IsPositiveInfinity(NiveauInf);
        public string NiveauInfDisplay => EstPlateforme ? "-" : NiveauInf.ToString("F0");
        public int Numero { get => _numero; set { _numero = value; OnPropertyChanged(); OnPropertyChanged(nameof(NiveauDisplay)); } }
        public string NiveauDisplay => (Numero > 0) ? Numero.ToString() : "";
        public string Nature => Interface;
        public string InterfaceAffichee => Interface == Nature ? string.Empty : Interface;

        // Affichages et depassements
        public string EpsilonZSupDisplay => Critere == "EpsiZ" ? Math.Abs(EpsilonZSup).ToString("F1") : EpsilonZSup.ToString("F1");
        public bool EpsilonZSupDepasse => Critere == "EpsiZ" && ValeurAdmissible > 0 && Math.Abs(EpsilonZSup) > ValeurAdmissible;
        public bool SigmaTInfDepasse => Critere == "SigmaT" && ValeurAdmissible > 0 && Math.Abs(SigmaTInf) > ValeurAdmissible;
        public bool EpsilonTInfDepasse => Critere == "EpsiT" && ValeurAdmissible > 0 && Math.Abs(EpsilonTInf) > ValeurAdmissible;

        public CritereVerification SelectedCritere
        {
            get => _selectedCritere;
            set
            {
                if (_selectedCritere != value)
                {
                    _selectedCritere = value;
                    Critere = value switch
                    {
                        CritereVerification.EpsiZ => "EpsiZ",
                        CritereVerification.SigmaT => "SigmaT",
                        _ => "EpsiT"
                    };
                    OnPropertyChanged();
                }
            }
        }

        public string Critere
        {
            get => _critere;
            set
            {
                if (_critere != value && !string.IsNullOrWhiteSpace(value))
                {
                    _critere = value;
                    _selectedCritere = value switch
                    {
                        "EpsiZ" => CritereVerification.EpsiZ,
                        "SigmaT" => CritereVerification.SigmaT,
                        _ => CritereVerification.EpsiT
                    };
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(EpsilonZSupDisplay));
                    OnPropertyChanged(nameof(ValeurCritique));
                    OnPropertyChanged(nameof(TauxUtilisation));
                    OnPropertyChanged(nameof(EpsilonZSupDepasse));
                    OnPropertyChanged(nameof(SigmaTInfDepasse));
                    OnPropertyChanged(nameof(EpsilonTInfDepasse));
                    ReevaluateValidation();
                    CritereChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler? CritereChanged;

        private void ReevaluateValidation()
        {
            if (ValeurAdmissible <= 0) return;
            EstValide = Math.Abs(ValeurCritique) <= ValeurAdmissible;
            OnPropertyChanged(nameof(TauxUtilisation));
            OnPropertyChanged(nameof(EpsilonZSupDepasse));
            OnPropertyChanged(nameof(SigmaTInfDepasse));
            OnPropertyChanged(nameof(EpsilonTInfDepasse));
        }
    }

    /// <summary>
    /// R�sultat pour une interface entre deux couches
    /// </summary>
    public class ResultatInterface : ResultatItem
    {
        private string _typeInterface = "";
        private string _description = "";
        public string TypeInterface { get => _typeInterface; set { _typeInterface = value; OnPropertyChanged(); OnPropertyChanged(nameof(CouleurInterface)); } }
        public string Description { get => _description; set { _description = value; OnPropertyChanged(); } }
        public string CouleurInterface => TypeInterface switch
        {
            "Coll�e" => "#28a745",
            "Semi-coll�e" => "#ffc107",
            "D�coll�e" => "#dc3545",
            _ => "#6c757d"
        };
    }
}