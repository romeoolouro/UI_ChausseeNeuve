using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace UI_ChausseeNeuve.Reports.Sections
{
    /// <summary>
    /// Courbes des valeurs critiques et admissibles en fonction de la profondeur (z)
    /// - Sous-graphe 1: critères en déformation (EpsiT + EpsiZ) en µdef
    /// - Sous-graphe 2: critères en contrainte (SigmaT) en MPa
    /// - Sous-graphe 3: toutes valeurs (unités natives des critères)
    /// </summary>
    public sealed class GraphContraintesSection : IReportSection
    {
        public string Id => "courbes.contraintes";
        public string Title => "Courbes contraintes vs profondeur";
        public int Order { get; set; } = 8;
        public bool IsEnabled { get; set; } = true;

        public FlowDocument Build(ReportContext ctx)
        {
            var fd = new FlowDocument();
            fd.Blocks.Add(new Paragraph(new Bold(new Run("8. Courbes des valeurs critique et admissible en fonction de la profondeur")))
            { FontSize = 18, Margin = new Thickness(0, 16, 0, 12) });

            try
            {
                var snaps = ctx.Project?.DerniersResultats?.ToList() ?? new();
                if (snaps.Count == 0)
                {
                    fd.Blocks.Add(new Paragraph(new Run("Aucun résultat persistant pour tracer les courbes. Lancer un calcul dans l'onglet Résultats."))
                    { Foreground = Brushes.OrangeRed });
                    return fd;
                }

                // Calcul des profondeurs cumulées (hors PF) par niveau (Order)
                var orderedLayers = ctx.LayerInfos.OrderBy(l => l.Order).ToList();
                var nonPlatform = orderedLayers.Where(l => !l.Role.Equals("Plateforme", StringComparison.OrdinalIgnoreCase)).ToList();
                double totalTh = Math.Max(1e-9, nonPlatform.Sum(l => l.Thickness_m));
                var depthBottomByOrder = new Dictionary<int, double>();
                double cum = 0;
                foreach (var l in nonPlatform)
                {
                    cum += l.Thickness_m;
                    depthBottomByOrder[l.Order] = cum; // profondeur bas de couche
                }
                // Plateforme: profondeur = som. des épaisseurs (tête de PF)
                var pf = orderedLayers.FirstOrDefault(l => l.Role.Equals("Plateforme", StringComparison.OrdinalIgnoreCase));
                if (pf != null)
                    depthBottomByOrder[pf.Order] = cum;

                // Préparer sous-ensembles
                var epsilonRows = snaps.Where(s => s.Critere.Equals("EpsiT", StringComparison.OrdinalIgnoreCase) ||
                                                   s.Critere.Equals("EpsiZ", StringComparison.OrdinalIgnoreCase))
                                       .Where(s => !(s.Critere.Equals("EpsiZ", StringComparison.OrdinalIgnoreCase) && !s.SoupleEpsiZApplicable))
                                       .ToList();
                var sigmaRows = snaps.Where(s => s.Critere.Equals("SigmaT", StringComparison.OrdinalIgnoreCase)).ToList();

                // 1) Déformations
                if (epsilonRows.Count > 0)
                {
                    var bmp = DrawGraph(epsilonRows, depthBottomByOrder, totalTh,
                        title: "Déformations (µdef)", unit: "µdef",
                        extractor: r => (r.ValeurCritique, r.ValeurAdmissible));
                    fd.Blocks.Add(new BlockUIContainer(new System.Windows.Controls.Image { Source = bmp, Stretch = Stretch.None, Margin = new Thickness(0,0,0,10) }));
                }

                // 2) Contraintes
                if (sigmaRows.Count > 0)
                {
                    var bmp = DrawGraph(sigmaRows, depthBottomByOrder, totalTh,
                        title: "Contraintes (MPa)", unit: "MPa",
                        extractor: r => (r.ValeurCritique, r.ValeurAdmissible));
                    fd.Blocks.Add(new BlockUIContainer(new System.Windows.Controls.Image { Source = bmp, Stretch = Stretch.None, Margin = new Thickness(0,0,0,10) }));
                }

                // 3) Toutes valeurs (du début à la fin)
                var allRows = snaps
                    .Where(s => !(s.Critere.Equals("EpsiZ", StringComparison.OrdinalIgnoreCase) && !s.SoupleEpsiZApplicable))
                    .OrderBy(s => s.Niveau)
                    .ToList();
                if (allRows.Count > 0)
                {
                    var bmp = DrawGraph(allRows, depthBottomByOrder, totalTh,
                        title: "Toutes valeurs (unités natives)", unit: "mixte",
                        extractor: r => (r.ValeurCritique, r.ValeurAdmissible));
                    fd.Blocks.Add(new BlockUIContainer(new System.Windows.Controls.Image { Source = bmp, Stretch = Stretch.None, Margin = new Thickness(0,0,0,10) }));
                }

                // Légende
                var legend = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
                legend.Children.Add(new System.Windows.Shapes.Rectangle { Width = 22, Height = 4, Fill = Brushes.Firebrick, Margin = new Thickness(4,8,6,0) });
                legend.Children.Add(new System.Windows.Controls.TextBlock { Text = "Val. critique", Margin = new Thickness(0,0,20,0), VerticalAlignment = VerticalAlignment.Center });
                legend.Children.Add(new System.Windows.Shapes.Rectangle { Width = 22, Height = 4, Fill = Brushes.SteelBlue, Margin = new Thickness(4,8,6,0) });
                legend.Children.Add(new System.Windows.Controls.TextBlock { Text = "Val. admissible", VerticalAlignment = VerticalAlignment.Center });
                fd.Blocks.Add(new BlockUIContainer(legend));

                // Notes
                fd.Blocks.Add(new Paragraph(new Run("Toutes les couches sont tracées du début (surface) jusqu'à la tête de plateforme. Les valeurs EpsiZ non applicables sont ignorées. Le graphe 'Toutes valeurs' mélange les unités propres aux critères."))
                { FontSize = 11, Margin = new Thickness(0,0,0,6) });
            }
            catch (Exception ex)
            {
                fd.Blocks.Add(new Paragraph(new Run($"Erreur lors du tracé des courbes: {ex.Message}")) { Foreground = Brushes.OrangeRed });
            }

            return fd;
        }

        private RenderTargetBitmap DrawGraph(
            List<ChausseeNeuve.Domain.Models.ResultatCoucheSnapshotDto> rows,
            Dictionary<int, double> depthBottomByOrder,
            double totalTh,
            string title,
            string unit,
            Func<ChausseeNeuve.Domain.Models.ResultatCoucheSnapshotDto, (double crit, double adm)> extractor)
        {
            const int width = 680;
            const int height = 300;
            const int left = 70;
            const int right = 20;
            const int top = 26;
            const int bottom = 30;
            int plotW = width - left - right;
            int plotH = height - top - bottom;

            // X max (prend en compte seulement valeurs > 0)
            double xMax = 0;
            foreach (var r in rows)
            {
                var (c, a) = extractor(r);
                if (c > xMax) xMax = c;
                if (a > xMax) xMax = a;
            }
            if (xMax <= 0) xMax = 1;

            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                // Fond
                dc.DrawRectangle(Brushes.White, null, new Rect(0,0,width,height));

                // Titre
                var ftTitle = CreateFT(title, 13, FontWeights.Bold, Brushes.Black);
                dc.DrawText(ftTitle, new Point(left, 4));

                // Axes
                var plotRect = new Rect(left, top, plotW, plotH);
                dc.DrawRectangle(null, new Pen(Brushes.Black, 1), plotRect);

                // Graduations X (5)
                for (int i=0;i<=5;i++)
                {
                    double vx = i * xMax / 5.0;
                    double x = left + plotW * (vx / xMax);
                    dc.DrawLine(new Pen(Brushes.LightGray, 0.7), new Point(x, top), new Point(x, top+plotH));
                    var ft = CreateFT(vx.ToString("0.##"), 11, FontWeights.Normal, Brushes.Black);
                    dc.DrawText(ft, new Point(x - ft.Width/2, top + plotH + 6));
                }
                // Graduations Y par limites de couches
                var depths = depthBottomByOrder.Values.Distinct().OrderBy(d => d).ToList();
                foreach (var d in depths)
                {
                    double y = top + plotH * (d / totalTh);
                    dc.DrawLine(new Pen(Brushes.Gainsboro, 0.7), new Point(left, y), new Point(left+plotW, y));
                    var ft = CreateFT($"{d:0.000} m", 10, FontWeights.Normal, Brushes.Black);
                    dc.DrawText(ft, new Point(2, y - ft.Height/2));
                }

                // Courbes
                var penCrit = new Pen(Brushes.Firebrick, 1.6);
                var penAdm  = new Pen(Brushes.SteelBlue, 1.6);
                var geoCrit = new StreamGeometry();
                var geoAdm  = new StreamGeometry();
                bool critStarted = false, admStarted = false;

                using (var gcs = geoCrit.Open())
                using (var gas = geoAdm.Open())
                {
                    foreach (var r in rows.OrderBy(r => r.Niveau))
                    {
                        if (!depthBottomByOrder.TryGetValue(r.Niveau, out double d))
                            continue;
                        var (c, a) = extractor(r);
                        double y = top + plotH * (d / totalTh);

                        if (c > 0)
                        {
                            double x = left + plotW * (c / xMax);
                            if (!critStarted) { gcs.BeginFigure(new Point(x, y), false, false); critStarted = true; }
                            else { gcs.LineTo(new Point(x, y), true, false); }
                        }
                        if (a > 0)
                        {
                            double x = left + plotW * (a / xMax);
                            if (!admStarted) { gas.BeginFigure(new Point(x, y), false, false); admStarted = true; }
                            else { gas.LineTo(new Point(x, y), true, false); }
                        }
                    }
                }
                dc.DrawGeometry(null, penCrit, geoCrit);
                dc.DrawGeometry(null, penAdm, geoAdm);

                // Axe Y label
                var ftY = CreateFT("Profondeur (m)", 11, FontWeights.Normal, Brushes.Black);
                dc.PushTransform(new RotateTransform(-90, 12, top + plotH/2));
                dc.DrawText(ftY, new Point(12, top + plotH/2 - ftY.Width/2));
                dc.Pop();

                // Axe X label
                var ftX = CreateFT(unit, 11, FontWeights.Normal, Brushes.Black);
                dc.DrawText(ftX, new Point(left + plotW - ftX.Width, height - ftX.Height - 4));
            }

            var bmp = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(dv);
            return bmp;
        }

        private FormattedText CreateFT(string text, double size, FontWeight fw, Brush brush)
        {
            return new FormattedText(
                text,
                System.Globalization.CultureInfo.GetCultureInfo("fr-FR"),
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, fw, FontStretches.Normal),
                size,
                brush,
                96);
        }
    }
}
