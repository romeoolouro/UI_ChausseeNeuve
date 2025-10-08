using System.Linq;
using System.Windows.Documents;
using System.Windows;

namespace UI_ChausseeNeuve.Reports.Sections;

/// <summary>
/// Présente les coefficients ks / kd calculés, utiles à l'analyse scientifique.
/// </summary>
public sealed class CoefficientsSection : IReportSection
{
    public string Id => "coefficients";
    public string Title => "Coefficients correctifs";
    public int Order { get; set; } = 4;
    public bool IsEnabled { get; set; } = true;

    public FlowDocument Build(ReportContext ctx)
    {
        var fd = new FlowDocument();
        fd.Blocks.Add(new Paragraph(new Bold(new Run("4. Coefficients correctifs (ks / kd)")))
        {
            FontSize = 18,
            Margin = new Thickness(0,16,0,12)
        });

        var layers = ctx.LayerInfos.Where(l => !string.Equals(l.Role, "Plateforme", System.StringComparison.OrdinalIgnoreCase)).OrderBy(l => l.Order).ToList();
        if (layers.Count == 0)
        {
            fd.Blocks.Add(new Paragraph(new Run("Aucune couche exploitable.")) { Foreground = System.Windows.Media.Brushes.OrangeRed });
            return fd;
        }

        var t = new Table { CellSpacing = 0 };
        foreach (var _ in Enumerable.Range(0,7)) t.Columns.Add(new TableColumn());
        var rg = new TableRowGroup(); t.RowGroups.Add(rg);

        TableRow Header(params string[] cols)
        {
            var hr = new TableRow();
            foreach (var c in cols)
                hr.Cells.Add(new TableCell(new Paragraph(new Bold(new Run(c)))));
            return hr;
        }
        rg.Rows.Add(Header("Ordre","Rôle","Matériau","Ep.(m)","E (MPa)","ks","kd"));

        foreach (var l in layers)
        {
            var r = new TableRow();
            r.Cells.Add(new TableCell(new Paragraph(new Run(l.Order.ToString()))));
            r.Cells.Add(new TableCell(new Paragraph(new Run(l.Role))));
            r.Cells.Add(new TableCell(new Paragraph(new Run(l.MaterialName))));
            r.Cells.Add(new TableCell(new Paragraph(new Run(l.Thickness_m.ToString("0.000")))));
            r.Cells.Add(new TableCell(new Paragraph(new Run(l.Modulus_MPa.ToString("0")))));
            r.Cells.Add(new TableCell(new Paragraph(new Run(l.Ks.ToString("0.00")))));
            r.Cells.Add(new TableCell(new Paragraph(new Run(l.Kd.ToString("0.00")))));
            rg.Rows.Add(r);
        }
        fd.Blocks.Add(t);

        return fd;
    }
}
