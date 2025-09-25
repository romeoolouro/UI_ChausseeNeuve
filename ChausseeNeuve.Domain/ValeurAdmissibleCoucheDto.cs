using System;
using System.Collections.ObjectModel;

namespace ChausseeNeuve.Domain.Models
{
    /// <summary>
    /// DTO sérialisable pour la persistance des valeurs admissibles par couche (pas de logique UI ni INotifyPropertyChanged)
    /// </summary>
    public class ValeurAdmissibleCoucheDto
    {
        public string Materiau { get; set; } = string.Empty;
        public int Niveau { get; set; }
        public string Critere { get; set; } = "EpsiT";
        public double Sn { get; set; }
        public double Sh { get; set; }
        public double B { get; set; }
        public double Kc { get; set; }
        public double Kr { get; set; }
        public double Ks { get; set; }
        public double Ktheta { get; set; }
        public double Kd { get; set; }
        public double Risque { get; set; }
        public double Ne { get; set; }
        public double Epsilon6 { get; set; }
        public double ValeurAdmissible { get; set; }
        public double AmplitudeValue { get; set; }
        public double Sigma6 { get; set; }
        public double Cam { get; set; }
        public double E10C10Hz { get; set; }
        public double Eteq10Hz { get; set; }
        public bool KthetaAuto { get; set; }
    }
}
