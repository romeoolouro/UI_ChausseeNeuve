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

                List<MaterialItem> materials;
                if (!File.Exists(dataPath))
                {
                    materials = GetDefaultMaterials(libraryName);
                }
                else
                {
                    string jsonContent = await File.ReadAllTextAsync(dataPath);
                    var libraryData = JsonSerializer.Deserialize<MaterialLibraryData>(jsonContent, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    materials = libraryData?.Materials ?? GetDefaultMaterials(libraryName);
                }

                // Neutraliser tout facteur de calibration pour les matériaux bitumineux normatifs (MB)
                foreach (var m in materials.Where(m => string.Equals(m.Category, "MB", StringComparison.OrdinalIgnoreCase)))
                {
                    m.CalibrationFactor = 1.0; // Le modèle normatif fournira directement E(T,f)
                }

                _materialCache[libraryName] = materials;
                return materials;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement des matériaux pour {libraryName}: {ex.Message}");
                var defaultMaterials = GetDefaultMaterials(libraryName);
                foreach (var m in defaultMaterials.Where(m => string.Equals(m.Category, "MB", StringComparison.OrdinalIgnoreCase)))
                {
                    m.CalibrationFactor = 1.0;
                }
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
                // ===== MB =====
                // eb-bbsg1
                new() { Statut = "system", Name = "eb-bbsg1", 
                       Modulus_MPa = 5500,
                        PoissonRatio = 0.35, 
                        Epsi0_10C = 100, InverseB = 5, SN = 0.25, 
                        Sh = null, ShStatus = "standard", Kc = 1.1, 
                        EvsTemperature = new Dictionary<int, double>{
                            {-10,14800}, {0,12000}, {10,7315}, {20,3685}, {30,1300}, {40,1000}
                        }, 
                        Category = "MB" },

                // eb-bbsg2
                new() { Statut = "system", Name = "eb-bbsg2",
                       Modulus_MPa = 7000,
                        PoissonRatio = 0.35,
                        Epsi0_10C = 100, InverseB = 5, SN = 0.25,
                        Sh = null, ShStatus = "standard", Kc = 1.1,
                        EvsTemperature = new Dictionary<int, double>{
                            {-10,16000}, {0,13500}, {10,9310}, {20,4690}, {30,1800}, {40,1000}
                        },
                        Category = "MB" },

                // eb-bbsg3 (corrigé: valeur 10°C alignée sur eb-gb2 pour ratio normatif)
                new() { Statut = "system", Name = "eb-bbsg3",
                       Modulus_MPa = 7000,
                        PoissonRatio = 0.35,
                        Epsi0_10C = 100, InverseB = 5, SN = 0.25,
                        Sh = null, ShStatus = "standard", Kc = 1.1,
                        EvsTemperature = new Dictionary<int, double>{
                            {-10,16000}, {0,13500}, {10,9310}, {20,4690}, {30,1800}, {40,1000}
                        },
                        Category = "MB" },

                // eb-bbme1
                 new() { Statut = "system", Name = "eb-bbme1",
                       Modulus_MPa = 9000,
                        PoissonRatio = 0.35,
                        Epsi0_10C = 100, InverseB = 5, SN = 0.25,
                        Sh = null, ShStatus = "standard", Kc = 1.1,
                        EvsTemperature = new Dictionary<int, double>{
                           {-10,17300}, {0,15400}, {10,11970}, {20,6030}, {30,3000}, {40,1900}
                        },
                        Category = "MB" },

                // eb-bbme2
                 new() { Statut = "system", Name = "eb-bbme2",
                       Modulus_MPa = 11000,
                        PoissonRatio = 0.35,
                        Epsi0_10C = 100, InverseB = 5, SN = 0.25,
                        Sh = null, ShStatus = "standard", Kc = 1.1,
                        EvsTemperature = new Dictionary<int, double>{
                            {-10,19500}, {0,18200}, {10,14630}, {20,7370}, {30,3800}, {40,2300}
                        },
                        Category = "MB" },

                // eb-bbme3
                 new() { Statut = "system", Name = "eb-bbme3",
                       Modulus_MPa = 11000,
                        PoissonRatio = 0.35,
                        Epsi0_10C = 100, InverseB = 5, SN = 0.25,
                        Sh = null, ShStatus = "standard", Kc = 1.1,
                        EvsTemperature = new Dictionary<int, double>{
                            {-10,19500}, {0,18200}, {10,14630}, {20,7370}, {30,3800}, {40,2300}
                        },
                        Category = "MB" },

                // bbm
                new() { Statut = "system", Name = "bbm",
                       Modulus_MPa = 5500, PoissonRatio = 0.35,
                       SN = 0.25, Sh = null, ShStatus = "standard", Kc = 1.1,
                       EvsTemperature = new Dictionary<int, double>
                       {
                           {-10,14800}, {0,12000}, {10,7315}, {20,3685}, {30,1300}, {40,1000}
                       },
                       Category = "MB" },
                // bbtm
                new() { Statut = "system", Name = "bbtm",
                       Modulus_MPa = 3000, PoissonRatio = 0.35,
                       SN = 0.25, Sh = null, ShStatus = "standard", Kc = 1.1,
                       EvsTemperature = new Dictionary<int, double>
                       {
                           {-10,8500}, {0,7000}, {10,4200}, {20,1800}, {30,1000}, {40,800}
                       },
                       Category = "MB" },

                 // bbdr
                new() { Statut = "system", Name = "bbdr",
                       Modulus_MPa = 3000,
                        PoissonRatio = 0.35,
                        SN = 0.25, Sh = null, ShStatus = "standard", Kc = 1.1,
                        EvsTemperature = new Dictionary<int, double>
                        {
                            {-10,8500}, {0,7000}, {10,4200}, {20,1800}, {30,1000}, {40,800}
                        },
                       Category = "MB" },

                 // acr
                new() { Statut = "system", Name = "acr",
                       Modulus_MPa = 5500,
                        PoissonRatio = 0.35,
                        SN = 0.25, Sh = null, ShStatus = "standard", Kc = 1.1,
                        EvsTemperature = new Dictionary<int, double>
                        {
                            {-10,14800}, {0,12000}, {10,7315}, {20,3685}, {30,1300}, {40,1000}
                        },
                       Category = "MB" },

                 // eb-gb2
                new() { Statut = "system", Name = "eb-gb2",
                       Modulus_MPa = 9000,
                        PoissonRatio = 0.35,
                        Epsi0_10C = 80, InverseB = 5, SN = 0.3,
                        Sh = null, ShStatus = "standard", Kc = 1.3,
                        EvsTemperature = new Dictionary<int, double>
                        {
                            {-10,22800}, {0,18300}, {10,11880}, {20,6120}, {30,2700}, {40,1000}
                        },
                        Category = "MB" },

                 // eb-gb3 (corrigé: 10°C=11880 au lieu de 13800)
                new() { Statut = "system", Name = "eb-gb3",
                       Modulus_MPa = 9000,
                        PoissonRatio = 0.35,
                        Epsi0_10C = 90, InverseB = 5, SN = 0.3,
                        Sh = null, ShStatus = "standard", Kc = 1.3,
                        EvsTemperature = new Dictionary<int, double>
                        {
                           {-10,22800}, {0,18300}, {10,11880}, {20,6120}, {30,2700}, {40,1000}
                        },
                        Category = "MB" },

                 // eb-gb4
                new() { Statut = "system", Name = "eb-gb4",
                       Modulus_MPa = 11000,
                        PoissonRatio = 0.35,
                        Epsi0_10C = 100, InverseB = 5, SN = 0.3,
                        Sh = null, ShStatus = "standard", Kc = 1.3,
                        EvsTemperature = new Dictionary<int, double>
                        {
                           {-10,25300}, {0,20000}, {10,14300}, {20,7700}, {30,3500}, {40,1200}
                        },
                        Category = "MB" },

                 // eb-eme1
                new() { Statut = "system", Name = "eb-eme1",
                       Modulus_MPa = 14000,
                        PoissonRatio = 0.35,
                        Epsi0_10C = 100, InverseB = 5, SN = 0.25,
                        Sh = null, ShStatus = "standard", Kc = 1.0,
                        EvsTemperature = new Dictionary<int, double>
                        {
                            {-10,30000}, {0,24000}, {10,16940}, {20,11060}, {30,6000}, {40,3000}
                        },
                        Category = "MB" },

                 // eb-eme2
                new() { Statut = "system", Name = "eb-eme2",
                       Modulus_MPa = 14000,
                        PoissonRatio = 0.35,
                        Epsi0_10C = 130, InverseB = 5, SN = 0.25,
                        Sh = null, ShStatus = "standard", Kc = 1.0,
                        EvsTemperature = new Dictionary<int, double>
                        {
                            {-10,30000}, {0,24000}, {10,16940}, {20,11060}, {30,6000}, {40,3000}
                        },
                        Category = "MB" },

                // ===== MTLH (exemples par défaut) =====
                new() { Statut = "system", Name = "gc-t3", Category = "MTLH", MaterialFamily = MaterialFamily.MTLH,
                       Modulus_MPa = 23000, PoissonRatio = 0.25, Sigma6 = 0.75, InverseB = 15, SN = 1, Sh = 0.03, Kc = 1.4, Kd = 1 },
                new() { Statut = "system", Name = "gc-t4", Category = "MTLH", MaterialFamily = MaterialFamily.MTLH,
                       Modulus_MPa = 25000, PoissonRatio = 0.25, Sigma6 = 1.2, InverseB = 15, SN = 1, Sh = 0.03, Kc = 1.4, Kd = 0.8 },
                new() { Statut = "system", Name = "glhr-t3", Category = "MTLH", MaterialFamily = MaterialFamily.MTLH,
                       Modulus_MPa = 23000, PoissonRatio = 0.25, Sigma6 = 0.75, InverseB = 15, SN = 1, Sh = 0.03, Kc = 1.4, Kd = 1 },
                new() { Statut = "system", Name = "gch-t3", Category = "MTLH", MaterialFamily = MaterialFamily.MTLH,
                       Modulus_MPa = 23000, PoissonRatio = 0.25, Sigma6 = 0.75, InverseB = 15, SN = 1, Sh = 0.03, Kc = 1.5, Kd = 1 },
                new() { Statut = "system", Name = "glg-t2", Category = "MTLH", MaterialFamily = MaterialFamily.MTLH,
                       Modulus_MPa = 15000, PoissonRatio = 0.25, Sigma6 = 0.5, InverseB = 12.5, SN = 1, Sh = 0.03, Kc = 1.5, Kd = 1 },
                new() { Statut = "system", Name = "glp-t2", Category = "MTLH", MaterialFamily = MaterialFamily.MTLH,
                       Modulus_MPa = 15000, PoissonRatio = 0.25, Sigma6 = 0.5, InverseB = 12.5, SN = 1, Sh = 0.03, Kc = 1.5, Kd = 1 },
                new() { Statut = "system", Name = "glp-t3", Category = "MTLH", MaterialFamily = MaterialFamily.MTLH,
                       Modulus_MPa = 20000, PoissonRatio = 0.25, Sigma6 = 0.7, InverseB = 13.7, SN = 1, Sh = 0.03, Kc = 1.5, Kd = 1 },
                new() { Statut = "system", Name = "gcv-t3", Category = "MTLH", MaterialFamily = MaterialFamily.MTLH,
                       Modulus_MPa = 20000, PoissonRatio = 0.25, Sigma6 = 1.4, InverseB = 16, SN = 1, Sh = 0.03, Kc = 1.3, Kd = 1 },
                new() { Statut = "system", Name = "bcr-t5", Category = "MTLH", MaterialFamily = MaterialFamily.MTLH,
                       Modulus_MPa = 28000, PoissonRatio = 0.25, Sigma6 = 1.85, InverseB = 15, SN = 1, Sh = 0.03, Kc = 1.5, Kd = 0.8 },
                // sables/sols traités et pouzzolanes
                new() { Statut = "system", Name = "sl-t1", Category = "MTLH", MaterialFamily = MaterialFamily.MTLH,
                       Modulus_MPa = 3700, PoissonRatio = 0.25, Sigma6 = 0.175, InverseB = 10, SN = 0.8, Sh = 0.025, Kc = 1.5, Kd = 1 },
                new() { Statut = "system", Name = "sl-t2", Category = "MTLH", MaterialFamily = MaterialFamily.MTLH,
                       Modulus_MPa = 8500, PoissonRatio = 0.25, Sigma6 = 0.40, InverseB = 12, SN = 0.8, Sh = 0.025, Kc = 1.5, Kd = 1 },
                new() { Statut = "system", Name = "sl-t3", Category = "MTLH", MaterialFamily = MaterialFamily.MTLH,
                       Modulus_MPa = 12500, PoissonRatio = 0.25, Sigma6 = 0.63, InverseB = 12, SN = 0.8, Sh = 0.025, Kc = 1.5, Kd = 1 },
                new() { Statut = "system", Name = "spch-t1", Category = "MTLH", MaterialFamily = MaterialFamily.MTLH,
                       Modulus_MPa = 3700, PoissonRatio = 0.25, Sigma6 = 0.175, InverseB = 10, SN = 0.8, Sh = 0.025, Kc = 1.5, Kd = 1 },
                new() { Statut = "system", Name = "spch-t2", Category = "MTLH", MaterialFamily = MaterialFamily.MTLH,
                       Modulus_MPa = 8500, PoissonRatio = 0.25, Sigma6 = 0.40, InverseB = 12, SN = 0.8, Sh = 0.025, Kc = 1.5, Kd = 1 },
                new() { Statut = "system", Name = "spch-t3", Category = "MTLH", MaterialFamily = MaterialFamily.MTLH,
                       Modulus_MPa = 12500, PoissonRatio = 0.25, Sigma6 = 0.63, InverseB = 12, SN = 0.8, Sh = 0.025, Kc = 1.5, Kd = 1 },
                new() { Statut = "system", Name = "sc-t1", Category = "MTLH", MaterialFamily = MaterialFamily.MTLH,
                       Modulus_MPa = 5000, PoissonRatio = 0.25, Sigma6 = 0.21, InverseB = 12, SN = 0.8, Sh = 0.025, Kc = 1.5, Kd = 1 },
                new() { Statut = "system", Name = "sc-t2", Category = "MTLH", MaterialFamily = MaterialFamily.MTLH,
                       Modulus_MPa = 12000, PoissonRatio = 0.25, Sigma6 = 0.50, InverseB = 12, SN = 0.8, Sh = 0.025, Kc = 1.5, Kd = 1 },
                new() { Statut = "system", Name = "sc-t3", Category = "MTLH", MaterialFamily = MaterialFamily.MTLH,
                       Modulus_MPa = 17200, PoissonRatio = 0.25, Sigma6 = 0.75, InverseB = 12, SN = 0.8, Sh = 0.025, Kc = 1.5, Kd = 1 },
                new() { Statut = "system", Name = "scv-t1", Category = "MTLH", MaterialFamily = MaterialFamily.MTLH,
                       Modulus_MPa = 5000, PoissonRatio = 0.25, Sigma6 = 0.21, InverseB = 12, SN = 0.8, Sh = 0.025, Kc = 1.5, Kd = 1 },
                 new() { Statut = "system", Name = "slhr-t1", Category = "MTLH", MaterialFamily = MaterialFamily.MTLH,
                        Modulus_MPa = 5000, PoissonRatio = 0.25, Sigma6 = 0.21, InverseB = 12, SN = 0.8, Sh = 0.025, Kc = 1.5, Kd = 1 },
                 new() { Statut = "system", Name = "slhr-t2", Category = "MTLH", MaterialFamily = MaterialFamily.MTLH,
                        Modulus_MPa = 12000, PoissonRatio = 0.25, Sigma6 = 0.50, InverseB = 12, SN = 0.8, Sh = 0.025, Kc = 1.5, Kd = 1 },
                 new() { Statut = "system", Name = "slhr-t3", Category = "MTLH", MaterialFamily = MaterialFamily.MTLH,
                        Modulus_MPa = 17200, PoissonRatio = 0.25, Sigma6 = 0.75, InverseB = 12, SN = 0.8, Sh = 0.025, Kc = 1.5, Kd = 1 },
                 new() { Statut = "system", Name = "scv-t2", Category = "MTLH", MaterialFamily = MaterialFamily.MTLH,
                        Modulus_MPa = 12000, PoissonRatio = 0.25, Sigma6 = 0.50, InverseB = 12, SN = 0.8, Sh = 0.025, Kc = 1.5, Kd = 1 },
                 new() { Statut = "system", Name = "scv-t3", Category = "MTLH", MaterialFamily = MaterialFamily.MTLH,
                        Modulus_MPa = 17200, PoissonRatio = 0.25, Sigma6 = 0.75, InverseB = 12, SN = 0.8, Sh = 0.025, Kc = 1.5, Kd = 1 },

                // ===== BETON (par défaut) =====
                new() { Statut = "system", Name = "bc5", Category = "Beton", MaterialFamily = MaterialFamily.BetonCiment,
                       Modulus_MPa = 35000, PoissonRatio = 0.25, Sigma6 = 2.15, InverseB = 16, SN = 1, Kc = 1.5 },
                new() { Statut = "system", Name = "bc4", Category = "Beton", MaterialFamily = MaterialFamily.BetonCiment,
                       Modulus_MPa = 24000, PoissonRatio = 0.25, Sigma6 = 1.95, InverseB = 15, SN = 1, Kc = 1.5 },
                new() { Statut = "system", Name = "bc3", Category = "Beton", MaterialFamily = MaterialFamily.BetonCiment,
                       Modulus_MPa = 24000, PoissonRatio = 0.25, Sigma6 = 1.63, InverseB = 15, SN = 1, Kc = 1.5 },
                new() { Statut = "system", Name = "bc2", Category = "Beton", MaterialFamily = MaterialFamily.BetonCiment,
                       Modulus_MPa = 20000, PoissonRatio = 0.25, Sigma6 = 1.37, InverseB = 14, SN = 1, Kc = 1.5 },

                // ===== SOL & GNT (par défaut) =====
                new() { Statut = "system", Name = "gnt1", Category = "Sol_GNT", MaterialFamily = MaterialFamily.GNT,
                       Modulus_MPa = 600, PoissonRatio = 0.35,
                       AdditionalProperties = new Dictionary<string, object>{{"A_faible","non défini"},{"A_fort","non défini"},{"pente_b","non défini"}} },
                new() { Statut = "system", Name = "gnt2", Category = "Sol_GNT", MaterialFamily = MaterialFamily.GNT,
                       Modulus_MPa = 400, PoissonRatio = 0.35,
                       AdditionalProperties = new Dictionary<string, object>{{"A_faible","non défini"},{"A_fort","non défini"},{"pente_b","non défini"}} },
                new() { Statut = "system", Name = "gnt3", Category = "Sol_GNT", MaterialFamily = MaterialFamily.GNT,
                       Modulus_MPa = 200, PoissonRatio = 0.35,
                       AdditionalProperties = new Dictionary<string, object>{{"A_faible","non défini"},{"A_fort","non défini"},{"pente_b","non défini"}} },
                new() { Statut = "system", Name = "gnt-be", Category = "Sol_GNT", MaterialFamily = MaterialFamily.GNT,
                       Modulus_MPa = 360, PoissonRatio = 0.35,
                       AdditionalProperties = new Dictionary<string, object>{{"A_faible","non défini"},{"A_fort",12000},{"pente_b",-0.222}} },
                new() { Statut = "system", Name = "gnt-inv", Category = "Sol_GNT", MaterialFamily = MaterialFamily.GNT,
                       Modulus_MPa = 480, PoissonRatio = 0.35,
                       AdditionalProperties = new Dictionary<string, object>{{"A_faible","non défini"},{"A_fort",14400},{"pente_b",-0.222}} },
                new() { Statut = "system", Name = "pf1", Category = "Sol_GNT", MaterialFamily = MaterialFamily.GNT,
                       Modulus_MPa = 20, PoissonRatio = 0.35,
                       AdditionalProperties = new Dictionary<string, object>{{"A_faible",16000},{"A_fort",12000},{"pente_b",-0.222}} },
                new() { Statut = "system", Name = "pf2", Category = "Sol_GNT", MaterialFamily = MaterialFamily.GNT,
                       Modulus_MPa = 50, PoissonRatio = 0.35,
                       AdditionalProperties = new Dictionary<string, object>{{"A_faible",16000},{"A_fort",12000},{"pente_b",-0.222}} },
                new() { Statut = "system", Name = "pf2qs", Category = "Sol_GNT", MaterialFamily = MaterialFamily.GNT,
                       Modulus_MPa = 80, PoissonRatio = 0.35,
                       AdditionalProperties = new Dictionary<string, object>{{"A_faible",16000},{"A_fort",12000},{"pente_b",-0.222}} },
                new() { Statut = "system", Name = "pf3", Category = "Sol_GNT", MaterialFamily = MaterialFamily.GNT,
                       Modulus_MPa = 120, PoissonRatio = 0.35,
                       AdditionalProperties = new Dictionary<string, object>{{"A_faible",16000},{"A_fort",12000},{"pente_b",-0.222}} },
                new() { Statut = "system", Name = "pf4", Category = "Sol_GNT", MaterialFamily = MaterialFamily.GNT,
                       Modulus_MPa = 200, PoissonRatio = 0.35,
                       AdditionalProperties = new Dictionary<string, object>{{"A_faible",16000},{"A_fort",12000},{"pente_b",-0.222}} },
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
                    // Pour les matériaux bitumineux normatifs on ne calibre plus
                    if (string.Equals(m.Category, "MB", StringComparison.OrdinalIgnoreCase))
                    {
                        m.CalibrationFactor = 1.0;
                        continue;
                    }

                    double? target = null;
                    if (m.EvsTempFreq != null && m.EvsTempFreq.Count > 0)
                    {
                        if (m.EvsTempFreq.TryGetValue(referenceTemperature, out var row) && row.TryGetValue(referenceFrequency, out var val))
                        {
                            target = val;
                        }
                        else if (m.EvsTempFreq.TryGetValue(referenceTemperature, out var anyRow))
                        {
                            var freqs = anyRow.Keys.OrderBy(f => f).ToArray();
                            if (freqs.Length > 0)
                            {
                                int f0 = freqs.First();
                                int f1 = freqs.Last();
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

                    if (target == null) target = m.Modulus_MPa;

                    double raw = GetRawModulusAt(m, referenceTemperature, referenceFrequency);
                    if (raw <= 0) { m.CalibrationFactor = 1.0; continue; }

                    m.CalibrationFactor = target.Value / raw;
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
            // Valeurs exactes Alizé pour T=15°C, F=11Hz (legacy)
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

            if (m.EvsTemperature != null && m.EvsTemperature.Count > 0)
            {
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

                if (frequencyHz != 10)
                {
                    double ratio;
                    if (temperatureC <= -5)
                        ratio = 1.01;
                    else if (temperatureC <= 15)
                        ratio = 1.024;
                    else if (temperatureC <= 30)
                        ratio = 1.05;
                    else
                        ratio = 1.08;

                    e *= ratio;
                }

                return e;
            }

            return m.Modulus_MPa;
        }

        public void FillStandardShValues(IEnumerable<MaterialItem> materials)
        {
            foreach (var material in materials.Where(m => m.ShStatus == "standard" && m.Category == "MB"))
            {
                material.FillShFromStandard();
            }
        }

        private void ApplyNFP98Corrections(List<MaterialItem> materials, string libraryName)
        {
            if (materials == null || materials.Count == 0) return;
            if (!string.Equals(libraryName, "NFP98_086_2019", StringComparison.OrdinalIgnoreCase)) return;

            foreach (var m in materials)
            {
                try
                {
                    if (!string.Equals(m.Category, "MB", StringComparison.OrdinalIgnoreCase)) continue;

                    var lname = (m.Name ?? string.Empty).ToLowerInvariant();
                    bool isBBMFamily = lname is "bbm" or "bbtm" or "bbdr" or "acr";

                    if (!isBBMFamily)
                    {
                        if (m.SN == null)
                        {
                            if (lname.Contains("eb-gb"))
                                m.SN = 0.3;
                            else
                                m.SN = 0.25;
                        }
                        if (m.InverseB == null)
                            m.InverseB = 5;
                    }

                    if (m.Epsi0_10C == null)
                    {
                        if (lname.StartsWith("eb-gb2")) m.Epsi0_10C = 80;
                        else if (lname.StartsWith("eb-gb3")) m.Epsi0_10C = 90;
                        else if (lname.StartsWith("eb-gb4")) m.Epsi0_10C = 100;
                        else if (lname.StartsWith("eb-eme2")) m.Epsi0_10C = 130;
                        else if (lname.StartsWith("eb-eme1") || lname.StartsWith("eb-bbsg") || lname.StartsWith("eb-bbme")) m.Epsi0_10C = 100;
                    }

                    if (lname.Contains("eb-gb"))
                    {
                        if (m.Sh == null) m.Sh = 0.30;
                        if (m.Kc == null) m.Kc = 1.3;
                    }
                    else if (lname.Contains("eme"))
                    {
                        if (m.Sh == null) m.Sh = 0.25;
                        if (m.Kc == null) m.Kc = 1.0;
                    }
                    else
                    {
                        if (m.Sh == null) m.Sh = 0.25;
                        if (m.Kc == null) m.Kc = 1.1;
                    }
                }
                catch { }
            }
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
