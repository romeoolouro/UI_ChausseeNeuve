using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace UI_ChausseeNeuve.Services
{
    public static class RecentFiles
    {
        private static string AppFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BENIROUTE");
        private static string FilePath => Path.Combine(AppFolder, "recent.json");
        private const int MaxCount = 10;

        public static void Add(string path)
        {
            try
            {
                var list = Get().ToList();
                list.RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
                list.Insert(0, path);
                while (list.Count > MaxCount) list.RemoveAt(list.Count - 1);
                if (!Directory.Exists(AppFolder)) Directory.CreateDirectory(AppFolder);
                File.WriteAllText(FilePath, JsonSerializer.Serialize(list));
            }
            catch { /* swallow IO exceptions */ }
        }

        public static IEnumerable<string> Get()
        {
            try
            {
                if (!File.Exists(FilePath)) return Enumerable.Empty<string>();
                var json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<List<string>>(json) ?? Enumerable.Empty<string>();
            }
            catch { return Enumerable.Empty<string>(); }
        }
    }
}
