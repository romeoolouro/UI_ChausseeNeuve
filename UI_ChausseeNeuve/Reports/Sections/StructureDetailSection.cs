using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace UI_ChausseeNeuve.Reports.Sections;

public sealed class StructureDetailSection : IReportSection
{
    public string Id => "structure.detail";
    public string Title => "Structure détaillée";
    public int Order { get; set; } = 5;
    public bool IsEnabled { get; set; } = true;

    public FlowDocument Build(ReportContext ctx)
    {
        var fd = new FlowDocument();
        fd.Blocks.Add(new Paragraph(new Bold(new Run("5. Structure détaillée")))
        { FontSize = 18, Margin = new Thickness(0,16,0,12) });

        var ordered = ctx.LayerInfos.OrderBy(l => l.Order).ToList();
        if (ordered.Count == 0)
        {
            fd.Blocks.Add(new Paragraph(new Run("Aucune couche disponible.")) { Foreground = Brushes.OrangeRed });
            return fd;
        }

        // Tableau récapitulatif (ajout colonne interface)
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0,0,0,16) };
        foreach (var _ in Enumerable.Range(0,10)) table.Columns.Add(new TableColumn());
        var rg = new TableRowGroup(); table.RowGroups.Add(rg);
        rg.Rows.Add(Header("Ordre","Rôle","Famille","Matériau","Ep.(m)","E (MPa)","coef. pois","ks","kd","Interface?"));
        foreach (var l in ordered)
        {
            bool isPF = l.Role.Equals("Plateforme", System.StringComparison.OrdinalIgnoreCase);
            rg.Rows.Add(Row(l.Order.ToString(), l.Role, l.Family, l.MaterialName, isPF?"PF":l.Thickness_m.ToString("0.000"), l.Modulus_MPa.ToString("0"), l.Poisson.ToString("0.00"), l.Ks.ToString("0.00"), l.Kd.ToString("0.00"), l.InterfaceWithBelow ?? ""));
        }
        fd.Blocks.Add(table);

        // Coupe proportionnelle hors plateforme
        var nonPlatform = ordered.Where(l => !l.Role.Equals("Plateforme", System.StringComparison.OrdinalIgnoreCase)).ToList();
        double totalTh = nonPlatform.Sum(l => l.Thickness_m);
        if (totalTh <= 0) totalTh = 1.0;
        const double targetHeight = 260.0;
        double scale = targetHeight / totalTh;
        fd.Blocks.Add(new Paragraph(new Bold(new Run("Coupe schématique (proportions réelles hors PF)"))) { Margin = new Thickness(0,0,0,4) });

        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(15,0,0,0)), null, new Rect(0,0,620,targetHeight+2));

            double y = 0; double cumul = 0;
            for (int idx = 0; idx < nonPlatform.Count; idx++)
            {
                var layer = nonPlatform[idx];
                double hPx = layer.Thickness_m * scale;
                var rect = new Rect(120, y, 360, hPx);
                var fill = GetFamilyBrush(layer.Family);
                double radius = layer == nonPlatform.First()?8:0;
                var geo = new RectangleGeometry(rect, radius, radius);
                dc.DrawGeometry(fill, new Pen(Brushes.Black, 1), geo);

                var label = $"{layer.Role} ({layer.Thickness_m:0.000} m)";
                var textBrush = NeedsLightLabel(fill) ? Brushes.White : Brushes.Black;
                var ft = CreateFT(label, 11, FontWeights.SemiBold, textBrush);
                if (ft.Width + 12 < rect.Width && ft.Height + 4 < rect.Height)
                    dc.DrawText(ft, new Point(rect.X + 6, y + (hPx - ft.Height)/2));
                else
                    dc.DrawText(ft, new Point(rect.Right + 6, y));

                cumul += layer.Thickness_m;
                var ftDepth = CreateFT($"{cumul:0.000} m", 10, FontWeights.Normal, Brushes.Black);
                dc.DrawLine(new Pen(Brushes.Black,0.7), new Point(120, y + hPx), new Point(480, y + hPx));
                dc.DrawText(ftDepth, new Point(0, y + hPx - ftDepth.Height/2));

                // Interface avec la couche du dessous (affichée à droite de la coupe, centrée sur la limite)
                if (idx < nonPlatform.Count - 1)
                {
                    var current = nonPlatform[idx];
                    var iface = current.InterfaceWithBelow; // nature interface entre cette couche et celle du dessous
                    if (!string.IsNullOrWhiteSpace(iface))
                    {
                        string shortTxt = iface switch
                        {
                            var s when s.Contains("Collee", System.StringComparison.OrdinalIgnoreCase) && !s.Contains("Semi", System.StringComparison.OrdinalIgnoreCase) => "Collée",
                            var s when s.Contains("Semi", System.StringComparison.OrdinalIgnoreCase) => "Semi-collée",
                            var s when s.Contains("Decollee", System.StringComparison.OrdinalIgnoreCase) || s.Contains("Décol", System.StringComparison.OrdinalIgnoreCase) => "Décollée",
                            _ => iface
                        };
                        var ftIface = CreateFT(shortTxt, 10, FontWeights.Normal, Brushes.DarkSlateGray);
                        double boundaryY = y + hPx; // limite
                        // petit trait repère
                        dc.DrawLine(new Pen(Brushes.DarkSlateGray,0.8), new Point(482, boundaryY), new Point(492, boundaryY));
                        // bulle simple (fond clair)
                        var bubbleRect = new Rect(495, boundaryY - ftIface.Height/2 - 2, ftIface.Width + 12, ftIface.Height + 4);
                        dc.DrawRoundedRectangle(Brushes.White, new Pen(Brushes.DarkSlateGray,0.8), bubbleRect, 4,4);
                        dc.DrawText(ftIface, new Point(bubbleRect.X + 6, bubbleRect.Y + 2));
                    }
                }

                y += hPx;
            }
            var ft0 = CreateFT("0.000 m",10,FontWeights.Normal,Brushes.Black);
            dc.DrawText(ft0, new Point(0,-ft0.Height/2));
            var pf = ordered.FirstOrDefault(l => l.Role.Equals("Plateforme", System.StringComparison.OrdinalIgnoreCase));
            if (pf != null)
            {
                dc.DrawLine(new Pen(GetFamilyBrush(pf.Family),4), new Point(120,y), new Point(480,y));
                var ftPF = CreateFT($"Plateforme (E={pf.Modulus_MPa:0} MPa)",11,FontWeights.Bold,Brushes.Black);
                dc.DrawText(ftPF, new Point(120, y+6));
            }
        }
        var bmp = new System.Windows.Media.Imaging.RenderTargetBitmap(640,(int)(targetHeight+60),96,96,PixelFormats.Pbgra32);
        bmp.Render(dv);
        fd.Blocks.Add(new BlockUIContainer(new System.Windows.Controls.Image { Source=bmp, Width=640, Stretch=Stretch.None, Margin=new Thickness(0,0,0,8)}));
        fd.Blocks.Add(BuildLegend());
        return fd;
    }

    private TableRow Header(params string[] cols)
    {
        var r = new TableRow();
        foreach (var c in cols) r.Cells.Add(new TableCell(new Paragraph(new Run(c))));
        return r;
    }
    private TableRow Row(params string[] cols)
    {
        var r = new TableRow();
        foreach (var c in cols) r.Cells.Add(new TableCell(new Paragraph(new Run(c))));
        return r;
    }

    private Brush GetFamilyBrush(string family)
    {
        if (family.Contains("BetonBitumineux", System.StringComparison.OrdinalIgnoreCase)) return Brushes.Black;
        if (family.Contains("GNT", System.StringComparison.OrdinalIgnoreCase) || family.Contains("Plateforme", System.StringComparison.OrdinalIgnoreCase)) return FromHex("#E4B99F");
        if (family.Contains("MTLH", System.StringComparison.OrdinalIgnoreCase)) return FromHex("#D3D3D3");
        if (family.Contains("BetonCiment", System.StringComparison.OrdinalIgnoreCase)) return FromHex("#A9A9A9");
        if (family.Contains("Bibliotheque", System.StringComparison.OrdinalIgnoreCase)) return FromHex("#BEBEBE");
        return Brushes.LightGray;
    }

    private bool NeedsLightLabel(Brush b)
    {
        if (b is SolidColorBrush sc)
        {
            var c = sc.Color;
            double l = 0.2126 * c.R/255.0 + 0.7152 * c.G/255.0 + 0.0722 * c.B/255.0;
            return l < 0.35;
        }
        return false;
    }

    private SolidColorBrush FromHex(string hex)
    {
        var conv = new BrushConverter();
        var br = conv.ConvertFromString(hex) as SolidColorBrush;
        return br ?? Brushes.LightGray;
    }

    private BlockUIContainer BuildLegend()
    {
        var sp = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0,0,0,8) };
        void Add(string label, Brush b)
        {
            sp.Children.Add(new System.Windows.Controls.Border { Background=b, Width=26, Height=16, BorderBrush=Brushes.Black, BorderThickness=new Thickness(1), CornerRadius=new CornerRadius(3), Margin=new Thickness(4,0,4,0)});
            sp.Children.Add(new System.Windows.Controls.TextBlock { Text=label, VerticalAlignment=VerticalAlignment.Center, Margin=new Thickness(2,0,12,0)});
        }
        Add("Bitumineux", GetFamilyBrush("BetonBitumineux"));
        Add("GNT / PF", GetFamilyBrush("GNT"));
        Add("MTLH", GetFamilyBrush("MTLH"));
        Add("Béton", GetFamilyBrush("BetonCiment"));
        Add("Bibliothèque", GetFamilyBrush("Bibliotheque"));
        Add("Interface: Collée / Semi-collée / Décollée", Brushes.Transparent);
        return new BlockUIContainer(sp);
    }

    private FormattedText CreateFT(string text,double size,FontWeight fw,Brush brush) => new(text,System.Globalization.CultureInfo.GetCultureInfo("fr-FR"),FlowDirection.LeftToRight,new Typeface(new FontFamily("Segoe UI"),FontStyles.Normal,fw,FontStretches.Normal),size,brush,96);
}
