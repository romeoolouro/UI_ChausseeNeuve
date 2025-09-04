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
}
