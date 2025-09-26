using System;
using System.Collections.Generic;
using System.Linq;
using UI_ChausseeNeuve.ViewModels;

namespace UI_ChausseeNeuve.Services
{
    /// <summary>
    /// Modèle normatif unifié pour les enrobés bitumineux (NF P98-086 / Alizé)
    /// E(?,f) = E(15°C,10Hz) * R(?,f) avec R(?,10Hz) dérivé automatiquement de la table EvsTemperature.
    /// R(?,f) pour f = 1 et 3 Hz est pris des tableaux normatifs. Pour autres fréquences, interpolation / extrapolation.
    /// </summary>
    internal static class NormativeBitumeModel
    {
        public static readonly int[] TemperatureGrid = { -10, 0, 10, 20, 30, 40 };
        private static readonly int[] BaseFreqs = { 1, 3, 10 }; // fréquences tabulées
        private const int FreqMax = 30; // limite sup d'utilisation (figure fréquence)

        // Tables partielles: pour chaque matériau normatif, R(?,f) uniquement pour f=1 et 3 Hz.
        // Les valeurs pour f=10 Hz sont reconstruites via EvsTemperature (garantit cohérence exacte).
        // Chaque double[] suit l'ordre TemperatureGrid.
        private static readonly Dictionary<string, Dictionary<int, double[]>> PartialRatioTable = new(StringComparer.OrdinalIgnoreCase)
        {
            // BBSG / BBA familles
            { "eb-bbsg1", new Dictionary<int,double[]>{{1,new[]{2.45,2.01,1.23,0.57,0.22,0.16}}, {3,new[]{2.57,2.09,1.27,0.60,0.23,0.17}} }},
            { "eb-bbsg2", new Dictionary<int,double[]>{{1,new[]{2.45,2.01,1.23,0.57,0.22,0.16}}, {3,new[]{2.57,2.09,1.27,0.60,0.23,0.17}} }},
            { "eb-bbsg3", new Dictionary<int,double[]>{{1,new[]{2.11,1.89,1.26,0.52,0.21,0.13}}, {3,new[]{2.24,1.96,1.31,0.54,0.22,0.14}} }},
            // BBME
            { "eb-bbme1", new Dictionary<int,double[]>{{1,new[]{1.69,1.44,0.99,0.44,0.21,0.15}}, {3,new[]{1.85,1.74,1.26,0.60,0.32,0.26}} }},
            { "eb-bbme2", new Dictionary<int,double[]>{{1,new[]{1.56,1.39,0.99,0.41,0.16,0.08}}, {3,new[]{1.66,1.51,1.14,0.49,0.25,0.19}} }},
            { "eb-bbme3", new Dictionary<int,double[]>{{1,new[]{1.56,1.39,0.99,0.41,0.16,0.08}}, {3,new[]{1.66,1.51,1.14,0.49,0.25,0.19}} }},
            // GB (2/3 regroupés) et GB4
            { "eb-gb2", new Dictionary<int,double[]>{{1,new[]{2.23,1.71,0.98,0.42,0.14,0.04}}, {3,new[]{2.37,1.86,1.13,0.53,0.20,0.07}} }},
            { "eb-gb3", new Dictionary<int,double[]>{{1,new[]{2.23,1.71,0.98,0.42,0.14,0.04}}, {3,new[]{2.37,1.86,1.13,0.53,0.20,0.07}} }},
            { "eb-gb4", new Dictionary<int,double[]>{{1,new[]{2.00,1.53,0.97,0.43,0.15,0.04}}, {3,new[]{2.13,1.66,1.11,0.54,0.22,0.07}} }},
            // EME (mêmes ratios pour 1 & 2)
            { "eb-eme1", new Dictionary<int,double[]>{{1,new[]{1.89,1.44,0.90,0.49,0.20,0.08}}, {3,new[]{2.00,1.57,1.04,0.61,0.29,0.13}} }},
            { "eb-eme2", new Dictionary<int,double[]>{{1,new[]{1.89,1.44,0.90,0.49,0.20,0.08}}, {3,new[]{2.00,1.57,1.04,0.61,0.29,0.13}} }},
        };

        /// <summary>
        /// Calcule E(?,f) si matériau MB normatif. Retourne null sinon (laisser autre logique).
        /// </summary>
        public static double? ComputeNormativeE(MaterialItem m, int temperatureC, int frequencyHz)
        {
            if (m == null || m.Name == null) return null;
            if (!string.Equals(m.Category, "MB", StringComparison.OrdinalIgnoreCase)) return null;
            string key = m.Name.ToLowerInvariant();
            if (!PartialRatioTable.ContainsKey(key)) return null; // pas de ratios => on laisse fallback (ex: bbm, bbtm, etc.)

            double e15 = GetE15_10(m);
            double rTheta10 = GetRTheta10(m, temperatureC, e15);
            double freqRatio = GetFrequencyRatio(key, temperatureC, frequencyHz, rTheta10);
            return e15 * rTheta10 * freqRatio;
        }

        private static double GetE15_10(MaterialItem m)
        {
            if (m.EvsTemperature != null && m.EvsTemperature.Count > 0)
            {
                if (m.EvsTemperature.TryGetValue(15, out var v)) return v;
                var temps = m.EvsTemperature.Keys.OrderBy(t => t).ToArray();
                if (temps.Length == 1) return m.EvsTemperature[temps[0]];
                if (15 <= temps.First()) return m.EvsTemperature[temps.First()];
                if (15 >= temps.Last()) return m.EvsTemperature[temps.Last()];
                int t0 = temps.Where(t => t < 15).Max();
                int t1 = temps.Where(t => t > 15).Min();
                double e0 = m.EvsTemperature[t0];
                double e1 = m.EvsTemperature[t1];
                return e0 + (e1 - e0) * (15 - t0) / (double)(t1 - t0);
            }
            return m.Modulus_MPa;
        }

        private static double GetRTheta10(MaterialItem m, int temperatureC, double e15)
        {
            double eTheta10 = InterpEvsTemperature(m, temperatureC);
            return eTheta10 / e15;
        }

        private static double InterpEvsTemperature(MaterialItem m, int t)
        {
            if (m.EvsTemperature == null || m.EvsTemperature.Count == 0) return m.Modulus_MPa;
            if (m.EvsTemperature.TryGetValue(t, out var exact)) return exact;
            var keys = m.EvsTemperature.Keys.OrderBy(k => k).ToArray();
            if (t <= keys.First()) return m.EvsTemperature[keys.First()];
            if (t >= keys.Last()) return m.EvsTemperature[keys.Last()];
            int k0 = keys.Where(k => k < t).Max();
            int k1 = keys.Where(k => k > t).Min();
            double e0 = m.EvsTemperature[k0];
            double e1 = m.EvsTemperature[k1];
            return e0 + (e1 - e0) * (t - k0) / (double)(k1 - k0);
        }

        private static double GetFrequencyRatio(string key, int temperatureC, int f, double rTheta10)
        {
            if (f == 10) return 1.0; // ratio fréquentiel = 1 à la fréquence de référence
            if (!PartialRatioTable.TryGetValue(key, out var freqDict)) return 1.0;

            double r1 = InterpRatioTemperature(freqDict, 1, temperatureC, rTheta10);
            double r3 = InterpRatioTemperature(freqDict, 3, temperatureC, rTheta10);
            double r10 = rTheta10; // R(?,10) obtenu dynamiquement

            if (f < 1) f = 1;
            if (f > FreqMax) f = FreqMax;

            if (f <= 1) return r1 / r10;
            if (f >= 10)
            {
                double mExp = ComputeExponent(r3, r10, 3, 10);
                double rF = r10 * Math.Pow(f / 10.0, mExp);
                return rF / r10;
            }
            if (f <= 3) return InterpLog(f, 1, 3, r1, r3) / r10;
            return InterpLog(f, 3, 10, r3, r10) / r10;
        }

        private static double InterpRatioTemperature(Dictionary<int,double[]> freqDict, int freq, int temperatureC, double fallback)
        {
            if (!freqDict.TryGetValue(freq, out var arr) || arr.Length != TemperatureGrid.Length) return fallback;
            int t = temperatureC;
            if (t <= TemperatureGrid.First()) return arr[0];
            if (t >= TemperatureGrid.Last()) return arr[^1];
            for (int i = 0; i < TemperatureGrid.Length - 1; i++)
            {
                int t0 = TemperatureGrid[i];
                int t1 = TemperatureGrid[i + 1];
                if (t0 <= t && t <= t1)
                {
                    double r0 = arr[i];
                    double r1 = arr[i + 1];
                    return r0 + (r1 - r0) * (t - t0) / (double)(t1 - t0);
                }
            }
            return arr[0];
        }

        private static double InterpLog(double f, double f0, double f1, double r0, double r1)
        {
            double lf = Math.Log(f);
            double lf0 = Math.Log(f0);
            double lf1 = Math.Log(f1);
            return r0 + (r1 - r0) * (lf - lf0) / (lf1 - lf0);
        }

        private static double ComputeExponent(double rA, double rB, double fA, double fB)
        {
            if (rA <= 0 || rB <= 0) return 0;
            return Math.Log(rB / rA) / Math.Log(fB / fA);
        }
    }
}
