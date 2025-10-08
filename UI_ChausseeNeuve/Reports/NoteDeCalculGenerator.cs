using System.Linq;
using System.Windows.Documents;
using UI_ChausseeNeuve.Reports.Sections;
using ChausseeNeuve.Domain.Models;
using System.Reflection;
using System.Collections.Generic;

namespace UI_ChausseeNeuve.Reports;

public static class NoteDeCalculGenerator
{
    public static FlowDocument Generate(Project project, string? commitHash = null, string detailLevel = "Normal", string? fingerprint = null, bool includeDebug = false)
    {
        var layerInfos = new List<LayerInfo>();
        try
        {
            var layers = project.PavementStructure?.Layers?.ToList() ?? new();
            foreach (var l in layers)
            {
                layerInfos.Add(new LayerInfo
                {
                    Order = l.Order,
                    Role = l.Role.ToString(),
                    Family = l.Family.ToString(),
                    MaterialName = l.MaterialName ?? string.Empty,
                    Thickness_m = l.Thickness_m,
                    Modulus_MPa = l.Modulus_MPa,
                    Poisson = l.Poisson,
                    Ks = l.CoeffKs,
                    Kd = l.CoeffKd,
                    InterfaceWithBelow = l.InterfaceWithBelow?.ToString()
                });
            }
        }
        catch { }

        var admissibles = new List<AdmissibleValueInfo>();
        try
        {
            var src = project.ValeursAdmissibles;
            if (src != null)
            {
                foreach (var v in src)
                {
                    admissibles.Add(new AdmissibleValueInfo
                    {
                        Niveau = v.Niveau,
                        Materiau = v.Materiau ?? string.Empty,
                        Critere = v.Critere ?? string.Empty,
                        Cam = v.Cam,
                        Ne = v.Ne,
                        B = v.B,
                        AmplitudeValue = v.AmplitudeValue,
                        Epsilon6 = v.Epsilon6,
                        Sigma6 = v.Sigma6,
                        Sn = v.Sn,
                        Sh = v.Sh,
                        Kc = v.Kc,
                        Kr = v.Kr,
                        Ks = v.Ks,
                        Ktheta = v.Ktheta,
                        Kd = v.Kd,
                        ValeurAdmissible = v.ValeurAdmissible
                    });
                }
            }
        }
        catch { }

        var ctx = new ReportContext
        {
            Project = project,
            CommitHash = commitHash,
            DetailLevel = detailLevel,
            InputFingerprint = fingerprint,
            AppVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0",
            LayerInfos = layerInfos,
            AdmissibleValues = admissibles
        };

        var builder = new ReportBuilder()
            .Add(new CoverPageSection())
            .Add(new ExecutiveSummarySection())
            .Add(new MethodologieSection())
            .Add(new ParametresChargementSection())
            .Add(new StructureDetailSection())
            .Add(new ValeursAdmissiblesSection());

        if (includeDebug)
            builder.Add(new LayersDebugSection());

        return builder.Build(ctx);
    }
}
