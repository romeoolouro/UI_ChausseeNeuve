using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ChausseeNeuve.Domain.Models
{
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
        private double _sigma6 = 1.0;
        private double _cam;
        private double _e10C10Hz = 0.0;
        private double _eteq10Hz = 0.0;
        private bool _kthetaAuto = false;

        // Marqueur interne: A saisi par l'utilisateur (pour EpsiZ)
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
                // Notifier états d'édition dépendants
                SafePropertyChanged(nameof(IsEpsiZ));
                SafePropertyChanged(nameof(IsEpsiT));
                SafePropertyChanged(nameof(IsSigmaT));
                SafePropertyChanged(nameof(CanEditEpsilon6));
                SafePropertyChanged(nameof(CanEditAmplitude));
                SafePropertyChanged(nameof(CanEditSn));
                SafePropertyChanged(nameof(CanEditSh));
                SafePropertyChanged(nameof(CanEditKc));
                SafePropertyChanged(nameof(CanEditKr));
                SafePropertyChanged(nameof(CanEditKs));
                SafePropertyChanged(nameof(CanEditKtheta));
                SafePropertyChanged(nameof(CanEditKd));
            }
        }

        // Etats d'édition/visibilité selon le critère
        public bool IsEpsiZ => string.Equals(_critere, "EpsiZ", StringComparison.OrdinalIgnoreCase);
        public bool IsEpsiT => string.Equals(_critere, "EpsiT", StringComparison.OrdinalIgnoreCase);
        public bool IsSigmaT => string.Equals(_critere, "SigmaT", StringComparison.OrdinalIgnoreCase);

        // ?6 éditable uniquement pour EpsiT
        public bool CanEditEpsilon6 => IsEpsiT;

        // Amplitude éditabe uniquement pour EpsiZ (critère "A")
        public bool CanEditAmplitude => IsEpsiZ;

        // Sn/Sh désactivés quand Kr n'est pas éditable
        public bool CanEditSn => CanEditKr;
        public bool CanEditSh => CanEditKr;

        // Coefficients par critère
        public bool CanEditKc => IsEpsiT || IsSigmaT;
        public bool CanEditKr => IsEpsiT || IsSigmaT;
        public bool CanEditKs => IsEpsiT || IsSigmaT;
        public bool CanEditKtheta => IsEpsiT; // k? éditable uniquement pour EpsiT
        public bool CanEditKd => IsSigmaT;    // seulement pour ?t

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

        public double Ne
        {
            get => _ne;
            set { _ne = value; SafePropertyChanged(); }
        }

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
            "SigmaT" => "?6",
            _ => "?6"
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