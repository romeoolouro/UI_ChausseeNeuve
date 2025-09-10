using ChausseeNeuve.Domain.Models;
using System;

namespace UI_ChausseeNeuve 
{ 
    public static class AppState 
    { 
        private static Project _currentProject = new Project();
        
        public static Project CurrentProject 
        { 
            get => _currentProject;
            set 
            {
                _currentProject = value;
                StructureChanged?.Invoke();
            }
        }
        
        public static string? CurrentFilePath { get; set; } = null;
        
        /// <summary>
        /// Événement déclenché quand la structure change
        /// </summary>
        public static event Action? StructureChanged;
        
        /// <summary>
        /// Méthode pour déclencher manuellement la notification de changement de structure
        /// À appeler depuis StructureEditorViewModel quand une couche est modifiée
        /// </summary>
        public static void NotifyStructureChanged()
        {
            StructureChanged?.Invoke();
        }
    } 
}
