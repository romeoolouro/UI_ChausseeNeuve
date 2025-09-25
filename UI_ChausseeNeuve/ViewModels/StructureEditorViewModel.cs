using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using ChausseeNeuve.Domain.Models;
using UI_ChausseeNeuve.Services;
using System.Windows; // Pour ShowDialog Owner

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

    public class StructureEditorViewModel : INotifyPropertyChanged
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

        public StructureEditorViewModel()
        {
            // Connecter les notifications de Layer aux toasts
            Layer.NotifyToast = (message, type) => ToastRequested?.Invoke(message, type);

            // Charger les données depuis AppState ou initialiser
            LoadFromAppState();

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
            pavementStructure.Layers.Add(new Layer { Order = 1, Role = LayerRole.Roulement, Family = MaterialFamily.BetonBitumineux, MaterialName = "EB-BBSG 0/10", Thickness_m = 0.060, Modulus_MPa = 7000, Poisson = 0.35, InterfaceWithBelow = InterfaceType.Collee });
            pavementStructure.Layers.Add(new Layer { Order = 2, Role = LayerRole.Base, Family = MaterialFamily.MTLH, MaterialName = "MTLH Base", Thickness_m = 0.150, Modulus_MPa = 23000, Poisson = 0.25, InterfaceWithBelow = InterfaceType.SemiCollee });
            pavementStructure.Layers.Add(new Layer { Order = 3, Role = LayerRole.Fondation, Family = MaterialFamily.MTLH, MaterialName = "MTLH Fondation", Thickness_m = 0.150, Modulus_MPa = 23000, Poisson = 0.25, InterfaceWithBelow = InterfaceType.Collee });
            
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

            // Important: conserver la famille Bibliotheque pour ne pas forcer de contraintes
            layer.Family = MaterialFamily.Bibliotheque;
            // Nom affiché
            layer.MaterialName = mat.Name ?? layer.MaterialName;
            // Module: utiliser la valeur choisie/calculée
            double modulus = mat.ComputedModulus > 0 ? mat.ComputedModulus : (mat.Modulus_MPa > 0 ? mat.Modulus_MPa : layer.Modulus_MPa);
            layer.Modulus_MPa = modulus;
            // Coefficient de Poisson (utiliser tel quel si fourni)
            if (mat.PoissonRatio > 0) layer.Poisson = mat.PoissonRatio;
            // Épaisseur: respecter min/max si la fiche l’impose (facultatif)
            if (mat.MinThickness_m.HasValue && layer.Thickness_m < mat.MinThickness_m.Value)
                layer.Thickness_m = mat.MinThickness_m.Value;
            if (mat.MaxThickness_m.HasValue && layer.Thickness_m > mat.MaxThickness_m.Value)
                layer.Thickness_m = mat.MaxThickness_m.Value;
        }

        // + / - juste au-dessus de la Plateforme
        private void AddLayer()
        {
            var platIdx = Layers.ToList().FindIndex(l => l.Role == LayerRole.Plateforme);
            if (platIdx >= 0)
            {
                Layers.Insert(platIdx, new Layer
                {
                    Role = LayerRole.Base,
                    Family = MaterialFamily.GNT,
                    MaterialName = "GNT (suppl.)",
                    Thickness_m = 0.10,
                    Modulus_MPa = 800,
                    Poisson = 0.35,
                    InterfaceWithBelow = InterfaceType.Collee
                });
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

            // Validation des interfaces selon NF P98-086
            ValidateInterfaces(errors, warnings);

            // Validation de la structure globale selon le type
            ValidateGlobalStructure(errors, warnings);

            // LOT 5 - Système de validation moderne avec MessageBox améliorée
            var result = CreateValidationReport(errors, warnings);

            var style = errors.Count > 0 ? System.Windows.MessageBoxImage.Error :
                        warnings.Count > 0 ? System.Windows.MessageBoxImage.Warning :
                        System.Windows.MessageBoxImage.Information;

            System.Windows.MessageBox.Show(result.Message, result.Title, System.Windows.MessageBoxButton.OK, style);
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
                msg.AppendLine("✅ Structure conforme aux exigences NF P98-086");
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
            // Règles selon NF P98-086 Section 8.5.1.3

            // Fondation/Base sur Plate-forme : toujours collée
            if (lower.Role == LayerRole.Plateforme)
                return InterfaceType.Collee;

            // Base MTLH - règles spécifiques
            if (upper.Role == LayerRole.Base && upper.Family == MaterialFamily.MTLH)
            {
                // Simplification : MTLH = semi-collée par défaut (sauf cas spéciaux)
                return InterfaceType.SemiCollee;
            }

            // Surface - Base : collée (sauf si Base en sol traité)
            if (upper.Role == LayerRole.Roulement && lower.Role == LayerRole.Base)
            {
                return lower.Family == MaterialFamily.MTLH ? InterfaceType.SemiCollee : InterfaceType.Collee;
            }

            // Défaut : collée
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
                    ValidateSoupleStructure(orderedLayers, errors, warnings);
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
            }
        }

        private void ValidateSoupleStructure(List<Layer> layers, List<string> errors, List<string> warnings)
        {
            // NF P98-086 Section 3.1.11 et 8.2
            var totalBB = layers.Where(l => l.Family == MaterialFamily.BetonBitumineux).Sum(l => l.Thickness_m);
            var totalGNT = layers.Where(l => l.Family == MaterialFamily.GNT).Sum(l => l.Thickness_m);

            if (totalBB > 0.12)
                errors.Add($"Structure Souple : BB total ({totalBB:F3}m) > 0.12m interdit (NF §3.1.11)");

            if (totalGNT < 0.15)
                errors.Add($"Structure Souple : GNT total ({totalGNT:F3}m) < 0.15m requis (NF §3.1.11)");

            // Vérification absence MTLH/Béton en assises
            var assises = layers.Where(l => l.Role == LayerRole.Base || l.Role == LayerRole.Fondation);
            if (assises.Any(l => l.Family == MaterialFamily.MTLH || l.Family == MaterialFamily.BetonCiment))
                errors.Add("Structure Souple : MTLH/Béton interdits en assises (NF §3.1.11)");
        }

        private void ValidateSemiRigideStructure(List<Layer> layers, List<string> errors, List<string> warnings)
        {
            // NF P98-086 Section 3.1.13 et 8.5
            var roulement = layers.FirstOrDefault(l => l.Role == LayerRole.Roulement);
            if (roulement?.Family != MaterialFamily.BetonBitumineux)
                errors.Add("Structure Semi-rigide : Roulement doit être en Béton Bitumineux (NF §3.1.13)");

            if (roulement?.Thickness_m < 0.06)
                errors.Add($"Structure Semi-rigide : Épaisseur surface minimum 0.06m (actuel: {roulement?.Thickness_m:F3}m)");

            var assises = layers.Where(l => l.Role == LayerRole.Base || l.Role == LayerRole.Fondation);
            if (!assises.All(l => l.Family == MaterialFamily.MTLH))
                errors.Add("Structure Semi-rigide : Assises (Base/Fondation) doivent être en MTLH (NF §3.1.13)");

            var fondation = layers.Where(l => l.Role == LayerRole.Fondation).Sum(l => l.Thickness_m);
            if (fondation < 0.15)
                warnings.Add($"Structure Semi-rigide : Fondation ({fondation:F3}m) < 0.15m recommandé selon classe PF");
        }

        private void ValidateBitumineusseEpaisseStructure(List<Layer> layers, List<string> errors, List<string> warnings)
        {
            // NF P98-086 Section 3.1.12 et 8.3
            var roulement = layers.FirstOrDefault(l => l.Role == LayerRole.Roulement);
            var base_ = layers.FirstOrDefault(l => l.Role == LayerRole.Base);

            if (roulement?.Family != MaterialFamily.BetonBitumineux)
                errors.Add("Structure Bitumineuse épaisse : Roulement doit être en BB (NF §3.1.12)");

            if (base_?.Family != MaterialFamily.BetonBitumineux)
                errors.Add("Structure Bitumineuse épaisse : Base doit être en BB (NF §3.1.12)");

            var totalBB = layers.Where(l => l.Family == MaterialFamily.BetonBitumineux).Sum(l => l.Thickness_m);
            var totalChaussee = layers.Sum(l => l.Thickness_m);
            var ratio = totalChaussee > 0 ? totalBB / totalChaussee : 0;

            if (ratio < 0.45 || ratio > 0.60)
                warnings.Add($"Structure Bitumineuse épaisse : Ratio BB/total ({ratio:F2}) hors plage [0.45-0.60] recommandée");
        }

        private void ValidateRigideStructure(List<Layer> layers, List<string> errors, List<string> warnings)
        {
            // NF P98-086 Section 3.1.16 et 8.8
            var betonLayers = layers.Where(l => l.Family == MaterialFamily.BetonCiment);
            if (!betonLayers.Any())
                errors.Add("Structure Rigide : Au moins une couche en Béton de Ciment requise (NF §3.1.16)");

            var maxBetonThickness = betonLayers.Any() ? betonLayers.Max(l => l.Thickness_m) : 0;
            if (maxBetonThickness < 0.12)
                errors.Add($"Structure Rigide : Couche béton ≥ 0.12m requise (actuel max: {maxBetonThickness:F3}m)");
        }

        private void Recompute()
        {
            UpdateGraduations();
        }

        private void TryAutoScale()
        {
            if (!AutoScaleEnabled) { UpdateGraduations(); return; }
            double total = Layers.Where(l => l.Role != LayerRole.Plateforme).Sum(l => l.Thickness_m);
            if (total <= 0 || ViewportHeight <= 0) { UpdateGraduations(); return; }
            double padding = 24;
            double usable = System.Math.Max(0, ViewportHeight - padding);
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
                double px = System.Math.Max(2, l.Thickness_m * DepthScale);
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
}
