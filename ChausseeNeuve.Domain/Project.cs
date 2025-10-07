using System.Collections.ObjectModel;

namespace ChausseeNeuve.Domain.Models;

public class Project
{
    public string Name { get; set; } = "";
    public string Author { get; set; } = "";
    public string Location { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DimensionnementMode Mode { get; set; } = DimensionnementMode.Expert;

    // Structure de chaussée NF P98-086
    public PavementStructure PavementStructure { get; set; } = new PavementStructure();

    // Valeurs admissibles calculées (persistantes, DTO)
    public ObservableCollection<ValeurAdmissibleCoucheDto> ValeursAdmissibles { get; set; } = new ObservableCollection<ValeurAdmissibleCoucheDto>();

    // Paramètres trafic (persistés pour la note de calcul)
    public double? TraficMJA { get; set; }
    public double? TauxAccroissement { get; set; }
    public string? TypeTauxAccroissement { get; set; }
    public int? DureeService { get; set; }
    public double? TraficCumuleNPL { get; set; }
}
