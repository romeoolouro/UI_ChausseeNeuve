using System;
using System.Collections.Generic;
using System.Linq;
using ChausseeNeuve.Domain.Models;
using UI_ChausseeNeuve.Services;

namespace UI_ChausseeNeuve.ViewModels
{
    public partial class ResultatViewModel
    {
        /// <summary>
        /// Construit le tableau des sollicitations pour la structure résiduelle (Etape 2)
        /// - Interface Base MB / couche inférieure : Décollée
        /// - Couches MTLH : E -> E/5
        /// </summary>
        internal List<ResultatCouche> BuildResidualResultsForStep2()
        {
            var list = new List<ResultatCouche>();
            try
            {
                var project = AppState.CurrentProject;
                var structure = project?.PavementStructure;
                if (structure?.Layers == null || structure.Layers.Count == 0) return list;

                // Identifier Base MB
                var baseMb = structure.Layers
                    .Where(l => l.Role == LayerRole.Base && l.Family == MaterialFamily.BetonBitumineux)
                    .OrderBy(l => l.Order)
                    .LastOrDefault();
                if (baseMb == null) return list;

                // Cloner la structure et appliquer les conditions résiduelles
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

                // Interface décollée sous la Base MB
                var baseClone = cloned.Layers.FirstOrDefault(x => x.Order == baseMb.Order);
                if (baseClone != null)
                    baseClone.InterfaceWithBelow = InterfaceType.Decollee;

                // E/5 pour toutes les MTLH
                foreach (var l in cloned.Layers.Where(x => x.Family == MaterialFamily.MTLH))
                    l.Modulus_MPa = Math.Max(1.0, l.Modulus_MPa / 5.0);

                // Calculer les sollicitations sur la structure résiduelle
                var calc = _calculationService.CalculateSolicitations(cloned);
                if (!calc.IsSuccessful || calc.LayerResults.Count == 0) return list;

                // Conversion vers ResultatCouche pour affichage tableau (style section Résultats)
                var orderedLayers = cloned.Layers.Where(l => l.Role != LayerRole.Plateforme).OrderBy(l => l.Order).ToList();
                var platform = cloned.Layers.FirstOrDefault(l => l.Role == LayerRole.Plateforme);

                // Niveaux en cm
                double cumul = 0;
                var topLevels = new Dictionary<int, double>();
                foreach (var l in orderedLayers)
                {
                    topLevels[l.Order] = cumul;
                    cumul += l.Thickness_m * 100.0;
                }

                int numero = 1;
                foreach (var l in orderedLayers)
                {
                    var lr = calc.LayerResults.FirstOrDefault(x => x.Layer.Order == l.Order);
                    if (lr == null) continue;

                    list.Add(new ResultatCouche
                    {
                        Numero = numero++,
                        Interface = l.Role.ToString(),
                        Materiau = l.MaterialName ?? string.Empty,
                        NiveauSup = topLevels.TryGetValue(l.Order, out var ns) ? ns : 0,
                        NiveauInf = (topLevels.TryGetValue(l.Order, out var ns2) ? ns2 : 0) + l.Thickness_m * 100.0,
                        Module = lr.Module,
                        CoefficientPoisson = lr.CoefficientPoisson,
                        SigmaTSup = lr.SigmaTTop,
                        SigmaTInf = lr.SigmaTBottom,
                        EpsilonTSup = lr.EpsilonTTop,
                        EpsilonTInf = lr.EpsilonTBottom,
                        SigmaZ = lr.SigmaZTop,
                        EpsilonZ = lr.EpsilonZTop,
                        SigmaZSup = lr.SigmaZTop,
                        SigmaZInf = lr.SigmaZBottom,
                        EpsilonZSup = lr.EpsilonZTop,
                        EpsilonZInf = lr.EpsilonZBottom,
                        DeflexionSup = lr.DeflectionTop,
                        DeflexionInf = lr.DeflectionBottom,
                        ValeurAdmissible = 0,
                        HasValeurAdmissible = false,
                        EstValide = true
                    });
                }

                if (platform != null)
                {
                    var lr = calc.LayerResults.FirstOrDefault(x => x.Layer.Order == platform.Order);
                    if (lr != null)
                    {
                        list.Add(new ResultatCouche
                        {
                            Numero = orderedLayers.Count + 1,
                            Interface = platform.Role.ToString(),
                            Materiau = platform.MaterialName ?? string.Empty,
                            NiveauSup = cumul,
                            NiveauInf = double.PositiveInfinity,
                            Module = lr.Module,
                            CoefficientPoisson = lr.CoefficientPoisson,
                            SigmaTSup = lr.SigmaTTop,
                            SigmaTInf = lr.SigmaTBottom,
                            EpsilonTSup = lr.EpsilonTTop,
                            EpsilonTInf = lr.EpsilonTBottom,
                            SigmaZ = lr.SigmaZTop,
                            EpsilonZ = lr.EpsilonZTop,
                            SigmaZSup = lr.SigmaZTop,
                            SigmaZInf = lr.SigmaZBottom,
                            EpsilonZSup = lr.EpsilonZTop,
                            EpsilonZInf = lr.EpsilonZBottom,
                            DeflexionSup = lr.DeflectionTop,
                            DeflexionInf = lr.DeflectionBottom,
                            ValeurAdmissible = 0,
                            HasValeurAdmissible = false,
                            EstValide = true
                        });
                    }
                }
            }
            catch { }

            return list;
        }
    }
}
