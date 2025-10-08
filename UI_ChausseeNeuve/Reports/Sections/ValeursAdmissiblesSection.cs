using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace UI_ChausseeNeuve.Reports.Sections;

public sealed class ValeursAdmissiblesSection : IReportSection
{
    public string Id => "valeurs.admissibles";
    public string Title => "Valeurs admissibles";
    public int Order { get; set; } = 6;
    public bool IsEnabled { get; set; } = true;

    public FlowDocument Build(ReportContext ctx)
    {
        var fd = new FlowDocument();
        fd.Blocks.Add(new Paragraph(new Bold(new Run("6. Valeurs admissibles")))
        { FontSize = 18, Margin = new Thickness(0,16,0,12) });

        var p = ctx.Project;
        if (p.TraficMJA.HasValue || p.TraficCumuleNPL.HasValue)
        {
            string resume = $"Paramètres trafic: MJA={(p.TraficMJA?.ToString("0") ?? "-")} PL/j | Croissance={(p.TauxAccroissement?.ToString("0.##") ?? "-")}% {(p.TypeTauxAccroissement ?? "")} | Durée={(p.DureeService?.ToString() ?? "-")} ans | NPL cumulé={(p.TraficCumuleNPL?.ToString("N0") ?? "-")}";
            fd.Blocks.Add(new Paragraph(new Run(resume)) { FontStyle=FontStyles.Italic, FontSize=12, Foreground=Brushes.DimGray, Margin=new Thickness(0,0,0,4)});
        }

        var vals = ctx.AdmissibleValues.OrderBy(v => v.Niveau).ToList();
        if (vals.Count == 0)
        {
            fd.Blocks.Add(new Paragraph(new Run("Aucune valeur admissible disponible. Générer/calculer les valeurs dans l'onglet 'Valeurs admissibles'.")) { Foreground = Brushes.OrangeRed });
            return fd;
        }

        // Tableau principal
        var t = new Table { CellSpacing = 0, Margin = new Thickness(0,4,0,12) };
        foreach (var _ in Enumerable.Range(0,13)) t.Columns.Add(new TableColumn());
        var rg = new TableRowGroup(); t.RowGroups.Add(rg);

        TableRow Header(params string[] cols)
        {
            var r = new TableRow();
            foreach (var c in cols)
                r.Cells.Add(new TableCell(new Paragraph(new Bold(new Run(c)))) { Padding = new Thickness(2,0,4,2) });
            return r;
        }
        rg.Rows.Add(Header("Ordre","Matériau","Critère","CAM","NE","Base ref","-1/b","kc","kr","ks","kd","ktheta","Val. adm."));

        foreach (var v in vals)
        {
            string baseRefLabel = v.Critere switch
            {
                var s when s.Equals("EpsiZ", System.StringComparison.OrdinalIgnoreCase) => v.AmplitudeValue.ToString("0.##"),
                var s when s.Equals("EpsiT", System.StringComparison.OrdinalIgnoreCase) => v.Epsilon6 > 0 ? v.Epsilon6.ToString("0.##") : v.AmplitudeValue.ToString("0.##"),
                var s when s.Equals("SigmaT", System.StringComparison.OrdinalIgnoreCase) => v.Sigma6.ToString("0.##"),
                _ => v.AmplitudeValue.ToString("0.##")
            };
            var r = new TableRow();
            void Cell(string txt) => r.Cells.Add(new TableCell(new Paragraph(new Run(txt))) { Padding = new Thickness(2,0,4,2) });
            Cell(v.Niveau.ToString());
            Cell(v.Materiau);
            Cell(v.Critere);
            Cell(v.Cam.ToString("0.00"));
            Cell(v.Ne.ToString("N0"));
            Cell(baseRefLabel);
            Cell(v.B.ToString("0.###"));
            Cell(v.Kc.ToString("0.00"));
            Cell(v.Kr.ToString("0.00"));
            Cell(v.Ks.ToString("0.00"));
            Cell(v.Kd.ToString("0.00"));
            Cell(v.Ktheta.ToString("0.00"));
            Cell(v.ValeurAdmissible.ToString("0.###"));
            rg.Rows.Add(r);
        }

        fd.Blocks.Add(t);
        fd.Blocks.Add(new Paragraph(new Run("Base ref = A (EpsiZ) / ?6 (EpsiT) / ?6 (SigmaT). Valeur adm. = contrainte ou déformation admissible calculée après coefficients."))
        { FontSize = 10, Foreground = Brushes.Gray, Margin = new Thickness(0,0,0,4) });
        return fd;
    }
}
