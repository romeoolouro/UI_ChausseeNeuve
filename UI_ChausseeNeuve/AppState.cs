using ChausseeNeuve.Domain.Models;
namespace UI_ChausseeNeuve { public static class AppState { public static Project CurrentProject { get; set; } = new Project();
        public static string? CurrentFilePath { get; set; } = null;
    } }
