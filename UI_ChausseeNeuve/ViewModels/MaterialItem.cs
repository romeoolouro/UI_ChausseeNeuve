using ChausseeNeuve.Domain.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace UI_ChausseeNeuve.ViewModels
{
    public class MaterialItem : INotifyPropertyChanged
    {
        public string? Name { get; set; }
        public MaterialFamily MaterialFamily { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; } // MB, MTLH, Beton, Sol_GNT

        // Propriétés mécaniques selon NF P98-086
        public double Modulus_MPa { get; set; }          // Module d'Young
        public double PoissonRatio { get; set; }         // Coefficient de Poisson
        public double? MinThickness_m { get; set; }      // Épaisseur minimale
        public double? MaxThickness_m { get; set; }      // Épaisseur maximale
        public string? Source { get; set; }              // Référence (NF P98-086, etc.)

        // Propriétés avancées pour MTLH (hydraulique)
        public string? Statut { get; set; } // "system" ou "user"
        public double? Sigma6 { get; set; } // Sigma6 (MPa)
        public double? InverseB { get; set; } // -1/b
        public double? Sl { get; set; } // Sl
        private double? _sh; // Sh (m)
        public double? Sh
        {
            get => _sh;
            set
            {
                if (_sh != value)
                {
                    _sh = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ShDisplay));
                }
            }
        }
        private string? _shStatus; // "standard" ou "filled"
        public string? ShStatus
        {
            get => _shStatus;
            set
            {
                if (_shStatus != value)
                {
                    _shStatus = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ShDisplay));
                }
            }
        }
        public double? Kc { get; set; } // Kc
        public double? Kd { get; set; } // Kd

        // Ajout pour affichage Alizé
        public double? SN { get; set; } // SN
        public double? Epsi0_10C { get; set; } // Epsi0 (10°C)
        public Dictionary<int, double>? EvsTemperature { get; set; } // E(Température) - values typically at reference frequency

        // 2D table: E(T, f) -> outer key: temperature (°C), inner key: frequency (Hz)
        // Example: EvsTempFreq[10][10] == E at 10°C and 10Hz
        public Dictionary<int, Dictionary<int, double>>? EvsTempFreq { get; set; }

        // Parameters for frequency dependence
        public int ReferenceFrequency { get; set; } = 10; // Hz, default reference frequency
        public double? FrequencyExponent { get; set; } = 0.25; // exponent m for E(f) = E_ref * (f/f_ref)^m

        // Calibration factor (to adjust computed modulus to match reference Alizé)
        public double CalibrationFactor { get; set; } = 1.0;

        // Last calibration target value (for display/debug)
        private double? _lastCalibrationTarget;
        public double? LastCalibrationTarget
        {
            get => _lastCalibrationTarget;
            set
            {
                if (_lastCalibrationTarget != value)
                {
                    _lastCalibrationTarget = value;
                    OnPropertyChanged();
                }
            }
        }

        // Computed modulus for current T/F (updated by VM)
        private double _computedModulus;
        public double ComputedModulus
        {
            get => _computedModulus;
            set
            {
                if (Math.Abs(_computedModulus - value) > 1e-6)
                {
                    _computedModulus = value;
                    OnPropertyChanged();
                }
            }
        }

        // Propriétés additionnelles génériques
        public Dictionary<string, object>? AdditionalProperties { get; set; }

        /// <summary>
        /// Propriété calculée pour l'affichage de Sh dans l'interface selon le statut
        /// </summary>
        public string ShDisplay => (ShStatus == "standard" || !Sh.HasValue) ? (ShStatus == "standard" ? "standard" : "/") : Sh.Value.ToString("0.###");

        /// <summary>
        /// Méthode pour remplir automatiquement la valeur Sh selon les règles d'Alizé
        /// </summary>
        public void FillShFromStandard()
        {
            if (ShStatus != "standard" || Category != "MB") return;

            var lname = (Name ?? string.Empty).ToLowerInvariant();
            
            if (lname.Contains("eb-gb"))
                Sh = 0.030; // Graves bitumes (m)
            else if (lname.Contains("eme"))
                Sh = 0.025; // EME (m)
            else
                Sh = 0.025; // BBSG, BBME, autres (m)

            ShStatus = "filled";
            NotifyPropertyChanged(nameof(ShDisplay));
        }

        /// <summary>
        /// Crée un objet Layer à partir de ce MaterialItem
        /// </summary>
        public Layer ToLayer(LayerRole role, double thickness = 0.1)
        {
            return new Layer
            {
                Role = role,
                MaterialName = Name ?? "Matériau inconnu",
                Family = MaterialFamily,
                Thickness_m = thickness,
                Modulus_MPa = Modulus_MPa,
                Poisson = PoissonRatio
            };
        }

        /// <summary>
        /// Retourne le module à la température et fréquence demandées.
        /// Logique:
        /// - Si une table 2D EvsTempFreq est disponible : bilinear interpolation using surrounding T and f points.
        /// - Sinon si EvsTemperature disponible: interpolation linéaire en T entre points fournis.
        /// - Sinon utiliser Modulus_MPa comme valeur de référence.
        /// - Si no 2D table, ajuster la fréquence par la loi E = E_ref * (f/f_ref)^m où m = FrequencyExponent.
        /// - Clamping: si temperature ou frequency demandées en dehors des bornes de la table, la valeur est clampée aux bornes (pas d'extrapolation agressive).
        /// </summary>
        public double GetModulusAt(int temperatureC, int frequencyHz)
        {
            // Prefer 2D table if available
            if (EvsTempFreq != null && EvsTempFreq.Count > 0)
            {
                // Get sorted temperature keys
                var temps = EvsTempFreq.Keys.OrderBy(t => t).ToArray();
                // Clamp temperature to available range
                int tLow = temps.First();
                int tHigh = temps.Last();
                if (temperatureC <= tLow) temperatureC = tLow;
                if (temperatureC >= tHigh) temperatureC = tHigh;

                // Find bounding temperatures
                int t0 = temps.Where(t => t <= temperatureC).Max();
                int t1 = temps.Where(t => t >= temperatureC).Min();

                // For each temperature row, perform frequency interpolation
                double e_t0 = InterpolateInFrequencyRow(EvsTempFreq[t0], frequencyHz);
                double e_t1 = InterpolateInFrequencyRow(EvsTempFreq[t1], frequencyHz);

                if (t0 == t1) return e_t0; // exact temp available

                // Linear interpolation in temperature between t0 and t1
                double e = e_t0 + (e_t1 - e_t0) * (temperatureC - t0) / (double)(t1 - t0);
                return e;
            }

            // Fallback to 1D temperature table if available
            double baseE;
            if (EvsTemperature != null && EvsTemperature.Count > 0)
            {
                if (EvsTemperature.TryGetValue(temperatureC, out var exact))
                {
                    baseE = exact;
                }
                else
                {
                    var keys = EvsTemperature.Keys.OrderBy(k => k).ToArray();
                    if (temperatureC <= keys.First())
                    {
                        baseE = EvsTemperature[keys.First()];
                    }
                    else if (temperatureC >= keys.Last())
                    {
                        baseE = EvsTemperature[keys.Last()];
                    }
                    else
                    {
                        int lower = keys.Where(k => k < temperatureC).Max();
                        int upper = keys.Where(k => k > temperatureC).Min();
                        double eL = EvsTemperature[lower];
                        double eU = EvsTemperature[upper];
                        baseE = eL + (eU - eL) * (temperatureC - lower) / (double)(upper - lower);
                    }
                }
            }
            else
            {
                baseE = Modulus_MPa;
            }

            // Adjust for frequency using power law
            if (frequencyHz <= 0) return baseE;

            int fRef = ReferenceFrequency > 0 ? ReferenceFrequency : 10;
            double m = FrequencyExponent ?? 0.25;

            try
            {
                if (frequencyHz == fRef) return baseE * CalibrationFactor;
                double factor = Math.Pow((double)frequencyHz / (double)fRef, m);
                return baseE * factor * CalibrationFactor;
            }
            catch
            {
                return baseE * CalibrationFactor;
            }
        }

        // Helper: interpolate in a frequency row (dictionary freq -> E), clamped to bounds, linear interpolation
        private double InterpolateInFrequencyRow(Dictionary<int, double> freqRow, int frequencyHz)
        {
            if (freqRow == null || freqRow.Count == 0) return Modulus_MPa;
            var freqs = freqRow.Keys.OrderBy(f => f).ToArray();

            if (frequencyHz <= freqs.First()) return freqRow[freqs.First()];
            if (frequencyHz >= freqs.Last()) return freqRow[freqs.Last()];

            int f0 = freqs.Where(f => f <= frequencyHz).Max();
            int f1 = freqs.Where(f => f >= frequencyHz).Min();
            if (f0 == f1) return freqRow[f0];

            double e0 = freqRow[f0];
            double e1 = freqRow[f1];
            double e = e0 + (e1 - e0) * (frequencyHz - f0) / (double)(f1 - f0);
            return e;
        }

        public override string ToString()
        {
            return Name ?? "Unnamed Material";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Méthode publique pour déclencher OnPropertyChanged depuis l'extérieur
        /// </summary>
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
