using System.Linq;
using System.Windows;
using System.Windows.Documents;

namespace UI_ChausseeNeuve.Reports.Sections;

/// <summary>
/// Section de diagnostic listant les couches capturées dans le snapshot.
/// Permet de vérifier stabilité et contenu.
/// </summary>
public sealed class LayersDebugSection : IReportSection
{
    public string Id => "layers.debug";
    public string Title => "Debug Couches";
    public int Order { get; set; } = 999; // fin
    public bool IsEnabled { get; set; } = true;

    public FlowDocument Build(ReportContext context)
    {
        var fd = new FlowDocument();
        var p = new Paragraph(new Bold(new Run("[SECTION DEBUG - Snapshot des couches]")))
        { Foreground = System.Windows.Media.Brushes.DarkSlateBlue, Margin = new Thickness(0,20,0,8) };
        fd.Blocks.Add(p);

        fd.Blocks.Add(new Paragraph(new Run($"Nombre de couches snapshot (incl. plateforme): {context.LayerInfos.Count}")) { FontSize = 12 });

        if (context.LayerInfos.Count == 0)
        {
            fd.Blocks.Add(new Paragraph(new Run("Aucune couche capturée.")) { Foreground = System.Windows.Media.Brushes.OrangeRed });
            return fd;
        }

        var table = new Table { CellSpacing = 0 };
        foreach (var _ in Enumerable.Range(0,6)) table.Columns.Add(new TableColumn());
        var rg = new TableRowGroup(); table.RowGroups.Add(rg);
        TableRow Header(params string[] cols)
        {
            var hr = new TableRow();
            foreach (var c in cols)
                hr.Cells.Add(new TableCell(new Paragraph(new Bold(new Run(c)))));
            return hr;
        }
        rg.Rows.Add(Header("Ordre","Rôle","Matériau","Ep.","E","?"));

        foreach (var l in context.LayerInfos.OrderBy(li => li.Order))
        {
            var tr = new TableRow();
            tr.Cells.Add(new TableCell(new Paragraph(new Run(l.Order.ToString()))));
            tr.Cells.Add(new TableCell(new Paragraph(new Run(l.Role))));
            tr.Cells.Add(new TableCell(new Paragraph(new Run(l.MaterialName))));
            tr.Cells.Add(new TableCell(new Paragraph(new Run(l.Thickness_m.ToString("0.000")))));
            tr.Cells.Add(new TableCell(new Paragraph(new Run(l.Modulus_MPa.ToString("0")))));
            tr.Cells.Add(new TableCell(new Paragraph(new Run(l.Poisson.ToString("0.00")))));
            rg.Rows.Add(tr);
        }
        fd.Blocks.Add(table);
        return fd;
    }
}
