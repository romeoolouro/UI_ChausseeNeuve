using System.Windows.Documents;
using System.Windows;

namespace UI_ChausseeNeuve.Reports.Sections;

/// <summary>
/// Section décrivant la méthodologie de dimensionnement (structure, coefficients, normes).
/// </summary>
public sealed class MethodologieSection : IReportSection
{
    public string Id => "methodologie";
    public string Title => "Méthodologie";
    public int Order { get; set; } = 2;
    public bool IsEnabled { get; set; } = true;

    public FlowDocument Build(ReportContext ctx)
    {
        var fd = new FlowDocument();
        fd.Blocks.Add(new Paragraph(new Bold(new Run("2. Méthodologie de dimensionnement")))
        {
            FontSize = 18,
            Margin = new Thickness(0,16,0,12)
        });

        fd.Blocks.Add(new Paragraph(new Run(
            "La présente note est élaborée sur la base de la norme NF P98-086. " +
            "Les couches sont modélisées comme un empilement de milieux horizontaux homogènes, isotropes et élastiques. " +
            "Les sollicitations considérées proviennent d'une charge circulaire équivalente appliquée en surface. " +
            "Les critères de dimensionnement prennent en compte : (i) la fatigue en traction (EpsiT / SigmaT), (ii) la déformation verticale cumulée (EpsiZ), " +
            "(iii) les effets d'hétérogénéité via des coefficients correctifs (ks, kd, kc, kr, k?)."))
        { TextAlignment = TextAlignment.Justify, Margin = new Thickness(0,0,0,8) });

        fd.Blocks.Add(new Paragraph(new Run(
            "Les coefficients ks et kd reportés plus loin sont calculés automatiquement à partir des familles de matériaux et des épaisseurs. " +
            "Les valeurs de module élastique utilisées correspondent soit à des valeurs normatives (catalogues) soit à des entrées utilisateur en mode Expert."))
        { TextAlignment = TextAlignment.Justify });

        return fd;
    }
}
