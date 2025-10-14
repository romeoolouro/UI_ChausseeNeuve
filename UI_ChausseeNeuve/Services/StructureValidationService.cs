using System;
using System.Collections.Generic;
using System.Linq;
using ChausseeNeuve.Domain.Models;

namespace UI_ChausseeNeuve.Services
{
    public enum ValidationSeverity { Info, Warning, Error }

    public record ValidationIssue(
        ValidationSeverity Severity,
        string Message,
        int? LayerOrder = null,
        string? Role = null,
        string? Family = null,
        string? Material = null
    );

    public record StructureValidationResult(bool IsValid, List<ValidationIssue> Issues);

    public static class StructureValidationService
    {
        public static StructureValidationResult Validate(Project project)
        {
            var issues = new List<ValidationIssue>();
            try
            {
                var structure = project?.PavementStructure;
                if (structure == null || structure.Layers == null || structure.Layers.Count == 0)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, "Structure absente ou vide."));
                    return new StructureValidationResult(false, issues);
                }

                var layers = structure.Layers.OrderBy(l => l.Order).ToList();
                if (layers.Count < 2)
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, "La structure doit contenir au moins 2 couches."));

                // Plateforme unique et dernière
                var platforms = layers.Where(l => l.Role == LayerRole.Plateforme).ToList();
                if (platforms.Count != 1)
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, "La structure doit contenir exactement 1 Plateforme (PF)."));
                var last = layers.LastOrDefault();
                if (last != null && last.Role != LayerRole.Plateforme)
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, "La dernière couche doit être la Plateforme."));

                // Propriétés élémentaires
                int expectedOrder = layers.Min(l => l.Order);
                foreach (var l in layers)
                {
                    if (l.Order != expectedOrder)
                        issues.Add(new ValidationIssue(ValidationSeverity.Warning, $"Ordre inattendu: attendu {expectedOrder}, trouvé {l.Order}.", l.Order, l.Role.ToString(), l.Family.ToString(), l.MaterialName));

                    if (l.Role != LayerRole.Plateforme && l.Thickness_m <= 0)
                        issues.Add(new ValidationIssue(ValidationSeverity.Error, "Épaisseur doit être > 0 (hors PF).", l.Order, l.Role.ToString(), l.Family.ToString(), l.MaterialName));

                    if (l.Modulus_MPa <= 0)
                        issues.Add(new ValidationIssue(ValidationSeverity.Error, "Module élastique (E) doit être > 0.", l.Order, l.Role.ToString(), l.Family.ToString(), l.MaterialName));

                    if (l.Poisson < 0 || l.Poisson > 0.5)
                        issues.Add(new ValidationIssue(ValidationSeverity.Error, "Coefficient de Poisson doit être dans [0; 0.5].", l.Order, l.Role.ToString(), l.Family.ToString(), l.MaterialName));

                    expectedOrder++;
                }

                // Règles spécifiques "Mixte"
                var type = structure.StructureType ?? string.Empty;
                if (type.Equals("Mixte", StringComparison.OrdinalIgnoreCase))
                {
                    // MB en surface (Roulement) et en Base
                    bool hasMbSurface = layers.Any(l => l.Role == LayerRole.Roulement && l.Family == MaterialFamily.BetonBitumineux);
                    bool hasMbBase = layers.Any(l => l.Role == LayerRole.Base && l.Family == MaterialFamily.BetonBitumineux);
                    if (!hasMbSurface)
                        issues.Add(new ValidationIssue(ValidationSeverity.Error, "Mixte: la couche de surface doit être en Matériau Bitumineux (MB)."));
                    if (!hasMbBase)
                        issues.Add(new ValidationIssue(ValidationSeverity.Error, "Mixte: au moins une couche de base doit être en Matériau Bitumineux (MB)."));

                    // MTLH en fondation
                    bool hasMtlhFond = layers.Any(l => l.Role == LayerRole.Fondation && l.Family == MaterialFamily.MTLH);
                    if (!hasMtlhFond)
                        issues.Add(new ValidationIssue(ValidationSeverity.Error, "Mixte: la couche de fondation doit être en MTLH."));

                    // Ratio 0,45 ? ep.MB(surface+base) / ep.Structure(hors PF) ? 0,60
                    double epStructure = layers.Where(l => l.Role != LayerRole.Plateforme).Sum(l => l.Thickness_m);
                    double epMB = layers.Where(l => l.Role != LayerRole.Plateforme && l.Family == MaterialFamily.BetonBitumineux && (l.Role == LayerRole.Roulement || l.Role == LayerRole.Base)).Sum(l => l.Thickness_m);
                    if (epStructure <= 0)
                    {
                        issues.Add(new ValidationIssue(ValidationSeverity.Error, "Mixte: épaisseur de structure hors PF nulle (ep.Structure = 0)."));
                    }
                    else
                    {
                        double ratio = epMB / epStructure;
                        if (ratio < 0.45 || ratio > 0.60)
                        {
                            issues.Add(new ValidationIssue(ValidationSeverity.Error, $"Mixte: ratio épaisseur bitumineux (surface+base)/structure hors plage [0,45; 0,60]. Actuel: {ratio:0.###} (ep.MB={epMB:0.000} m / ep.Structure={epStructure:0.000} m)"));
                        }
                    }
                }

                bool isValid = !issues.Any(i => i.Severity == ValidationSeverity.Error);
                return new StructureValidationResult(isValid, issues);
            }
            catch (Exception ex)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, $"Exception validation: {ex.Message}"));
                return new StructureValidationResult(false, issues);
            }
        }
    }
}
