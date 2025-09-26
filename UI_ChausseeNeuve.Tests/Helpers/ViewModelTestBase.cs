using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using UI_ChausseeNeuve.Tests.Helpers;

namespace UI_ChausseeNeuve.Tests.Helpers
{
    /// <summary>
    /// Classe de base pour les tests de ViewModels avec fonctionnalités communes MVVM
    /// Simplifie les tests d'INotifyPropertyChanged et la gestion des événements
    /// </summary>
    [TestClass]
    public abstract class ViewModelTestBase
    {
        protected MockAppState MockAppState { get; private set; } = null!;
        protected List<string> PropertyChangedEvents { get; private set; } = null!;

        [TestInitialize]
        public virtual void TestInitialize()
        {
            // Initialiser le MockAppState pour chaque test
            MockAppState = new MockAppState();
            PropertyChangedEvents = new List<string>();
        }

        [TestCleanup]
        public virtual void TestCleanup()
        {
            // Nettoyer l'état entre les tests
            MockAppState?.Reset();
            PropertyChangedEvents?.Clear();
        }

        #region PropertyChanged Helpers

        /// <summary>
        /// Enregistre les événements PropertyChanged d'un ViewModel
        /// </summary>
        /// <param name="viewModel">ViewModel à surveiller</param>
        protected void MonitorPropertyChanged(INotifyPropertyChanged viewModel)
        {
            if (viewModel == null)
                throw new ArgumentNullException(nameof(viewModel));

            viewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName != null)
                    PropertyChangedEvents.Add(e.PropertyName);
            };
        }

        /// <summary>
        /// Vérifie qu'un PropertyChanged a été déclenché pour une propriété spécifique
        /// </summary>
        /// <param name="propertyName">Nom de la propriété</param>
        protected void AssertPropertyChanged(string propertyName)
        {
            Assert.IsTrue(PropertyChangedEvents.Contains(propertyName),
                $"PropertyChanged event for '{propertyName}' was not triggered. Triggered events: [{string.Join(", ", PropertyChangedEvents)}]");
        }

        /// <summary>
        /// Vérifie qu'aucun PropertyChanged n'a été déclenché pour une propriété
        /// </summary>
        /// <param name="propertyName">Nom de la propriété</param>
        protected void AssertPropertyNotChanged(string propertyName)
        {
            Assert.IsFalse(PropertyChangedEvents.Contains(propertyName),
                $"PropertyChanged event for '{propertyName}' was unexpectedly triggered.");
        }

        /// <summary>
        /// Vérifie l'ordre des événements PropertyChanged
        /// </summary>
        /// <param name="expectedOrder">Ordre attendu des propriétés</param>
        protected void AssertPropertyChangedOrder(params string[] expectedOrder)
        {
            Assert.AreEqual(expectedOrder.Length, PropertyChangedEvents.Count,
                $"Expected {expectedOrder.Length} PropertyChanged events, but got {PropertyChangedEvents.Count}");

            for (int i = 0; i < expectedOrder.Length; i++)
            {
                Assert.AreEqual(expectedOrder[i], PropertyChangedEvents[i],
                    $"Expected property '{expectedOrder[i]}' at position {i}, but got '{PropertyChangedEvents[i]}'");
            }
        }

        /// <summary>
        /// Nettoie la liste des événements PropertyChanged
        /// </summary>
        protected void ClearPropertyChangedEvents()
        {
            PropertyChangedEvents.Clear();
        }

        #endregion

        #region AppState Event Helpers

        /// <summary>
        /// Vérifie que l'événement StructureChanged a été déclenché
        /// </summary>
        /// <param name="expectedCount">Nombre attendu de déclenchements (optionnel)</param>
        protected void AssertStructureChanged(int? expectedCount = null)
        {
            if (expectedCount.HasValue)
            {
                Assert.AreEqual(expectedCount.Value, MockAppState.StructureChangedCallCount,
                    $"Expected StructureChanged to be called {expectedCount.Value} times, but was called {MockAppState.StructureChangedCallCount} times");
            }
            else
            {
                Assert.IsTrue(MockAppState.StructureChangedCallCount > 0,
                    "Expected StructureChanged to be triggered at least once");
            }
        }

        /// <summary>
        /// Vérifie qu'aucun événement StructureChanged n'a été déclenché
        /// </summary>
        protected void AssertStructureNotChanged()
        {
            Assert.AreEqual(0, MockAppState.StructureChangedCallCount,
                $"Expected StructureChanged to not be triggered, but was called {MockAppState.StructureChangedCallCount} times");
        }

        /// <summary>
        /// Attend qu'un événement asynchrone se déclenche dans un délai donné
        /// </summary>
        /// <param name="timeoutMs">Délai d'attente en millisecondes</param>
        /// <returns>Task pour attendre l'événement</returns>
        protected async Task WaitForStructureChangedAsync(int timeoutMs = 1000)
        {
            var startCount = MockAppState.StructureChangedCallCount;
            var startTime = DateTime.Now;

            while (MockAppState.StructureChangedCallCount == startCount)
            {
                if ((DateTime.Now - startTime).TotalMilliseconds > timeoutMs)
                {
                    Assert.Fail($"StructureChanged was not triggered within {timeoutMs}ms");
                }

                await Task.Delay(10); // Attendre un peu avant de revérifier
            }
        }

        #endregion

        #region Threading Helpers

        /// <summary>
        /// Exécute une action et attend que tous les événements asynchrones se stabilisent
        /// </summary>
        /// <param name="action">Action à exécuter</param>
        /// <param name="stabilizationDelayMs">Délai pour laisser les événements se stabiliser</param>
        protected async Task ExecuteAndStabilizeAsync(Action action, int stabilizationDelayMs = 100)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            // Sauvegarder l'état avant l'action
            var initialPropertyCount = PropertyChangedEvents.Count;
            var initialStructureCount = MockAppState.StructureChangedCallCount;

            // Exécuter l'action
            action.Invoke();

            // Attendre la stabilisation
            await Task.Delay(stabilizationDelayMs);

            // Optionnel: log pour debugging
            var propertyChanges = PropertyChangedEvents.Count - initialPropertyCount;
            var structureChanges = MockAppState.StructureChangedCallCount - initialStructureCount;

            System.Diagnostics.Debug.WriteLine(
                $"Action triggered {propertyChanges} PropertyChanged and {structureChanges} StructureChanged events");
        }

        /// <summary>
        /// Simule des actions concurrentes pour tester les race conditions
        /// </summary>
        /// <param name="actions">Actions à exécuter en parallèle</param>
        /// <returns>Task pour attendre toutes les actions</returns>
        protected async Task ExecuteConcurrentActionsAsync(params Action[] actions)
        {
            if (actions == null || actions.Length == 0)
                return;

            await TestDispatcherHelper.ExecuteConcurrentlyAsync(actions);

            // Petit délai pour laisser les événements se propager
            await Task.Delay(50);
        }

        #endregion

        #region Assertion Helpers

        /// <summary>
        /// Vérifie qu'une valeur est dans une plage attendue (pour les tests de timing)
        /// </summary>
        /// <param name="actual">Valeur actuelle</param>
        /// <param name="expected">Valeur attendue</param>
        /// <param name="tolerance">Tolérance</param>
        /// <param name="message">Message d'erreur</param>
        protected void AssertWithinRange(double actual, double expected, double tolerance, string? message = null)
        {
            var diff = Math.Abs(actual - expected);
            Assert.IsTrue(diff <= tolerance,
                message ?? $"Expected {expected} ± {tolerance}, but got {actual} (difference: {diff})");
        }

        /// <summary>
        /// Vérifie qu'une chaîne match selon la logique de l'application
        /// </summary>
        /// <param name="actual">Valeur actuelle</param>
        /// <param name="expected">Valeur attendue</param>
        /// <param name="message">Message d'erreur</param>
        protected void AssertStringMatch(string? actual, string? expected, string? message = null)
        {
            var matches = TestDataBuilder.ShouldMatch(actual, expected);
            Assert.IsTrue(matches,
                message ?? $"Expected '{actual}' to match '{expected}' using application logic");
        }

        #endregion
    }
}