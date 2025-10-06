using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;

namespace ChausseeNeuve.Domain.Models
{
    public partial class Layer : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        private const double PLATFORM_INFINITE_THICKNESS = 10_000_000.0; // 10 millions de mètres

        public static Action<string, ToastType>? NotifyToast { get; set; }

        private int _order;
        private LayerRole _role;
        private string _material = "";
        private MaterialFamily _family;
        private double _t;
        private double _E;
        private double _nu;
        private InterfaceType? _iface;
        private readonly Dictionary<string, List<string>> _errors = new();

        // Nouveau: mode dimensionnement (impacte la validation)
        private DimensionnementMode _mode = DimensionnementMode.Expert;
        public DimensionnementMode Mode
        {
            get => _mode;
            set
            {
                if (_mode != value)
                {
                    _mode = value;
                    OnPropertyChanged();
                    // Revalider selon le mode courant sans perdre les valeurs utilisateur
                    ValidateAll();
                }
            }
        }

        // États hors norme (pour styliser l'UI)
        private bool _isThicknessOutOfNorm;
        public bool IsThicknessOutOfNorm { get => _isThicknessOutOfNorm; private set { if (_isThicknessOutOfNorm != value) { _isThicknessOutOfNorm = value; OnPropertyChanged(); OnPropertyChanged(nameof(ThicknessWarningText)); } } }
        private bool _isModulusOutOfNorm;
        public bool IsModulusOutOfNorm { get => _isModulusOutOfNorm; private set { if (_isModulusOutOfNorm != value) { _isModulusOutOfNorm = value; OnPropertyChanged(); OnPropertyChanged(nameof(ModulusWarningText)); } } }
        private bool _isPoissonOutOfNorm;
        public bool IsPoissonOutOfNorm { get => _isPoissonOutOfNorm; private set { if (_isPoissonOutOfNorm != value) { _isPoissonOutOfNorm = value; OnPropertyChanged(); OnPropertyChanged(nameof(PoissonWarningText)); } } }

        // Textes d'avertissement pour tooltip (vides si conforme ou mode Automatique)
        public string ThicknessWarningText
        {
            get
            {
                if (!IsThicknessOutOfNorm) return string.Empty;
                var (min, max) = GetThicknessRange();
                if (ShouldExemptMinThickness(min))
                {
                    // Si exemptée, pas de message (considérée conforme)
                    return string.Empty;
                }
                return $"Épaisseur {Thickness_m:F3} m hors plage normative [{min:F3}; {max:F3}] (NF P98-086).";
            }
        }
        public string ModulusWarningText
        {
            get
            {
                if (!IsModulusOutOfNorm) return string.Empty;
                var (min, max) = GetModulusRange();
                return $"Module E={Modulus_MPa:0.##} MPa hors plage normative [{min}-{max}] (NF P98-086).";
            }
        }
        public string PoissonWarningText
        {
            get
            {
                if (!IsPoissonOutOfNorm) return string.Empty;
                double exp = GetExpectedPoisson();
                return $"Coefficient ?={Poisson:0.###} ? valeur normative attendue {exp:0.##} (NF P98-086).";
            }
        }

        // Pour limiter les toasts répétés en mode Expert
        private readonly HashSet<string> _expertWarned = new();

        public int Order { get => _order; set { _order = value; OnPropertyChanged(); } }

        public LayerRole Role
        {
            get => _role;
            set
            {
                _role = value;
                OnPropertyChanged();
                ValidateAll();
                NotifyCoefficientsChanged();

                if (_role == LayerRole.Plateforme && Math.Abs(_t - PLATFORM_INFINITE_THICKNESS) > 0.001)
                {
                    _t = PLATFORM_INFINITE_THICKNESS;
                    OnPropertyChanged(nameof(Thickness_m));
                    OnPropertyChanged(nameof(ThicknessDisplay));
                    NotifyToast?.Invoke($"Épaisseur plateforme fixée à {PLATFORM_INFINITE_THICKNESS:N0} m (infini)", ToastType.Info);
                }
            }
        }

        public string MaterialName { get => _material; set { _material = value; OnPropertyChanged(); } }
        public MaterialFamily Family { get => _family; set { _family = value; OnPropertyChanged(); ValidateAll(); NotifyCoefficientsChanged(); } }

        public double Thickness_m
        {
            get => _t;
            set
            {
                var previous = _t;
                var validatedValue = ValidateThickness(value, previous);
                if (Math.Abs(_t - validatedValue) > 1e-9)
                {
                    _t = validatedValue;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ThicknessDisplay));
                    NotifyCoefficientsChanged();
                }
            }
        }

        public double Modulus_MPa
        {
            get => _E;
            set
            {
                var validatedValue = ValidateModulus(value);
                if (Math.Abs(_E - validatedValue) > 1e-9)
                {
                    _E = validatedValue;
                    OnPropertyChanged();
                }
            }
        }

        public double Poisson
        {
            get => _nu;
            set
            {
                var validatedValue = ValidatePoisson(value);
                if (Math.Abs(_nu - validatedValue) > 1e-9)
                {
                    _nu = validatedValue;
                    OnPropertyChanged();
                }
            }
        }

        // LOT 4 - Coefficients ks/kd selon NF P98-086
        public double CoeffKs => CalculateKs();
        public double CoeffKd => CalculateKd();

        public InterfaceType? InterfaceWithBelow { get => _iface; set { _iface = value; OnPropertyChanged(); } }

        public bool IsPlatform => Role == LayerRole.Plateforme;
        public string ThicknessDisplay => Role == LayerRole.Plateforme ? "?" : $"{Thickness_m:F3} m";

        private bool _hasAutoCorrection;
        public bool HasAutoCorrection { get => _hasAutoCorrection; private set { if (_hasAutoCorrection != value) { _hasAutoCorrection = value; OnPropertyChanged(); } } }
        private string _autoCorrectionNote = string.Empty;
        public string AutoCorrectionNote { get => _autoCorrectionNote; private set { if (_autoCorrectionNote != value) { _autoCorrectionNote = value; OnPropertyChanged(); } } }

        private void AppendCorrectionNote(string msg)
        {
            if (string.IsNullOrWhiteSpace(_autoCorrectionNote)) _autoCorrectionNote = msg;
            else if (!_autoCorrectionNote.Contains(msg, StringComparison.OrdinalIgnoreCase)) _autoCorrectionNote += " | " + msg;
            HasAutoCorrection = true;
            OnPropertyChanged(nameof(AutoCorrectionNote));
        }

        private void ResetAutoCorrection()
        {
            if (HasAutoCorrection || !string.IsNullOrEmpty(_autoCorrectionNote))
            {
                _autoCorrectionNote = string.Empty;
                HasAutoCorrection = false;
                OnPropertyChanged(nameof(AutoCorrectionNote));
            }
        }

        public double? LibraryEpsilon6 { get; set; }
        public double? LibrarySh { get; set; }

        private double ValidateModulus(double value)
        {
            ClearErrors(nameof(Modulus_MPa));
            var (min, max) = GetModulusRange();

            if (Mode == DimensionnementMode.Expert)
            {
                bool outNorm = value < min || value > max;
                IsModulusOutOfNorm = outNorm;
                if (outNorm)
                {
                    AddError(nameof(Modulus_MPa), $"Avertissement: E={value:0.##} MPa hors plage [{min}-{max}] (Expert—non corrigé)");
                    WarnOnce(nameof(Modulus_MPa), $"Module hors norme conservé (E={value:0.##}). Basculez en mode Automatique pour corrections.");
                }
                return value; // Pas de correction
            }

            // Automatique: correction
            if (value < min)
            {
                IsModulusOutOfNorm = false; // après correction
                AddError(nameof(Modulus_MPa), $"Module ajusté à {min} MPa (min NF P98-086)");
                NotifyToast?.Invoke($"E corrigé ({value:0.##}?{min}) - passez en mode Expert pour dépasser", ToastType.Warning);
                AppendCorrectionNote($"Module E ajusté à {min} MPa (min NF P98-086)");
                return min;
            }
            if (value > max)
            {
                IsModulusOutOfNorm = false;
                AddError(nameof(Modulus_MPa), $"Module ajusté à {max} MPa (max NF P98-086)");
                NotifyToast?.Invoke($"E corrigé ({value:0.##}?{max}) - passez en mode Expert pour dépasser", ToastType.Warning);
                AppendCorrectionNote($"Module E ajusté à {max} MPa (max NF P98-086)");
                return max;
            }
            IsModulusOutOfNorm = false;
            // Valeur déjà conforme: on efface un éventuel indicateur de correction précédente
            ResetAutoCorrection();
            return value;
        }

        private double ValidatePoisson(double value)
        {
            ClearErrors(nameof(Poisson));
            var expected = GetExpectedPoisson();

            if (Mode == DimensionnementMode.Expert)
            {
                bool outNorm = Family != MaterialFamily.Bibliotheque && Math.Abs(value - expected) > 0.0005;
                IsPoissonOutOfNorm = outNorm;
                if (outNorm)
                {
                    AddError(nameof(Poisson), $"Avertissement: ?={value:0.###} ? {expected:0.##} attendu (Expert—non forcé)");
                    WarnOnce(nameof(Poisson), $"? hors valeur normative conservé ({value:0.###}). Mode Automatique le forcera.");
                }
                return value;
            }

            if (Family != MaterialFamily.Bibliotheque && Math.Abs(value - expected) > 0.0005)
            {
                IsPoissonOutOfNorm = false; // corrigé
                AddError(nameof(Poisson), $"? fixé à {expected:0.##} (NF P98-086)");
                NotifyToast?.Invoke($"? corrigé ({value:0.###}?{expected:0.##}) - passez en mode Expert pour le garder", ToastType.Info);
                AppendCorrectionNote($"Coefficient de Poisson ? corrigé à {expected:0.##} (NF P98-086)");
                return expected;
            }
            IsPoissonOutOfNorm = false;
            ResetAutoCorrection();
            return value;
        }

        // Interaction utilisateur (fournie par la couche UI)
        public enum ThicknessCorrectionChoice { Apply, Keep, Cancel }
        public static Func<double, double, double, ThicknessCorrectionChoice>? AskThicknessCorrection; // (newValue, min, max)
        public static Func<Layer, bool>? IsSmallestFoundationProvider; // retourne true si cette couche est la plus mince des fondations

        private double ValidateThickness(double value, double previousValue)
        {
            ClearErrors(nameof(Thickness_m));

            if (Role == LayerRole.Plateforme)
            {
                IsThicknessOutOfNorm = false;
                return PLATFORM_INFINITE_THICKNESS;
            }

            var (min, max) = GetThicknessRange();

            // Exemption (mode Expert) : plus petite sous-couche de fondation pour structure Souple ou Bitumineuse épaisse
            if (ShouldExemptMinThickness(min))
            {
                // Neutraliser le minimum pour la validation (on considère conforme)
                min = 0.0;
            }

            // Mode Expert interactif: proposer correction
            if (Mode == DimensionnementMode.Expert)
            {
                bool outNorm = value < min || value > max;
                IsThicknessOutOfNorm = outNorm;
                if (outNorm)
                {
                    // Si un gestionnaire interactif est defini on l utilise
                    if (AskThicknessCorrection != null)
                    {
                        var choice = AskThicknessCorrection(value, min, max);
                        switch (choice)
                        {
                            case ThicknessCorrectionChoice.Apply:
                                double corrected = value < min ? min : (value > max ? max : value);
                                AppendCorrectionNote($"Épaisseur ajustée manuellement à {corrected:F3} m (plage [{min:F3};{max:F3}])");
                                NotifyToast?.Invoke($"Épaisseur couche {Role} ajustée à {corrected:F3} m (norme)", ToastType.Success);
                                IsThicknessOutOfNorm = false;
                                return corrected;
                            case ThicknessCorrectionChoice.Keep:
                                AddError(nameof(Thickness_m), $"Avertissement: épaisseur {value:F3} m hors plage [{min:F3};{max:F3}] conservée (Expert)");
                                WarnOnce(nameof(Thickness_m), $"Épaisseur hors norme conservée ({value:F3} m)");
                                return value;
                            case ThicknessCorrectionChoice.Cancel:
                                NotifyToast?.Invoke($"Modification épaisseur annulée (retour {previousValue:F3} m)", ToastType.Info);
                                return previousValue;
                        }
                    }
                    AddError(nameof(Thickness_m), $"Avertissement: épaisseur {value:F3} m hors plage [{min:F3};{max:F3}] (Expert—non corrigée)");
                    WarnOnce(nameof(Thickness_m), $"Épaisseur hors norme conservée ({value:F3} m). Mode Automatique l'ajusterait.");
                }
                return value;
            }

            // Mode automatique (inchangé)
            if (value < min)
            {
                IsThicknessOutOfNorm = false;
                AddError(nameof(Thickness_m), $"Épaisseur ajustée à {min:F3} m (min NF P98-086)");
                NotifyToast?.Invoke($"Épaisseur corrigée ({value:F3}?{min:F3}) - passez en mode Expert pour conserver", ToastType.Warning);
                AppendCorrectionNote($"Épaisseur ajustée à {min:F3} m (min NF P98-086)");
                return min;
            }
            if (value > max)
            {
                IsThicknessOutOfNorm = false;
                AddError(nameof(Thickness_m), $"Épaisseur ajustée à {max:F3} m (max NF P98-086)");
                NotifyToast?.Invoke($"Épaisseur corrigée ({value:F3}?{max:F3}) - passez en mode Expert pour dépasser", ToastType.Warning);
                AppendCorrectionNote($"Épaisseur ajustée à {max:F3} m (max NF P98-086)");
                return max;
            }
            IsThicknessOutOfNorm = false;
            ResetAutoCorrection();
            return value;
        }

        private void ValidateAll()
        {
            if (Mode == DimensionnementMode.Automatique)
            {
                // Réinitialiser les notes de correction avant une nouvelle passe
                HasAutoCorrection = false;
                _autoCorrectionNote = string.Empty;
                OnPropertyChanged(nameof(AutoCorrectionNote));
            }
            _E = ValidateModulus(_E);
            _nu = ValidatePoisson(_nu);
            // passer previousValue = _t pour réévaluer (pas de changement effectif ici)
            _t = ValidateThickness(_t, _t);
            OnPropertyChanged(nameof(Modulus_MPa));
            OnPropertyChanged(nameof(Poisson));
            OnPropertyChanged(nameof(Thickness_m));
            OnPropertyChanged(nameof(ThicknessDisplay));
            OnPropertyChanged(nameof(ThicknessWarningText));
            OnPropertyChanged(nameof(ModulusWarningText));
            OnPropertyChanged(nameof(PoissonWarningText));
        }

        private (double min, double max) GetModulusRange() => Family switch
        {
            MaterialFamily.GNT => (100, 1000),
            MaterialFamily.MTLH => (3000, 32000),
            MaterialFamily.BetonBitumineux => (3000, 18000),
            MaterialFamily.BetonCiment => (18000, 40000),
            MaterialFamily.Bibliotheque => (1, 100000),
            _ => (1, 100000)
        };

        private double GetExpectedPoisson() => Family switch
        {
            MaterialFamily.GNT => 0.35,
            MaterialFamily.BetonBitumineux => 0.35,
            MaterialFamily.MTLH => 0.25,
            MaterialFamily.BetonCiment => 0.25,
            MaterialFamily.Bibliotheque => _nu, // libre
            _ => _nu
        };

        private (double min, double max) GetThicknessRange()
        {
            // Cas particulier : structure Bitumineuse épaisse -> couche de surface min 0.12 m, plus de plafond 0.08
            if (string.Equals(CurrentStructureType, "Bitumineuse épaisse", StringComparison.OrdinalIgnoreCase) && Role == LayerRole.Roulement)
            {
                return (0.12, 0.35); // tolérance jusqu'à 35 cm (au besoin ajuster)
            }
            // Si c'est une couche de fondation en GNT dans une structure souple en mode automatique,
            // on ne met pas de limite maximale d'épaisseur
            if (Mode == DimensionnementMode.Automatique && 
                Role == LayerRole.Fondation && 
                Family == MaterialFamily.GNT)
            {
                return (0.15, double.MaxValue); // Minimum 15cm, pas de maximum
            }

            return Role switch
            {
                LayerRole.Roulement => (0.02, 0.08),
                LayerRole.Base => (0.10, 0.35),
                LayerRole.Fondation => (0.15, 0.35),
                _ => (0.0, double.MaxValue)
            };
        }

        private bool ShouldExemptMinThickness(double currentMin)
        {
            if (Mode != DimensionnementMode.Expert) return false;
            if (Role != LayerRole.Fondation) return false;
            if (string.IsNullOrEmpty(CurrentStructureType)) return false;
            if (!CurrentStructureType.Equals("Souple", StringComparison.OrdinalIgnoreCase) &&
                !CurrentStructureType.Equals("Bitumineuse épaisse", StringComparison.OrdinalIgnoreCase)) return false;
            if (IsSmallestFoundationProvider?.Invoke(this) != true) return false;
            // Exemption seulement si la regle impose un min > 0 (ex: 0.15)
            return currentMin > 0 && Thickness_m < currentMin;
        }

        // INotifyDataErrorInfo
        public bool HasErrors => _errors.Count > 0;
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
        public System.Collections.IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName)) return _errors.Values.SelectMany(x => x);
            return _errors.ContainsKey(propertyName) ? _errors[propertyName] : Enumerable.Empty<string>();
        }
        private void AddError(string propertyName, string error)
        {
            if (!_errors.ContainsKey(propertyName)) _errors[propertyName] = new List<string>();
            if (!_errors[propertyName].Contains(error)) { _errors[propertyName].Add(error); OnErrorsChanged(propertyName); }
        }
        private void ClearErrors(string propertyName)
        {
            if (_errors.ContainsKey(propertyName)) { _errors.Remove(propertyName); OnErrorsChanged(propertyName); }
        }
        private void OnErrorsChanged(string propertyName) => ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void NotifyCoefficientsChanged()
        {
            OnPropertyChanged(nameof(CoeffKs));
            OnPropertyChanged(nameof(CoeffKd));
        }

        private void WarnOnce(string propKey, string message)
        {
            var key = $"{propKey}:{Order}";
            if (_mode == DimensionnementMode.Expert && _expertWarned.Add(key))
            {
                NotifyToast?.Invoke(message, ToastType.Warning);
            }
        }

        // LOT 4 - Calcul coefficients ks/kd selon NF P98-086
        private double CalculateKs()
        {
            return Role switch
            {
                LayerRole.Roulement => Family switch
                {
                    MaterialFamily.BetonBitumineux => 1.0,
                    MaterialFamily.MTLH => 1.15,
                    MaterialFamily.BetonCiment => 1.35,
                    _ => 1.0
                },
                LayerRole.Base => Family switch
                {
                    MaterialFamily.GNT => 1.0,
                    MaterialFamily.MTLH => 1.3,
                    MaterialFamily.BetonBitumineux => 1.2,
                    MaterialFamily.BetonCiment => 1.5,
                    _ => 1.0
                },
                LayerRole.Fondation => Family switch
                {
                    MaterialFamily.GNT => 1.0,
                    MaterialFamily.MTLH => 1.2,
                    _ => 1.0
                },
                LayerRole.Plateforme => 1.0,
                _ => 1.0
            };
        }

        private double CalculateKd()
        {
            var baseKd = Family switch
            {
                MaterialFamily.GNT => 2.0,
                MaterialFamily.MTLH => 1.5,
                MaterialFamily.BetonBitumineux => 1.0,
                MaterialFamily.BetonCiment => 0.8,
                MaterialFamily.Bibliotheque => 1.8,
                _ => 2.0
            };

            if (Role == LayerRole.Plateforme)
            {
                return Math.Round(baseKd, 2);
            }

            var thicknessMultiplier = Thickness_m switch
            {
                < 0.10 => 1.2,
                >= 0.10 and < 0.15 => 1.1,
                >= 0.15 and < 0.20 => 1.0,
                >= 0.20 and < 0.30 => 0.95,
                _ => 0.9
            };

            return Math.Round(baseKd * thicknessMultiplier, 2);
        }

        private string? _currentStructureType;
        public string? CurrentStructureType
        {
            get => _currentStructureType;
            set { if (_currentStructureType != value) { _currentStructureType = value; OnPropertyChanged(); ValidateAll(); } }
        }
    }

    // ToastType enum for Layer.cs compatibility (inchangé)
    public enum ToastType
    {
        Success,
        Warning,
        Error,
        Info
    }
}
