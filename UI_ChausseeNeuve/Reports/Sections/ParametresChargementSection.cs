using System.Windows.Documents;
using System.Windows;
using System.Globalization;

namespace UI_ChausseeNeuve.Reports.Sections;

/// <summary>
/// Paramètres de chargement (charge de référence, pression, rayon, etc.).
/// </summary>
public sealed class ParametresChargementSection : IReportSection
{
    public string Id => "charge.params";
    public string Title => "Paramètres de chargement";
    public int Order { get; set; } = 3;
    public bool IsEnabled { get; set; } = true;

    public FlowDocument Build(ReportContext ctx)
    {
        var fd = new FlowDocument();
        fd.Blocks.Add(new Paragraph(new Bold(new Run("3. Paramètres de chargement")))
        {
            FontSize = 18,
            Margin = new Thickness(0,16,0,12)
        });

        var charge = ctx.Structure?.ChargeReference;
        if (charge == null)
        {
            fd.Blocks.Add(new Paragraph(new Run("Aucune charge de référence définie.")) { Foreground = System.Windows.Media.Brushes.OrangeRed });
        }
        else
        {
            var tbl = new Table { CellSpacing = 0 };
            tbl.Columns.Add(new TableColumn { Width = new GridLength(280) });
            tbl.Columns.Add(new TableColumn { Width = GridLength.Auto });
            var rg = new TableRowGroup(); tbl.RowGroups.Add(rg);

            void Row(string label, string value)
            {
                var tr = new TableRow();
                tr.Cells.Add(new TableCell(new Paragraph(new Run(label)) { FontWeight = FontWeights.SemiBold }));
                tr.Cells.Add(new TableCell(new Paragraph(new Run(value))));
                rg.Rows.Add(tr);
            }

            var ci = CultureInfo.GetCultureInfo("fr-FR");
            Row("Type", charge.Type.ToString());
            Row("Pression (MPa)", charge.PressionMPa.ToString("0.000", ci));
            Row("Rayon équivalent (m)", charge.RayonMetres.ToString("0.000", ci));
            Row("Poids (MN)", charge.PoidsMN.ToString("0.00000", ci));
            if (charge.IsDistanceRouesApplicable)
                Row("Entraxe roues (m)", charge.DistanceRouesMetres.ToString("0.000", ci));
            Row("Position X (m)", charge.PositionXDisplay);
            Row("Position Y (m)", charge.PositionYDisplay);

            fd.Blocks.Add(tbl);
        }

        // Tableau supplémentaire : paramètres trafic
        var trafficTbl = new Table { CellSpacing = 0, Margin = new Thickness(0,12,0,0) };
        trafficTbl.Columns.Add(new TableColumn { Width = new GridLength(280) });
        trafficTbl.Columns.Add(new TableColumn { Width = GridLength.Auto });
        var trg = new TableRowGroup(); trafficTbl.RowGroups.Add(trg);

        void TRow(string l, string v)
        {
            var tr = new TableRow();
            tr.Cells.Add(new TableCell(new Paragraph(new Run(l)) { FontWeight = FontWeights.SemiBold }));
            tr.Cells.Add(new TableCell(new Paragraph(new Run(v))));
            trg.Rows.Add(tr);
        }
        var ci2 = CultureInfo.GetCultureInfo("fr-FR");
        TRow("Trafic MJA (PL/j)", (ctx.Project?.TraficMJA ?? 0).ToString("N0", ci2));
        TRow("Taux d'accroissement (%)", (ctx.Project?.TauxAccroissement ?? 0).ToString("0.##", ci2));
        TRow("Type accroissement", ctx.Project?.TypeTauxAccroissement ?? "");
        TRow("Durée de service (ans)", (ctx.Project?.DureeService ?? 0).ToString());
        TRow("Trafic cumulé NPL", (ctx.Project?.TraficCumuleNPL ?? 0).ToString("N0", ci2));

        fd.Blocks.Add(new Paragraph(new Bold(new Run("Paramètres trafic"))) { Margin = new Thickness(0,8,0,4) });
        fd.Blocks.Add(trafficTbl);

        // Formules explicatives – présentation claire (ASCII '-' instead of Unicode minus)
        fd.Blocks.Add(new Paragraph(new Bold(new Run("Formules de calcul du trafic cumulé NPL")))
        {
            Margin = new Thickness(0,10,0,4)
        });

        fd.Blocks.Add(new Paragraph(new Run("Définitions : MJA = trafic moyen journalier (PL/j). n = durée de service (années). i = taux d'accroissement annuel (en décimal, i = taux% / 100)."))
        {
            Margin = new Thickness(0,0,0,4)
        });

        var list = new List { MarkerStyle = TextMarkerStyle.Disc, Margin = new Thickness(24,0,0,4) };
        list.ListItems.Add(new ListItem(new Paragraph(new Run("Accroissement arithmétique :  NPL = 365 x MJA x n x [ 1 + (n - 1) x i / 2 ]"))));
        list.ListItems.Add(new ListItem(new Paragraph(new Run("Accroissement géométrique  :  NPL = 365 x MJA x ( (1 + i)^n - 1 ) / i  (si i > 0)"))));
        list.ListItems.Add(new ListItem(new Paragraph(new Run("Cas limite i = 0            :  NPL = 365 x MJA x n"))));
        fd.Blocks.Add(list);

        return fd;
    }
}
