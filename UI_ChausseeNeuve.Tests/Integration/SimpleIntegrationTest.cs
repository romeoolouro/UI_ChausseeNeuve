using Microsoft.VisualStudio.TestTools.UnitTesting;
using UI_ChausseeNeuve.Tests.Helpers;
using ChausseeNeuve.Domain.Models;

namespace UI_ChausseeNeuve.Tests.Integration
{
    /// <summary>
    /// Test simple pour vérifier que la infrastructure fonctionne
    /// </summary>
    [TestClass]
    public class SimpleIntegrationTest
    {
        [TestMethod]
        [TestCategory("Integration")]
        public void CreateProject_ShouldWork()
        {
            // Test basique pour vérifier que les références fonctionnent
            var project = TestDataBuilder.CreateProject("Test");

            Assert.IsNotNull(project, "Project doit être créé");
            Assert.AreEqual("Test", project.Name, "Nom du projet doit être correct");
            Assert.IsNotNull(project.ValeursAdmissibles, "ValeursAdmissibles doit exister");
            Assert.IsNotNull(project.PavementStructure, "PavementStructure doit exister");
            Assert.IsNotNull(project.PavementStructure.Layers, "Layers doit exister");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void CreateValeurAdmissibleDto_ShouldWork()
        {
            // Test création DTO
            var dto = TestDataBuilder.CreateValeurAdmissibleDto("Test Material", 1, 100.0);

            Assert.IsNotNull(dto, "DTO doit être créé");
            Assert.AreEqual("Test Material", dto.Materiau, "Materiau doit être correct");
            Assert.AreEqual(1, dto.Niveau, "Niveau doit être correct");
            Assert.AreEqual(100.0, dto.ValeurAdmissible, 0.01, "ValeurAdmissible doit être correcte");
        }
    }
}