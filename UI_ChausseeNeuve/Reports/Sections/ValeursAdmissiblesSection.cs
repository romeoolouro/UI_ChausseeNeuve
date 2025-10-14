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
        var fd = new FlowDocument { FontFamily = new FontFamily("Segoe UI") };

        // Constantes symboles
        const string eps = "\u03B5";      // ε
        const string sig = "\u03C3";      // σ
        const string theta = "\u03B8";    // θ
        const string phi = "\u03A6";      // Φ
        const string delta = "\u03B4";    // δ
        const string micro = "\u00B5";    // µ
        const string sqrt = "\u221A";     // √

        fd.Blocks.Add(new Paragraph(new Bold(new Run("6. Valeurs admissibles")))
        { FontSize = 19, Margin = new Thickness(0,16,0,12) });

        var p = ctx.Project;
        if (p.TraficMJA.HasValue || p.TraficCumuleNPL.HasValue)
        {
            string resume = $"Paramètres trafic: MJA={(p.TraficMJA?.ToString("0") ?? "-")} PL/j | Croissance={(p.TauxAccroissement?.ToString("0.##") ?? "-")}% {(p.TypeTauxAccroissement ?? "")} | Durée={(p.DureeService?.ToString() ?? "-")} ans | NPL cumulé={(p.TraficCumuleNPL?.ToString("N0") ?? "-")}";
            fd.Blocks.Add(new Paragraph(new Run(resume)) { FontStyle=FontStyles.Italic, FontSize=13, Foreground=Brushes.Black, Margin=new Thickness(0,0,0,4)});
        }

        var vals = ctx.AdmissibleValues.OrderBy(v => v.Niveau).ToList();
        if (vals.Count == 0)
        {
            fd.Blocks.Add(new Paragraph(new Run("Aucune valeur admissible disponible. Générer/calculer les valeurs dans l'onglet 'Valeurs admissibles'.")) { Foreground = Brushes.OrangeRed, FontSize=13 });
            return fd;
        }

        // 6.1 Formules (table déplacé plus bas en 6.3)
        fd.Blocks.Add(new Paragraph(new Bold(new Run("6.1 Formules de dimensionnement")))
        { FontSize = 15, Margin = new Thickness(0,8,0,8) });

        fd.Blocks.Add(new Paragraph(new Run(
            $"La colonne \"Base ref\" contient : A (critère EpsiZ), {eps}6 (critère EpsiT) ou {sig}6 (critère SigmaT). La colonne \"-1/b\" affiche B = -1/b (valeur positive). Le paramètre fatigue réel est b = -1/B. NE = NPL x CAM."))
        { TextAlignment = TextAlignment.Justify, FontSize = 13, Margin = new Thickness(0,0,0,10), Foreground = Brushes.Black });

        // Equations (renumérotation après suppression ks)
        AddEq(fd, "6-1", $"{eps}_z,adm = A * NE^b   (b = -1 / B)");
        AddEq(fd, "6-2", $"{eps}_t,adm = {eps}6 * (NE / 10^6)^b * k_c * k_r * k_s * k_{theta}");
        AddEq(fd, "6-3", $"{sig}_t,adm = {sig}6 * (NE / 10^6)^b * k_c * k_r * k_s * k_d");
        AddEq(fd, "6-4", $"k_r = 10^(- b * u0 * {delta})  avec  {delta} = {sqrt}( S_n^2 + (c*S_h / b)^2 ),  c = 2 m^-1");
        AddEq(fd, "6-5", $"u0 = {phi}^-1(p)  (quantile normal, p = Risque/100)");
        AddEq(fd, "6-6", $"k_{theta} = {sqrt}( E(10°C,25Hz) / E(teq,25Hz) ) (bitumineux)");
        AddEq(fd, "6-7", $"NE inverse: traction {eps}_t => 10^6*( {eps}_t / ({eps}6*k_c*k_r*k_s*k_{theta}) )^(1/b); traction {sig}_t => 10^6*( {sig}_t / ({sig}6*k_c*k_r*k_s*k_d) )^(1/b); compression {eps}_z => (|{eps}_z| / A)^(1/b)");

        // 6.2 Table des symboles
        fd.Blocks.Add(new Paragraph(new Bold(new Run("6.2 Table des symboles")))
        { FontSize = 15, Margin = new Thickness(0,12,0,6) });
        fd.Blocks.Add(BuildSymbolTable(eps, sig, theta, delta, micro, phi));

        // 6.3 Tableau des valeurs admissibles (déplacé ici)
        fd.Blocks.Add(new Paragraph(new Bold(new Run("6.3 Tableau des valeurs admissibles (Tableau 6-3)")))
        { FontSize = 15, Margin = new Thickness(0,14,0,6) });

        fd.Blocks.Add(BuildAdmissibleTable(vals));

        fd.Blocks.Add(new Paragraph(new Run(
            $"Unités : {eps}, {eps}6 en microdéformation ({micro}def) ; {sig}, {sig}6 en MPa ; A en {micro}def ; NE & NPL sans unité. Les valeurs admissibles intègrent k_c,k_r,k_s,k_{theta},k_d."))
        { FontSize = 11.5, Foreground = Brushes.Black, Margin = new Thickness(0,6,0,0) });

        return fd;
    }

    private static void AddEq(FlowDocument fd, string num, string txt)
    {
        fd.Blocks.Add(new Paragraph(new Run($"(Eq. {num})  {txt}"))
        {
            Margin = new Thickness(10,0,0,4),
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 13.5,
            Foreground = Brushes.Black
        });
    }

    private static Table BuildAdmissibleTable(System.Collections.Generic.List<AdmissibleValueInfo> vals)
    {
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0,0,0,8) };
        foreach (var _ in Enumerable.Range(0,13)) table.Columns.Add(new TableColumn());
        var rg = new TableRowGroup(); table.RowGroups.Add(rg);

        TableRow Header(params string[] cols)
        {
            var r = new TableRow();
            foreach (var c in cols)
                r.Cells.Add(new TableCell(new Paragraph(new Bold(new Run(c))) { FontSize = 12.5, Foreground = Brushes.Black }) { Padding = new Thickness(3,1,6,3) });
            return r;
        }
        rg.Rows.Add(Header("Ordre","Matériau","Critère","CAM","NE","Base ref","-1/b","kc","kr","ks","kd","ktheta","Val. adm."));

        foreach (var v in vals)
        {
            string baseRefLabel = v.Critere switch
            {
                var s when s.Equals("EpsiZ", System.StringComparison.OrdinalIgnoreCase) => v.AmplitudeValue.ToString("0.###"),
                var s when s.Equals("EpsiT", System.StringComparison.OrdinalIgnoreCase) => v.Epsilon6 > 0 ? v.Epsilon6.ToString("0.###") : v.AmplitudeValue.ToString("0.###"),
                var s when s.Equals("SigmaT", System.StringComparison.OrdinalIgnoreCase) => v.Sigma6.ToString("0.###"),
                _ => v.AmplitudeValue.ToString("0.###")
            };
            var r = new TableRow();
            void Cell(string txt) => r.Cells.Add(new TableCell(new Paragraph(new Run(txt)) { FontSize = 12, Foreground = Brushes.Black }) { Padding = new Thickness(3,1,6,3) });
            Cell(v.Niveau.ToString());
            Cell(v.Materiau);
            Cell(v.Critere);
            Cell(v.Cam.ToString("0.000"));
            Cell(v.Ne.ToString("N0"));
            Cell(baseRefLabel);
            Cell(v.B.ToString("0.000"));
            Cell(v.Kc.ToString("0.000"));
            Cell(v.Kr.ToString("0.000"));
            Cell(v.Ks.ToString("0.000"));
            Cell(v.Kd.ToString("0.000"));
            Cell(v.Ktheta.ToString("0.000"));
            Cell(v.ValeurAdmissible.ToString("0.000"));
            rg.Rows.Add(r);
        }
        return table;
    }

    private static Table BuildSymbolTable(string eps, string sig, string theta, string delta, string micro, string phi)
    {
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0,0,0,8) };
        foreach (var _ in Enumerable.Range(0,3)) table.Columns.Add(new TableColumn());
        var rg = new TableRowGroup(); table.RowGroups.Add(rg);

        TableRow Row(string a, string b, string c, bool header=false)
        {
            var r = new TableRow();
            void Add(string t)
            {
                var p = new Paragraph(new Run(t)) { Margin = new Thickness(0), FontSize = header ? 12.5 : 12, Foreground = Brushes.Black };
                if (header) p.FontWeight = FontWeights.Bold;
                r.Cells.Add(new TableCell(p) { Padding = new Thickness(6,3,8,3), BorderBrush = Brushes.Gray, BorderThickness = new Thickness(0,0,0,0.5) });
            }
            Add(a); Add(b); Add(c);
            return r;
        }

        rg.Rows.Add(Row("Symbole","Description","Unité / Remarque", header:true));
        rg.Rows.Add(Row("NE","Nombre d’essieux équivalents (cumulé)","-"));
        rg.Rows.Add(Row("NPL","Trafic cumulé poids lourds","-"));
        rg.Rows.Add(Row("CAM","Coeff. d’agressivité moyen","-"));
        rg.Rows.Add(Row("A","Amplitude déformation verticale ref (plate-forme)","" + micro + "def"));
        rg.Rows.Add(Row($"{eps}6 / {sig}6","Déformation / contrainte de référence à 10^6 cycles",$"{micro}def / MPa"));
        rg.Rows.Add(Row("B = -1/b","Valeur affichée (colonne -1/b)","> 0"));
        rg.Rows.Add(Row("b","Exposant fatigue réel (= -1/B)","< 0 (traction)"));
        rg.Rows.Add(Row("Sn, Sh","Dispersion matériau / épaisseur",$"({micro}def)/(m)"));
        rg.Rows.Add(Row("kr","Coeff. risque / dispersion","-"));
        rg.Rows.Add(Row("kc, ks, kd, kθ","Coefficients correctifs","-"));
        rg.Rows.Add(Row("u0",$"Quantile normal {phi}^-1(p)","p = Risque/100"));
        rg.Rows.Add(Row(delta,"Terme global de dispersion",$"{micro}def équivalent"));
        rg.Rows.Add(Row("c","Sensibilité épaisseur (fixé)","2 m^-1"));
        return table;
    }
}
