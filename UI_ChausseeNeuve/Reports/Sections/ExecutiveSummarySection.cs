using System.Windows.Documents;
using System.Windows;
using System.Linq;

namespace UI_ChausseeNeuve.Reports.Sections;

public sealed class ExecutiveSummarySection : IReportSection
{
    public string Id => "summary";
    public string Title => "Résumé exécutif";
    public int Order { get; set; } = 1;
    public bool IsEnabled { get; set; } = true;

    public FlowDocument Build(ReportContext ctx)
    {
        var fd = new FlowDocument();
        // Titre renuméroté (1.)
        fd.Blocks.Add(new Paragraph(new Bold(new Run("1. Résumé exécutif")))
        {
            FontSize = 18,
            Margin = new Thickness(0,0,0,12)
        });

        var tbl = new Table { CellSpacing = 0, Margin = new Thickness(0,0,0,18) };
        tbl.Columns.Add(new TableColumn { Width = new GridLength(210) });
        tbl.Columns.Add(new TableColumn { Width = GridLength.Auto });
        var body = new TableRowGroup();
        tbl.RowGroups.Add(body);

        void Row(string h, string v)
        {
            var r = new TableRow();
            r.Cells.Add(new TableCell(new Paragraph(new Run(h)) { FontWeight = FontWeights.SemiBold }));
            r.Cells.Add(new TableCell(new Paragraph(new Run(v ?? string.Empty))));
            body.Rows.Add(r);
        }

        Row("Projet", ctx.Project?.Name ?? "");
        Row("Auteur", ctx.Project?.Author ?? "");
        Row("Localisation", ctx.Project?.Location ?? "");
        Row("Type de structure", ctx.Structure?.StructureType ?? "");
        Row("NE", (ctx.Structure?.NE ?? 0).ToString("N0", ctx.Culture));
        Row("Nombre de couches (hors PF)", ctx.LayerInfos.Count(li => !string.Equals(li.Role, "Plateforme", System.StringComparison.OrdinalIgnoreCase)).ToString());
        Row("Date génération", ctx.GeneratedAtUtc.ToLocalTime().ToString("g", ctx.Culture));
        Row("Niveau détail", ctx.DetailLevel);

        fd.Blocks.Add(tbl);

        var layers = ctx.LayerInfos.Where(l => !string.Equals(l.Role, "Plateforme", System.StringComparison.OrdinalIgnoreCase)).OrderBy(l => l.Order).ToList();
        if (layers.Count > 0)
        {
            fd.Blocks.Add(new Paragraph(new Bold(new Run("Structure proposée"))) { Margin = new Thickness(0,0,0,6) });
            var t2 = new Table { CellSpacing = 0, Margin = new Thickness(0,0,0,8) };
            foreach (var _ in Enumerable.Range(0,6)) t2.Columns.Add(new TableColumn());
            var rg = new TableRowGroup(); t2.RowGroups.Add(rg);
            TableRow Header(params string[] cols)
            {
                var hr = new TableRow();
                foreach (var c in cols)
                    hr.Cells.Add(new TableCell(new Paragraph(new Bold(new Run(c)))));
                return hr;
            }
            rg.Rows.Add(Header("Ordre","Rôle","Matériau","Ep. (m)","E (MPa)","Coefficient de Poisson"));
            foreach (var l in layers)
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
            fd.Blocks.Add(t2);
        }

        return fd;
    }
}
