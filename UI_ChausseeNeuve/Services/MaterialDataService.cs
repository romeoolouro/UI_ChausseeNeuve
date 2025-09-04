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
            // Matériaux bitumineux NF P98-086 (annexe F normative, format Alizé)
            return new List<MaterialItem>
            {
                new() { Statut = "system", Name = "eb-bbsg1", Modulus_MPa = 1000, PoissonRatio = 0.35, Epsi0_10C = 100, InverseB = 5, SN = 5, Sh = 0.25, Kc = 1.1, EvsTemperature = new Dictionary<int, double>{{-10,14800},{0,12000},{10,7315},{20,3685},{30,1300},{40,1000}}, Category = "MB" },
                new() { Statut = "system", Name = "eb-bbsg2", Modulus_MPa = 1600, PoissonRatio = 0.35, Epsi0_10C = 100, InverseB = 5, SN = 5, Sh = 0.25, Kc = 1.1, EvsTemperature = new Dictionary<int, double>{{-10,16000},{0,13500},{10,9310},{20,4690},{30,1600},{40,1000}}, Category = "MB" },
                new() { Statut = "system", Name = "eb-bbsg3", Modulus_MPa = 2000, PoissonRatio = 0.35, Epsi0_10C = 100, InverseB = 5, SN = 5, Sh = 0.25, Kc = 1.1, EvsTemperature = new Dictionary<int, double>{{-10,16000},{0,13500},{10,9310},{20,4690},{30,1800},{40,1000}}, Category = "MB" },
                new() { Statut = "system", Name = "eb-bbme1", Modulus_MPa = 1200, PoissonRatio = 0.35, Epsi0_10C = 100, InverseB = 5, SN = 5, Sh = 0.25, Kc = 1.1, EvsTemperature = new Dictionary<int, double>{{-10,14800},{0,12000},{10,7315},{20,3685},{30,1300},{40,1000}}, Category = "MB" },
                new() { Statut = "system", Name = "eb-bbme2", Modulus_MPa = 2040, PoissonRatio = 0.35, Epsi0_10C = 100, InverseB = 5, SN = 5, Sh = 0.25, Kc = 1.1, EvsTemperature = new Dictionary<int, double>{{-10,19500},{0,18200},{10,11630},{20,7300},{30,3800},{40,2300}}, Category = "MB" },
                new() { Statut = "system", Name = "bbm", Modulus_MPa = 1000, PoissonRatio = 0.35, Epsi0_10C = 100, InverseB = 5, SN = 5, Sh = 0.25, Kc = 1.1, EvsTemperature = new Dictionary<int, double>{{-10,14800},{0,12000},{10,7315},{20,3685},{30,1300},{40,1000}}, Category = "MB" },
                new() { Statut = "system", Name = "bbm2", Modulus_MPa = 1600, PoissonRatio = 0.35, Epsi0_10C = 100, InverseB = 5, SN = 5, Sh = 0.25, Kc = 1.1, EvsTemperature = new Dictionary<int, double>{{-10,16000},{0,13500},{10,9310},{20,4690},{30,1600},{40,1000}}, Category = "MB" },
                new() { Statut = "system", Name = "acr", Modulus_MPa = 8500, PoissonRatio = 0.35, Epsi0_10C = 100, InverseB = 5, SN = 5, Sh = 0.25, Kc = 1.1, EvsTemperature = new Dictionary<int, double>{{-10,8500},{0,8000},{10,4200},{20,1600},{30,800},{40,300}}, Category = "MB" },
                new() { Statut = "system", Name = "eb-gb1", Modulus_MPa = 1600, PoissonRatio = 0.35, Epsi0_10C = 100, InverseB = 5, SN = 5, Sh = 0.3, Kc = 1.1, EvsTemperature = new Dictionary<int, double>{{-10,16000},{0,13500},{10,9310},{20,4690},{30,1600},{40,1000}}, Category = "MB" },
                new() { Statut = "system", Name = "eb-gb2", Modulus_MPa = 2000, PoissonRatio = 0.35, Epsi0_10C = 100, InverseB = 5, SN = 5, Sh = 0.3, Kc = 1.1, EvsTemperature = new Dictionary<int, double>{{-10,19500},{0,18200},{10,11630},{20,7300},{30,3800},{40,2300}}, Category = "MB" },
                new() { Statut = "system", Name = "eb-gb3", Modulus_MPa = 3200, PoissonRatio = 0.35, Epsi0_10C = 100, InverseB = 5, SN = 5, Sh = 0.3, Kc = 1.1, EvsTemperature = new Dictionary<int, double>{{-10,23000},{0,21000},{10,13800},{20,9100},{30,2700},{40,1300}}, Category = "MB" },
                new() { Statut = "system", Name = "eb-eme1", Modulus_MPa = 14000, PoissonRatio = 0.35, Epsi0_10C = 100, InverseB = 5, SN = 5, Sh = 0.25, Kc = 1.1, EvsTemperature = new Dictionary<int, double>{{-10,30000},{0,24000},{10,16940},{20,11600},{30,6000},{40,3000}}, Category = "MB" },
                new() { Statut = "system", Name = "eb-eme2", Modulus_MPa = 16000, PoissonRatio = 0.35, Epsi0_10C = 100, InverseB = 5, SN = 5, Sh = 0.25, Kc = 1.1, EvsTemperature = new Dictionary<int, double>{{-10,30000},{0,24000},{10,16940},{20,11600},{30,6000},{40,3000}}, Category = "MB" }
            };
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
