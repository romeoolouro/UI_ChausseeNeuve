using System;
using System.IO;
using System.Text.Json;
using ChausseeNeuve.Domain.Models;

namespace UI_ChausseeNeuve.Services
{
    public static class ProjectStorage
    {
        private static readonly JsonSerializerOptions _opts = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            // Support for inheritance and complex types
            IncludeFields = false
        };

        public static void Save(Project project, string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir!);

            // Ensure PavementStructure is initialized
            if (project.PavementStructure == null)
            {
                project.PavementStructure = new PavementStructure();
            }

            File.WriteAllText(path, JsonSerializer.Serialize(project, _opts));
            AppState.CurrentFilePath = path;
            RecentFiles.Add(path);
        }

        public static Project Load(string path)
        {
            var json = File.ReadAllText(path);
            var proj = JsonSerializer.Deserialize<Project>(json, _opts) ?? new Project();

            // Ensure backward compatibility for projects without PavementStructure
            if (proj.PavementStructure == null)
            {
                proj.PavementStructure = new PavementStructure();
            }

            AppState.CurrentFilePath = path;
            AppState.CurrentProject = proj;
            RecentFiles.Add(path);
            return proj;
        }
    }
}
