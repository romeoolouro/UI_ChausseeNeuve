using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using ChausseeNeuve.Domain.Models;

namespace UI_ChausseeNeuve.ViewModels
{
    public partial class ResultatViewModel
    {
        public bool MixedVerificationVisible
        {
            get
            {
                try
                {
                    var type = AppState.CurrentProject?.PavementStructure?.StructureType ?? string.Empty;
                    var mode = GetGlobalMode();
                    return mode == DimensionnementMode.Automatique &&
                           type.Equals("Mixte", StringComparison.OrdinalIgnoreCase);
                }
                catch { return false; }
            }
        }

        public double MixedNPL1 => ComputeMixed(out var npl1, out _, out _, out _, out _).NPL1;
        public double MixedNPL2 => ComputeMixed(out _, out var npl2, out _, out _, out _).NPL2;
        public double MixedNPLTotal => ComputeMixed(out _, out _, out var total, out _, out _).NPLTotal;
        public double MixedNPLRequired => ComputeMixed(out _, out _, out _, out var required, out _).NPLRequired;
        public bool IsMixedVerificationOk => ComputeMixed(out _, out _, out _, out _, out var ok).IsOk;

        // A appeler quand la structure/VA/résultats changent
        public void InvalidateMixedBindings()
        {
            OnPropertyChanged(nameof(MixedVerificationVisible));
            OnPropertyChanged(nameof(MixedNPL1));
            OnPropertyChanged(nameof(MixedNPL2));
            OnPropertyChanged(nameof(MixedNPLTotal));
            OnPropertyChanged(nameof(MixedNPLRequired));
            OnPropertyChanged(nameof(IsMixedVerificationOk));
            OnPropertyChanged(nameof(MixedDetails));
        }

        public MixedVerificationDetails MixedDetails => BuildMixedDetails();

        // --- Nouveaux helpers: calcul direct des NE/NPL (sans objets intermédiaires) ---
        private static double ComputeNPL(double ne, double cam) =>
            (cam > 0 && ne > 0 && double.IsFinite(ne)) ? ne / cam : 0;

        private static double ComputeNEFromSigmaT(double sigmaT, double sigma6, double b,
                                                  double kc, double kr, double ks, double kd)
        {
            if (b <= 0 || sigma6 <= 0 || kc <= 0 || kr <= 0 || ks <= 0 || kd <= 0) return 0;
            var denom = sigma6 * kc * kr * ks * kd;
            if (denom <= 0) return 0;
            var ratio = Math.Abs(sigmaT) / denom;
            if (ratio <= 0) return 0;
            return 1e6 * Math.Pow(ratio, -b);
        }

        private static double ComputeNEFromEpsiT(double epsiT, double eps6, double b,
                                                 double kc, double kr, double ks, double ktheta)
        {
            if (b <= 0 || eps6 <= 0 || kc <= 0 || kr <= 0 || ks <= 0 || ktheta <= 0) return 0;
            var denom = eps6 * kc * kr * ks * ktheta;
            if (denom <= 0) return 0;
            var ratio = Math.Abs(epsiT) / denom;
            if (ratio <= 0) return 0;
            return 1e6 * Math.Pow(ratio, -b);
        }
        // -------------------------------------------------------------------------------

        // Calcule les totaux NPL pour la vérification mixte (NPL1, NPL2, Total, Cible, OK)
        private (double NPL1, double NPL2, double NPLTotal, double NPLRequired, bool IsOk) ComputeMixed(
            out double npl1, out double npl2, out double total, out double required, out bool ok)
        {
            npl1 = npl2 = total = required = 0; ok = false;

            if (!MixedVerificationVisible)
                return (npl1, npl2, total, required, ok);

            try
            {
                var project = AppState.CurrentProject;
                var structure = project?.PavementStructure;
                if (project == null || structure?.Layers == null || structure.Layers.Count == 0)
                    return (0, 0, 0, 0, false);

                var ordered = structure.Layers.OrderBy(l => l.Order).ToList();

                // Sélection des couches cibles
                var fondationMtlh = ordered.Where(l => l.Role == LayerRole.Fondation && l.Family == MaterialFamily.MTLH)
                                           .OrderBy(l => l.Order).LastOrDefault();
                var baseMb = ordered.Where(l => l.Role == LayerRole.Base && l.Family == MaterialFamily.BetonBitumineux)
                                    .OrderBy(l => l.Order).LastOrDefault();

                // NPL1: ?t à la base de la fondation MTLH (structure nominale) en utilisant ResultatsInverse existants
                if (fondationMtlh != null)
                {
                    var inv = ResultatsInverse?.ToList() ?? new List<ResultatInverseItem>();
                    var item = inv.FirstOrDefault(i => i.Numero == fondationMtlh.Order)
                               ?? inv.FirstOrDefault(i => string.Equals(i.Materiau?.Trim(), (fondationMtlh.MaterialName ?? string.Empty).Trim(), StringComparison.InvariantCultureIgnoreCase));
                    if (item != null)
                    {
                        var ne1 = ComputeNEFromSigmaT(item.SigmaT, item.Sigma6, item.B, item.Kc, item.Kr, item.Ks, item.Kd);
                        npl1 = ComputeNPL(ne1, item.CAM);
                    }
                }

                // NPL2: ?t à la base de la Base MB avec conditions résiduelles
                if (baseMb != null)
                {
                    var res = ComputeResidualEpsiTAtBaseOfMB(baseMb, fondationMtlh);
                    var ne2 = ComputeNEFromEpsiT(res.epsT, res.eps6, res.b, res.kc, res.kr, res.ks, res.kt);
                    npl2 = ComputeNPL(ne2, res.cam);
                }

                total = npl1 + npl2;
                required = project.TraficCumuleNPL ?? 0;
                ok = (required > 0) && (total >= required);

                return (npl1, npl2, total, required, ok);
            }
            catch
            {
                return (0, 0, 0, 0, false);
            }
        }

        private MixedVerificationDetails BuildMixedDetails()
        {
            var details = new MixedVerificationDetails();
            if (!MixedVerificationVisible) return details;

            var project = AppState.CurrentProject;
            var structure = project?.PavementStructure;
            if (project == null || structure?.Layers == null || structure.Layers.Count == 0)
                return details;

            var ordered = structure.Layers.OrderBy(l => l.Order).ToList();
            var fondationMtlh = ordered.Where(l => l.Role == LayerRole.Fondation && l.Family == MaterialFamily.MTLH)
                                       .OrderBy(l => l.Order).LastOrDefault();
            var baseMb = ordered.Where(l => l.Role == LayerRole.Base && l.Family == MaterialFamily.BetonBitumineux)
                                .OrderBy(l => l.Order).LastOrDefault();

            var inv = ResultatsInverse?.ToList() ?? new System.Collections.Generic.List<ResultatInverseItem>();
            ResultatInverseItem? FindItemForLayer(Layer? layer) =>
                (layer == null) ? null : inv.FirstOrDefault(i => i.Numero == layer.Order)
                    ?? inv.FirstOrDefault(i => string.Equals(i.Materiau?.Trim(), (layer.MaterialName ?? string.Empty).Trim(), System.StringComparison.InvariantCultureIgnoreCase));

            // Etape 1 - Fondation MTLH
            if (fondationMtlh != null)
            {
                var f = FindItemForLayer(fondationMtlh);
                if (f != null)
                {
                    var ne1 = ComputeNEFromSigmaT(f.SigmaT, f.Sigma6, f.B, f.Kc, f.Kr, f.Ks, f.Kd);
                    var npl1 = ComputeNPL(ne1, f.CAM);

                    details.FondationName = fondationMtlh.MaterialName ?? "";
                    details.FondationOrder = fondationMtlh.Order;
                    details.SigmaTBase = f.SigmaT;
                    details.CAMFondation = f.CAM;
                    details.BFondation = f.B;
                    details.Sigma6Fondation = f.Sigma6;
                    details.KcFondation = f.Kc;
                    details.KrFondation = f.Kr;
                    details.KsFondation = f.Ks;
                    details.KdFondation = f.Kd;
                    details.NE1 = ne1;
                    details.NPL1 = npl1;
                }
            }

            // Etape 2 - Base MB
            if (baseMb != null)
            {
                var res = ComputeResidualEpsiTAtBaseOfMB(baseMb, fondationMtlh);
                var ne2 = ComputeNEFromEpsiT(res.epsT, res.eps6, res.b, res.kc, res.kr, res.ks, res.kt);
                var npl2 = ComputeNPL(ne2, res.cam);

                details.BaseName = baseMb.MaterialName ?? "";
                details.BaseOrder = baseMb.Order;
                details.EpsiTBaseResidual = res.epsT;
                details.CAMBase = res.cam;
                details.BBase = res.b;
                details.Epsilon6Base = res.eps6;
                details.KcBase = res.kc;
                details.KrBase = res.kr;
                details.KsBase = res.ks;
                details.KthetaBase = res.kt;
                details.NE2 = ne2;
                details.NPL2 = npl2;

                // Infos residuelles et session de calcul pour le recalcul
                details.RemodelingDetails = res.remodelingInfo;
                details.CalculationSummary = res.calcSummary; // remettre la session pour le recalcul
                // Texte structuré décrivant le clone (en complément de la table ModeledRows)
                details.StructureText = BuildStructuredText(res.clonedStructureSummary, res.layersSummaryLines, res.interfaceSummaryLines);

                // Tableau (modele)
                details.ModeledRows = BuildModeledRowsFor(AppState.CurrentProject.PavementStructure, baseMb.Order);
                // Les résultats recalculés détaillés ne sont pas renvoyés ici; laisser vide pour l'instant
                details.ResidualResults = BuildResidualResultsForStep2();
            }

            // Synthese
            double total = details.NPL1 + details.NPL2;
            double required = project.TraficCumuleNPL ?? 0;
            details.TotalNPL = total;
            details.TargetNPL = required;
            details.IsOk = (required > 0) && (total >= required);
            return details;
        }

        private static string BuildStructuredText(string header, List<string> layerLines, List<string> interfaceLines)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(header)) sb.AppendLine(header.Trim());
            if (layerLines != null && layerLines.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Detail des couches:");
                foreach (var line in layerLines)
                    sb.AppendLine(line);
            }
            if (interfaceLines != null && interfaceLines.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Interfaces:");
                foreach (var line in interfaceLines)
                    sb.AppendLine(line);
            }
            return sb.ToString();
        }

        private List<MixedModeledRowDto> BuildModeledRowsFor(PavementStructure source, int baseMbOrder)
        {
            var rows = new List<MixedModeledRowDto>();
            if (source?.Layers == null) return rows;
            var ordered = source.Layers.OrderBy(l => l.Order).ToList();
            foreach (var l in ordered)
            {
                // Couche
                rows.Add(new MixedModeledRowDto
                {
                    IsInterface = false,
                    IsPlateforme = l.Role == LayerRole.Plateforme,
                    Numero = l.Order,
                    Nature = l.Role.ToString(),
                    Materiau = l.MaterialName ?? string.Empty,
                    Famille = l.Family.ToString(),
                    Epaisseur_m = l.Thickness_m,
                    // Marqueur: si MTLH, E résiduel = E/5
                    Module_MPa = l.Family == MaterialFamily.MTLH ? Math.Max(1.0, l.Modulus_MPa / 5.0) : l.Modulus_MPa,
                    OriginalModule_MPa = l.Modulus_MPa,
                    ModuleChanged = l.Family == MaterialFamily.MTLH,
                    Poisson = l.Poisson
                });

                // Interface sous la couche (sauf PF)
                if (l.Role != LayerRole.Plateforme)
                {
                    var isBaseMb = l.Order == baseMbOrder;
                    var originalIface = l.InterfaceWithBelow?.ToString() ?? "Collee";
                    var residualIface = isBaseMb ? "Decollee" : originalIface;
                    rows.Add(new MixedModeledRowDto
                    {
                        IsInterface = true,
                        Numero = l.Order,
                        Nature = "Interface",
                        InterfaceWithBelow = residualIface,
                        OriginalInterfaceWithBelow = originalIface,
                        InterfaceChanged = isBaseMb && !string.Equals(originalIface, residualIface, StringComparison.OrdinalIgnoreCase)
                    });
                }
            }
            return rows;
        }

        // Retourne: (epsilon_t, CAM, B, epsilon6, kc, kr, ks, ktheta, kd, remodelingInfo, calcSummary, header, layerLines, interfaceLines)
        private (double epsT, double cam, double b, double eps6, double kc, double kr, double ks, double kt, double kd,
                 string remodelingInfo, string calcSummary, string clonedStructureSummary, List<string> layersSummaryLines, List<string> interfaceSummaryLines)
            ComputeResidualEpsiTAtBaseOfMB(Layer baseMb, Layer? fondationMtlh)
        {
            try
            {
                var project = AppState.CurrentProject;
                var structure = project?.PavementStructure;
                if (structure == null) return (0, 0, 0, 0, 1, 1, 1, 1, 1, string.Empty, string.Empty, string.Empty, new List<string>(), new List<string>());

                // Clone minimal
                var cloned = new PavementStructure
                {
                    NE = structure.NE,
                    StructureType = structure.StructureType,
                    ChargeReference = structure.ChargeReference,
                };

                foreach (var l in structure.Layers.OrderBy(l => l.Order))
                {
                    var c = new Layer
                    {
                        Order = l.Order,
                        Role = l.Role,
                        Family = l.Family,
                        MaterialName = l.MaterialName,
                        Thickness_m = l.Thickness_m,
                        Modulus_MPa = l.Modulus_MPa,
                        Poisson = l.Poisson,
                        InterfaceWithBelow = l.InterfaceWithBelow,
                        Mode = l.Mode,
                        CurrentStructureType = l.CurrentStructureType,
                        LibrarySh = l.LibrarySh,
                        LibraryEpsilon6 = l.LibraryEpsilon6
                    };
                    cloned.Layers.Add(c);
                }

                var remodelSb = new StringBuilder();
                var layerLines = new List<string>();
                var interfaceLines = new List<string>();

                // Set interface base/decollee
                var baseClone = cloned.Layers.FirstOrDefault(x => x.Order == baseMb.Order);
                if (baseClone != null)
                {
                    var before = baseClone.InterfaceWithBelow?.ToString() ?? "(defaut)";
                    baseClone.InterfaceWithBelow = InterfaceType.Decollee;
                    remodelSb.Append($"Interface {baseClone.MaterialName} / couche inferieure: {before} -> Decollee. ");
                }

                // Set residual E for MTLH (E/5)
                foreach (var l in cloned.Layers.Where(x => x.Family == MaterialFamily.MTLH))
                {
                    var oldE = l.Modulus_MPa;
                    l.Modulus_MPa = Math.Max(1.0, l.Modulus_MPa / 5.0);
                    remodelSb.Append($"MTLH '{l.MaterialName}': E {oldE:F0} -> {l.Modulus_MPa:F0} MPa. ");
                }

                // Header summary
                var header = new StringBuilder();
                header.AppendLine($"Structure: {cloned.Layers.Count} couches");
                header.AppendLine($"Charge: {(cloned.ChargeReference?.Type == ChargeType.RoueIsolee ? "Roue isolee" : "Jumelage")} ");
                if (cloned.ChargeReference != null)
                {
                    header.AppendLine($"Pression: {cloned.ChargeReference.PressionMPa:0.###} MPa");
                    header.AppendLine($"Rayon contact: {cloned.ChargeReference.RayonMetres:0.###} m");
                    header.AppendLine($"Distance roues: {cloned.ChargeReference.DistanceRouesMetres:0.###} m");
                }

                // Layer lines
                foreach (var l in cloned.Layers.OrderBy(x => x.Order))
                {
                    string iface = l.InterfaceWithBelow?.ToString() ?? "-";
                    layerLines.Add($"Couche {l.Order} ({l.Role}): E={l.Modulus_MPa:0} MPa, mu={l.Poisson:0.##}, h={l.Thickness_m:0.000} m");
                }
                // Build explicit interface list from upper layers
                var layersOrdered = cloned.Layers.OrderBy(x => x.Order).ToList();
                for (int i = 0; i < layersOrdered.Count - 1; i++)
                {
                    var up = layersOrdered[i];
                    var iface = up.InterfaceWithBelow?.ToString() ?? "Collee";
                    interfaceLines.Add($"Interface {i + 1}: {iface}");
                }

                // Recalculate solicitations on remodeled structure
                var calc = _calculationService.CalculateSolicitations(cloned);
                if (!calc.IsSuccessful)
                {
                    return (0, 0, 0, 0, 1, 1, 1, 1, 1, remodelSb.ToString().Trim(), "Calcul des sollicitations echoue", header.ToString().Trim(), layerLines, interfaceLines);
                }

                string calcSummary = calc.InputData?.GetSummary() ?? string.Empty;

                // Extract epsT bottom of base MB
                var lr = calc.LayerResults.FirstOrDefault(r => r.Layer.Order == baseMb.Order);
                double epsT = lr?.EpsilonTBottom ?? 0;
                if (epsT <= 0)
                {
                    return (0, 0, 0, 0, 1, 1, 1, 1, 1, remodelSb.ToString().Trim(), calcSummary, header.ToString().Trim(), layerLines, interfaceLines);
                }

                // Fatigue constants from VA
                var vaList = project.ValeursAdmissibles?.ToList() ?? new List<ValeurAdmissibleCoucheDto>();
                var va = vaList.FirstOrDefault(v => v.Niveau == baseMb.Order)
                         ?? vaList.FirstOrDefault(v => string.Equals(v.Materiau?.Trim(), (baseMb.MaterialName ?? string.Empty).Trim(),
                                                                 StringComparison.InvariantCultureIgnoreCase));
                double cam = va?.Cam ?? 0;
                double b = va?.B ?? 0;
                double eps6 = va?.Epsilon6 ?? 0;
                double kc = va?.Kc ?? 1;
                double kr = va?.Kr ?? 1;
                double ks = va?.Ks ?? 1;
                double kt = va?.Ktheta ?? 1;
                double kd = va?.Kd ?? 1;

                return (epsT, cam, b, eps6, kc, kr, ks, kt, kd, remodelSb.ToString().Trim(), calcSummary, header.ToString().Trim(), layerLines, interfaceLines);
            }
            catch
            {
                return (0, 0, 0, 0, 1, 1, 1, 1, 1, string.Empty, string.Empty, string.Empty, new List<string>(), new List<string>());
            }
        }

        private static ResultatInverseItem CloneForCriterion(ResultatInverseItem src, string crit)
        {
            return new ResultatInverseItem
            {
                Numero = src.Numero,
                Nature = src.Nature,
                Materiau = src.Materiau,
                Critere = crit,
                Module = src.Module,
                Poisson = src.Poisson,
                SigmaT = src.SigmaT,
                EpsilonT = src.EpsilonT,
                SigmaZ = src.SigmaZ,
                EpsilonZ = src.EpsilonZ,
                CAM = src.CAM,
                TypeAccroissement = src.TypeAccroissement,
                Risque = src.Risque,
                B = src.B,
                Epsilon6 = src.Epsilon6,
                Sigma6 = src.Sigma6,
                AAmplitude = src.AAmplitude,
                Kc = src.Kc,
                Kr = src.Kr,
                Ks = src.Ks,
                Ktheta = src.Ktheta,
                Kd = src.Kd,
                ValeurAdmissible = src.ValeurAdmissible
            };
        }
    }
}
