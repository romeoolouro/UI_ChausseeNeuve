using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;

namespace ChausseeNeuve.Domain.Models
{
    public class Layer : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        // Délégué pour notifications toast
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

        public int Order { get => _order; set { _order = value; OnPropertyChanged(); } }
        public LayerRole Role { get => _role; set { _role = value; OnPropertyChanged(); ValidateAll(); NotifyCoefficientsChanged(); } }
        public string MaterialName { get => _material; set { _material = value; OnPropertyChanged(); } }
        public MaterialFamily Family { get => _family; set { _family = value; OnPropertyChanged(); ValidateAll(); NotifyCoefficientsChanged(); } }

        public double Thickness_m
        {
            get => _t;
            set
            {
                var validatedValue = ValidateThickness(value);
                if (_t != validatedValue)
                {
                    _t = validatedValue;
                    OnPropertyChanged();
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
                if (_E != validatedValue)
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
                if (_nu != validatedValue)
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

        private double ValidateModulus(double value)
        {
            ClearErrors(nameof(Modulus_MPa));

            var (min, max) = GetModulusRange();
            if (value < min)
            {
                AddError(nameof(Modulus_MPa), $"Module ajusté à {min} MPa (min. NF P98-086 pour {Family})");
                NotifyToast?.Invoke($"Module E ajusté à {min} MPa pour {Family} ({Role})", ToastType.Warning);
                return min;
            }
            if (value > max)
            {
                AddError(nameof(Modulus_MPa), $"Module ajusté à {max} MPa (max. NF P98-086 pour {Family})");
                NotifyToast?.Invoke($"Module E ajusté à {max} MPa pour {Family} ({Role})", ToastType.Warning);
                return max;
            }
            return value;
        }

        private double ValidatePoisson(double value)
        {
            ClearErrors(nameof(Poisson));

            var expectedPoisson = GetExpectedPoisson();
            if (Math.Abs(value - expectedPoisson) > 0.001)
            {
                AddError(nameof(Poisson), $"Coefficient de Poisson fixé à {expectedPoisson} (NF P98-086 pour {Family})");
                NotifyToast?.Invoke($"Coefficient ν ajusté à {expectedPoisson} pour {Family}", ToastType.Info);
                return expectedPoisson;
            }
            return value;
        }

        private double ValidateThickness(double value)
        {
            ClearErrors(nameof(Thickness_m));

            if (Role == LayerRole.Plateforme) return value; // Pas de limite pour plate-forme

            var (min, max) = GetThicknessRange();
            if (value < min)
            {
                AddError(nameof(Thickness_m), $"Épaisseur ajustée à {min:F3} m (min. NF P98-086 pour {Family})");
                return min;
            }
            if (value > max)
            {
                AddError(nameof(Thickness_m), $"Épaisseur ajustée à {max:F3} m (max. NF P98-086 pour {Family})");
                return max;
            }
            return value;
        }

        private void ValidateAll()
        {
            // Re-validate all properties when Family or Role changes
            Modulus_MPa = ValidateModulus(_E);
            Poisson = ValidatePoisson(_nu);
            Thickness_m = ValidateThickness(_t);
        }

        private (double min, double max) GetModulusRange()
        {
            return Family switch
            {
                MaterialFamily.GNT => (100, 1000),
                MaterialFamily.MTLH => (3000, 32000),
                MaterialFamily.BetonBitumineux => (3000, 18000),
                MaterialFamily.BetonCiment => (18000, 40000),
                MaterialFamily.Bibliotheque => (1, 100000), // Pas de limite pour bibliothèque
                _ => (1, 100000)
            };
        }

        private double GetExpectedPoisson()
        {
            return Family switch
            {
                MaterialFamily.GNT => 0.35,
                MaterialFamily.BetonBitumineux => 0.35,
                MaterialFamily.MTLH => 0.25,
                MaterialFamily.BetonCiment => 0.25,
                MaterialFamily.Bibliotheque => _nu, // Pas de contrainte pour bibliothèque
                _ => _nu
            };
        }

        private (double min, double max) GetThicknessRange()
        {
            return Family switch
            {
                MaterialFamily.GNT => (0.10, 0.35),
                MaterialFamily.MTLH => (0.15, 0.32),
                MaterialFamily.BetonBitumineux => (0.05, 0.16),
                MaterialFamily.BetonCiment => (0.12, 0.45),
                MaterialFamily.Bibliotheque => (0.01, 2.0), // Plage large pour bibliothèque
                _ => (0.01, 2.0)
            };
        }

        // INotifyDataErrorInfo implementation
        public bool HasErrors => _errors.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public System.Collections.IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                return _errors.Values.SelectMany(x => x);

            return _errors.ContainsKey(propertyName) ? _errors[propertyName] : Enumerable.Empty<string>();
        }

        private void AddError(string propertyName, string error)
        {
            if (!_errors.ContainsKey(propertyName))
                _errors[propertyName] = new List<string>();

            if (!_errors[propertyName].Contains(error))
            {
                _errors[propertyName].Add(error);
                OnErrorsChanged(propertyName);
            }
        }

        private void ClearErrors(string propertyName)
        {
            if (_errors.ContainsKey(propertyName))
            {
                _errors.Remove(propertyName);
                OnErrorsChanged(propertyName);
            }
        }

        private void OnErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void NotifyCoefficientsChanged()
        {
            OnPropertyChanged(nameof(CoeffKs));
            OnPropertyChanged(nameof(CoeffKd));
        }

        // LOT 4 - Calcul coefficients ks/kd selon NF P98-086
        private double CalculateKs()
        {
            // Coefficient de structure ks selon Section 6.2.2 NF P98-086
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
            // Coefficient de déformation kd selon Section 6.2.3 NF P98-086
            var baseKd = Family switch
            {
                MaterialFamily.GNT => 2.0,
                MaterialFamily.MTLH => 1.5,
                MaterialFamily.BetonBitumineux => 1.0,
                MaterialFamily.BetonCiment => 0.8,
                MaterialFamily.Bibliotheque => 1.8,
                _ => 2.0
            };

            // Ajustement selon l'épaisseur (Section 6.2.3.2)
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
    }

    // ToastType enum for Layer.cs compatibility
    public enum ToastType
    {
        Success,
        Warning,
        Error,
        Info
    }
}
