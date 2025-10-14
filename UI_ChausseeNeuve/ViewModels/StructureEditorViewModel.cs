using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using ChausseeNeuve.Domain.Models;
using UI_ChausseeNeuve.Services;
using UI_ChausseeNeuve.Windows;  // Pour ModeSelectionWindow

namespace UI_ChausseeNeuve.ViewModels
{
    public abstract class RowVM { }

    public class LayerRowVM : RowVM, INotifyPropertyChanged
    {
        public Layer Layer { get; }
        public LayerRowVM(Layer layer)
        {
            Layer = layer;
            Layer.PropertyChanged += Layer_PropertyChanged;
        }

        private void Layer_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Layer.Role) || e.PropertyName == nameof(Layer.Family))
            {
                OnPropertyChanged(nameof(AvailableMaterials));
            }
        }

        public IEnumerable<MaterialFamily> AvailableMaterials
        {
            get
            {
                if (Layer.Role == LayerRole.Plateforme)
                    return new[] { MaterialFamily.GNT, MaterialFamily.Bibliotheque };
                return System.Enum.GetValues(typeof(MaterialFamily)).Cast<MaterialFamily>();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class InterfaceRowVM : RowVM
    {
        public Layer UpperLayer { get; }
        public InterfaceRowVM(Layer upperLayer) { UpperLayer = upperLayer; }
    }

    public class GradItem
    {
        public double Pixel { get; set; }
        public string Label { get; set; } = "";
    }

    public partial class StructureEditorViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Layer> Layers { get; } = new();

        // Événement pour les notifications toast
        public event Action<string, ToastType>? ToastRequested;

        // Garde pour éviter une réouverture récursive de la Bibliothèque
        private bool _isOpeningLibrary;
        private bool _pendingOpen;

        // Mémoire: dernier état NON-Bibliothèque par couche (pour restaurer si annulation)
        private readonly Dictionary<Layer, LayerState> _lastNonLibraryState = new();

        private double _ne = 80_000;
        public double NE { get => _ne; set { _ne = value; OnPropertyChanged(); Recompute(); } }

        public int LayerCount => Layers.Count;
        public string LayerCountDisplay => $"{LayerCount} couches";

        private double _depthScale = 800;
        public double DepthScale { get => _depthScale; set { _depthScale = value; OnPropertyChanged(); } }
        public bool AutoScaleEnabled { get; set; } = true;

        private double _viewportHeight;
        public double ViewportHeight { get => _viewportHeight; set { _viewportHeight = value; OnPropertyChanged(); TryAutoScale(); } }

        public ObservableCollection<string> Graduations { get; } = new();
        public ObservableCollection<GradItem> GradItems { get; } = new();

        public IReadOnlyList<string> StructureTypes { get; } =
            new[] { "Souple", "Bitumineuse épaisse", "Semi-rigide", "Mixte", "Inversée", "Rigide", "Autre" };

        private string _selectedStructureType = "Souple";
        public string SelectedStructureType { get => _selectedStructureType; set { _selectedStructureType = value; OnPropertyChanged(); UpdateAppState(); } }

        public ObservableCollection<string> Errors { get; } = new();
        public ObservableCollection<string> Warnings { get; } = new();

        public RelayCommand AddLayerCommand { get; }
        public RelayCommand RemoveTopLayerCommand { get; }
        public RelayCommand<Layer> DeleteLayerCommand { get; }
        public RelayCommand ValidateStructureCommand { get; }
        public RelayCommand<Layer> MoveLayerUpCommand { get; }
        public RelayCommand<Layer> MoveLayerDownCommand { get; }

        public ObservableCollection<RowVM> Rows
        {
            get
            {
                var outRows = new ObservableCollection<RowVM>();
                var ordered = Layers.OrderBy(l => l.Order).ToList();
                for (int i = 0; i < ordered.Count; i++)
                {
                    outRows.Add(new LayerRowVM(ordered[i]));
                    if (i < ordered.Count - 1) outRows.Add(new InterfaceRowVM(ordered[i]));
                }
                return outRows;
            }
        }

        private readonly DimensionnementMode _mode;
        public bool IsExpert => _mode == DimensionnementMode.Expert;
        public bool IsAutomatique => _mode == DimensionnementMode.Automatique;

        public StructureEditorViewModel()
        {
            _mode = AppState.CurrentProject.Mode;

            // Delegate pour exemption epaisseur minimale de la plus petite sous-couche de fondation (mode Expert)
            Layer.IsSmallestFoundationProvider = (layer) =>
            {
                if (_mode != DimensionnementMode.Expert) return false;
                if (layer.Role != LayerRole.Fondation) return false;
                if (string.IsNullOrEmpty(SelectedStructureType)) return false;
                if (!SelectedStructureType.Equals("Souple", StringComparison.OrdinalIgnoreCase) &&
                    !SelectedStructureType.Equals("Bitumineuse épaisse", StringComparison.OrdinalIgnoreCase)) return false;
                // Trouver les couches de fondation courantes (hors plateforme) dans la structure
                var fondations = Layers.Where(l => l.Role == LayerRole.Fondation).ToList();
                if (fondations.Count == 0) return false;
                var minThickness = fondations.Min(f => f.Thickness_m);
                // On autorise une petite tolérance numérique
                return Math.Abs(layer.Thickness_m - minThickness) < 1e-6;
            };

            // Connecter les notifications de Layer aux toasts
            Layer.NotifyToast = (message, type) => ToastRequested?.Invoke(message, type);

            // Charger les données depuis AppState ou initialiser
            LoadFromAppState();

            // Commandes activées dans les deux modes (Expert et Automatique)
            AddLayerCommand = new RelayCommand(AddLayer);
            RemoveTopLayerCommand = new RelayCommand(RemoveTopLayer, () => Layers.Count > 3);
            DeleteLayerCommand = new RelayCommand<Layer>(DeleteLayer, l => l is not null && l.Role != LayerRole.Plateforme);
            ValidateStructureCommand = new RelayCommand(ValidateNow);
            MoveLayerUpCommand = new RelayCommand<Layer>(MoveLayerUp, CanMoveUp);
            MoveLayerDownCommand = new RelayCommand<Layer>(MoveLayerDown, CanMoveDown);

            Layers.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null) foreach (Layer L in e.NewItems) L.PropertyChanged += LayerChanged;
                if (e.OldItems != null) foreach (Layer L in e.OldItems) L.PropertyChanged -= LayerChanged;
                OnPropertyChanged(nameof(Rows));
                Recompute();
                TryAutoScale();
                UpdateAppState();
                // Mettre à jour l'état des commandes
                RemoveTopLayerCommand?.RaiseCanExecuteChanged();
                MoveLayerUpCommand?.RaiseCanExecuteChanged();
                MoveLayerDownCommand?.RaiseCanExecuteChanged();
                AddLayerCommand?.RaiseCanExecuteChanged();
                DeleteLayerCommand?.RaiseCanExecuteChanged();
            };
            foreach (var L in Layers) L.PropertyChanged += LayerChanged;
        }

        private void LoadFromAppState()
        {
            var pavementStructure = AppState.CurrentProject.PavementStructure;

            // Si pas de couches dans le projet, initialiser avec exemple
            if (pavementStructure.Layers.Count == 0)
            {
                InitializeDefaultStructure(pavementStructure);
            }

            // Charger les couches
            Layers.Clear();
            foreach (var layer in pavementStructure.Layers)
            {
                layer.Mode = _mode; // Propager mode
                layer.CurrentStructureType = pavementStructure.StructureType; // Propager type de structure
                Layers.Add(layer);
            }

            _ne = pavementStructure.NE;
            _selectedStructureType = pavementStructure.StructureType;
            OnPropertyChanged(nameof(NE));
            OnPropertyChanged(nameof(SelectedStructureType));
            OnPropertyChanged(nameof(Rows));
            Recompute();
            TryAutoScale();
        }

        private void InitializeDefaultStructure(PavementStructure pavementStructure)
        {
            // Structure compatible avec votre exemple C++
            // nbrecouche = 4, Young[0] = 7000, Young[1] = 23000, Young[2] = 23000, Young[3] = 120
            // epais[0] = 0.06, epais[1] = 0.15, epais[2] = 0.15, epais[3] = 10000000
            pavementStructure.Layers.Add(new Layer { Order = 1, Role = LayerRole.Roulement, Family = MaterialFamily.BetonBitumineux, MaterialName = "EB-BBSG 0/10", Thickness_m = 0.060, Modulus_MPa = 7000, Poisson = 0.35, InterfaceWithBelow = InterfaceType.Collee, Mode = _mode });
            pavementStructure.Layers.Add(new Layer { Order = 2, Role = LayerRole.Base, Family = MaterialFamily.MTLH, MaterialName = "MTLH Base", Thickness_m = 0.150, Modulus_MPa = 23000, Poisson = 0.25, InterfaceWithBelow = InterfaceType.SemiCollee, Mode = _mode });
            pavementStructure.Layers.Add(new Layer { Order = 3, Role = LayerRole.Fondation, Family = MaterialFamily.MTLH, MaterialName = "MTLH Fondation", Thickness_m = 0.150, Modulus_MPa = 23000, Poisson = 0.25, InterfaceWithBelow = InterfaceType.Collee, Mode = _mode });
            
            // La plateforme sera automatiquement initialisée avec l'épaisseur fixe de 10,000,000 m grâce au setter du Role
            var platformLayer = new Layer 
            { 
                Order = 4, 
                Role = LayerRole.Plateforme, // Ceci déclenche automatiquement l'épaisseur fixe
                Family = MaterialFamily.GNT, 
                MaterialName = "Plateforme", 
                Modulus_MPa = 120,  // Valeur compatible avec votre exemple C++
                Poisson = 0.35, 
                InterfaceWithBelow = null 
            };
            pavementStructure.Layers.Add(platformLayer);
        }

        private void UpdateAppState()
        {
            var pavementStructure = AppState.CurrentProject.PavementStructure;
            pavementStructure.Layers.Clear();
            foreach (var layer in Layers)
            {
                pavementStructure.Layers.Add(layer);
            }
            pavementStructure.NE = _ne;
            pavementStructure.StructureType = _selectedStructureType;
            foreach (var layer in Layers) layer.CurrentStructureType = _selectedStructureType; // mise à jour pour règles d'épaisseur
            
            // Notifier le changement de structure pour synchroniser les résultats
            AppState.NotifyStructureChanged();
        }

        private void LayerChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is Layer changed && e.PropertyName == nameof(Layer.Family))
            {
                if (changed.Family != MaterialFamily.Bibliotheque)
                {
                    // Mémoriser l’état courant comme dernier état non-bibliothèque
                    _lastNonLibraryState[changed] = new LayerState(changed);
                }
            }

            if (_isOpeningLibrary) return;
            if (sender is Layer L && e.PropertyName == nameof(L.Family) && L.Family == MaterialFamily.Bibliotheque)
            {
                // Déclencher l'ouverture de la bibliothèque après la fin du cycle d'UI
                if (_pendingOpen) return;
                _pendingOpen = true;
                Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        OpenLibraryAndApply(L);
                    }
                    finally
                    {
                        _pendingOpen = false;
                    }
                }));
            }
            OnPropertyChanged(nameof(Rows));
            Recompute();
            TryAutoScale();
            UpdateAppState();
        }

        private void OpenLibraryAndApply(Layer targetLayer)
        {
            try
            {
                _isOpeningLibrary = true;
                var win = new UI_ChausseeNeuve.Windows.BibliothequeWindow();
                if (Application.Current?.MainWindow != null)
                    win.Owner = Application.Current.MainWindow;

                var result = win.ShowDialog();
                var selected = win.ViewModel?.SelectedMaterial;
                if (result == true && selected != null)
                {
                    ApplyMaterialToLayer(targetLayer, selected);
                    ToastRequested?.Invoke($"Matériau '{targetLayer.MaterialName}' appliqué à la couche {targetLayer.Role}", ToastType.Success);
                }
                else
                {
                    // Annulé: restaurer l’état précédent s’il existe (pas de repli arbitraire)
                    if (_lastNonLibraryState.TryGetValue(targetLayer, out var prev))
                    {
                        prev.RestoreTo(targetLayer);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur ouverture Bibliothèque: {ex.Message}");
            }
            finally
            {
                _isOpeningLibrary = false;
            }
        }

        private void ApplyMaterialToLayer(Layer layer, MaterialItem mat)
        {
            if (mat == null) return;

            layer.Family = MaterialFamily.Bibliotheque;
            layer.MaterialName = mat.Name ?? layer.MaterialName;
            double modulus = mat.ComputedModulus > 0 ? mat.ComputedModulus : (mat.Modulus_MPa > 0 ? mat.Modulus_MPa : layer.Modulus_MPa);
            layer.Modulus_MPa = modulus;
            if (mat.PoissonRatio > 0) layer.Poisson = mat.PoissonRatio;
            if (mat.MinThickness_m.HasValue && layer.Thickness_m < mat.MinThickness_m.Value)
                layer.Thickness_m = mat.MinThickness_m.Value;
            if (mat.MaxThickness_m.HasValue && layer.Thickness_m > mat.MaxThickness_m.Value)
                layer.Thickness_m = mat.MaxThickness_m.Value;
            try
            {
                if (mat.Category != null && mat.Category.Equals("MB", StringComparison.OrdinalIgnoreCase))
                {
                    // Assurer Sh rempli (utilise logique standard si besoin)
                    if (mat.ShStatus == "standard")
                    {
                        mat.FillShFromStandard();
                    }
                    if (mat.Sh.HasValue && mat.Sh.Value > 0)
                        layer.LibrarySh = mat.Sh.Value;
                }
                if (mat.Epsi0_10C.HasValue && mat.Epsi0_10C.Value > 0)
                    layer.LibraryEpsilon6 = mat.Epsi0_10C.Value;
            }
            catch { }
        }

        // + / - juste au-dessus de la Plateforme
        private void AddLayer()
        {
            var platIdx = Layers.ToList().FindIndex(l => l.Role == LayerRole.Plateforme);
            if (platIdx >= 0)
            {
                var newLayer = new Layer
                {
                    Role = LayerRole.Base,
                    Family = MaterialFamily.GNT,
                    MaterialName = "GNT (suppl.)",
                    Thickness_m = 0.10,
                    Modulus_MPa = 800,
                    Poisson = 0.35,
                    InterfaceWithBelow = InterfaceType.Collee,
                    Mode = _mode
                };
                Layers.Insert(platIdx, newLayer);
                Renumber();
            }
        }

        private void RemoveTopLayer()
        {
            var platIdx = Layers.ToList().FindIndex(l => l.Role == LayerRole.Plateforme);
            if (platIdx > 0)
            {
                var target = Layers[platIdx - 1];
                if (target.Role != LayerRole.Fondation)
                {
                    Layers.RemoveAt(platIdx - 1);
                    Renumber();
                }
            }
        }

        private void DeleteLayer(Layer? l)
        {
            if (l is null || l.Role == LayerRole.Plateforme) return;
            Layers.Remove(l);
            Renumber();
        }

        private bool CanMoveUp(Layer? l)
        {
            if (l is null || l.Role == LayerRole.Plateforme) return false;
            var idx = Layers.IndexOf(l);
            return idx > 0;
        }

        private bool CanMoveDown(Layer? l)
        {
            if (l is null || l.Role == LayerRole.Plateforme) return false;
            var idx = Layers.IndexOf(l);
            if (idx < 0) return false;
            var platIdx = Layers.ToList().FindIndex(x => x.Role == LayerRole.Plateforme);
            int lastMovable = platIdx >= 0 ? platIdx - 1 : Layers.Count - 1;
            return idx < lastMovable;
        }

        private void MoveLayerUp(Layer? l)
        {
            if (!CanMoveUp(l)) return;
            var idx = Layers.IndexOf(l!);
            Layers.Move(idx, idx - 1);
            Renumber();
        }

        private void MoveLayerDown(Layer? l)
        {
            if (!CanMoveDown(l)) return;
            var idx = Layers.IndexOf(l!);
            Layers.Move(idx, idx + 1);
            Renumber();
        }

        private void Renumber()
        {
            for (int i = 0; i < Layers.Count; i++) Layers[i].Order = i + 1;
            OnPropertyChanged(nameof(Rows));
            MoveLayerUpCommand?.RaiseCanExecuteChanged();
            MoveLayerDownCommand?.RaiseCanExecuteChanged();
        }

        private void ValidateNow()
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            // Structurer automatiquement la fondation si en mode automatique
            if (IsAutomatique)
            {
                var fondation = Layers.FirstOrDefault(l => l.Role == LayerRole.Fondation);
                if (fondation == null) return;

                // Si l'épaisseur est suffisante pour subdiviser
                if (fondation.Thickness_m > 0.25)
                {
                    try
                    {
                        int indexFondation = Layers.IndexOf(fondation);
                        Layer coucheInferieure = (indexFondation + 1 < Layers.Count) ? Layers[indexFondation + 1] : null;

                        if (coucheInferieure == null) return;

                        // Utiliser le module de la couche inférieure comme "module de plateforme"
                        double moduleBase = coucheInferieure.Modulus_MPa;
                        
                        // Déterminer la catégorie GNT selon le module de la GNT (fondation)
                        string categorieGNT;
                        double moduleGNT = fondation.Modulus_MPa;  // Module E_GNT de référence
                        double k;  // Coefficient multiplicateur

                        if (moduleGNT >= 500 && moduleGNT <= 1000)
                        {
                            categorieGNT = "CG1";
                            k = 3.0;
                        }
                        else if (moduleGNT >= 300 && moduleGNT < 500)
                        {
                            categorieGNT = "CG2";
                            k = 2.5;
                        }
                        else if (moduleGNT >= 100 && moduleGNT < 300)
                        {
                            categorieGNT = "CG3";
                            k = 2.0;
                        }
                        else
                        {
                            MessageBox.Show($"Module de la GNT invalide : {moduleGNT:F0} MPa\nDoit être entre 100 et 1000 MPa",
                                "Module GNT invalide", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        // Calculer les sous-couches
                        int nbSousCouches = (int)Math.Ceiling(fondation.Thickness_m / 0.25);
                        var sousCouches = new List<Windows.SousCoucheInfo>();
                        
                        double epaisseurRestante = fondation.Thickness_m;
                        
                        // 1. Première sous-couche (i=1) - Utilise le module de la couche inférieure
                        double modulePremiereSubCouche = Math.Min(k * moduleBase, moduleGNT);
                        sousCouches.Add(new Windows.SousCoucheInfo
                        {
                            Name = $"GNT {categorieGNT} - Fondation 1/{nbSousCouches}",
                            Thickness = 0.25,
                            Module = modulePremiereSubCouche
                        });
                        epaisseurRestante -= 0.25;
                        double modulePrecedent = modulePremiereSubCouche;

                        // 2. Sous-couches suivantes (i>1)
                        int nombreCouchesCompletes = (int)Math.Floor(fondation.Thickness_m / 0.25);
                        
                        for (int i = 2; i <= nombreCouchesCompletes; i++)
                        {
                            double moduleCourant = Math.Min(k * modulePrecedent, moduleGNT);
                            
                            sousCouches.Add(new Windows.SousCoucheInfo
                            {
                                Name = $"GNT {categorieGNT} - Fondation {i}/{nbSousCouches}",
                                Thickness = 0.25,
                                Module = moduleCourant
                            });

                            epaisseurRestante -= 0.25;
                            modulePrecedent = moduleCourant;
                        }

                        // 3. Dernière sous-couche si épaisseur restante
                        if (epaisseurRestante > 0.001)
                        {
                            double moduleDerniereCouche = Math.Min(k * modulePrecedent, moduleGNT);
                            
                            sousCouches.Add(new Windows.SousCoucheInfo
                            {
                                Name = $"GNT {categorieGNT} - Fondation {nbSousCouches}/{nbSousCouches}",
                                Thickness = epaisseurRestante,
                                Module = moduleDerniereCouche
                            });
                        }

                        // Préparer le texte des paramètres
                        string parametersText = $"Module GNT : {moduleGNT:F0} MPa => Catégorie {categorieGNT}\n" +
                                             $"Module couche inférieure : {moduleBase:F0} MPa\n" +
                                             $"Coefficient k = {k:F1}\n" +
                                             $"E_GNT(1) = min(k×E_inf, E_GNT)\n" +
                                             $"E_GNT(i) = min(k×E_GNT(i-1), E_GNT)";

                        // Afficher la fenêtre d'explication
                        var explanationWindow = new Windows.StructurationExplicationWindow(sousCouches, parametersText);
                        if (Application.Current.MainWindow != null)
                        {
                            explanationWindow.Owner = Application.Current.MainWindow;
                        }

                        if (explanationWindow.ShowDialog() == true)
                        {
                            // L'utilisateur a validé - Appliquer les changements
                            Layers.RemoveAt(indexFondation);

                            // Créer les sous-couches dans l'ordre (du bas vers le haut)
                            foreach (var info in sousCouches)
                            {
                                var sousCouche = new Layer
                                {
                                    Role = LayerRole.Fondation,
                                    Family = MaterialFamily.GNT,
                                    MaterialName = info.Name,
                                    Thickness_m = info.Thickness,
                                    Modulus_MPa = info.Module,
                                    Poisson = 0.35,
                                    InterfaceWithBelow = InterfaceType.Collee,
                                    Mode = _mode
                                };
                                // Important : insérer chaque nouvelle couche à la même position
                                // pour qu'elles s'empilent dans le bon ordre (première au-dessus de la couche inférieure)
                                Layers.Insert(indexFondation, sousCouche);
                            }

                            // Renuméroter les couches
                            Renumber();

                            // Notifier des changements pour mettre à jour l'interface
                            OnPropertyChanged(nameof(Rows));
                            OnPropertyChanged(nameof(LayerCount));
                            OnPropertyChanged(nameof(LayerCountDisplay));

                            // Mettre à jour l'échelle si nécessaire
                            TryAutoScale();

                            // Notification à l'utilisateur
                            ToastRequested?.Invoke($"Fondation subdivisée en {nbSousCouches} sous-couches (GNT {categorieGNT})", ToastType.Success);

                            // Mettre à jour l'état du projet
                            UpdateAppState();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erreur lors de la subdivision de la fondation : {ex.Message}",
                            "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

            // Continuer avec la validation standard
            ValidateInterfaces(errors, warnings);
            ValidateGlobalStructure(errors, warnings);

            // Afficher le rapport de validation
            var result = CreateValidationReport(errors, warnings);
            var style = errors.Count > 0 ? MessageBoxImage.Error :
                       warnings.Count > 0 ? MessageBoxImage.Warning :
                       MessageBoxImage.Information;

            MessageBox.Show(result.Message, result.Title, MessageBoxButton.OK, style);
        }

        private (string Title, string Message) CreateValidationReport(List<string> errors, List<string> warnings)
        {
            var msg = new StringBuilder();
            msg.AppendLine($"🏗️ Structure : {Layers.Count} couches (NE={NE:0})");
            msg.AppendLine($"📋 Type : {SelectedStructureType}");

            // Coefficients moyens (LOT 4)
            if (Layers.Any(l => l.Role != LayerRole.Plateforme))
            {
                var avgKs = Layers.Where(l => l.Role != LayerRole.Plateforme).Average(l => l.CoeffKs);
                var avgKd = Layers.Where(l => l.Role != LayerRole.Plateforme).Average(l => l.CoeffKd);
                msg.AppendLine($"⚙️ Coefficients moyens : ks={avgKs:F2}, kd={avgKd:F2}");
            }

            msg.AppendLine();

            string title;
            if (errors.Count > 0)
            {
                title = "❌ Validation échouée";
                msg.AppendLine("❌ ERREURS CRITIQUES (NF P98-086) :");
                foreach (var error in errors)
                    msg.AppendLine($"   • {error}");
                msg.AppendLine();
            }
            else if (warnings.Count > 0)
            {
                title = "⚠️ Validation avec avertissements";
                msg.AppendLine("⚠️ AVERTISSEMENTS :");
                foreach (var warning in warnings)
                    msg.AppendLine($"   • {warning}");
                msg.AppendLine();
            }
            else
            {
                title = "✅ Validation réussie";
                msg.AppendLine("✅ Structure Conforme aux exigences NF P98-086");
                msg.AppendLine("🎯 Tous les critères de la norme sont respectés");
            }

            return (title, msg.ToString());
        }

        private void ValidateInterfaces(List<string> errors, List<string> warnings)
        {
            var orderedLayers = Layers.OrderBy(l => l.Order).ToList();

            for (int i = 0; i < orderedLayers.Count - 1; i++)
            {
                var upperLayer = orderedLayers[i];
                var lowerLayer = orderedLayers[i + 1];

                var expectedInterface = GetExpectedInterface(upperLayer, lowerLayer);

                if (upperLayer.InterfaceWithBelow != expectedInterface)
                {
                    // Auto-correction selon la norme
                    upperLayer.InterfaceWithBelow = expectedInterface;
                    warnings.Add($"Interface {upperLayer.Role}/{lowerLayer.Role} ajustée à '{GetInterfaceDescription(expectedInterface)}' (NF P98-086 §8.5.1.3)");
                }
            }
        }

        private InterfaceType GetExpectedInterface(Layer upper, Layer lower)
        {
            // Règles selon NF P98-086 §8.5.1.3 + tableau fourni pour semi‑rigides

            // Fondation/Base sur Plate-forme : toujours collée
            if (lower.Role == LayerRole.Plateforme)
                return InterfaceType.Collee;

            // Spécifique aux chaussées Semi‑rigides
            if (!string.IsNullOrWhiteSpace(SelectedStructureType) &&
                SelectedStructureType.Equals("Semi-rigide", StringComparison.OrdinalIgnoreCase))
            {
                // Couche de roulement (BB) sur couche de base (MTLH) => Collée
                if (upper.Role == LayerRole.Roulement && lower.Role == LayerRole.Base)
                    return InterfaceType.Collee;

                // Couche de base (MTLH) sur toute couche de fondation (MTLH) => Semi‑Collée par défaut
                if (upper.Role == LayerRole.Base && lower.Role == LayerRole.Fondation)
                {
                    // Exceptions du tableau:
                    //  - GCV / GCV => Glissante
                    //  - GC4 / GC4 => Glissante
                    //  - GLG / GLG => Collée
                    string up = upper.MaterialName?.ToUpperInvariant() ?? string.Empty;
                    string lo = lower.MaterialName?.ToUpperInvariant() ?? string.Empty;
                    bool both(string token) => up.Contains(token) && lo.Contains(token);

                    if (both("GCV") || both("GC4"))
                        return InterfaceType.Decollee; // Glissante
                    if (both("GLG"))
                        return InterfaceType.Collee;   // Collée

                    return InterfaceType.SemiCollee;   // Règle générale
                }
            }

            // Règles génériques par défaut (autres types de structures)
            if (upper.Role == LayerRole.Base && upper.Family == MaterialFamily.MTLH)
                return InterfaceType.SemiCollee; // MTLH par défaut: semi‑collée

            if (upper.Role == LayerRole.Roulement && lower.Role == LayerRole.Base)
                return lower.Family == MaterialFamily.MTLH ? InterfaceType.SemiCollee : InterfaceType.Collee;

            // Défaut
            return InterfaceType.Collee;
        }

        private string GetInterfaceDescription(InterfaceType interfaceType)
        {
            return interfaceType switch
            {
                InterfaceType.Collee => "Collée",
                InterfaceType.SemiCollee => "Semi-collée",
                InterfaceType.Decollee => "Décollée",
                _ => "Inconnue"
            };
        }

        private void ValidateGlobalStructure(List<string> errors, List<string> warnings)
        {
            var orderedLayers = Layers.Where(l => l.Role != LayerRole.Plateforme).OrderBy(l => l.Order).ToList();

            switch (SelectedStructureType)
            {
                case "Souple":
                    ValidateSoupleStructureInline(orderedLayers, errors, warnings);
                    break;
                case "Semi-rigide":
                    ValidateSemiRigideStructure(orderedLayers, errors, warnings);
                    break;
                case "Bitumineuse épaisse":
                    ValidateBitumineusseEpaisseStructure(orderedLayers, errors, warnings);
                    break;
                case "Rigide":
                    ValidateRigideStructure(orderedLayers, errors, warnings);
                    break;
                case "Mixte":
                    ValidateMixteStructure(orderedLayers, errors, warnings);
                    break;
            }
        }

        // Méthode locale minimale pour éviter échec compilation et appliquer garde‑fous de base
        private void ValidateSoupleStructureInline(List<Layer> layers, List<string> errors, List<string> warnings)
        {
            var roulement = layers.FirstOrDefault(l => l.Role == LayerRole.Roulement);
            if (roulement == null)
            {
                errors.Add("Souple : couche de roulement manquante");
                return;
            }
            if (roulement.Family != MaterialFamily.BetonBitumineux)
                errors.Add("Souple : la couche de roulement doit être en Béton Bitumineux");
            if (roulement.Thickness_m < 0.02 || roulement.Thickness_m > 0.08)
                warnings.Add($"Souple : épaisseur roulement {roulement.Thickness_m:F3} m hors [0.02;0.08]");

            var totalBB = layers.Where(l => l.Family == MaterialFamily.BetonBitumineux).Sum(l => l.Thickness_m);
            var totalGNT = layers.Where(l => l.Family == MaterialFamily.GNT).Sum(l => l.Thickness_m);
            var total = layers.Sum(l => l.Thickness_m);

            if (totalBB > 0.12) errors.Add($"Souple : BB total {totalBB:F3} m > 0.12 m interdit");
            if (totalGNT < 0.15) warnings.Add($"Souple : GNT total {totalGNT:F3} m < 0.15 m recommandé");
            if (total < 0.30) warnings.Add($"Souple : épaisseur totale {total:F3} m < 0.30 m inhabituelle");
        }

        // Règles minimales Semi‑rigide
        private void ValidateSemiRigideStructure(List<Layer> layers, List<string> errors, List<string> warnings)
        {
            var roulement = layers.FirstOrDefault(l => l.Role == LayerRole.Roulement);
            if (roulement?.Family != MaterialFamily.BetonBitumineux)
                errors.Add("Semi‑rigide : surface doit être en Béton Bitumineux");

            var assises = layers.Where(l => l.Role == LayerRole.Base || l.Role == LayerRole.Fondation).ToList();
            if (!assises.Any()) { errors.Add("Semi‑rigide : base/fondation manquantes"); return; }
            if (!assises.All(l => l.Family == MaterialFamily.MTLH))
                errors.Add("Semi‑rigide : base/fondation doivent être en MTLH");
        }

        // Règles minimales Bitumineuse épaisse
        private void ValidateBitumineusseEpaisseStructure(List<Layer> layers, List<string> errors, List<string> warnings)
        {
            var roulement = layers.FirstOrDefault(l => l.Role == LayerRole.Roulement);
            var base_ = layers.FirstOrDefault(l => l.Role == LayerRole.Base);
            if (roulement?.Family != MaterialFamily.BetonBitumineux)
                errors.Add("Bitumineuse épaisse : surface doit être en BB");
            if (base_?.Family != MaterialFamily.BetonBitumineux)
                errors.Add("Bitumineuse épaisse : base doit être en BB");

            var fondations = layers.Where(l => l.Role == LayerRole.Fondation).ToList();
            foreach (var f in fondations)
            {
                if (f.Family != MaterialFamily.BetonBitumineux && f.Family != MaterialFamily.GNT)
                    errors.Add($"Bitumineuse épaisse : fondation '{f.MaterialName}' non autorisée ({f.Family})");
            }
        }

        // Règles minimales Rigide
        private void ValidateRigideStructure(List<Layer> layers, List<string> errors, List<string> warnings)
        {
            var betonLayers = layers.Where(l => l.Family == MaterialFamily.BetonCiment).ToList();
            if (!betonLayers.Any())
                errors.Add("Rigide : au moins une couche Béton de Ciment requise");
            var maxTh = betonLayers.Any() ? betonLayers.Max(l => l.Thickness_m) : 0;
            if (maxTh < 0.12)
                errors.Add($"Rigide : épaisseur béton ≥ 0,12 m requise (max actuel {maxTh:F3} m)");
        }

        // Règles Mixte
        private void ValidateMixteStructure(List<Layer> layers, List<string> errors, List<string> warnings)
        {
            var roulement = layers.FirstOrDefault(l => l.Role == LayerRole.Roulement);
            var @base = layers.FirstOrDefault(l => l.Role == LayerRole.Base);
            var fondations = layers.Where(l => l.Role == LayerRole.Fondation).ToList();

            if (roulement?.Family != MaterialFamily.BetonBitumineux)
                errors.Add("Mixte : la couche de surface doit être en Matériau Bitumineux (MB)");
            if (@base?.Family != MaterialFamily.BetonBitumineux)
                errors.Add("Mixte : la couche de base doit être en Matériau Bitumineux (MB)");
            if (!fondations.Any(f => f.Family == MaterialFamily.MTLH))
                errors.Add("Mixte : au moins une couche de fondation doit être en MTLH");

            double epStructure = layers.Sum(l => l.Thickness_m);
            double epMB = layers.Where(l => l.Family == MaterialFamily.BetonBitumineux && (l.Role == LayerRole.Roulement || l.Role == LayerRole.Base))
                                 .Sum(l => l.Thickness_m);
            if (epStructure > 0)
            {
                double ratio = epMB / epStructure;
                if (ratio < 0.45 || ratio > 0.60)
                {
                    errors.Add($"Mixte : 0,45 ≤ ep.MB(surface+base)/ep.Structure ≤ 0,60 non respecté (actuel {ratio:0.###})");
                }
            }
            else
            {
                errors.Add("Mixte : épaisseur de structure hors PF nulle");
            }
        }

        private void Recompute() => UpdateGraduations();
        private void TryAutoScale()
        {
            if (!AutoScaleEnabled) { UpdateGraduations(); return; }
            double total = Layers.Where(l => l.Role != LayerRole.Plateforme).Sum(l => l.Thickness_m);
            if (total <= 0 || ViewportHeight <= 0) { UpdateGraduations(); return; }
            double padding = 24;
            double usable = Math.Max(0, ViewportHeight - padding);
            double scale = usable / total;
            if (scale > 0 && !double.IsInfinity(scale) && !double.IsNaN(scale)) DepthScale = scale;
            UpdateGraduations();
        }
        private void UpdateGraduations()
        {
            var layers = Layers.Where(l => l.Role != LayerRole.Plateforme).OrderBy(l => l.Order).ToList();
            Graduations.Clear();
            GradItems.Clear();
            if (layers.Count == 0) { Graduations.Add("0.00 m"); return; }
            double cumul = 0;
            foreach (var l in layers)
            {
                cumul += l.Thickness_m;
                Graduations.Add(cumul.ToString("0.00") + " m");
                double px = Math.Max(2, l.Thickness_m * DepthScale);
                GradItems.Add(new GradItem { Pixel = px, Label = cumul.ToString("0.00") + " m" });
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // Snapshot simple de l’état d’une couche
    internal sealed class LayerState
    {
        public MaterialFamily Family { get; }
        public string Name { get; }
        public double Thickness { get; }
        public double Modulus { get; }
        public double Poisson { get; }

        public LayerState(Layer layer)
        {
            Family = layer.Family;
            Name = layer.MaterialName;
            Thickness = layer.Thickness_m;
            Modulus = layer.Modulus_MPa;
            Poisson = layer.Poisson;
        }

        public void RestoreTo(Layer layer)
        {
            layer.Family = Family;
            layer.MaterialName = Name;
            layer.Thickness_m = Thickness;
            layer.Modulus_MPa = Modulus;
            layer.Poisson = Poisson;
        }
    }

    // Infos pour la création dynamique des sous-couches
    internal class SousCoucheInfo
    {
        public string Name { get; set; } = "";
        public double Thickness { get; set; }
        public double Module { get; set; }
    }
}
