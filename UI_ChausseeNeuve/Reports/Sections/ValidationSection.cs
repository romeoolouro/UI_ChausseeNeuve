using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using UI_ChausseeNeuve.Services;

namespace UI_ChausseeNeuve.Reports.Sections
{
    public sealed class ValidationSection : IReportSection
    {
        public string Id => "structure.validation";
        public string Title => "Contrôles de conformité";
        public int Order { get; set; } = 3;
        public bool IsEnabled { get; set; } = true;

        public FlowDocument Build(ReportContext ctx)
        {
            var fd = new FlowDocument();
            var result = StructureValidationService.Validate(ctx.Project);

            var titleText = result.IsValid ? "3. Contrôles de conformité (OK)" : "3. Contrôles de conformité (NON CONFORME)";
            var titleColor = result.IsValid ? Brushes.SeaGreen : Brushes.IndianRed;

            fd.Blocks.Add(new Paragraph(new Bold(new Run(titleText)))
            {
                FontSize = 18,
                Margin = new Thickness(0, 16, 0, 12),
                Foreground = titleColor
            });

            if (result.Issues.Count == 0)
            {
                fd.Blocks.Add(new Paragraph(new Run("Aucun écart détecté.")));
                return fd;
            }

            var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 12) };
            foreach (var _ in Enumerable.Range(0, 6)) table.Columns.Add(new TableColumn());
            var rg = new TableRowGroup(); table.RowGroups.Add(rg);

            rg.Rows.Add(Header("Niveau", "Rôle", "Famille", "Matériau", "Sévérité", "Message"));
            foreach (var i in result.Issues.OrderByDescending(x => x.Severity).ThenBy(x => x.LayerOrder ?? 0))
            {
                var sevColor = i.Severity switch
                {
                    ValidationSeverity.Error => Brushes.IndianRed,
                    ValidationSeverity.Warning => Brushes.DarkOrange,
                    _ => Brushes.SlateGray
                };
                rg.Rows.Add(Row(
                    i.LayerOrder?.ToString() ?? "-",
                    i.Role ?? "-",
                    i.Family ?? "-",
                    i.Material ?? "-",
                    new Run(i.Severity.ToString()) { Foreground = sevColor },
                    new Run(i.Message)
                ));
            }
            fd.Blocks.Add(table);
            return fd;
        }

        private TableRow Header(params string[] cols)
        {
            var r = new TableRow();
            foreach (var c in cols) r.Cells.Add(new TableCell(new Paragraph(new Bold(new Run(c)))) { Padding = new Thickness(2) });
            return r;
        }
        private TableRow Row(params object[] cols)
        {
            var r = new TableRow();
            foreach (var c in cols)
            {
                if (c is Run run)
                    r.Cells.Add(new TableCell(new Paragraph(run)) { Padding = new Thickness(2) });
                else
                    r.Cells.Add(new TableCell(new Paragraph(new Run(c?.ToString() ?? ""))) { Padding = new Thickness(2) });
            }
            return r;
        }
    }
}
