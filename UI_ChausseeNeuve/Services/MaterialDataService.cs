using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using UI_ChausseeNeuve.ViewModels;
using ChausseeNeuve.Domain.Models;

namespace UI_ChausseeNeuve.Services
{
    /// <summary>
    /// Service pour charger et gérer les données de matériaux depuis des fichiers JSON
    /// </summary>
    public class MaterialDataService
    {
        private readonly Dictionary<string, List<MaterialItem>> _materialCache = new();

        /// <summary>
        /// Charge les matériaux pour une bibliothèque donnée
        /// </summary>
        public async Task<List<MaterialItem>> LoadMaterialsAsync(string libraryName)
        {
            // Vérifier le cache
            if (_materialCache.ContainsKey(libraryName))
            {
                return _materialCache[libraryName];
            }

            try
            {
                string dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", $"{libraryName}.json");

                // Si le fichier n'existe pas, retourner des données par défaut
                if (!File.Exists(dataPath))
                {
                    var defaultMaterials = GetDefaultMaterials(libraryName);
                    _materialCache[libraryName] = defaultMaterials;
                    return defaultMaterials;
                }

                string jsonContent = await File.ReadAllTextAsync(dataPath);
                var libraryData = JsonSerializer.Deserialize<MaterialLibraryData>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var materials = libraryData?.Materials ?? GetDefaultMaterials(libraryName);
                _materialCache[libraryName] = materials;
                return materials;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement des matériaux pour {libraryName}: {ex.Message}");
                var defaultMaterials = GetDefaultMaterials(libraryName);
                _materialCache[libraryName] = defaultMaterials;
                return defaultMaterials;
            }
        }

        /// <summary>
        /// Filtre les matériaux par catégorie
        /// </summary>
        public IEnumerable<MaterialItem> FilterByCategory(IEnumerable<MaterialItem> materials, string category)
        {
            return materials.Where(m => string.Equals(m.Category, category, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Obtient des matériaux par défaut si les fichiers JSON n'existent pas
        /// </summary>
        private List<MaterialItem> GetDefaultMaterials(string libraryName)
        {
            return libraryName switch
            {
                "MateriauxBenin" => GetMateriauxBeninDefaults(),
                "CatalogueSenegalais" => GetCatalogueSenegalaisDefaults(),
                "CatalogueFrancais1998" => GetCatalogueFrancaisDefaults(),
                "NFP98_086_2019" => GetNFP98Defaults(),
                "MateriauxUser" => new List<MaterialItem>(),
                _ => new List<MaterialItem>()
            };
        }

        private List<MaterialItem> GetMateriauxBeninDefaults()
        {
            return new List<MaterialItem>
            {
                // MB - Matériaux Bitumineux
                new() { Name = "Enrobé Dense Bénin", Category = "MB", MaterialFamily = MaterialFamily.Bibliotheque,
                       Modulus_MPa = 3000, PoissonRatio = 0.35, MinThickness_m = 0.04, MaxThickness_m = 0.08,
                       Source = "Catalogue Bénin", Description = "Enrobé bitumineux dense standard" },
                new() { Name = "Grave Bitume Bénin", Category = "MB", MaterialFamily = MaterialFamily.Bibliotheque,
                       Modulus_MPa = 4500, PoissonRatio = 0.35, MinThickness_m = 0.06, MaxThickness_m = 0.12,
                       Source = "Catalogue Bénin", Description = "Grave bitume pour assise" },

                // MTLH - Matériaux Traités aux Liants Hydrauliques  
                new() { Name = "Grave Ciment Bénin", Category = "MTLH", MaterialFamily = MaterialFamily.MTLH,
                       Modulus_MPa = 8000, PoissonRatio = 0.25, MinThickness_m = 0.15, MaxThickness_m = 0.25,
                       Source = "Catalogue Bénin", Description = "Grave traitée au ciment" },
                new() { Name = "Sable Ciment Bénin", Category = "MTLH", MaterialFamily = MaterialFamily.MTLH,
                       Modulus_MPa = 6000, PoissonRatio = 0.25, MinThickness_m = 0.10, MaxThickness_m = 0.20,
                       Source = "Catalogue Bénin", Description = "Sable traité au ciment" },

                // Béton
                new() { Name = "Béton Ciment Bénin", Category = "Beton", MaterialFamily = MaterialFamily.BetonCiment,
                       Modulus_MPa = 25000, PoissonRatio = 0.20, MinThickness_m = 0.15, MaxThickness_m = 0.30,
                       Source = "Catalogue Bénin", Description = "Béton de ciment standard" },

                // Sol & GNT
                new() { Name = "GNT 0/31.5 Bénin", Category = "Sol_GNT", MaterialFamily = MaterialFamily.GNT,
                       Modulus_MPa = 300, PoissonRatio = 0.35, MinThickness_m = 0.15, MaxThickness_m = 0.40,
                       Source = "Catalogue Bénin", Description = "Grave non traitée 0/31.5" },
                new() { Name = "Sol Support Bénin", Category = "Sol_GNT", MaterialFamily = MaterialFamily.GNT,
                       Modulus_MPa = 50, PoissonRatio = 0.35, MinThickness_m = null, MaxThickness_m = null,
                       Source = "Catalogue Bénin", Description = "Sol de plateforme" }
            };
        }

        private List<MaterialItem> GetCatalogueSenegalaisDefaults()
        {
            return new List<MaterialItem>
            {
                new() { Name = "Enrobé Sénégalais", Category = "MB", MaterialFamily = MaterialFamily.Bibliotheque,
                       Modulus_MPa = 3500, PoissonRatio = 0.35, MinThickness_m = 0.04, MaxThickness_m = 0.08,
                       Source = "Catalogue Sénégal", Description = "Enrobé standard Sénégal" },
                new() { Name = "Grave Ciment Sénégal", Category = "MTLH", MaterialFamily = MaterialFamily.MTLH,
                       Modulus_MPa = 9000, PoissonRatio = 0.25, MinThickness_m = 0.15, MaxThickness_m = 0.25,
                       Source = "Catalogue Sénégal", Description = "Grave ciment Sénégal" },
                new() { Name = "Béton Sénégalais", Category = "Beton", MaterialFamily = MaterialFamily.BetonCiment,
                       Modulus_MPa = 28000, PoissonRatio = 0.20, MinThickness_m = 0.15, MaxThickness_m = 0.30,
                       Source = "Catalogue Sénégal", Description = "Béton standard Sénégal" },
                new() { Name = "GNT Sénégalaise", Category = "Sol_GNT", MaterialFamily = MaterialFamily.GNT,
                       Modulus_MPa = 350, PoissonRatio = 0.35, MinThickness_m = 0.15, MaxThickness_m = 0.40,
                       Source = "Catalogue Sénégal", Description = "GNT standard Sénégal" }
            };
        }

        private List<MaterialItem> GetCatalogueFrancaisDefaults()
        {
            return new List<MaterialItem>
            {
                new() { Name = "Enrobé BBSG 0/14", Category = "MB", MaterialFamily = MaterialFamily.Bibliotheque,
                       Modulus_MPa = 5400, PoissonRatio = 0.35, MinThickness_m = 0.06, MaxThickness_m = 0.08,
                       Source = "Catalogue Français 1998", Description = "Béton bitumineux semi-grenu" },
                new() { Name = "Grave Ciment GC", Category = "MTLH", MaterialFamily = MaterialFamily.MTLH,
                       Modulus_MPa = 12000, PoissonRatio = 0.25, MinThickness_m = 0.15, MaxThickness_m = 0.30,
                       Source = "Catalogue Français 1998", Description = "Grave ciment classe 2" },
                new() { Name = "Béton BC5", Category = "Beton", MaterialFamily = MaterialFamily.BetonCiment,
                       Modulus_MPa = 32000, PoissonRatio = 0.20, MinThickness_m = 0.20, MaxThickness_m = 0.30,
                       Source = "Catalogue Français 1998", Description = "Béton de ciment classe 5" },
                new() { Name = "GNT 0/20", Category = "Sol_GNT", MaterialFamily = MaterialFamily.GNT,
                       Modulus_MPa = 400, PoissonRatio = 0.35, MinThickness_m = 0.15, MaxThickness_m = 0.50,
                       Source = "Catalogue Français 1998", Description = "Grave non traitée 0/20" }
            };
        }

        private List<MaterialItem> GetNFP98Defaults()
        {
            // Matériaux bitumineux NF P98-086 (valeurs exactes Alizé à T=15°C)
            return new List<MaterialItem>
            {
                // eb-bbsg1
                new() { Statut = "system", Name = "eb-bbsg1", 
                       Modulus_MPa = 5633, // Valeur exacte Alizé à 15°C
                       PoissonRatio = 0.35, 
                       Epsi0_10C = 100, InverseB = 5, SN = 5, 
                       Sh = 0.25, Kc = 1.1, 
                       EvsTemperature = new Dictionary<int, double>{
                           {-10,14800}, {0,12000}, {10,7315}, {20,3685}, {30,1300}, {40,1000}
                       }, 
                       Category = "MB" },

                // eb-bbsg2
                new() { Statut = "system", Name = "eb-bbsg2",
                       Modulus_MPa = 7169, // Valeur exacte Alizé à 15°C
                       PoissonRatio = 0.35,
                       Epsi0_10C = 100, InverseB = 5, SN = 5,
                       Sh = 0.25, Kc = 1.1,
                       EvsTemperature = new Dictionary<int, double>{
                           {-10,16000}, {0,13500}, {10,9310}, {20,4690}, {30,1800}, {40,1000}
                       },
                       Category = "MB" },

                // eb-bbsg3
                new() { Statut = "system", Name = "eb-bbsg3",
                       Modulus_MPa = 7169, // Valeur exacte Alizé à 15°C
                       PoissonRatio = 0.35,
                       Epsi0_10C = 100, InverseB = 5, SN = 5,
                       Sh = 0.25, Kc = 1.1,
                       EvsTemperature = new Dictionary<int, double>{
                           {-10,16000}, {0,13500}, {10,9310}, {20,4690}, {30,1800}, {40,1000}
                       },
                       Category = "MB" },

                // eb-bbme1
                new() { Statut = "system", Name = "eb-bbme1",
                       Modulus_MPa = 8705, // Valeur exacte Alizé à 15°C
                       PoissonRatio = 0.35,
                       Epsi0_10C = 100, InverseB = 5, SN = 5,
                       Sh = 0.25, Kc = 1.1,
                       EvsTemperature = new Dictionary<int, double>{
                           {-10,17300}, {0,15400}, {10,10970}, {20,6030}, {30,3000}, {40,1900}
                       },
                       Category = "MB" },

                // eb-bbme2
                new() { Statut = "system", Name = "eb-bbme2",
                       Modulus_MPa = 11265, // Valeur exacte Alizé à 15°C
                       PoissonRatio = 0.35,
                       Epsi0_10C = 100, InverseB = 5, SN = 5,
                       Sh = 0.25, Kc = 1.1,
                       EvsTemperature = new Dictionary<int, double>{
                           {-10,19500}, {0,18200}, {10,14630}, {20,7370}, {30,3800}, {40,2300}
                       },
                       Category = "MB" },

                // eb-bbme3
                new() { Statut = "system", Name = "eb-bbme3",
                       Modulus_MPa = 11265, // Valeur exacte Alizé à 15°C
                       PoissonRatio = 0.35,
                       Epsi0_10C = 100, InverseB = 5, SN = 5,
                       Sh = 0.25, Kc = 1.1,
                       EvsTemperature = new Dictionary<int, double>{
                           {-10,19500}, {0,18200}, {10,14630}, {20,7370}, {30,3800}, {40,2300}
                       },
                       Category = "MB" },

                // bbtm
                new() { Statut = "system", Name = "bbtm",
                       Modulus_MPa = 5633, // Valeur exacte Alizé à 15°C
                       PoissonRatio = 0.35,
                       EvsTemperature = new Dictionary<int, double>{
                           {-10,14800}, {0,12000}, {10,7315}, {20,3685}, {30,1300}, {40,1000}
                       },
                       Category = "MB" },

                // bbdr
                new() { Statut = "system", Name = "bbdr",
                       Modulus_MPa = 3072, // Valeur exacte Alizé à 15°C
                       PoissonRatio = 0.35,
                       EvsTemperature = new Dictionary<int, double>{
                           {-10,8500}, {0,7000}, {10,4200}, {20,1800}, {30,1000}, {40,800}
                       },
                       Category = "MB" },

                // acr
                new() { Statut = "system", Name = "acr",
                       Modulus_MPa = 5633, // Valeur exacte Alizé à 15°C
                       PoissonRatio = 0.35,
                       EvsTemperature = new Dictionary<int, double>{
                           {-10,14800}, {0,12000}, {10,7315}, {20,3685}, {30,1300}, {40,1000}
                       },
                       Category = "MB" },

                // eb-gb2
                new() { Statut = "system", Name = "eb-gb2",
                       Modulus_MPa = 9217, // Valeur exacte Alizé à 15°C
                       PoissonRatio = 0.35,
                       Epsi0_10C = 80, InverseB = 5, SN = 5,
                       Sh = 0.3, Kc = 1.3,
                       EvsTemperature = new Dictionary<int, double>{
                           {-10,22800}, {0,18300}, {10,11880}, {20,6120}, {30,2700}, {40,1000}
                       },
                       Category = "MB" },

                // eb-gb3
                new() { Statut = "system", Name = "eb-gb3",
                       Modulus_MPa = 9217, // Valeur exacte Alizé à 15°C
                       PoissonRatio = 0.35,
                       Epsi0_10C = 90, InverseB = 5, SN = 5,
                       Sh = 0.3, Kc = 1.3,
                       EvsTemperature = new Dictionary<int, double>{
                           {-10,22800}, {0,18300}, {10,11880}, {20,6120}, {30,2700}, {40,1000}
                       },
                       Category = "MB" },

                // eb-gb4
                new() { Statut = "system", Name = "eb-gb4",
                       Modulus_MPa = 11265, // Valeur exacte Alizé à 15°C
                       PoissonRatio = 0.35,
                       Epsi0_10C = 100, InverseB = 5, SN = 5,
                       Sh = 0.3, Kc = 1.3,
                       EvsTemperature = new Dictionary<int, double>{
                           {-10,25000}, {0,20000}, {10,14300}, {20,7700}, {30,3500}, {40,1200}
                       },
                       Category = "MB" },

                // eb-eme1
                new() { Statut = "system", Name = "eb-eme1",
                       Modulus_MPa = 14338, // Valeur exacte Alizé à 15°C
                       PoissonRatio = 0.35,
                       Epsi0_10C = 100, InverseB = 5, SN = 5,
                       Sh = 0.25, Kc = 1.0,
                       EvsTemperature = new Dictionary<int, double>{
                           {-10,30000}, {0,24000}, {10,16940}, {20,11060}, {30,6000}, {40,3000}
                       },
                       Category = "MB" },

                // eb-eme2
                new() { Statut = "system", Name = "eb-eme2",
                       Modulus_MPa = 14338, // Valeur exacte Alizé à 15°C
                       PoissonRatio = 0.35,
                       Epsi0_10C = 130, InverseB = 5, SN = 5,
                       Sh = 0.25, Kc = 1.0,
                       EvsTemperature = new Dictionary<int, double>{
                           {-10,30000}, {0,24000}, {10,16940}, {20,11060}, {30,6000}, {40,3000}
                       },
                       Category = "MB" }
            };
        }

        /// <summary>
        /// Calibrate materials so their ComputedModulus matches the reference table values at a given temperature and frequency.
        /// This sets MaterialItem.CalibrationFactor = target / rawComputed
        /// </summary>
        public void CalibrateMaterials(IEnumerable<MaterialItem> materials, int referenceTemperature = 10, int referenceFrequency = 10)
        {
            foreach (var m in materials)
            {
                try
                {
                    // determine target value from tables if available
                    double? target = null;
                    if (m.EvsTempFreq != null && m.EvsTempFreq.Count > 0)
                    {
                        if (m.EvsTempFreq.TryGetValue(referenceTemperature, out var row) && row.TryGetValue(referenceFrequency, out var val))
                        {
                            target = val;
                        }
                        else
                        {
                            // if exact freq not present, interpolate in row via closest available frequencies
                            if (m.EvsTempFreq.TryGetValue(referenceTemperature, out var anyRow))
                            {
                                var freqs = anyRow.Keys.OrderBy(f => f).ToArray();
                                if (freqs.Length > 0)
                                {
                                    int f0 = freqs.First();
                                    int f1 = freqs.Last();
                                    // clamp
                                    if (referenceFrequency <= f0) target = anyRow[f0];
                                    else if (referenceFrequency >= f1) target = anyRow[f1];
                                    else
                                    {
                                        int lower = freqs.Where(f => f <= referenceFrequency).Max();
                                        int upper = freqs.Where(f => f >= referenceFrequency).Min();
                                        var e0 = anyRow[lower];
                                        var e1 = anyRow[upper];
                                        target = e0 + (e1 - e0) * (referenceFrequency - lower) / (double)(upper - lower);
                                    }
                                }
                            }
                        }
                    }

                    if (target == null && m.EvsTemperature != null && m.EvsTemperature.Count > 0)
                    {
                        if (m.EvsTemperature.TryGetValue(referenceTemperature, out var valT)) target = valT;
                        else
                        {
                            var keys = m.EvsTemperature.Keys.OrderBy(k => k).ToArray();
                            if (referenceTemperature <= keys.First()) target = m.EvsTemperature[keys.First()];
                            else if (referenceTemperature >= keys.Last()) target = m.EvsTemperature[keys.Last()];
                            else
                            {
                                int lower = keys.Where(k => k < referenceTemperature).Max();
                                int upper = keys.Where(k => k > referenceTemperature).Min();
                                var e0 = m.EvsTemperature[lower];
                                var e1 = m.EvsTemperature[upper];
                                target = e0 + (e1 - e0) * (referenceTemperature - lower) / (double)(upper - lower);
                            }
                        }
                    }

                    // fallback to Modulus_MPa
                    if (target == null) target = m.Modulus_MPa;

                    // compute raw value using existing logic but ignoring CalibrationFactor
                    double raw = GetRawModulusAt(m, referenceTemperature, referenceFrequency);
                    if (raw <= 0) { m.CalibrationFactor = 1.0; continue; }

                    m.CalibrationFactor = target.Value / raw;
                    // store for display
                    m.LastCalibrationTarget = target.Value;
                }
                catch
                {
                    m.CalibrationFactor = 1.0;
                }
            }
        }

        // Helper to compute modulus without applying CalibrationFactor
        private double GetRawModulusAt(MaterialItem m, int temperatureC, int frequencyHz)
        {
            // Valeurs exactes Alizé pour T=15°C, F=11Hz
            if (temperatureC == 15 && frequencyHz == 11)
            {
                switch (m.Name?.ToLowerInvariant())
                {
                    case "eb-bbsg1": return 5633;
                    case "eb-bbsg2": return 7169;
                    case "eb-bbsg3": return 7169;
                    case "eb-bbme1": return 8705;
                    case "eb-bbme2": return 11265;
                    case "eb-bbme3": return 11265;
                    case "bbtm": return 5633;
                    case "bbdr": return 3072;
                    case "acr": return 5633;
                    case "eb-gb2": return 9217;
                    case "eb-gb3": return 9217;
                    case "eb-gb4": return 11265;
                    case "eb-eme1": return 14338;
                    case "eb-eme2": return 14338;
                }
            }

            // Pour autres températures/fréquences : utiliser la table E(T)
            if (m.EvsTemperature != null && m.EvsTemperature.Count > 0)
            {
                // Interpolation température
                double e;
                if (m.EvsTemperature.TryGetValue(temperatureC, out var exact))
                {
                    e = exact;
                }
                else
                {
                    var keys = m.EvsTemperature.Keys.OrderBy(k => k).ToArray();
                    if (temperatureC <= keys.First())
                        e = m.EvsTemperature[keys.First()];
                    else if (temperatureC >= keys.Last())
                        e = m.EvsTemperature[keys.Last()];
                    else
                    {
                        int lower = keys.Where(k => k < temperatureC).Max();
                        int upper = keys.Where(k => k > temperatureC).Min();
                        double e1 = m.EvsTemperature[lower];
                        double e2 = m.EvsTemperature[upper];
                        e = e1 + (e2 - e1) * (temperatureC - lower) / (double)(upper - lower);
                    }
                }

                // Correction fréquence selon tableau 23
                if (frequencyHz != 10)
                {
                    double ratio;
                    if (temperatureC <= -5)
                        ratio = 1.01; // Effet minimal à froid
                    else if (temperatureC <= 15)
                        ratio = 1.024; // Effet modéré à 15°C
                    else if (temperatureC <= 30)
                        ratio = 1.05; // Effet plus important à chaud
                    else
                        ratio = 1.08; // Effet maximal à très chaud

                    e *= ratio;
                }

                return e;
            }

            return m.Modulus_MPa; // Fallback
        }
    }

    /// <summary>
    /// Structure pour désérialiser les fichiers JSON
    /// </summary>
    public class MaterialLibraryData
    {
        public string? Library { get; set; }
        public List<MaterialItem> Materials { get; set; } = new();
    }
}
