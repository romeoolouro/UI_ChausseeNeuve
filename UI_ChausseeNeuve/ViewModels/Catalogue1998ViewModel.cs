using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace UI_ChausseeNeuve.ViewModels
{
    /// <summary>
    /// ViewModel pour la fenêtre Catalogue 1998 (sélection CAM et Risque R) inspirée d'Alizé
    /// </summary>
    public class Catalogue1998ViewModel : INotifyPropertyChanged
    {
        // Classification du trafic (millions de PL) - valeurs indicatives issues du visuel
        public double[] VrsLimits { get; } = new double[] { 0, 0.5, 1, 3, 6, 14, 38, 94 };
        public double[] VrnsLimits { get; } = new double[] { 0, 0.2, 0.5, 1.5, 2.5, 6.5, 17.5, 43.5 };

        // CAM du trafic - Structures (VRS/VRNS)
        public double CamBitumineuxVrs { get; set; } = 0.8;
        public double CamBitumineuxVrns { get; set; } = 0.5;

        public double CamGntVrs { get; set; } = 1.0;
        public double CamGntVrns { get; set; } = 1.0;

        public double CamMixteVrs { get; set; } = 1.2;
        public double CamMixteVrns { get; set; } = 0.8;

        public double CamSemiRigideBetonVrs { get; set; } = 1.3;
        public double CamSemiRigideBetonVrns { get; set; } = 0.8;

        // Plate-forme support (TC2, TC3, >TC3)
        public double CamPF_TC2 { get; set; } = 0.50;
        public double CamPF_TC3 { get; set; } = 0.75;
        public double CamPF_TCSup3 { get; set; } = 1.00;

        // Sélection de la classe de trafic
        private string? _selectedSystem; // VRS/VRNS
        private string? _selectedTrafficClass; // TC1..TC8
        private string? _selectedTrafficKey;   // VRS:TC1 etc.

        public string? SelectedSystem
        {
            get => _selectedSystem;
            set { _selectedSystem = value; OnPropertyChanged(); OnPropertyChanged(nameof(SelectedTrafficDisplay)); }
        }
        public string? SelectedTrafficClass
        {
            get => _selectedTrafficClass;
            set { _selectedTrafficClass = value; OnPropertyChanged(); OnPropertyChanged(nameof(SelectedTrafficDisplay)); }
        }
        public string? SelectedTrafficKey
        {
            get => _selectedTrafficKey;
            set { _selectedTrafficKey = value; OnPropertyChanged(); }
        }

        public string SelectedTrafficDisplay
            => (SelectedSystem != null && SelectedTrafficClass != null)
                ? $"Classe selectionnee: {SelectedSystem} - {SelectedTrafficClass}"
                : "Aucune classe selectionnee";

        // Sélection CAM/cellules
        public ICommand SelectCamCommand { get; }
        public ICommand SelectTrafficClassCommand { get; }
        public ICommand SelectRiskCommand { get; }

        public Catalogue1998ViewModel()
        {
            SelectCamCommand = new RelayCommand<string>(OnSelectCam);
            SelectTrafficClassCommand = new RelayCommand<string>(OnSelectTrafficClass);
            SelectRiskCommand = new RelayCommand<string>(OnSelectRisk);
        }

        private void OnSelectTrafficClass(string? key)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            // key format: "VRS:TC1" or "VRNS:TC5"
            var parts = key.Split(':');
            if (parts.Length == 2)
            {
                SelectedSystem = parts[0];
                SelectedTrafficClass = parts[1];
                SelectedTrafficKey = key;
            }
        }

        private void OnSelectCam(string? kind)
        {
            if (string.IsNullOrEmpty(kind)) return;

            double value = kind switch
            {
                "bitu_vrs" => CamBitumineuxVrs,
                "bitu_vrns" => CamBitumineuxVrns,
                "gnt_vrs" => CamGntVrs,
                "gnt_vrns" => CamGntVrns,
                "mixte_vrs" => CamMixteVrs,
                "mixte_vrns" => CamMixteVrns,
                "srbeton_vrs" => CamSemiRigideBetonVrs,
                "srbeton_vrns" => CamSemiRigideBetonVrns,
                "pf_tc2" => CamPF_TC2,
                "pf_tc3" => CamPF_TC3,
                "pf_tcsup3" => CamPF_TCSup3,
                _ => 1.0
            };

            string display = kind switch
            {
                "bitu_vrs" => "Bitumineux VRS",
                "bitu_vrns" => "Bitumineux VRNS",
                "gnt_vrs" => "GNT/GNT VRS",
                "gnt_vrns" => "GNT/GNT VRNS",
                "mixte_vrs" => "Mixtes VRS",
                "mixte_vrns" => "Mixtes VRNS",
                "srbeton_vrs" => "Semi-rigides/Béton VRS",
                "srbeton_vrns" => "Semi-rigides/Béton VRNS",
                "pf_tc2" => "Plate-forme TC2",
                "pf_tc3" => "Plate-forme TC3",
                "pf_tcsup3" => "Plate-forme >TC3",
                _ => "CAM"
            };

            OnCamValueSelected?.Invoke(value, display);
        }

        private void OnSelectRisk(string? payload)
        {
            if (string.IsNullOrWhiteSpace(payload)) return;
            var parts = payload.Split('|');
            if (parts.Length >= 1 && double.TryParse(parts[0].Replace('%',' ').Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var r))
            {
                string label = parts.Length > 1 ? parts[1] : "Risque";
                OnRiskValueSelected?.Invoke(r, label);
            }
        }

        public event System.Action<double, string>? OnCamValueSelected;
        public event System.Action<double, string>? OnRiskValueSelected;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
