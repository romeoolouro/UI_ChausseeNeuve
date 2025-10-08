using System.Linq;
using System.Windows.Documents;
using System.Collections.Generic;
using System;

namespace UI_ChausseeNeuve.Reports;

/// <summary>
/// Assemble les différentes sections en un FlowDocument unique.
/// </summary>
public sealed class ReportBuilder
{
    private readonly List<IReportSection> _sections = new();

    public ReportBuilder Add(IReportSection section)
    {
        _sections.Add(section);
        return this;    
    }

    public FlowDocument Build(ReportContext ctx)
    {
        var doc = new FlowDocument();
        // Snapshot immuable des sections pour éviter InvalidOperationException si la collection est modifiée ailleurs
        var snapshot = _sections.Where(s => s.IsEnabled).OrderBy(s => s.Order).ToList();
        foreach (var section in snapshot)
        {
            FlowDocument? part = null;
            try
            {
                part = section.Build(ctx);
            }
            catch (Exception ex)
            {
                // Injecter un bloc d'erreur au lieu de faire échouer toute la génération
                var err = new Paragraph(new Bold(new Run($"[Section '{section.Title}' en erreur]")))
                { Foreground = System.Windows.Media.Brushes.Red, FontSize = 14 };
                doc.Blocks.Add(err);
                doc.Blocks.Add(new Paragraph(new Run(ex.Message)) { FontSize = 11 });
                continue;
            }
            if (part == null) continue;
            foreach (var block in part.Blocks.ToList())
            {
                doc.Blocks.Add(block);
            }
        }
        return doc;
    }
}
