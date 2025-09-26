using ChausseeNeuve.Domain.Models;
using System;
using System.Collections.Generic;

namespace UI_ChausseeNeuve.Tests.Helpers
{
    /// <summary>
    /// Mock pour AppState permettant de contrôler les événements StructureChanged dans les tests
    /// Remplace le AppState static pour isoler les tests
    /// </summary>
    public class MockAppState
    {
        private Project _currentProject = new Project();
        private readonly List<Action> _structureChangedEvents = new();

        /// <summary>
        /// Project courant avec contrôle des événements pour les tests
        /// </summary>
        public Project CurrentProject
        {
            get => _currentProject;
            set
            {
                _currentProject = value;
                TriggerStructureChanged();
            }
        }

        /// <summary>
        /// Chemin du fichier courant
        /// </summary>
        public string? CurrentFilePath { get; set; }

        /// <summary>
        /// Événement StructureChanged pour surveillance dans les tests
        /// </summary>
        public event Action? StructureChanged;

        /// <summary>
        /// Liste des callbacks enregistrés (pour vérification dans les tests)
        /// </summary>
        public IReadOnlyList<Action> RegisteredCallbacks => _structureChangedEvents.AsReadOnly();

        /// <summary>
        /// Nombre de fois que StructureChanged a été déclenché
        /// </summary>
        public int StructureChangedCallCount { get; private set; }

        /// <summary>
        /// Historique des déclenchements avec timestamps
        /// </summary>
        public List<DateTime> StructureChangedHistory { get; } = new();

        /// <summary>
        /// Déclenche manuellement StructureChanged (équivalent à NotifyStructureChanged())
        /// </summary>
        public void NotifyStructureChanged()
        {
            TriggerStructureChanged();
        }

        /// <summary>
        /// Déclenche manuellement StructureChanged (équivalent à OnStructureChanged())
        /// </summary>
        public void OnStructureChanged()
        {
            TriggerStructureChanged();
        }

        /// <summary>
        /// Enregistre un callback pour l'événement StructureChanged
        /// </summary>
        /// <param name="callback">Action à exécuter lors du changement</param>
        public void RegisterStructureChangedCallback(Action callback)
        {
            if (callback != null)
            {
                _structureChangedEvents.Add(callback);
                StructureChanged += callback;
            }
        }

        /// <summary>
        /// Désabonne un callback spécifique
        /// </summary>
        /// <param name="callback">Callback à désabonner</param>
        public void UnregisterStructureChangedCallback(Action callback)
        {
            if (callback != null)
            {
                _structureChangedEvents.Remove(callback);
                StructureChanged -= callback;
            }
        }

        /// <summary>
        /// Déclenche l'événement et met à jour les compteurs
        /// </summary>
        public void TriggerStructureChanged()
        {
            StructureChangedCallCount++;
            StructureChangedHistory.Add(DateTime.Now);
            StructureChanged?.Invoke();
        }

        /// <summary>
        /// Configure le projet courant pour les tests
        /// </summary>
        public void SetCurrentProject(Project project)
        {
            CurrentProject = project;
        }

        /// <summary>
        /// Réinitialise tous les compteurs et callbacks pour un nouveau test
        /// </summary>
        public void Reset()
        {
            _currentProject = new Project();
            CurrentFilePath = null;
            StructureChangedCallCount = 0;
            StructureChangedHistory.Clear();
            _structureChangedEvents.Clear();

            // Nettoyer tous les événements
            if (StructureChanged != null)
            {
                foreach (var d in StructureChanged.GetInvocationList())
                {
                    StructureChanged -= (Action)d;
                }
            }
        }

        /// <summary>
        /// Vérifie si l'événement a été déclenché dans un délai spécifique
        /// </summary>
        /// <param name="withinMs">Délai en millisecondes</param>
        /// <returns>True si déclenché récemment</returns>
        public bool WasTriggeredWithin(int withinMs)
        {
            if (StructureChangedHistory.Count == 0)
                return false;

            var lastTrigger = StructureChangedHistory[^1];
            return (DateTime.Now - lastTrigger).TotalMilliseconds <= withinMs;
        }

        /// <summary>
        /// Simule un délai asynchrone comme BeginInvoke()
        /// </summary>
        /// <param name="delayMs">Délai avant déclenchement</param>
        /// <returns>Task pour attendre la completion</returns>
        public async System.Threading.Tasks.Task TriggerStructureChangedAfterDelayAsync(int delayMs = 50)
        {
            await System.Threading.Tasks.Task.Delay(delayMs);
            TriggerStructureChanged();
        }
    }
}