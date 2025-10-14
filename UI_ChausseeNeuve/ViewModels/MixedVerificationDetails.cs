namespace UI_ChausseeNeuve.ViewModels
{
    // DTO simple (classe) au lieu d'un record pour compatibilité
    public sealed class MixedVerificationDetails
    {
        public string FondationName { get; set; } = "";
        public int FondationOrder { get; set; }
        public double SigmaTBase { get; set; }
        public double CAMFondation { get; set; }
        public double BFondation { get; set; }
        public double Sigma6Fondation { get; set; }
        public double KcFondation { get; set; }
        public double KrFondation { get; set; }
        public double KsFondation { get; set; }
        public double KdFondation { get; set; }
        public double NE1 { get; set; }
        public double NPL1 { get; set; }
        public string BaseName { get; set; } = "";
        public int BaseOrder { get; set; }
        public double EpsiTBaseResidual { get; set; }
        public string ResidualInfo { get; set; } = "Interface glissante; E(MTLH)=E/5";
        public double CAMBase { get; set; }
        public double BBase { get; set; }
        public double Epsilon6Base { get; set; }
        public double KcBase { get; set; }
        public double KrBase { get; set; }
        public double KsBase { get; set; }
        public double KthetaBase { get; set; }
        public double NE2 { get; set; }
        public double NPL2 { get; set; }
        public double TargetNPL { get; set; }
        public double TotalNPL { get; set; }
        public bool IsOk { get; set; }

        // Détails pour l'étape 2 (texte + vue tableau)
        public string RemodelingDetails { get; set; } = string.Empty;
        public string CalculationSummary { get; set; } = string.Empty;
        public string StructureText { get; set; } = string.Empty;

        // Tableau style "Modéliser la structure" (résiduel)
        public System.Collections.Generic.List<MixedModeledRowDto> ModeledRows { get; set; } = new();

        // NOUVEAU: résultats recalculés (structure résiduelle) affichés comme un tableau
        public System.Collections.Generic.List<ResultatCouche> ResidualResults { get; set; } = new();
    }

    public sealed class MixedModeledRowDto
    {
        public bool IsInterface { get; set; }
        public bool IsPlateforme { get; set; } // Affichage spécifique (épaisseur ?)
        public int Numero { get; set; }           // For layer rows
        public string Nature { get; set; } = ""; // Roulement, Base, Fondation, Plateforme or "Interface"
        public string Materiau { get; set; } = "";
        public string Famille { get; set; } = "";
        public double Epaisseur_m { get; set; }
        public double Module_MPa { get; set; }
        public double Poisson { get; set; }
        public string InterfaceWithBelow { get; set; } = ""; // For interface row

        // Mise en évidence des changements (structure résiduelle vs nominale)
        public bool ModuleChanged { get; set; }          // E -> E/5 pour MTLH
        public double OriginalModule_MPa { get; set; }   // Valeur nominale
        public bool InterfaceChanged { get; set; }       // Collée -> Décollée à la base MB
        public string OriginalInterfaceWithBelow { get; set; } = ""; // Valeur nominale
    }
}
