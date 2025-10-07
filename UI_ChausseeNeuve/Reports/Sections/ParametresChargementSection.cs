using System.Windows.Documents;
using System.Windows;
using System.Globalization;

namespace UI_ChausseeNeuve.Reports.Sections;

/// <summary>
/// Param�tres de chargement (charge de r�f�rence, pression, rayon, etc.).
/// </summary>
public sealed class ParametresChargementSection : IReportSection
{
    public string Id => "charge.params";
    public string Title => "Param�tres de chargement";
    public int Order { get; set; } = 3;
    public bool IsEnabled { get; set; } = true;

    public FlowDocument Build(ReportContext ctx)
    {
        var fd = new FlowDocument();
        fd.Blocks.Add(new Paragraph(new Bold(new Run("3. Param�tres de chargement")))
        {
            FontSize = 18,
            Margin = new Thickness(0,16,0,12)
        });

        var charge = ctx.Structure?.ChargeReference;
        if (charge == null)
        {
            fd.Blocks.Add(new Paragraph(new Run("Aucune charge de r�f�rence d�finie.")) { Foreground = System.Windows.Media.Brushes.OrangeRed });
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
            Row("Rayon �quivalent (m)", charge.RayonMetres.ToString("0.000", ci));
            Row("Poids (MN)", charge.PoidsMN.ToString("0.00000", ci));
            if (charge.IsDistanceRouesApplicable)
                Row("Entraxe roues (m)", charge.DistanceRouesMetres.ToString("0.000", ci));
            Row("Position X (m)", charge.PositionXDisplay);
            Row("Position Y (m)", charge.PositionYDisplay);

            fd.Blocks.Add(tbl);
        }

        // Tableau suppl�mentaire : param�tres trafic
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
        TRow("Dur�e de service (ans)", (ctx.Project?.DureeService ?? 0).ToString());
        TRow("Trafic cumul� NPL", (ctx.Project?.TraficCumuleNPL ?? 0).ToString("N0", ci2));

        fd.Blocks.Add(new Paragraph(new Bold(new Run("Param�tres trafic"))) { Margin = new Thickness(0,8,0,4) });
        fd.Blocks.Add(trafficTbl);

        // Formules explicatives � pr�sentation claire (ASCII '-' instead of Unicode minus)
        fd.Blocks.Add(new Paragraph(new Bold(new Run("Formules de calcul du trafic cumul� NPL")))
        {
            Margin = new Thickness(0,10,0,4)
        });

        fd.Blocks.Add(new Paragraph(new Run("D�finitions : MJA = trafic moyen journalier (PL/j). n = dur�e de service (ann�es). i = taux d'accroissement annuel (en d�cimal, i = taux% / 100)."))
        {
            Margin = new Thickness(0,0,0,4)
        });

        var list = new List { MarkerStyle = TextMarkerStyle.Disc, Margin = new Thickness(24,0,0,4) };
        list.ListItems.Add(new ListItem(new Paragraph(new Run("Accroissement arithm�tique :  NPL = 365 x MJA x n x [ 1 + (n - 1) x i / 2 ]"))));
        list.ListItems.Add(new ListItem(new Paragraph(new Run("Accroissement g�om�trique  :  NPL = 365 x MJA x ( (1 + i)^n - 1 ) / i  (si i > 0)"))));
        list.ListItems.Add(new ListItem(new Paragraph(new Run("Cas limite i = 0            :  NPL = 365 x MJA x n"))));
        fd.Blocks.Add(list);

        return fd;
    }
}
