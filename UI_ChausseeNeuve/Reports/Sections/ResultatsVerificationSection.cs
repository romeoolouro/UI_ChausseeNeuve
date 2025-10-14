using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using ChausseeNeuve.Domain.Models;

namespace UI_ChausseeNeuve.Reports.Sections;

public sealed class ResultatsVerificationSection : IReportSection
{
    public string Id => "resultats.verification";
    public string Title => "Résultats de vérification";
    public int Order { get; set; } = 7;
    public bool IsEnabled { get; set; } = true;

    public FlowDocument Build(ReportContext ctx)
    {
        // Symboles (unicode + fallback ASCII dans les textes)
        const string eps = "\u03B5"; // ε
        const string sig = "\u03C3"; // σ

        var fd = new FlowDocument();
        fd.Blocks.Add(new Paragraph(new Bold(new Run("7. Résultats de vérification")))
        { FontSize = 18, Margin = new Thickness(0,16,0,12) });

        var p = ctx.Project;
        var snap = p.DerniersResultats;
        if (snap == null || snap.Count == 0)
        {
            fd.Blocks.Add(new Paragraph(new Run("Aucun calcul de sollicitations n'a encore été persisté. Effectuer un calcul dans l'onglet Résultats puis régénérer la note."))
            { Foreground = Brushes.OrangeRed });
            return fd;
        }

        string type = p.PavementStructure?.StructureType ?? "-";
        fd.Blocks.Add(new Paragraph(new Run($"Type de structure : {type} | Couches évaluées : {snap.Count}"))
        { FontStyle = FontStyles.Italic, FontSize = 12, Foreground = Brushes.Black, Margin=new Thickness(0,0,0,6)});

        // Règles spécifiques
        if (type.Equals("Souple", System.StringComparison.OrdinalIgnoreCase))
        {
            AddSoupleRuleInfo(fd, ctx, eps);
        }
        else if (type.Equals("Semi-rigide", System.StringComparison.OrdinalIgnoreCase))
        {
            AddSemiRigideRuleInfo(fd, ctx, eps, sig);
        }
        // Rappel générique: contrôle fatigue bitumineux en εt à la base des couches bitumineuses (ROULEMENT prioritaire)
        AddBituminousFatigueControl(fd, ctx, eps);

        // Résumé synthétique
        var tauxMax = snap.Where(s=>s.ValeurAdmissible>0 && (s.SoupleEpsiZApplicable || !s.Critere.Equals("EpsiZ", System.StringComparison.OrdinalIgnoreCase))).Select(s=>s.TauxUtilisation).DefaultIfEmpty(0).Max();
        var couchesCritiques = snap.Where(s=>s.TauxUtilisation >= tauxMax - 0.5 && (s.SoupleEpsiZApplicable || !s.Critere.Equals("EpsiZ", System.StringComparison.OrdinalIgnoreCase))).Select(s=>s.Niveau.ToString()).ToList();
        string resume = $"Taux d'utilisation maximal = {tauxMax:0.0}% (couche(s) {string.Join(",", couchesCritiques)})";
        fd.Blocks.Add(new Paragraph(new Run(resume)) { Margin=new Thickness(0,4,0,10) });

        // Tableau principal
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0,0,0,6) };
        foreach (var _ in Enumerable.Range(0,9)) table.Columns.Add(new TableColumn());
        var rg = new TableRowGroup(); table.RowGroups.Add(rg);

        TableRow Header(params string[] h)
        {
            var tr = new TableRow();
            foreach (var head in h)
                tr.Cells.Add(new TableCell(new Paragraph(new Bold(new Run(head))) { Margin=new Thickness(0) })
                { Padding = new Thickness(3,0,6,3), Background = Brushes.Transparent });
            return tr;
        }
        rg.Rows.Add(Header("Niv","Rôle","Matériau","Critère","Val. critique","Val. adm.","Taux %","NE","CAM"));

        foreach (var r in snap.OrderBy(r=>r.Niveau))
        {
            var tr = new TableRow();
            // Dépassement pour tout critère si (valcrit > valadm), sauf cas EpsiZ non applicable en souple
            bool depasse = r.ValeurAdmissible>0 && r.ValeurCritique>r.ValeurAdmissible;
            string valCritTxt = r.ValeurCritique.ToString("0.###");
            string tauxTxt = r.TauxUtilisation.ToString("0.0");

            // Si aucune VA n'est disponible (0 ou négative) on affiche NA et on neutralise le dépassement
            string valAdmTxt = r.ValeurAdmissible > 0 ? r.ValeurAdmissible.ToString("0.###") : "-";
            if (r.ValeurAdmissible <= 0)
            {
                tauxTxt = "NA";
                depasse = false;
            }

            if (!r.SoupleEpsiZApplicable && r.Critere.Equals("EpsiZ", System.StringComparison.OrdinalIgnoreCase) &&
                ctx.Project.PavementStructure?.StructureType?.Equals("Souple", System.StringComparison.OrdinalIgnoreCase)==true)
            {
                valCritTxt = "NA";
                tauxTxt = "NA";
                depasse = false; // ne pénalise pas
            }

            // Accentuer la ligne Roulement (BB) en EpsiT
            bool accent = r.Critere.Equals("EpsiT", System.StringComparison.OrdinalIgnoreCase)
                          && r.Role.Equals("Roulement", System.StringComparison.OrdinalIgnoreCase)
                          && (r.Materiau?.ToLowerInvariant().Contains("bb") == true
                              || r.Famille?.ToLowerInvariant().Contains("bitum") == true);

            Brush bg = Brushes.Transparent;
            if (depasse)
                bg = new SolidColorBrush(Color.FromArgb(40,220,53,69));
            else if (accent)
                bg = new SolidColorBrush(Color.FromArgb(26, 70, 130, 180)); // léger surlignage info

            void Cell(string txt) => tr.Cells.Add(new TableCell(new Paragraph(new Run(txt))) { Padding=new Thickness(3,0,6,3), Background = bg });
            Cell(r.Niveau.ToString());
            Cell(r.Role);
            Cell(r.Materiau);
            Cell(r.Critere);
            Cell(valCritTxt);
            Cell(valAdmTxt);
            Cell(tauxTxt);
            Cell(r.NE>0? r.NE.ToString("N0"):"-");
            Cell(r.CAM>0? r.CAM.ToString("0.00"):"-");
            rg.Rows.Add(tr);
        }
        fd.Blocks.Add(table);

        // Explications (avec symboles corrigés)
        fd.Blocks.Add(new Paragraph(new Bold(new Run("Interprétation selon le type de structure (références NF P98-086 / Catalogue Sénégalais / CEBTP)")))
        { FontSize=13, Margin=new Thickness(0,6,0,4)});
        fd.Blocks.Add(new Paragraph(new Run(BuildExplications(type, eps, sig))) { FontSize=11, TextAlignment=TextAlignment.Justify, Foreground = Brushes.Black });

        fd.Blocks.Add(new Paragraph(new Run($"Critères : {eps}t (epsilon_t, fatigue traction bitumineux), {sig}t (sigma_t, fatigue traction matériaux traités), {eps}z (epsilon_z, déformation verticale plateforme / couches granulaires). Taux % = (Val. critique / Val. admissible)*100. Si aucune valeur admissible n'est disponible, le taux est noté 'NA'."))
        { FontSize=10.5, Foreground=Brushes.Black, Margin=new Thickness(0,6,0,0)});

        return fd;
    }

    private void AddSoupleRuleInfo(FlowDocument fd, ReportContext ctx, string eps)
    {
        // NE global = max des NE dans les valeurs admissibles
        double ne = (ctx.AdmissibleValues?.Count > 0) ? ctx.AdmissibleValues.Max(v => v.Ne) : 0;
        bool seuil = ne > 250_000;

        var unbound = ctx.LayerInfos.Where(l => l.Family.Contains("GNT") || l.Role == "Plateforme").OrderBy(l => l.Order).ToList();
        var autres = unbound.Where(l => l.Role != "Plateforme").ToList();

        string cas = seuil
            ? $"NE > 250 000 : contrôle {eps}z sur toutes les couches non liées (GNT) et la plateforme"
            : $"NE ≤ 250 000 : contrôle {eps}z uniquement sur la plateforme (PF)";

        fd.Blocks.Add(new Paragraph(new Bold(new Run($"Règle de vérification – Chaussée souple ({eps}z)")))
        { FontSize = 13, Margin = new Thickness(0,4,0,2) });
        fd.Blocks.Add(new Paragraph(new Run($"NE global (max VA) = {ne:N0} | {cas}"))
        { FontSize = 11, Foreground = Brushes.Black, Margin = new Thickness(0,0,0,2) });

        if (seuil)
        {
            string liste = string.Join(", ", autres.Select(a => $"{a.Role} {a.MaterialName}"));
            if (string.IsNullOrWhiteSpace(liste)) liste = "(aucune couche granulaire identifiée)";
            fd.Blocks.Add(new Paragraph(new Run($"Couches contrôlées en {eps}z: {liste} + Plateforme"))
            { FontSize = 11, Foreground = Brushes.Black });
        }
        else
        {
            fd.Blocks.Add(new Paragraph(new Run($"Couches contrôlées en {eps}z: Plateforme uniquement"))
            { FontSize = 11, Foreground = Brushes.Black });
        }

        fd.Blocks.Add(new Paragraph(new Run($"Note: les couches non liées marquées NA dans le tableau ne pénalisent pas la validation lorsque NE ≤ 250 000 (règle NF P98-086)."))
        { FontSize = 10.5, Foreground = Brushes.Black, Margin = new Thickness(0,0,0,6) });
    }

    private void AddSemiRigideRuleInfo(FlowDocument fd, ReportContext ctx, string eps, string sig)
    {
        // Lister les couches traitées (MTLH) en base/fondation
        var mtlh = ctx.LayerInfos
            .Where(l => l.Family.Contains("MTLH", System.StringComparison.OrdinalIgnoreCase) &&
                        (l.Role.Equals("Base", System.StringComparison.OrdinalIgnoreCase) || l.Role.Equals("Fondation", System.StringComparison.OrdinalIgnoreCase)))
            .OrderBy(l => l.Order)
            .ToList();
        bool uneSeule = mtlh.Count <= 1;
        bool toutesCollees = false;
        if (mtlh.Count >= 2)
        {
            toutesCollees = true;
            for (int i = 0; i < mtlh.Count - 1; i++)
            {
                var up = mtlh[i];
                // InterfaceWithBelow est portée par la couche supérieure
                var iface = up.InterfaceWithBelow ?? string.Empty;
                if (!iface.Contains("Collee", System.StringComparison.OrdinalIgnoreCase)) { toutesCollees = false; break; }
            }
        }

        double ne = (ctx.AdmissibleValues?.Count > 0) ? ctx.AdmissibleValues.Max(v => v.Ne) : 0;

        fd.Blocks.Add(new Paragraph(new Bold(new Run($"Règle de vérification – Chaussée semi-rigide ({sig}t, {eps}z)")))
        { FontSize = 13, Margin = new Thickness(0,4,0,2) });

        // Fatigue traction MTLH
        var p1 = new Paragraph() { FontSize = 11, Foreground = Brushes.Black, Margin = new Thickness(0,0,0,2) };
        p1.Inlines.Add(new Run($"• Rupture par fatigue en traction des couches traitées: la contrainte de traction {sig}t à la base des couches MTLH doit rester < {sig}t,adm (calculée). "));
        p1.Inlines.Add(new Run(uneSeule || toutesCollees
            ? "S'il n'y a qu'une couche ou si les couches restent collées, on vérifie au niveau de la base de l'assise traitée."
            : "Sinon, le critère est vérifié à la base de chaque couche traitée."));
        fd.Blocks.Add(p1);

        // Plateforme epsilon_z
        var p2 = new Paragraph(new Run($"• Déformation verticale réversible de la plateforme: la déformation {eps}z à la surface de la plate-forme doit rester < {eps}z,adm (fonction de NE). NE global (max VA) = {ne:N0}."))
        { FontSize = 11, Foreground = Brushes.Black, Margin = new Thickness(0,0,0,6) };
        fd.Blocks.Add(p2);

        // Liste informative des couches MTLH et interfaces
        if (mtlh.Count > 0)
        {
            string list = string.Join(" → ", mtlh.Select(l => $"{l.Role} {l.MaterialName}{(string.IsNullOrWhiteSpace(l.InterfaceWithBelow)?"":"/"+NormalizeIface(l.InterfaceWithBelow))}"));
            fd.Blocks.Add(new Paragraph(new Run($"Pile traitée détectée: {list}")) { FontSize = 10.5, Foreground = Brushes.DarkSlateGray });
        }

        static string NormalizeIface(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            if (s.Contains("Semi", System.StringComparison.OrdinalIgnoreCase)) return "Semi-collée";
            if (s.Contains("Decol", System.StringComparison.OrdinalIgnoreCase) || s.Contains("D e9col", System.StringComparison.OrdinalIgnoreCase)) return "Glissante";
            return "Collée";
        }
    }

    private void AddBituminousFatigueControl(FlowDocument fd, ReportContext ctx, string eps)
    {
        // Lister les couches bitumineuses (Family contient 'bitum' de façon robuste)
        var bitum = ctx.LayerInfos
            .Where(l => !string.IsNullOrWhiteSpace(l.Family) &&
                        (l.Family.Contains("bitum", System.StringComparison.OrdinalIgnoreCase) ||
                         l.Family.Contains("béton bit", System.StringComparison.OrdinalIgnoreCase) ||
                         l.Family.Contains("beton bit", System.StringComparison.OrdinalIgnoreCase)))
            .OrderBy(l => l.Order)
            .ToList();

        fd.Blocks.Add(new Paragraph(new Bold(new Run($"Contrôle fatigue bitumineux ({eps}t)")))
        { FontSize = 13, Margin = new Thickness(0,4,0,2) });

        if (bitum.Count == 0)
        {
            fd.Blocks.Add(new Paragraph(new Run("Aucune couche bitumineuse détectée dans la structure."))
            { FontSize = 11, Foreground = Brushes.Black });
            return;
        }

        var roul = bitum.FirstOrDefault(b => b.Role.Equals("Roulement", System.StringComparison.OrdinalIgnoreCase));
        if (roul != null)
        {
            fd.Blocks.Add(new Paragraph(new Run($"Toujours vérifier la couche de roulement bitumineuse en priorité: {eps}t,base < {eps}t,adm (au droit de la base de la couche de roulement)."))
            { FontSize = 11, Foreground = Brushes.Black });
        }

        string liste = string.Join(", ", bitum.Select(b => $"{b.Role} {b.MaterialName}"));
        fd.Blocks.Add(new Paragraph(new Run($"Couches bitumineuses concernées : {liste}."))
        { FontSize = 11, TextAlignment = TextAlignment.Left, Foreground = Brushes.Black });
    }

    private string BuildExplications(string type, string eps, string sig)
    {
        type = (type ?? "").ToLowerInvariant();
        return type switch
        {
            "souple" => $"Structure souple : le dimensionnement est gouverné par la fatigue en traction des couches bitumineuses ({eps}t) et la limitation de la déformation verticale cumulée en tête de plateforme ({eps}z). Les valeurs admissibles reposent sur les paramètres fatigue ({eps}6, B) et l'ajustement des coefficients kc, kr, ks, kθ. Vérifier prioritairement la couche bitumineuse présentant le plus fort taux d'utilisation.",
            "bitumineuse épaisse" or "bitumineuse epaisse" => $"Structure bitumineuse épaisse : toutes les couches supérieures bitumineuses sont vérifiées en fatigue ({eps}t) avec homogénéité modulaire ; la fondation non liée est contrôlée en {eps}z. Ratios d'épaisseur et modules conformes aux plages catalogue Sénégalais / NF P98-086. Un taux > 90% sur plusieurs couches indique une optimisation atteinte.",
            "semi-rigide" => $"Structure semi-rigide : la base/fondation traitée hydraulique est vérifiée en fatigue traction ({sig}t) tandis que la couche de roulement bitumineuse reste en fatigue ({eps}t). Une redistribution des contraintes peut survenir : surveiller les couches avec {sig}t proche de l'admissible et la plateforme en {eps}z pour éviter la formation de fissuration en dessous.",
            "rigide" => $"Structure rigide : le critère principal est la traction à la base de la dalle en béton ({sig}t). Les couches support sont vérifiées pour la déformation verticale ({eps}z) afin de limiter le pompage et assurer la répartition homogène. Les marges >20% sur {sig}t indiquent une réserve structurelle acceptable.",
            "mixte" => $"Structure mixte : appliquer hiérarchiquement {sig}t sur couches traitées, puis {eps}t sur bitumineux, enfin {eps}z sur couches non liées. Prioriser la réduction de la couche critique au plus haut taux tout en conservant l'équilibre modulaire.",
            _ => $"Les critères sont interprétés selon la hiérarchie : {sig}t (matériaux traités), {eps}t (bitumineux), {eps}z (granulaires / plateforme). Vérifier que les couches supérieures n'excèdent pas 100% et que la plateforme conserve une marge suffisante pour la portance long terme."
        };
    }
}
