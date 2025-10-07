using System.Windows.Documents;
using System.Windows;

namespace UI_ChausseeNeuve.Reports.Sections;

/// <summary>
/// Section d�crivant la m�thodologie de dimensionnement (structure, coefficients, normes).
/// </summary>
public sealed class MethodologieSection : IReportSection
{
    public string Id => "methodologie";
    public string Title => "M�thodologie";
    public int Order { get; set; } = 2;
    public bool IsEnabled { get; set; } = true;

    public FlowDocument Build(ReportContext ctx)
    {
        var fd = new FlowDocument();
        fd.Blocks.Add(new Paragraph(new Bold(new Run("2. M�thodologie de dimensionnement")))
        {
            FontSize = 18,
            Margin = new Thickness(0,16,0,12)
        });

        fd.Blocks.Add(new Paragraph(new Run(
            "La pr�sente note est �labor�e sur la base de la norme NF P98-086. " +
            "Les couches sont mod�lis�es comme un empilement de milieux horizontaux homog�nes, isotropes et �lastiques. " +
            "Les sollicitations consid�r�es proviennent d'une charge circulaire �quivalente appliqu�e en surface. " +
            "Les crit�res de dimensionnement prennent en compte : (i) la fatigue en traction (EpsiT / SigmaT), (ii) la d�formation verticale cumul�e (EpsiZ), " +
            "(iii) les effets d'h�t�rog�n�it� via des coefficients correctifs (ks, kd, kc, kr, k?)."))
        { TextAlignment = TextAlignment.Justify, Margin = new Thickness(0,0,0,8) });

        fd.Blocks.Add(new Paragraph(new Run(
            "Les coefficients ks et kd report�s plus loin sont calcul�s automatiquement � partir des familles de mat�riaux et des �paisseurs. " +
            "Les valeurs de module �lastique utilis�es correspondent soit � des valeurs normatives (catalogues) soit � des entr�es utilisateur en mode Expert."))
        { TextAlignment = TextAlignment.Justify });

        return fd;
    }
}
