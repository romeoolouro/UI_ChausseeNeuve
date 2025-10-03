using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Input;

namespace UI_ChausseeNeuve.ViewModels
{
    public class NFP98086ViewModel : INotifyPropertyChanged
    {
        private string? _selectedCamKey;
        public string? SelectedCamKey
        {
            get => _selectedCamKey;
            set { _selectedCamKey = value; OnPropertyChanged(); }
        }

        public RelayCommand<string> SelectCamCommand { get; }
        public RelayCommand<string> SelectRiskCommand { get; }

        public event Action<double,string>? OnCamValueSelected;
        public event Action<double,string>? OnRiskValueSelected;

        public NFP98086ViewModel()
        {
            SelectCamCommand = new RelayCommand<string>(ExecuteSelectCam, p => !string.IsNullOrWhiteSpace(p));
            SelectRiskCommand = new RelayCommand<string>(ExecuteSelectRisk, p => !string.IsNullOrWhiteSpace(p));
        }

        private void ExecuteSelectCam(string? param)
        {
            if (string.IsNullOrWhiteSpace(param)) return;
            var parts = param.Split('|');
            if (parts.Length != 3) return;
            var area = parts[0];
            if (!int.TryParse(parts[1], out int row)) return;
            if (!int.TryParse(parts[2], out int col)) return;
            double value = LookupCam(area, row, col, out string materialType);
            if (value <= 0) return;
            SelectedCamKey = param;
            OnCamValueSelected?.Invoke(value, materialType);
        }

        private void ExecuteSelectRisk(string? payload)
        {
            if (string.IsNullOrWhiteSpace(payload)) return;
            var parts = payload.Split('|');
            if (parts.Length >= 1 && double.TryParse(parts[0].Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var r))
            {
                string label = parts.Length > 1 ? parts[1] : "Risque";
                OnRiskValueSelected?.Invoke(r, label);
            }
        }

        private double LookupCam(string area, int row, int col, out string materialType)
        {
            materialType = "";
            if (area == "PNON") // Non autoroutier: rows 0..3, cols 0..4
            {
                if (row is < 0 or > 3 || col is < 0 or > 4) return 0;
                materialType = row switch
                {
                    0 => "bitumineux",
                    1 => "traites_hydrauliques",
                    2 => "sols_traites",
                    3 => "granulaires",
                    _ => "bitumineux"
                };
                return CamNonAutoroutier[row, col];
            }
            if (area == "PAUTO")
            {
                if (row is < 0 or > 2) return 0;
                materialType = row switch { 0 => "bitumineux", 1 => "traites_hydrauliques", 2 => "granulaires", _ => "bitumineux" };
                return row switch { 0 => 0.8, 1 => 1.3, 2 => 1.0, _ => 0 };
            }
            if (area == "URB")
            {
                if (row is < 0 or > 2 || col is < 0 or > 2) return 0;
                materialType = row switch { 0 => "bitumineux", 1 => "traites_hydrauliques", 2 => "giratoire", _ => "bitumineux" };
                return CamUrbain[row, col];
            }
            return 0;
        }

        // CAM non autoroutier: 4 lignes (MB, MTLH, Sols traites, Plate-forme GNT) x 5 colonnes (T5,T4,T3-,T3+,T2T1T0)
        private static readonly double[,] CamNonAutoroutier = new double[4,5]
        {
            {0.3,0.3,0.4,0.5,0.5},   // MB
            {0.4,0.5,0.6,0.6,0.8},   // MTLH + betons
            {0.4,0.5,0.7,0.7,0.8},   // Sols traites
            {0.4,0.5,0.6,0.75,1.0}   // Plate-forme / GNT
        };

        // CAM urbain (inchangé)
        private static readonly double[,] CamUrbain = new double[3,3]
        {
            {0.1,0.2,0.2},
            {0.1,0.2,0.4},
            {0.2,0.5,1.0}
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name=null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
