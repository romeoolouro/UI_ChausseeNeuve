using System.Windows.Documents;
using System.Windows;

namespace UI_ChausseeNeuve.Reports.Sections;

/// <summary>Page de garde simple.</summary>
public sealed class CoverPageSection : IReportSection
{
    public string Id => "cover";
    public string Title => "Page de garde";
    public int Order { get; set; } = 0;
    public bool IsEnabled { get; set; } = true;

    public FlowDocument Build(ReportContext context)
    {
        var fd = new FlowDocument();

        fd.Blocks.Add(new Paragraph(new Bold(new Run(context.Project.Name)))
        {
            FontSize = 28,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0,120,0,40)
        });

        fd.Blocks.Add(new Paragraph(new Run("Note de calcul de dimensionnement de chaussée"))
        {
            FontSize = 16,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0,0,0,60)
        });

        fd.Blocks.Add(new Paragraph(new Run($"Date génération : {context.GeneratedAtUtc.ToLocalTime():g}"))
        {
            FontSize = 12,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0,0,0,8)
        });

        fd.Blocks.Add(new Paragraph(new Run($"Version logiciel : {context.AppVersion}"))
        {
            FontSize = 12,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0,0,0,4)
        });

        if (!string.IsNullOrWhiteSpace(context.CommitHash))
        {
            fd.Blocks.Add(new Paragraph(new Run($"Commit : {context.CommitHash}"))
            {
                FontSize = 11,
                TextAlignment = TextAlignment.Center
            });
        }

        if (!string.IsNullOrWhiteSpace(context.InputFingerprint))
        {
            fd.Blocks.Add(new Paragraph(new Run($"Empreinte entrée : {context.InputFingerprint}"))
            {
                FontSize = 10,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0,40,0,0)
            });
        }

        // Saut de page logique
        fd.Blocks.Add(new Paragraph(new Run("")) { BreakPageBefore = true });
        return fd;
    }
}
