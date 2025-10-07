using System;
using System.Globalization;
using System.Collections.Generic;
using ChausseeNeuve.Domain.Models;

namespace UI_ChausseeNeuve.Reports;

/// <summary>
/// Contexte fig� pass� aux sections du rapport.
/// Contient uniquement des donn�es IMMUTABLES (snapshots) pour �viter les exceptions
/// "collection modifi�e" pendant la g�n�ration.
/// </summary>
public sealed class ReportContext
{
    public required Project Project { get; init; }
    /// <summary>Structure brute (� n'utiliser que pour m�tadonn�es simples)</summary>
    public PavementStructure Structure => Project.PavementStructure;
    /// <summary>Snapshot immuable des couches (ne r�f�rence pas les objets Layer WPF)</summary>
    public required IReadOnlyList<LayerInfo> LayerInfos { get; init; }
    /// <summary>Snapshot immuable des valeurs admissibles</summary>
    public IReadOnlyList<AdmissibleValueInfo> AdmissibleValues { get; init; } = Array.Empty<AdmissibleValueInfo>();
    public DateTime GeneratedAtUtc { get; init; } = DateTime.UtcNow;
    public string AppVersion { get; init; } = "1.0.0";
    public string? CommitHash { get; init; }
    public CultureInfo Culture { get; init; } = CultureInfo.GetCultureInfo("fr-FR");
    public string DetailLevel { get; init; } = "Normal";
    public string? InputFingerprint { get; init; }
}

/// <summary>
/// Snapshot simplifi� d'une couche utilis� dans la note (sans liaison UI)
/// </summary>
public sealed class LayerInfo
{
    public int Order { get; init; }
    public string Role { get; init; } = string.Empty;
    public string Family { get; init; } = string.Empty; // Ajout pour coloration coupe
    public string MaterialName { get; init; } = string.Empty;
    public double Thickness_m { get; init; }
    public double Modulus_MPa { get; init; }
    public double Poisson { get; init; }
    public double Ks { get; init; }
    public double Kd { get; init; }
    public string? InterfaceWithBelow { get; init; } // Collee / SemiCollee / Decollee / null
}

/// <summary>
/// Snapshot d'une ligne de valeurs admissibles
/// </summary>
public sealed class AdmissibleValueInfo
{
    public int Niveau { get; init; }
    public string Materiau { get; init; } = string.Empty;
    public string Critere { get; init; } = string.Empty;
    public double Cam { get; init; }
    public double Ne { get; init; }
    public double B { get; init; }
    public double AmplitudeValue { get; init; }
    public double Epsilon6 { get; init; }
    public double Sigma6 { get; init; }
    public double Sn { get; init; }
    public double Sh { get; init; }
    public double Kc { get; init; }
    public double Kr { get; init; }
    public double Ks { get; init; }
    public double Ktheta { get; init; }
    public double Kd { get; init; }
    public double ValeurAdmissible { get; init; }
}
