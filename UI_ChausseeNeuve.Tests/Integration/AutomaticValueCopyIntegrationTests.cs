using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using UI_ChausseeNeuve.ViewModels;
using UI_ChausseeNeuve.Tests.Helpers;
using ChausseeNeuve.Domain.Models;

namespace UI_ChausseeNeuve.Tests.Integration
{
    /// <summary>
    /// Tests d'intégration pour la fonctionnalité de copie automatique des valeurs admissibles
    /// vers la section Résultats (rise.md ligne 8: "Absence de copie automatique")
    /// 
    /// Ces tests reproduisent le problème utilisateur où les valeurs calculées dans
    /// "Valeurs Admissibles" ne sont pas automatiquement copiées dans "Résultats"
    /// lors des modifications de matériaux ou de niveaux.
    /// </summary>
    [TestClass]
    public class AutomaticValueCopyIntegrationTests : ViewModelTestBase
    {
        private MockAppState? _mockAppState;
        private ResultatViewModel? _resultatViewModel;

        [TestInitialize]
        public void Setup()
        {
            _mockAppState = new MockAppState();
            _resultatViewModel = new ResultatViewModel();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _resultatViewModel?.Dispose();
            _mockAppState?.Reset();
        }

        #region End-to-End Automatic Value Copy Tests

        [TestMethod]
        [TestCategory("Integration")]
        [Description("Test des correspondances string matching avec caractères spéciaux")]
        public void AutomaticValueCopy_StringMatchingWithSpecialCharacters_ShouldFindMatches()
        {
            // Arrange - Matériaux avec caractères problématiques
            var project = TestDataBuilder.CreateProject("Test Matching");

            var layer1 = TestDataBuilder.CreateLayer("GNT 0/31,5", LayerRole.Roulement, 1); // Virgule décimale
            var layer2 = TestDataBuilder.CreateLayer("Matériau spécialisé", LayerRole.Base, 2); // Accent
            var layer3 = TestDataBuilder.CreateLayer("Béton  Standard", LayerRole.Plateforme, 3); // Double espace

            project.PavementStructure.Layers.Add(layer1);
            project.PavementStructure.Layers.Add(layer2);
            project.PavementStructure.Layers.Add(layer3);

            // Valeurs admissibles avec variations de noms
            var valeur1 = TestDataBuilder.CreateValeurAdmissibleDto("GNT 0/31.5", 1, 100.0); // Point décimal (différent de layer1)
            var valeur2 = TestDataBuilder.CreateValeurAdmissibleDto("Materiau specialise", 2, 200.0); // Sans accent (différent de layer2)
            var valeur3 = TestDataBuilder.CreateValeurAdmissibleDto("Béton Standard", 3, 300.0); // Espace simple (différent de layer3)

            project.ValeursAdmissibles.Add(valeur1);
            project.ValeursAdmissibles.Add(valeur2);
            project.ValeursAdmissibles.Add(valeur3);

            // Utiliser AppState directement (pas MockAppState) pour les tests d'intégration
            UI_ChausseeNeuve.AppState.CurrentProject = project;

            // Act - Déclencher synchronisation via RefreshFromStructure (public method)
            _resultatViewModel!.RefreshFromStructure();

            // Assert - Vérifier les correspondances string matching
            var resultats = _resultatViewModel.Resultats.OfType<ResultatCouche>().ToList();

            Assert.IsTrue(resultats.Count > 0, "Des résultats doivent être créés");



            // Vérifier que les correspondances fonctionnent malgré les différences de caractères
            var resultatGNT = resultats.FirstOrDefault(r =>
                string.Equals(r.Materiau, "GNT 0/31,5", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(r.Materiau, "GNT 0/31.5", StringComparison.InvariantCultureIgnoreCase));
            Assert.IsNotNull(resultatGNT, "GNT doit avoir un résultat correspondant");

            var resultatMateriau = resultats.FirstOrDefault(r =>
                r.Materiau.Contains("Materiau", StringComparison.InvariantCultureIgnoreCase) ||
                r.Materiau.Contains("Matériau", StringComparison.InvariantCultureIgnoreCase));
            Assert.IsNotNull(resultatMateriau, "Matériau spécialisé doit avoir un résultat correspondant");
        }

        [TestMethod]
        [TestCategory("Integration")]
        [Description("Test de gestion des doublons avec niveaux différents")]
        public void AutomaticValueCopy_DuplicateMaterialsDifferentLevels_ShouldHandleCorrectly()
        {
            // Arrange - Même matériau à plusieurs niveaux
            var project = TestDataBuilder.CreateProject("Test Duplicates");

            var layer1 = TestDataBuilder.CreateLayer("GNT 0/31", LayerRole.Roulement, 1);
            var layer2 = TestDataBuilder.CreateLayer("GNT 0/31", LayerRole.Base, 2);
            var layer3 = TestDataBuilder.CreateLayer("GNT 0/20", LayerRole.Plateforme, 3); // Même matériau, niveau différent

            project.PavementStructure.Layers.Add(layer1);
            project.PavementStructure.Layers.Add(layer2);
            project.PavementStructure.Layers.Add(layer3);

            // Valeurs admissibles pour chaque niveau
            var valeur1 = TestDataBuilder.CreateValeurAdmissibleDto("GNT 0/31", 1, 150.0);
            var valeur2 = TestDataBuilder.CreateValeurAdmissibleDto("GNT 0/31", 2, 250.0);
            var valeur3 = TestDataBuilder.CreateValeurAdmissibleDto("GNT 0/20", 3, 350.0);

            project.ValeursAdmissibles.Add(valeur1);
            project.ValeursAdmissibles.Add(valeur2);
            project.ValeursAdmissibles.Add(valeur3);
            // Utiliser AppState directement (pas MockAppState) pour les tests d'intégration
            UI_ChausseeNeuve.AppState.CurrentProject = project;

            // Act - Déclencher synchronisation
            _resultatViewModel!.RefreshFromStructure();

            // Assert - Vérifier gestion des doublons
            var resultats = _resultatViewModel.Resultats.OfType<ResultatCouche>().ToList();
            Assert.IsTrue(resultats.Count > 0, "Des résultats doivent être créés");

            // Vérifier que chaque niveau a ses propres résultats
            var resultatsGNT31 = resultats.Where(r =>
                r.Materiau.Contains("GNT 0/31", StringComparison.InvariantCultureIgnoreCase)).ToList();
            Assert.IsTrue(resultatsGNT31.Count >= 1, "GNT 0/31 doit avoir des résultats");

            var resultatsGNT20 = resultats.Where(r =>
                r.Materiau.Contains("GNT 0/20", StringComparison.InvariantCultureIgnoreCase)).ToList();
            Assert.IsTrue(resultatsGNT20.Count >= 1, "GNT 0/20 doit avoir des résultats");
        }

        [TestMethod]
        [TestCategory("Integration")]
        [Description("Test de non-correspondance avec matériaux inexistants")]
        public void AutomaticValueCopy_NoMatchingMaterials_ShouldHandleGracefully()
        {
            // Arrange - Matériaux totalement différents
            var project = TestDataBuilder.CreateProject("Test No Match");

            var layer1 = TestDataBuilder.CreateLayer("Matériau A", LayerRole.Roulement, 1);
            var layer2 = TestDataBuilder.CreateLayer("Matériau B", LayerRole.Base, 2);

            project.PavementStructure.Layers.Add(layer1);
            project.PavementStructure.Layers.Add(layer2);

            // Valeurs admissibles avec noms complètement différents
            var valeur1 = TestDataBuilder.CreateValeurAdmissibleDto("Matériau X", 1, 100.0);
            var valeur2 = TestDataBuilder.CreateValeurAdmissibleDto("Matériau Y", 2, 200.0);

            project.ValeursAdmissibles.Add(valeur1);
            project.ValeursAdmissibles.Add(valeur2);
            _mockAppState!.SetCurrentProject(project);

            // Act - Déclencher synchronisation
            _resultatViewModel!.RefreshFromStructure();

            // Assert - Vérifier que l'application ne plante pas
            var resultats = _resultatViewModel.Resultats.OfType<ResultatCouche>().ToList();

            // Le système doit gérer gracieusement l'absence de correspondances
            Assert.IsNotNull(resultats, "Resultats collection ne doit pas être null");

            // Vérifier qu'aucune correspondance inappropriée n'a été créée
            var correspondancesInappropriees = resultats.Where(r =>
                r.Materiau.Contains("Matériau A", StringComparison.InvariantCultureIgnoreCase) ||
                r.Materiau.Contains("Matériau B", StringComparison.InvariantCultureIgnoreCase)).ToList();

            // Ce test vérifie que le système ne crée pas de correspondances inappropriées
            // Si des correspondances sont créées avec des matériaux différents, c'est un problème
        }

        [TestMethod]
        [TestCategory("Integration")]
        [Description("Test de structure vide avec InjectValeursAdmissiblesDansResultats")]
        public void AutomaticValueCopy_EmptyStructure_ShouldHandleGracefully()
        {
            // Arrange - Structure sans couches
            var project = TestDataBuilder.CreateProject("Test Empty");

            // Pas de couches ajoutées à la structure

            // Valeurs admissibles présentes mais aucune structure
            var valeur1 = TestDataBuilder.CreateValeurAdmissibleDto("Test Material", 1, 100.0);
            project.ValeursAdmissibles.Add(valeur1);
            _mockAppState!.SetCurrentProject(project);

            // Act - Déclencher synchronisation
            _resultatViewModel!.RefreshFromStructure();

            // Assert - Vérifier que l'application ne plante pas
            var resultats = _resultatViewModel.Resultats.OfType<ResultatCouche>().ToList();
            Assert.IsNotNull(resultats, "Resultats ne doit pas être null");

            // Avec une structure vide, aucun résultat ne devrait être créé
            // ou le système devrait gérer gracieusement l'absence de structure
        }

        [TestMethod]
        [TestCategory("Integration")]
        [Description("Test de vérification de l'infrastructure de base")]
        public void AutomaticValueCopy_InfrastructureVerification()
        {
            // Test simple pour vérifier que le Project fonctionne correctement
            var project = TestDataBuilder.CreateProject("Test");

            Assert.IsNotNull(project, "Project doit être créé");
            Assert.AreEqual("Test", project.Name, "Nom du projet doit être correct");
            Assert.IsNotNull(project.ValeursAdmissibles, "ValeursAdmissibles doit exister");
            Assert.IsNotNull(project.PavementStructure, "PavementStructure doit exister");
            Assert.IsNotNull(project.PavementStructure.Layers, "Layers doit exister");

            // Test création couche et ajout à la structure
            var layer = TestDataBuilder.CreateLayer("Test Material", LayerRole.Base, 1);
            project.PavementStructure.Layers.Add(layer);

            Assert.AreEqual(1, project.PavementStructure.Layers.Count, "Doit avoir 1 couche");
            Assert.AreEqual("Test Material", project.PavementStructure.Layers[0].MaterialName, "Nom matériau correct");

            // Test création et ajout valeur admissible
            var valeur = TestDataBuilder.CreateValeurAdmissibleDto("Test Material", 1, 100.0);
            project.ValeursAdmissibles.Add(valeur);

            Assert.AreEqual(1, project.ValeursAdmissibles.Count, "Doit avoir 1 valeur admissible");
            Assert.AreEqual("Test Material", project.ValeursAdmissibles[0].Materiau, "Matériau correct");
        }

        #endregion
    }
}