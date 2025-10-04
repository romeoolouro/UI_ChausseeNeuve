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
        
        // Contexte trafic global (utiliser des champs pour éviter restrictions ENC)
        public static double? TraficCumuleGlobal; 
        public static string? TypeAccroissementGlobal; 
        
        // Events
        public static event Action? StructureChanged;
        public static event Action? ValeursAdmissiblesUpdated;
        public static event Action? SolicitationsUpdated; 

        public static void NotifyStructureChanged() => StructureChanged?.Invoke();
        public static void OnStructureChanged() => StructureChanged?.Invoke();
        public static void RaiseValeursAdmissiblesUpdated() => ValeursAdmissiblesUpdated?.Invoke();
        public static void RaiseSolicitationsUpdated() => SolicitationsUpdated?.Invoke();
    } 
}
