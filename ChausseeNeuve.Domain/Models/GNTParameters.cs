using System;

namespace ChausseeNeuve.Domain.Models
{
    public enum GNTCategory
    {
        CG1,  // Module plateforme entre 500 et 700 MPa
        CG2,  // Module plateforme entre 300 et 500 MPa
        CG3   // Module plateforme entre 100 et 300 MPa
    }

    public class GNTParameters
    {
        public struct Parameters
        {
            public double K { get; }
            public double Emax { get; }

            public Parameters(double k, double emax)
            {
                K = k;
                Emax = emax;
            }
        }

        public static GNTCategory DetermineCategory(double plateformeModule)
        {
            if (plateformeModule >= 500 && plateformeModule <= 700)
                return GNTCategory.CG1;
            else if (plateformeModule >= 300 && plateformeModule < 500)
                return GNTCategory.CG2;
            else if (plateformeModule >= 100 && plateformeModule < 300)
                return GNTCategory.CG3;
            else
                throw new ArgumentException($"Module de plateforme invalide : {plateformeModule} MPa. Doit être entre 100 et 700 MPa.");
        }

        public static Parameters GetParams(GNTCategory category, bool isBitumineuse)
        {
            if (isBitumineuse)
            {
                return category switch
                {
                    GNTCategory.CG1 => new Parameters(3.0, 360),
                    _ => throw new ArgumentException("Seule la catégorie CG1 est autorisée pour les chaussées bitumineuses épaisses")
                };
            }

            // Chaussées souples
            return category switch
            {
                GNTCategory.CG1 => new Parameters(3.0, 600),  // Pour plateforme 500-700 MPa
                GNTCategory.CG2 => new Parameters(2.5, 400),  // Pour plateforme 300-500 MPa
                GNTCategory.CG3 => new Parameters(2.0, 200),  // Pour plateforme 100-300 MPa
                _ => throw new ArgumentException("Catégorie GNT non reconnue")
            };
        }
    }
}