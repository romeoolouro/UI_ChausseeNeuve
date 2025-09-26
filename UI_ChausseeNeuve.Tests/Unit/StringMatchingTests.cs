using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using UI_ChausseeNeuve.Tests.Helpers;

namespace UI_ChausseeNeuve.Tests.Unit
{
    /// <summary>
    /// Tests pour valider la robustesse de la comparaison de noms de matériaux
    /// Reproduit le problème "Non-actualisation des colonnes Matériaux" (rise.md ligne 7)
    /// 
    /// Ces tests vérifient la logique StringComparison.InvariantCultureIgnoreCase avec Trim()
    /// utilisée dans ResultatViewModel.InjectValeursAdmissiblesDansResultats() ligne 953
    /// </summary>
    [TestClass]
    public class StringMatchingTests : ViewModelTestBase
    {
        #region Tests de base - Cas qui devraient fonctionner

        [TestMethod]
        [DataRow("Material A", "Material A", true)]
        [DataRow("Material A", "MATERIAL A", true)]
        [DataRow("Material A", "material a", true)]
        [DataRow("BÉTON", "béton", true)]
        [DataRow("Béton Bitumineux", "BÉTON BITUMINEUX", true)]
        [DataRow("Sol Support", "sol support", true)]
        public void MaterialNameMatching_BasicCases_ShouldMatchCorrectly(string name1, string name2, bool expectedMatch)
        {
            // Act
            var actualMatch = TestDataBuilder.ShouldMatch(name1, name2);

            // Assert
            Assert.AreEqual(expectedMatch, actualMatch,
                $"Expected '{name1}' and '{name2}' match result to be {expectedMatch}");
        }

        #endregion

        #region Tests Edge Cases - Problèmes potentiels

        [TestMethod]
        [DataRow(" Material A ", "Material A", true, "Espaces avant/après")]
        [DataRow("Material A\t", "Material A", true, "Tab à la fin")]
        [DataRow("\nMaterial A", "Material A", true, "Newline au début")]
        [DataRow("Material A\r\n", "Material A", true, "CRLF à la fin")]
        [DataRow("  Material A  ", "Material A", true, "Multiples espaces")]
        public void MaterialNameMatching_WhitespaceVariations_ShouldMatchWithTrim(string name1, string name2, bool expectedMatch, string scenario)
        {
            // Act
            var actualMatch = TestDataBuilder.ShouldMatch(name1, name2);

            // Assert
            Assert.AreEqual(expectedMatch, actualMatch,
                $"Scenario '{scenario}': Expected '{name1}' and '{name2}' match result to be {expectedMatch}");
        }

        [TestMethod]
        [DataRow("Matériau Spécial", "Materiau Special", false, "Accents vs pas d'accents")]
        [DataRow("Béton", "Beton", false, "É vs E")]
        [DataRow("Sablé", "Sable", false, "Mots différents avec accents")]
        [DataRow("Sol Cœur", "Sol Coeur", false, "Œ vs Oe")]
        public void MaterialNameMatching_AccentVariations_CurrentBehavior(string name1, string name2, bool expectedMatch, string scenario)
        {
            // Act - Documenter le comportement actuel avec InvariantCultureIgnoreCase
            var actualMatch = TestDataBuilder.ShouldMatch(name1, name2);

            // Assert - Ces tests documentent le comportement actuel qui pourrait causer des problèmes
            Assert.AreEqual(expectedMatch, actualMatch,
                $"Scenario '{scenario}': Current behavior - '{name1}' and '{name2}' match result is {expectedMatch}");
        }

        #endregion

        #region Tests Cases Null/Empty - Robustesse

        [TestMethod]
        [DataRow(null, null, true)]
        [DataRow("", "", true)]
        [DataRow(" ", " ", true)]
        [DataRow(null, "", false)]
        [DataRow("Material A", null, false)]
        [DataRow("Material A", "", false)]
        [DataRow("", "Material A", false)]
        public void MaterialNameMatching_NullEmptyCases_ShouldHandleGracefully(string? name1, string? name2, bool expectedMatch)
        {
            // Act
            var actualMatch = TestDataBuilder.ShouldMatch(name1, name2);

            // Assert
            Assert.AreEqual(expectedMatch, actualMatch,
                $"Expected null/empty handling: '{name1}' and '{name2}' should return {expectedMatch}");
        }

        #endregion

        #region Tests Scenarios Réels - Reproduction des problèmes

        [TestMethod]
        [TestCategory("Bug Reproduction")]
        public void MaterialNameMatching_RealisticsScenarios_FromUserInput()
        {
            // Arrange - Scénarios basés sur l'utilisation réelle
            var testCases = new[]
            {
                // Cas qui pourraient causer la non-actualisation des colonnes
                ("MTLH 0/20", "MTLH 0/20", true, "Identique exact"),
                ("MTLH 0/20", "mtlh 0/20", true, "Casse différente"),
                ("MTLH 0/20 ", "MTLH 0/20", true, "Espace en fin d'un côté"),
                (" MTLH 0/20", "MTLH 0/20", true, "Espace au début d'un côté"),
                ("GNT 0/31.5", "GNT 0/31,5", false, "Point vs virgule décimale"),
                ("Béton BC5", "Beton BC5", false, "Accent manquant"),
                ("Sol Support Type A", "Sol Support  Type A", false, "Double espace vs simple"),
            };

            foreach (var (name1, name2, expectedMatch, description) in testCases)
            {
                // Act
                var actualMatch = TestDataBuilder.ShouldMatch(name1, name2);

                // Assert
                Assert.AreEqual(expectedMatch, actualMatch,
                    $"Scenario réel '{description}': '{name1}' vs '{name2}' should return {expectedMatch}");
            }
        }

        [TestMethod]
        [TestCategory("Bug Reproduction")]
        public void MaterialNameMatching_ValidVariations_AllShouldMatch()
        {
            // Arrange - Utiliser les données de test avec variations qui devraient matcher
            var (layers, variations) = TestDataBuilder.CreateStringMatchingScenario();

            // Act & Assert - Vérifier que toutes les variations d'un même matériau matchent
            foreach (var kvp in variations)
            {
                var baseName = kvp.Key;
                var variationsList = kvp.Value;

                for (int i = 0; i < variationsList.Length; i++)
                {
                    for (int j = i + 1; j < variationsList.Length; j++)
                    {
                        var name1 = variationsList[i];
                        var name2 = variationsList[j];

                        var shouldMatch = TestDataBuilder.ShouldMatch(name1, name2);

                        Assert.IsTrue(shouldMatch,
                            $"Material '{baseName}' variations should match: '{name1}' vs '{name2}'");
                    }
                }
            }
        }

        [TestMethod]
        [TestCategory("Bug Reproduction")]
        public void MaterialNameMatching_ProblematicCases_DocumentDesynchronizationCauses()
        {
            // Arrange - Cas qui causent la désynchronisation dans l'app réelle
            var problematicCases = TestDataBuilder.CreateProblematicStringMatchingCases();

            // Act & Assert - Documenter les cas où la synchronisation échoue
            foreach (var (original, problematic, reason) in problematicCases)
            {
                var matches = TestDataBuilder.ShouldMatch(original, problematic);

                // Ces cas NE MATCHENT PAS avec la logique actuelle - c'est la source des bugs
                Assert.IsFalse(matches,
                    $"EXPECTED FAILURE - {reason}: '{original}' vs '{problematic}' should NOT match with current logic, causing desync");

                // Log pour diagnostic
                System.Diagnostics.Debug.WriteLine(
                    $"Desync cause '{reason}': '{original}' vs '{problematic}' = {matches}");
            }
        }

        #endregion

        #region Performance Tests - Grande quantité de comparaisons

        [TestMethod]
        [TestCategory("Performance")]
        public void MaterialNameMatching_Performance_ManyComparisons()
        {
            // Arrange
            var materialNames = new string[100];
            for (int i = 0; i < 100; i++)
            {
                materialNames[i] = $"Material {i:D3}";
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act - Comparer tous avec tous (10,000 comparaisons)
            int matchCount = 0;
            for (int i = 0; i < materialNames.Length; i++)
            {
                for (int j = 0; j < materialNames.Length; j++)
                {
                    if (TestDataBuilder.ShouldMatch(materialNames[i], materialNames[j]))
                        matchCount++;
                }
            }

            stopwatch.Stop();

            // Assert
            Assert.AreEqual(100, matchCount, "Should have exactly 100 matches (each with itself)");
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000,
                $"String matching should be fast: took {stopwatch.ElapsedMilliseconds}ms for 10,000 comparisons");
        }

        #endregion

        #region Tests Integration avec TestDataBuilder

        [TestMethod]
        public void TestDataBuilder_StringVariations_ShouldCreateVariedCases()
        {
            // Arrange
            var baseName = "Material Test";

            // Act
            var variations = TestDataBuilder.CreateStringVariations(baseName);

            // Assert
            Assert.IsTrue(variations.Length >= 6, "Should create multiple variations");
            Assert.AreEqual(baseName, variations[0], "First variation should be original");

            // Vérifier que les variations contiennent différents types de modifications
            var hasUpperCase = false;
            var hasLowerCase = false;
            var hasSpaces = false;

            foreach (var variation in variations)
            {
                if (variation.Contains(baseName.ToUpperInvariant()))
                    hasUpperCase = true;
                if (variation.Contains(baseName.ToLowerInvariant()))
                    hasLowerCase = true;
                if (variation.StartsWith(" ") || variation.EndsWith(" "))
                    hasSpaces = true;
            }

            Assert.IsTrue(hasUpperCase, "Should include uppercase variation");
            Assert.IsTrue(hasLowerCase, "Should include lowercase variation");
            Assert.IsTrue(hasSpaces, "Should include space variations");
        }

        #endregion

        #region Tests Diagnostiques - Pour identifier les causes exactes

        [TestMethod]
        [TestCategory("Diagnostics")]
        public void StringComparison_DirectTest_InvariantCultureIgnoreCase()
        {
            // Test direct de la logique utilisée dans ResultatViewModel ligne 953
            var testCases = new[]
            {
                ("Material A", "Material A", true),
                ("Material A", " Material A ", true),  // Avec Trim()
                ("Material A", "MATERIAL A", true),
                ("Matériau", "MATÉRIAU", true),
                ("Matériau", "Materiau", false), // Sans normalisation des accents
            };

            foreach (var (name1, name2, expected) in testCases)
            {
                // Act - Reproduire exactement la logique de l'application
                var actual = string.Equals(name1?.Trim(), name2?.Trim(), StringComparison.InvariantCultureIgnoreCase);

                // Assert
                Assert.AreEqual(expected, actual,
                    $"StringComparison.InvariantCultureIgnoreCase with Trim(): '{name1}' vs '{name2}'");
            }
        }

        [TestMethod]
        [TestCategory("Diagnostics")]
        public void StringMatching_EdgeCasesThatMayFailSynchronization()
        {
            // Arrange - Cas spécifiques qui pourraient causer des problèmes de synchronisation
            var problematicCases = new[]
            {
                // Ces cas pourraient expliquer pourquoi les colonnes ne s'actualisent pas
                ("Material\u00A0A", "Material A", "Non-breaking space vs regular space"),
                ("Material A", "Material A\u200B", "Zero-width space"),
                ("Material–A", "Material-A", "En dash vs hyphen"),
                ("Material A", "Material\u2009A", "Thin space"),
            };

            foreach (var (name1, name2, description) in problematicCases)
            {
                // Act
                var matches = TestDataBuilder.ShouldMatch(name1, name2);

                // Log le résultat pour diagnostic
                System.Diagnostics.Debug.WriteLine(
                    $"Edge case '{description}': '{name1}' vs '{name2}' = {matches}");

                // Ces tests documentent le comportement actuel
                // Les failures ici indiqueraient des causes potentielles de non-synchronisation
            }
        }

        #endregion
    }
}