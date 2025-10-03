using System;
using System.Collections.Generic;
using System.Linq;
using UI_ChausseeNeuve.ViewModels;

namespace UI_ChausseeNeuve.Services
{
    internal static class NormativeBitumeModel
    {
        public static readonly int[] TemperatureGrid = { -10, 0, 10, 20, 30, 40 };
        public static readonly int[] FrequencyGrid = { 1, 3, 10 };

        public static bool UseGraphFrequencyModel = true;  
        public static bool UseLogFrequencyModel = true;    

        public const int MaxFreqExtrapolated = 30;         
        public const double GraphMinFreq = 0.5;            
        public const double GraphMaxFreq = 40.0;           

        // Nouveau tableau complet des modules normatifs E(?,10Hz) (MPa) aux températures ? = -10,0,10,20,30,40 °C.
        // Valeurs issues du tableau fourni (ordre supposé des matériaux). Ajuster si nécessaire.
        // TODO: confirmer la ligne pour eb-bbme3 (actuellement copie de eb-bbme2 si différente fournir valeurs).
        private static readonly Dictionary<string, double[]> NormativeE10Full = new(StringComparer.OrdinalIgnoreCase)
        {
            { "eb-bbsg1", new[]{14800d,12000d,7315d,3685d,1300d,1000d} },
            { "eb-bbsg2", new[]{16000d,13500d,9310d,4690d,1800d,1000d} },
            { "eb-bbsg3", new[]{17300d,15400d,11970d,6030d,3000d,1900d} },
            { "eb-bbme1", new[]{19500d,18200d,14630d,7370d,3800d,2300d} },
            { "eb-bbme2", new[]{19500d,18200d,14630d,7370d,3800d,2300d} },
            { "eb-bbme3", new[]{19500d,18200d,14630d,7370d,3800d,2300d} }, // à valider
            { "bbm",      new[]{14800d,12000d,7315d,3685d,1300d,1000d} },
            { "bbtm",     new[]{8500d,7000d,4200d,1800d,1000d,800d} },
            { "bbdr",     new[]{8500d,7000d,4200d,1800d,1000d,800d} },
            { "acr",      new[]{14800d,12000d,7315d,3685d,1300d,1000d} },
            { "eb-gb2",   new[]{22800d,18300d,11880d,6120d,2700d,1000d} },
            { "eb-gb3",   new[]{22800d,18300d,11880d,6120d,2700d,1000d} },
            { "eb-gb4",   new[]{25300d,20000d,14300d,7700d,3500d,1200d} },
            { "eb-eme1",  new[]{30000d,24000d,16940d,11060d,6000d,3000d} },
            { "eb-eme2",  new[]{30000d,24000d,16940d,11060d,6000d,3000d} },
        };

        #region Données tabulaires Alizé (R(T,1) & R(T,3)) + overrides R(T,10)
        private static readonly Dictionary<string, Dictionary<int, double[]>> PartialRatioTable = new(StringComparer.OrdinalIgnoreCase)
        {
            { "eb-bbsg1", new Dictionary<int,double[]>{{1,new[]{2.37, 1.84, 0.99, 0.41, 0.11, 0.07}}, {3,new[]{2.52, 1.99, 1.14, 0.52, 0.16, 0.11}} }},
            { "eb-bbsg2", new Dictionary<int,double[]>{{1,new[]{2.01, 1.63, 0.99, 0.41, 0.15, 0.08}}, {3,new[]{2.14, 1.76, 1.14, 0.52, 0.17, 0.09}} }},
            { "eb-bbsg3", new Dictionary<int,double[]>{{1,new[]{2.01, 1.63, 0.99, 0.41, 0.15, 0.08}}, {3,new[]{2.14, 1.76, 1.14, 0.52, 0.17, 0.09}} }},
            { "eb-bbme1", new Dictionary<int,double[]>{{1,new[]{1.69, 1.44, 0.99, 0.41, 0.16, 0.08}}, {3,new[]{1.80, 1.56, 1.14, 0.52, 0.23, 0.13}} }},
            { "eb-bbme2", new Dictionary<int,double[]>{{1,new[]{1.56, 1.39, 0.99, 0.41, 0.16, 0.08}}, {3,new[]{1.66, 1.51, 1.14, 0.52, 0.23, 0.13}} }},
            { "eb-bbme3", new Dictionary<int,double[]>{{1,new[]{1.56, 1.39, 0.99, 0.41, 0.16, 0.08}}, {3,new[]{1.66, 1.51, 1.14, 0.52, 0.23, 0.13}} }},
            { "eb-gb2",  new Dictionary<int,double[]>{{1,new[]{2.23,1.71,0.98,0.42,0.14,0.04}}, {3,new[]{2.37,1.86,1.13,0.53,0.20,0.07}} }},
            { "eb-gb3",  new Dictionary<int,double[]>{{1,new[]{2.23,1.71,0.98,0.42,0.14,0.04}}, {3,new[]{2.37,1.86,1.13,0.53,0.20,0.07}} }},
            { "eb-gb4",  new Dictionary<int,double[]>{{1,new[]{2.00,1.53,0.97,0.43,0.15,0.04}}, {3,new[]{2.13,1.66,1.11,0.54,0.22,0.07}} }},
            { "eb-eme1", new Dictionary<int,double[]>{{1,new[]{1.89,1.44,0.90,0.49,0.20,0.08}}, {3,new[]{2.00,1.57,1.04,0.61,0.29,0.13}} }},
            { "eb-eme2", new Dictionary<int,double[]>{{1,new[]{1.89,1.44,0.90,0.49,0.20,0.08}}, {3,new[]{2.00,1.57,1.04,0.61,0.29,0.13}} }},
        };

        private static readonly Dictionary<string, double[]> R10Override = new(StringComparer.OrdinalIgnoreCase)
        {
            { "eb-bbsg1", new[]{ 2.69, 2.18, 1.33, 0.67, 0.24, 0.18 } },
            { "eb-bbsg2", new[]{ 2.29, 1.93, 1.33, 0.67, 0.26, 0.14 } },
            { "eb-bbsg3", new[]{ 2.29, 1.93, 1.33, 0.67, 0.26, 0.14 } },
            { "eb-bbme1", new[]{ 1.92, 1.71, 1.33, 0.67, 0.33, 0.21 } },
            { "eb-bbme2", new[]{ 1.77, 1.65, 1.33, 0.67, 0.35, 0.21 } },
            { "eb-bbme3", new[]{ 1.77, 1.65, 1.33, 0.67, 0.35, 0.21 } },
            { "eb-gb2",  new[]{2.53,2.03,1.32,0.68,0.30,0.11} },
            { "eb-gb3",  new[]{2.53,2.03,1.32,0.68,0.30,0.11} },
            { "eb-gb4",  new[]{2.27,1.82,1.30,0.70,0.32,0.11} },
            { "eb-eme1", new[]{2.14,1.71,1.21,0.79,0.43,0.21} },
            { "eb-eme2", new[]{2.14,1.71,1.21,0.79,0.43,0.21} },
        };
        #endregion

        #region Données courbes graphiques génériques (T: -5,5,15,30,40 ; f: 2,10,30 ou 2..30 pas 2)
        private static readonly int[] GraphTempNodes = { -5, 5, 15, 30, 40 };
        private static readonly int[] GraphFreqNodesLegacy = { 2, 10, 30 };
        private static readonly int[] GraphFreqNodesFull = { 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30 };

        private static readonly Dictionary<int, double[]> GraphFrequencyCurves = new()
        {
            // -5°C : ratios calculés à partir des données Alizé eb-bbsg2 (E(f)/E(10Hz)) - E(10Hz) = 14750 MPa
            { -5, new[]{ 0.9146, 0.9504, 0.9721, 0.9877, 1.0000, 1.0102, 1.0188, 1.0264, 1.0332, 1.0393, 1.0447, 1.0498, 1.0530, 1.0588, 1.0628 } },
            // 5°C : ratios calculés à partir des données Alizé eb-bbsg2 (E(f)/E(10Hz)) - E(10Hz) = 11405 MPa
            { 5, new[]{ 0.8595, 0.9175, 0.9531, 0.9793, 1.0000, 1.0173, 1.0322, 1.0452, 1.0568, 1.0673, 1.0769, 1.0857, 1.0940, 1.1017, 1.1089 } },
            // 15°C : ratios calculés à partir des données Alizé eb-bbsg2 (E(f)/E(10Hz)) - E(10Hz) = 7000 MPa
            { 15, new[]{ 0.7674, 0.8601, 0.9194, 0.9640, 1.0000, 1.0304, 1.0540, 1.0804, 1.1016, 1.1207, 1.1384, 1.1549, 1.1701, 1.1846, 1.1981 } },
            // 30°C : ratios calculés à partir des données Alizé eb-bbsg2 (E(f)/E(10Hz)) - E(10Hz) = 1800 MPa
            { 30, new[]{ 0.5944, 0.7439, 0.8478, 0.9306, 1.0000, 1.0606, 1.1150, 1.1639, 1.2089, 1.2506, 1.2900, 1.3267, 1.3617, 1.3944, 1.4256 } },
            // 40°C : ratios calculés à partir des données Alizé eb-bbsg2 (E(f)/E(10Hz)) - E(10Hz) = 1000 MPa
            { 40, new[]{ 0.5180, 0.6880, 0.8120, 0.9130, 1.0000, 1.0770, 1.1470, 1.2120, 1.2710, 1.3270, 1.3800, 1.4300, 1.4770, 1.5230, 1.5660 } },
        };
        #endregion

        public static double? ComputeNormativeE(MaterialItem m, int temperatureC, int frequencyHz)
        {
            if (m == null || m.Name == null) return null;
            if (!string.Equals(m.Category, "MB", StringComparison.OrdinalIgnoreCase)) return null;

            if (UseGraphFrequencyModel)
                return ComputeGraphFrequencyModel(m, temperatureC, frequencyHz);

            string key = m.Name.ToLowerInvariant();
            if (!PartialRatioTable.ContainsKey(key)) return null;

            if (UseLogFrequencyModel)
                return ComputeLogFrequencyModel(key, m, temperatureC, frequencyHz);
            else
                return ComputeBilinearModel(key, m, temperatureC, frequencyHz);
        }

        #region MODE COURBES GRAPHIQUES
        private static double? ComputeGraphFrequencyModel(MaterialItem m, int temperatureC, int frequencyHz)
        {
            double eTheta10 = GetBaseE10OrMaterial(m, temperatureC); 
            double ratio = GetGraphRatio(temperatureC, frequencyHz);
            // Arrondir le résultat à l'entier le plus proche
            return Math.Round(eTheta10 * ratio);
        }

        private static double GetBaseE10OrMaterial(MaterialItem m, int temperatureC)
        {
            string key = m.Name?.ToLowerInvariant() ?? string.Empty;
            if (NormativeE10Full.TryGetValue(key, out var arr) && arr.Length == TemperatureGrid.Length)
            {
                return InterpOnGrid(arr, temperatureC);
            }
            return InterpEvsTemperature(m, temperatureC);
        }

        private static double GetGraphRatio(int temperatureC, int frequencyHz)
        {
            double f = frequencyHz;
            if (f < GraphMinFreq) f = GraphMinFreq;
            if (f > GraphMaxFreq) f = GraphMaxFreq;

            if (GraphFrequencyCurves.TryGetValue(temperatureC, out var ratiosExact))
                return InterpGraphFreq(ratiosExact, f);

            int T0 = GraphTempNodes.First();
            int T1 = GraphTempNodes.Last();
            if (temperatureC <= T0) return InterpGraphFreq(GraphFrequencyCurves[T0], f);
            if (temperatureC >= T1) return InterpGraphFreq(GraphFrequencyCurves[T1], f);
            for (int i = 0; i < GraphTempNodes.Length - 1; i++)
            {
                if (GraphTempNodes[i] <= temperatureC && temperatureC <= GraphTempNodes[i + 1]) { T0 = GraphTempNodes[i]; T1 = GraphTempNodes[i + 1]; break; }
            }
            double aT = (temperatureC - T0) / (double)(T1 - T0);
            double r0 = InterpGraphFreq(GraphFrequencyCurves[T0], f);
            double r1 = InterpGraphFreq(GraphFrequencyCurves[T1], f);
            return r0 + aT * (r1 - r0);
        }

        private static double InterpGraphFreq(double[] ratios, double f)
        {
            // Mode 3 points (2,10,30) - format legacy
            if (ratios.Length == 3)
            {
                double r2 = ratios[0];
                double r10 = ratios[1];
                double r30 = ratios[2];

                if (Math.Abs(f - 2) < 1e-9) return r2;
                if (Math.Abs(f - 10) < 1e-9) return r10;
                if (Math.Abs(f - 30) < 1e-9) return r30;

                if (f <= 10)
                {
                    double m210 = LogSlope(r2, r10, 2.0, 10.0);
                    return r2 * Math.Pow(f / 2.0, m210);
                }
                else
                {
                    double m1030 = LogSlope(r10, r30, 10.0, 30.0);
                    return r10 * Math.Pow(f / 10.0, m1030);
                }
            }

            // Mode étendu 15 points (2..30 pas 2) - format Alizé
            if (ratios.Length == GraphFreqNodesFull.Length)
            {
                var nodes = GraphFreqNodesFull;

                if (f <= nodes[0]) return ratios[0];
                if (f >= nodes[^1]) return ratios[^1];

                // Valeur exacte ?
                for (int i = 0; i < nodes.Length; i++)
                    if (Math.Abs(f - nodes[i]) < 1e-9)
                        return ratios[i];

                // Interpolation log-log entre points adjacents
                for (int i = 0; i < nodes.Length - 1; i++)
                {
                    int f0 = nodes[i];
                    int f1 = nodes[i + 1];
                    if (f0 <= f && f <= f1)
                    {
                        double r0 = ratios[i];
                        double r1 = ratios[i + 1];
                        if (r0 > 0 && r1 > 0)
                        {
                            double m = LogSlope(r0, r1, f0, f1);
                            return r0 * Math.Pow(f / f0, m);
                        }
                        // fallback linéaire si problème
                        double a = (f - f0) / (double)(f1 - f0);
                        return r0 + a * (r1 - r0);
                    }
                }
                return ratios[^1];
            }

            // Format inattendu - fallback sur valeur médiane
            return ratios.Length > 0 ? ratios[ratios.Length / 2] : 1.0;
        }
        private static double LogSlope(double ra, double rb, double fa, double fb)
        {
            if (ra <= 0 || rb <= 0) return 0.0;
            return Math.Log(rb / ra) / Math.Log(fb / fa);
        }
        #endregion

        #region MODE LOG FREQUENCE 1-3-10
        private static double? ComputeLogFrequencyModel(string key, MaterialItem m, int temperatureC, int frequencyHz)
        {
            double e15 = GetE15_10(m);
            double R1 = InterpTemperatureColumn(key, 1, temperatureC, 1.0, m, e15);
            double R3 = InterpTemperatureColumn(key, 3, temperatureC, R1, m, e15);
            double R10 = InterpTemperatureColumn(key, 10, temperatureC, R3, m, e15);
            int f = frequencyHz;
            if (f < 1) f = 1;
            if (f > MaxFreqExtrapolated) f = MaxFreqExtrapolated;
            if (f == 1) return Math.Round(e15 * R1);
            if (f == 3) return Math.Round(e15 * R3);
            if (f == 10) return Math.Round(e15 * R10);
            double m13 = LogSlope(R1, R3, 1, 3);
            double m310 = LogSlope(R3, R10, 3, 10);
            double Rf = f < 3 ? R1 * Math.Pow(f / 1.0, m13) : (f < 10 ? R3 * Math.Pow(f / 3.0, m310) : R10 * Math.Pow(f / 10.0, m310));
            // Arrondir le résultat final à l'entier le plus proche
            return Math.Round(e15 * Rf);
        }
        private static double InterpTemperatureColumn(string key, int freqNode, int temperatureC, double fallback, MaterialItem m, double e15)
        {
            if (freqNode == 10)
            {
                if (R10Override.TryGetValue(key, out var arrOv) && arrOv.Length == TemperatureGrid.Length)
                    return InterpOnGrid(arrOv, temperatureC);
                double eTheta = InterpEvsTemperature(m, temperatureC);
                return e15 == 0 ? 1.0 : eTheta / e15;
            }
            if (!PartialRatioTable.TryGetValue(key, out var dict) || !dict.TryGetValue(freqNode, out var arr) || arr.Length != TemperatureGrid.Length)
                return fallback;
            return InterpOnGrid(arr, temperatureC);
        }
        #endregion

        #region MODE BILINEAIRE
        private static double? ComputeBilinearModel(string key, MaterialItem m, int temperatureC, int frequencyHz)
        {
            temperatureC = ClampTemperature(temperatureC);
            frequencyHz = ClampFrequency(frequencyHz);
            double e15 = GetE15_10(m);
            if (IsGridTemperature(temperatureC) && IsGridFrequency(frequencyHz))
            {
                double rNode = GetRatioAtNode_Bilinear(key, m, temperatureC, frequencyHz, e15);
                // Arrondir le résultat à l'entier le plus proche
                return Math.Round(e15 * rNode);
            }
            int T0 = TemperatureGrid[0];
            int T1 = TemperatureGrid[^1];
            for (int i = 0; i < TemperatureGrid.Length - 1; i++)
                if (TemperatureGrid[i] <= temperatureC && temperatureC <= TemperatureGrid[i + 1]) { T0 = TemperatureGrid[i]; T1 = TemperatureGrid[i + 1]; break; }
            int f0 = FrequencyGrid[0];
            int f1 = FrequencyGrid[^1];
            for (int iF = 0; iF < FrequencyGrid.Length - 1; iF++)
                if (FrequencyGrid[iF] <= frequencyHz && frequencyHz <= FrequencyGrid[iF + 1]) { f0 = FrequencyGrid[iF]; f1 = FrequencyGrid[iF + 1]; break; }
            if (f0 == f1 && T0 == T1)
                // Arrondir le résultat à l'entier le plus proche
                return Math.Round(e15 * GetRatioAtNode_Bilinear(key, m, T0, f0, e15));
            if (f0 == f1)
            {
                double rT0 = GetRatioAtNode_Bilinear(key, m, T0, f0, e15);
                double rT1 = GetRatioAtNode_Bilinear(key, m, T1, f0, e15);
                double a = (temperatureC - T0) / (double)(T1 - T0);
                // Arrondir le résultat à l'entier le plus proche
                return Math.Round(e15 * (rT0 + a * (rT1 - rT0)));
            }
            if (T0 == T1)
            {
                double rF0 = GetRatioAtNode_Bilinear(key, m, T0, f0, e15);
                double rF1 = GetRatioAtNode_Bilinear(key, m, T0, f1, e15);
                double b = (frequencyHz - f0) / (double)(f1 - f0);
                // Arrondir le résultat à l'entier le plus proche
                return Math.Round(e15 * (rF0 + b * (rF1 - rF0)));
            }
            double R_T0_f0 = GetRatioAtNode_Bilinear(key, m, T0, f0, e15);
            double R_T1_f0 = GetRatioAtNode_Bilinear(key, m, T1, f0, e15);
            double R_T0_f1 = GetRatioAtNode_Bilinear(key, m, T0, f1, e15);
            double R_T1_f1 = GetRatioAtNode_Bilinear(key, m, T1, f1, e15);
            double alpha = (temperatureC - T0) / (double)(T1 - T0);
            double beta = (frequencyHz - f0) / (double)(f1 - f0);
            double rBilinear = R_T0_f0 * (1 - alpha) * (1 - beta) + R_T1_f0 * alpha * (1 - beta) + R_T0_f1 * (1 - alpha) * beta + R_T1_f1 * alpha * beta;
            // Arrondir le résultat final à l'entier le plus proche
            return Math.Round(e15 * rBilinear);
        }
        private static double GetRatioAtNode_Bilinear(string key, MaterialItem m, int temperatureNode, int freqNode, double e15)
        {
            if (freqNode == 10)
            {
                if (NormativeE10Full.TryGetValue(key, out var eArr) && eArr.Length == TemperatureGrid.Length)
                {
                    int idx = Array.IndexOf(TemperatureGrid, temperatureNode);
                    if (idx >= 0)
                    {
                        double eTheta10 = eArr[idx];
                        return e15 == 0 ? 1.0 : eTheta10 / e15;
                    }
                }
                double eTheta10Fallback = InterpEvsTemperature(m, temperatureNode);
                return e15 == 0 ? 1.0 : eTheta10Fallback / e15;
            }
            if (PartialRatioTable.TryGetValue(key, out var dict) && dict.TryGetValue(freqNode, out var arr) && arr.Length == TemperatureGrid.Length)
            {
                int idx = Array.IndexOf(TemperatureGrid, temperatureNode);
                if (idx >= 0) return arr[idx];
            }
            return 1.0;
        }
        #endregion

        #region Commun
        private static bool IsGridTemperature(int t) => Array.IndexOf(TemperatureGrid, t) >= 0;
        private static bool IsGridFrequency(int f) => Array.IndexOf(FrequencyGrid, f) >= 0;
        private static int ClampTemperature(int t)
        {
            if (t <= TemperatureGrid.First()) return TemperatureGrid.First();
            if (t >= TemperatureGrid.Last()) return TemperatureGrid.Last();
            return t;
        }
        private static int ClampFrequency(int f)
        {
            if (f <= FrequencyGrid.First()) return FrequencyGrid.First();
            if (f >= FrequencyGrid.Last()) return FrequencyGrid.Last();
            return f;
        }
        private static double GetE15_10(MaterialItem m)
        {
            string key = m.Name?.ToLowerInvariant() ?? string.Empty;
            if (NormativeE10Full.TryGetValue(key, out var arr) && arr.Length == TemperatureGrid.Length)
            {
                // Trouver interpolation autour de 15°C dans la grille normée
                return InterpOnGrid(arr, 15);
            }
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
        private static double InterpEvsTemperature(MaterialItem m, int t)
        {
            string key = m.Name?.ToLowerInvariant() ?? string.Empty;
            if (NormativeE10Full.TryGetValue(key, out var arr) && arr.Length == TemperatureGrid.Length)
            {
                return InterpOnGrid(arr, t);
            }
            if (m.EvsTemperature == null || m.EvsTemperature.Count == 0) return m.Modulus_MPa;
            if (m.EvsTemperature.TryGetValue(t, out var exact)) return exact;
            var keys = m.EvsTemperature.Keys.OrderBy(k => k).ToArray();
            if (t <= keys.First()) return m.EvsTemperature[keys.First()];
            if (t >= keys.Last()) return m.EvsTemperature[keys.Last()];
            int k0 = keys.Where(k => k < t).Max();
            int k1 = keys.Where(k => k > t).Min();
            double ev0 = m.EvsTemperature[k0];
            double ev1 = m.EvsTemperature[k1];
            return ev0 + (ev1 - ev0) * (t - k0) / (double)(k1 - k0);
        }

        private static double InterpOnGrid(double[] arr, int t)
        {
            if (t <= TemperatureGrid.First()) return arr[0];
            if (t >= TemperatureGrid.Last()) return arr[^1];
            for (int i = 0; i < TemperatureGrid.Length - 1; i++)
            {
                int T0 = TemperatureGrid[i];
                int T1 = TemperatureGrid[i + 1];
                if (T0 <= t && t <= T1)
                {
                    double e0 = arr[i];
                    double e1 = arr[i + 1];
                    double a = (t - T0) / (double)(T1 - T0);
                    return e0 + a * (e1 - e0);
                }
            }
            return arr[0];
        }
        #endregion
    }
}
