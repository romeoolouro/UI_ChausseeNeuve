using ChausseeNeuve.Domain.Models;
using UI_ChausseeNeuve.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace UI_ChausseeNeuve.Tests.Helpers
{
    /// <summary>
    /// Builder pour créer facilement des objets de test avec des données cohérentes
    /// Simplifie la création de scenarios de tests pour la synchronisation
    /// </summary>
    public static class TestDataBuilder
    {
        #region Layer Creation

        /// <summary>
        /// Crée un Layer avec les valeurs par défaut pour les tests
        /// </summary>
        /// <param name="materialName">Nom du matériau</param>
        /// <param name="role">Rôle de la couche</param>
        /// <param name="order">Ordre dans la structure</param>
        /// <returns>Layer configuré pour les tests</returns>
        public static Layer CreateLayer(
            string materialName = "Test Material",
            LayerRole role = LayerRole.Base,
            int order = 1)
        {
            return new Layer
            {
                MaterialName = materialName,
                Role = role,
                Order = order,
                Thickness_m = role == LayerRole.Plateforme ? 10_000_000.0 : 0.20, // 20cm par défaut
                Modulus_MPa = 1000, // 1000 MPa par défaut
                Poisson = 0.35,
                Family = MaterialFamily.GNT,
                InterfaceWithBelow = role == LayerRole.Plateforme ? null : InterfaceType.Collee
            };
        }

        /// <summary>
        /// Crée plusieurs couches avec des noms de matériaux différents
        /// </summary>
        /// <param name="materialNames">Noms des matériaux</param>
        /// <returns>Collection de Layer</returns>
        public static ObservableCollection<Layer> CreateLayers(params string[] materialNames)
        {
            var layers = new ObservableCollection<Layer>();
            for (int i = 0; i < materialNames.Length; i++)
            {
                var role = i == materialNames.Length - 1 ? LayerRole.Plateforme :
                          i == 0 ? LayerRole.Roulement :
                          i == 1 ? LayerRole.Base : LayerRole.Fondation;
                layers.Add(CreateLayer(materialNames[i], role, i + 1));
            }
            return layers;
        }

        /// <summary>
        /// Crée une structure avec des couches ayant des noms problématiques pour les tests de string matching
        /// </summary>
        /// <returns>Collection avec des cas edge pour le string matching</returns>
        public static ObservableCollection<Layer> CreateStringMatchingTestLayers()
        {
            return new ObservableCollection<Layer>
            {
                CreateLayer("Material A", LayerRole.Roulement, 1),
                CreateLayer(" Material A ", LayerRole.Base, 2), // Espaces avant/après
                CreateLayer("MATERIAL A", LayerRole.Fondation, 3), // Casse différente
                CreateLayer("Matériau Spécial", LayerRole.Base, 4), // Caractères spéciaux
                CreateLayer("Sol Support", LayerRole.Plateforme, 5)
            };
        }

        #endregion

        #region MaterialItem Creation

        /// <summary>
        /// Crée un MaterialItem avec les valeurs par défaut pour les tests
        /// </summary>
        /// <param name="name">Nom du matériau</param>
        /// <param name="family">Famille du matériau</param>
        /// <param name="modulus">Module d'Young en MPa</param>
        /// <returns>MaterialItem configuré</returns>
        public static MaterialItem CreateMaterialItem(
            string name = "Test Material",
            MaterialFamily family = MaterialFamily.GNT,
            double modulus = 1000)
        {
            return new MaterialItem
            {
                Name = name,
                MaterialFamily = family,
                Modulus_MPa = modulus,
                PoissonRatio = 0.35,
                Description = $"Material de test: {name}",
                Category = "Test",
                Source = "Test Data",
                MinThickness_m = 0.10,
                MaxThickness_m = 0.50
            };
        }

        /// <summary>
        /// Crée une collection de MaterialItem avec des noms spécifiques
        /// </summary>
        /// <param name="names">Noms des matériaux</param>
        /// <returns>Collection de MaterialItem</returns>
        public static ObservableCollection<MaterialItem> CreateMaterialItems(params string[] names)
        {
            var items = new ObservableCollection<MaterialItem>();
            foreach (var name in names)
            {
                items.Add(CreateMaterialItem(name));
            }
            return items;
        }

        #endregion

        #region Project Creation

        /// <summary>
        /// Crée un Project avec une structure de base pour les tests
        /// </summary>
        /// <param name="projectName">Nom du projet</param>
        /// <param name="materialNames">Noms des matériaux des couches</param>
        /// <returns>Project configuré</returns>
        public static Project CreateProject(string projectName = "Test Project", params string[] materialNames)
        {
            var project = new Project
            {
                Name = projectName,
                CreatedAt = DateTime.Now,
                PavementStructure = new PavementStructure()
            };

            // Ajouter structure avec couches si spécifiées
            if (materialNames.Length > 0)
            {
                foreach (var layer in CreateLayers(materialNames))
                {
                    project.PavementStructure.Layers.Add(layer);
                }
            }

            return project;
        }

        #endregion

        #region Test Scenarios

        /// <summary>
        /// Crée un scénario de test pour les race conditions
        /// Plusieurs couches avec le même nom de matériau
        /// </summary>
        /// <returns>Données pour test de race condition</returns>
        public static (Project project, List<Action> concurrentActions) CreateRaceConditionScenario()
        {
            var project = CreateProject("Race Condition Test", "Material A", "Material B", "Sol Support");
            var actions = new List<Action>();

            // Actions qui modifient simultanément les noms de matériaux
            actions.Add(() => project.PavementStructure.Layers[0].MaterialName = "Modified A");
            actions.Add(() => project.PavementStructure.Layers[1].MaterialName = "Modified B");
            actions.Add(() => project.PavementStructure.Layers[0].MaterialName = "Final A");

            return (project, actions);
        }

        /// <summary>
        /// Crée un scénario pour tester la synchronisation string matching - cas qui fonctionnent
        /// </summary>
        /// <returns>Données avec variations de noms qui devraient matcher</returns>
        public static (ObservableCollection<Layer> layers, Dictionary<string, string[]> variations) CreateStringMatchingScenario()
        {
            var layers = CreateStringMatchingTestLayers();
            var variations = new Dictionary<string, string[]>
            {
                ["Material A"] = new[] { "Material A", " Material A ", "MATERIAL A", "material a", "Material A\t" },
                ["Matériau Spécial"] = new[] { "Matériau Spécial", "MATÉRIAU SPÉCIAL", "matériau spécial" }, // Seulement les variations qui matchent
                ["Sol Support"] = new[] { "Sol Support", "SOL SUPPORT", "sol support", " Sol Support " }
            };

            return (layers, variations);
        }

        /// <summary>
        /// Crée un scénario pour tester les cas problématiques qui causent la désynchronisation
        /// Ces cas reproduisent le problème "Non-actualisation des colonnes Matériaux" de rise.md
        /// </summary>
        /// <returns>Paires (original, problématique) qui ne matchent pas mais devraient conceptuellement</returns>
        public static (string original, string problematic, string reason)[] CreateProblematicStringMatchingCases()
        {
            return new[]
            {
                ("Matériau Spécial", "Materiau Special", "Accents vs pas d'accents"),
                ("Béton", "Beton", "É vs E"),
                ("GNT 0/31.5", "GNT 0/31,5", "Point vs virgule décimale"),
                ("Sol Support Type A", "Sol Support  Type A", "Espace simple vs double"),
                ("Material–A", "Material-A", "En dash vs hyphen"),
                ("Sablé Fin", "Sable Fin", "Différence subtile de mots"),
                ("Material A", "Material B", "Noms complètement différents"),
            };
        }

        /// <summary>
        /// Crée un scénario avec un grand nombre de couches pour les tests de performance
        /// </summary>
        /// <param name="layerCount">Nombre de couches à créer</param>
        /// <returns>Project avec nombreuses couches</returns>
        public static Project CreateLargeDatasetScenario(int layerCount = 50)
        {
            var project = new Project
            {
                Name = $"Large Dataset Test ({layerCount} layers)",
                PavementStructure = new PavementStructure()
            };

            for (int i = 1; i <= layerCount; i++)
            {
                var role = i == layerCount ? LayerRole.Plateforme :
                          i % 4 == 1 ? LayerRole.Roulement :
                          i % 4 == 2 ? LayerRole.Base : LayerRole.Fondation;
                var layer = CreateLayer($"Material {i:D3}", role, i);
                project.PavementStructure.Layers.Add(layer);
            }

            return project;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Crée des variations de noms avec différentes problématiques de string matching
        /// </summary>
        /// <param name="baseName">Nom de base</param>
        /// <returns>Variations du nom</returns>
        public static string[] CreateStringVariations(string baseName)
        {
            return new[]
            {
                baseName,
                $" {baseName} ",           // Espaces avant/après
                baseName.ToUpperInvariant(), // Tout en majuscules
                baseName.ToLowerInvariant(), // Tout en minuscules
                $"{baseName}\t",           // Tab à la fin
                $"\n{baseName}",           // Newline au début
                baseName.Replace(" ", ""),  // Sans espaces
                baseName.Replace("é", "e").Replace("à", "a") // Sans accents si applicable
            };
        }

        /// <summary>
        /// Vérifie si deux noms de matériaux devraient matcher selon la logique de l'app
        /// Reproduit la logique StringComparison.InvariantCultureIgnoreCase avec Trim()
        /// </summary>
        /// <param name="name1">Premier nom</param>
        /// <param name="name2">Deuxième nom</param>
        /// <returns>True si les noms devraient matcher</returns>
        public static bool ShouldMatch(string? name1, string? name2)
        {
            return string.Equals(name1?.Trim(), name2?.Trim(), StringComparison.InvariantCultureIgnoreCase);
        }

        #endregion

        #region ValeurAdmissible and Resultat Creation

        /// <summary>
        /// Crée un ValeurAdmissibleCoucheDto pour tests
        /// </summary>
        public static ChausseeNeuve.Domain.Models.ValeurAdmissibleCoucheDto CreateValeurAdmissibleDto(
            string materiau = "Test Material",
            int niveau = 1,
            double valeurAdmissible = 100.0)
        {
            return new ChausseeNeuve.Domain.Models.ValeurAdmissibleCoucheDto
            {
                Materiau = materiau,
                Niveau = niveau,
                Critere = "EpsiT",
                Sn = 1000000,
                Sh = 1000000,
                B = -0.2,
                Kc = 1.0,
                Kr = 1.0,
                Ks = 1.0,
                Ktheta = 1.0,
                Kd = 1.0,
                Risque = 10.0,
                Ne = 1000000,
                Epsilon6 = 100.0,
                ValeurAdmissible = valeurAdmissible,
                AmplitudeValue = 50.0,
                Sigma6 = 0.5,
                Cam = 0.1,
                E10C10Hz = 10000,
                Eteq10Hz = 10000,
                KthetaAuto = true
            };
        }

        /// <summary>
        /// Crée une ResultatCouche pour tests
        /// </summary>
        public static UI_ChausseeNeuve.ViewModels.ResultatCouche CreateResultatCouche(
            string materiau = "Test Material",
            int numero = 1,
            double valeurAdmissible = 0.0)
        {
            return new UI_ChausseeNeuve.ViewModels.ResultatCouche
            {
                Materiau = materiau,
                Numero = numero,
                Interface = "Couche",
                NiveauSup = numero * 10,
                NiveauInf = (numero + 1) * 10,
                Module = 10000,
                CoefficientPoisson = 0.35,
                ValeurAdmissible = valeurAdmissible,
                EstValide = false
            };
        }

        #endregion
    }
}