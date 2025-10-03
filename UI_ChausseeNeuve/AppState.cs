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
        /// �v�nement d�clench� quand la structure change
        /// </summary>
        public static event Action? StructureChanged;

        /// <summary>
        /// Nouvel �v�nement d�clench� quand les valeurs admissibles sont recalcul�es/mises � jour
        /// (�vite de reconstruire toute la structure c�t� r�sultats)
        /// </summary>
        public static event Action? ValeursAdmissiblesUpdated;
        
        /// <summary>
        /// M�thode pour d�clencher manuellement la notification de changement de structure
        /// � appeler depuis StructureEditorViewModel quand une couche est modifi�e
        /// </summary>
        public static void NotifyStructureChanged()
        {
            StructureChanged?.Invoke();
        }
        /// <summary>
        /// M�thode pour d�clencher manuellement la notification de changement de structure
        /// </summary>
        public static void OnStructureChanged()
        {
            StructureChanged?.Invoke();
        }

        /// <summary>
        /// D�clenche l'�v�nement ValeursAdmissiblesUpdated (rafra�chissement cibl� des colonnes Val. Adm.)
        /// </summary>
        public static void RaiseValeursAdmissiblesUpdated()
        {
            ValeursAdmissiblesUpdated?.Invoke();
        }
    } 
}
