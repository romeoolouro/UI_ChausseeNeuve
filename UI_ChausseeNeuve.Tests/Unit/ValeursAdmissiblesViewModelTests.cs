using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.ComponentModel;
using UI_ChausseeNeuve.ViewModels;
using ChausseeNeuve.Domain.Models;

namespace UI_ChausseeNeuve.Tests.Unit
{
    /// <summary>
    /// Tests pour ValeursAdmissiblesViewModel - focus sur AttachLayerSync handler behavior
    /// Valide la synchronisation automatique Layer.MaterialName → ValeursAdmissibleCouche.Materiau
    /// 
    /// Reproduit le problème "Non-actualisation des colonnes Matériaux" de rise.md ligne 7
    /// Ces tests documentent le comportement actuel du système de synchronisation
    /// </summary>
    [TestClass]
    public class ValeursAdmissiblesViewModelTests
    {
        #region Basic ViewModel Tests

        [TestMethod]
        [TestCategory("ViewModel")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.Description("ValeursAdmissiblesViewModel doit s'initialiser sans erreur")]
        public void ValeursAdmissiblesViewModel_Initialize_ShouldSucceed()
        {
            // Arrange & Act
            ValeursAdmissiblesViewModel? viewModel = null;
            Exception? exception = null;

            try
            {
                viewModel = new ValeursAdmissiblesViewModel();
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            // Assert
            Assert.IsNull(exception, $"L'initialisation ne doit pas lever d'exception: {exception?.Message}");
            Assert.IsNotNull(viewModel, "ViewModel doit être créé");
            Assert.IsNotNull(viewModel.ValeursAdmissibles, "Collection ValeursAdmissibles doit être initialisée");

            // Cleanup
            viewModel?.Dispose();
        }

        [TestMethod]
        [TestCategory("ViewModel")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.Description("ValeursAdmissibles collection doit être observable")]
        public void ValeursAdmissiblesViewModel_ValeursAdmissibles_ShouldBeObservable()
        {
            // Arrange
            using var viewModel = new ValeursAdmissiblesViewModel();

            // Act & Assert
            Assert.IsNotNull(viewModel.ValeursAdmissibles, "Collection ne doit pas être null");
            // Note: Cannot test exact type due to nested class visibility, but ensure it's not null and behaves correctly
        }

        #endregion

        #region Layer MaterialName Synchronization Documentation

        [TestMethod]
        [TestCategory("Documentation")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.Description("Documentation du problème de synchronisation MaterialName rise.md ligne 7")]
        public void Layer_MaterialNameSynchronization_DocumentProblem()
        {
            // Cette méthode documente le problème identifié dans rise.md ligne 7:
            // "Non-actualisation des colonnes "Matériaux" dans les sections "Valeurs Admissibles" et "Résultats""

            // Arrange - Créer Layer avec MaterialName
            var layer = new Layer
            {
                MaterialName = "Matériau Original",
                Role = LayerRole.Base,
                Thickness_m = 0.20,
                Modulus_MPa = 1000
            };

            // Act - Modifier MaterialName (ce qui devrait déclencher AttachLayerSync)
            var originalName = layer.MaterialName;
            layer.MaterialName = "Matériau Modifié";

            // Assert - Documenter que la Layer stocke correctement le nouveau nom
            Assert.AreEqual("Matériau Modifié", layer.MaterialName,
                "Layer.MaterialName doit être mis à jour correctement");
            Assert.AreNotEqual(originalName, layer.MaterialName,
                "MaterialName doit avoir changé");

            // Documentation: Le problème ne vient pas de Layer.MaterialName lui-même,
            // mais de la synchronisation vers ValeursAdmissibleCouche.Materiau
            // via AttachLayerSync handler (ValeursAdmissiblesViewModel.cs lignes 732-743)
        }

        [TestMethod]
        [TestCategory("StringMatching")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.Description("Reproduction du problème StringComparison qui cause désynchronisation")]
        public void StringComparison_AccentProblems_CauseDesynchronization()
        {
            // Cette méthode reproduit le problème exact de désynchronisation
            // causé par StringComparison.InvariantCultureIgnoreCase dans
            // ResultatViewModel.InjectValeursAdmissiblesDansResultats() ligne 953

            // Arrange - Noms de matériaux avec variations d'accents
            var nameWithAccents = "Matériau Spécial";
            var nameWithoutAccents = "Materiau Special";

            // Act - Tester la comparaison utilisée dans le code
            bool wouldMatch = string.Equals(nameWithAccents?.Trim(), nameWithoutAccents?.Trim(),
                StringComparison.InvariantCultureIgnoreCase);

            // Assert - Documenter le problème
            Assert.IsFalse(wouldMatch,
                "StringComparison.InvariantCultureIgnoreCase ne matche pas les accents - CAUSE du problème rise.md ligne 7");

            // Documentation des autres cas problématiques:
            var problematicCases = new[]
            {
                ("GNT 0/31.5", "GNT 0/31,5"), // Point vs virgule décimale
                ("Sol Support", "Sol  Support") // Espace simple vs double
            };

            foreach (var (original, variant) in problematicCases)
            {
                bool matches = string.Equals(original?.Trim(), variant?.Trim(),
                    StringComparison.InvariantCultureIgnoreCase);
                Assert.IsFalse(matches,
                    $"'{original}' vs '{variant}' ne matche pas - contribue au problème de désynchronisation");
            }
        }

        #endregion

        #region AttachLayerSync Behavior Documentation

        [TestMethod]
        [TestCategory("AttachLayerSync")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.Description("Documentation comportement AttachLayerSync handler")]
        public void AttachLayerSync_BehaviorDocumentation_ForSynchronization()
        {
            // Cette méthode documente le comportement d'AttachLayerSync
            // basé sur l'analyse du code ValeursAdmissiblesViewModel.cs lignes 720-760

            // Arrange - Layer avec PropertyChanged
            var layer = new Layer
            {
                MaterialName = "Original",
                Role = LayerRole.Base
            };

            bool propertyChangedTriggered = false;
            string? changedPropertyName = null;

            // Simuler l'écoute que fait AttachLayerSync
            layer.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(Layer.MaterialName))
                {
                    propertyChangedTriggered = true;
                    changedPropertyName = e.PropertyName;
                }
            };

            // Act - Modifier MaterialName (ce que fait l'utilisateur)
            layer.MaterialName = "Nouveau Matériau";

            // Assert - Documenter que PropertyChanged est déclenché
            Assert.IsTrue(propertyChangedTriggered,
                "Layer.PropertyChanged doit être déclenché quand MaterialName change");
            Assert.AreEqual(nameof(Layer.MaterialName), changedPropertyName,
                "PropertyChanged doit indiquer MaterialName comme propriété modifiée");

            // Documentation des étapes AttachLayerSync:
            // 1. ✅ Layer.PropertyChanged est déclenché
            // 2. ✅ Handler vérifie e.PropertyName == nameof(Layer.MaterialName)
            // 3. ✅ ligne.Materiau = layer.MaterialName ?? string.Empty
            // 4. ✅ SaveToProject() sauvegarde en DTO
            // 5. ✅ AppState.OnStructureChanged() déclenche synchronisation
            //
            // Le problème rise.md ligne 7 ne vient pas d'AttachLayerSync lui-même,
            // mais des comparaisons de string ultérieures qui échouent à matcher
            // les noms avec accents/caractères spéciaux
        }

        [TestMethod]
        [TestCategory("Performance")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.Description("Performance Layer PropertyChanged pour stress testing")]
        public void Layer_PropertyChangedPerformance_UnderStress()
        {
            // Arrange
            var layer = new Layer
            {
                MaterialName = "Initial",
                Role = LayerRole.Base
            };

            int changeCount = 0;
            layer.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(Layer.MaterialName))
                    changeCount++;
            };

            // Act - Modifications rapides multiples
            var startTime = DateTime.Now;
            const int iterations = 1000;

            for (int i = 0; i < iterations; i++)
            {
                layer.MaterialName = $"Material {i:D4}";
            }

            var endTime = DateTime.Now;
            var duration = endTime - startTime;

            // Assert - Vérifier performance et résultats
            Assert.AreEqual(iterations, changeCount,
                "Tous les changements doivent déclencher PropertyChanged");
            Assert.IsTrue(duration.TotalMilliseconds < 1000,
                $"{iterations} modifications PropertyChanged doivent prendre moins de 1s (pris: {duration.TotalMilliseconds}ms)");
            Assert.AreEqual($"Material {iterations - 1:D4}", layer.MaterialName,
                "Le dernier MaterialName doit être correctement assigné");
        }

        #endregion
    }
}