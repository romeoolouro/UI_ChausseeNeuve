using ChausseeNeuve.Domain.Models;
using System.Collections.Generic;

namespace UI_ChausseeNeuve.ViewModels
{
    public class MaterialItem
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
        public double? Sh { get; set; } // Sh (m)
        public double? Kc { get; set; } // Kc
        public double? Kd { get; set; } // Kd

        // Ajout pour affichage Alizé
        public double? SN { get; set; } // SN
        public double? Epsi0_10C { get; set; } // Epsi0 (10°C)
        public Dictionary<int, double>? EvsTemperature { get; set; } // E(Température)

        // Propriétés additionnelles génériques
        public Dictionary<string, object>? AdditionalProperties { get; set; }

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

        public override string ToString()
        {
            return Name ?? "Unnamed Material";
        }
    }
}
