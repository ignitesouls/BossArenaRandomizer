using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace BossArenaRandomizer.Core
{
    public static class ClearArenaIds
    {
        private const string FileName = "ClearArenaIds.json";
        private static readonly Lazy<IReadOnlyCollection<string>> CachedIds = new(LoadCore);

        public static IReadOnlyCollection<string> Load()
        {
            return CachedIds.Value;
        }

        private static IReadOnlyCollection<string> LoadCore()
        {
            string path = ResolveDataPath();
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<string>>(json)
                ?? throw new InvalidDataException($"{FileName} must contain a JSON array of IDs.");
        }

        private static string ResolveDataPath()
        {
            foreach (var root in GetDataRoots())
            {
                string candidate = Path.Combine(root, "Data", FileName);
                if (File.Exists(candidate))
                    return candidate;
            }

            throw new FileNotFoundException($"Clear arena ID file not found. Expected Data\\{FileName}.");
        }

        private static IEnumerable<string> GetDataRoots()
        {
            yield return AppContext.BaseDirectory;
            yield return Directory.GetCurrentDirectory();

            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null)
            {
                if (File.Exists(Path.Combine(current.FullName, "BossArenaRandomizer.csproj")))
                {
                    yield return current.FullName;
                    yield break;
                }

                current = current.Parent;
            }
        }
    }
}