namespace ChausseeNeuve.Domain.Models;

public class ResultatCoucheSnapshotDto
{
    public int Niveau { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Materiau { get; set; } = string.Empty;
    public string Famille { get; set; } = string.Empty;
    public string Critere { get; set; } = string.Empty; // EpsiT / SigmaT / EpsiZ
    public double ValeurCritique { get; set; }
    public double ValeurAdmissible { get; set; }
    public bool EstValide { get; set; }
    public double TauxUtilisation { get; set; }
    public double CAM { get; set; }
    public double NE { get; set; }
    public double Epaisseur_m { get; set; }
    // Indique si pour structure souple la vérification epsilonZ est applicable à cette couche
    public bool SoupleEpsiZApplicable { get; set; } = true;
}
