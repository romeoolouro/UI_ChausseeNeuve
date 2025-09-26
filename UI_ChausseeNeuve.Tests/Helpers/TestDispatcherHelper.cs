using System;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace UI_ChausseeNeuve.Tests.Helpers
{
    /// <summary>
    /// Helper class pour tester les composants WPF qui utilisent Dispatcher.BeginInvoke()
    /// Permet de simuler les race conditions et timing issues dans les tests.
    /// </summary>
    public static class TestDispatcherHelper
    {
        /// <summary>
        /// Simule Dispatcher.BeginInvoke() de manière synchrone pour les tests
        /// </summary>
        /// <param name="action">Action à exécuter</param>
        public static void ExecuteSync(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            action.Invoke();
        }

        /// <summary>
        /// Simule Dispatcher.BeginInvoke() avec délai pour tester les race conditions
        /// </summary>
        /// <param name="action">Action à exécuter</param>
        /// <param name="delayMs">Délai en millisecondes</param>
        /// <returns>Task pour attendre la completion</returns>
        public static async Task ExecuteWithDelayAsync(Action action, int delayMs = 50)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            await Task.Delay(delayMs);
            action.Invoke();
        }

        /// <summary>
        /// Simule des appels BeginInvoke() concurrents pour tester les race conditions
        /// </summary>
        /// <param name="actions">Actions à exécuter en parallèle</param>
        /// <returns>Task pour attendre toutes les completions</returns>
        public static async Task ExecuteConcurrentlyAsync(params Action[] actions)
        {
            if (actions == null || actions.Length == 0)
                return;

            var tasks = new Task[actions.Length];
            for (int i = 0; i < actions.Length; i++)
            {
                var action = actions[i];
                tasks[i] = Task.Run(() => action.Invoke());
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Vérifie si le code s'exécute sur le Dispatcher thread
        /// Utilisé pour valider les tests de threading
        /// </summary>
        /// <returns>True si sur UI thread, false sinon</returns>
        public static bool IsOnUIThread()
        {
            try
            {
                var dispatcher = Dispatcher.CurrentDispatcher;
                return dispatcher != null && dispatcher.CheckAccess();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Force l'exécution sur le UI thread si disponible
        /// </summary>
        /// <param name="action">Action à exécuter</param>
        public static void InvokeOnUIThread(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            try
            {
                var dispatcher = Dispatcher.CurrentDispatcher;
                if (dispatcher != null && dispatcher.CheckAccess())
                {
                    action.Invoke();
                }
                else if (dispatcher != null)
                {
                    dispatcher.Invoke(action);
                }
                else
                {
                    // Fallback pour les tests sans UI thread
                    action.Invoke();
                }
            }
            catch
            {
                // Fallback pour les tests
                action.Invoke();
            }
        }
    }
}