using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace BossArenaRandomizer.Services
{
    public sealed class PresetService
    {
        private readonly string _basePath;

        public PresetService(string basePath)
        {
            _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
        }

        public string ArenaPresetDirectory => Path.Combine(_basePath, "Presets", "Arenas");
        public string BossPresetDirectory => Path.Combine(_basePath, "Presets", "Bosses");
        public string OptionsPresetDirectory => Path.Combine(_basePath, "Options");
        public string DataDirectory => Path.Combine(_basePath, "Data");
        public string PairingPresetDirectory => Path.Combine(DataDirectory, "Pairings");

        public static string ResolveContentPath(string basePath, params string[] relativeParts)
        {
            string relativePath = Path.Combine(relativeParts);
            foreach (var root in GetContentRoots(basePath))
            {
                string candidate = Path.Combine(root, relativePath);
                if (File.Exists(candidate) || Directory.Exists(candidate))
                    return candidate;
            }

            return Path.Combine(basePath, relativePath);
        }

        public List<string> GetArenaPresetFiles()
        {
            EnsureDirectory(ArenaPresetDirectory);
            return GetFilesFromContentDirectories(Path.Combine("Presets", "Arenas"), "*.json")
                .Select(Path.GetFileName)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList()!;
        }

        public List<string> GetBossPresetFiles()
        {
            EnsureDirectory(BossPresetDirectory);
            return GetFilesFromContentDirectories(Path.Combine("Presets", "Bosses"), "*.json")
                .Select(Path.GetFileName)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList()!;
        }

        public List<string> GetOptionsPresetNames()
        {
            EnsureDirectory(OptionsPresetDirectory);
            return GetFilesFromContentDirectories("Options", "*.randomizeopt")
                .Select(Path.GetFileNameWithoutExtension)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList()!;
        }

        public List<string> GetPairingPresetFiles()
        {
            EnsureDirectory(PairingPresetDirectory);
            return GetFilesFromContentDirectories(Path.Combine("Data", "Pairings"), "*.json")
                .Select(Path.GetFileName)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList()!;
        }

        public string GetArenaPresetPath(string presetFileName)
        {
            return ResolveContentPath(_basePath, "Presets", "Arenas", presetFileName);
        }

        public string GetBossPresetPath(string presetFileName)
        {
            return ResolveContentPath(_basePath, "Presets", "Bosses", presetFileName);
        }

        public string GetOptionsPresetPath(string presetName)
        {
            return ResolveContentPath(_basePath, "Options", presetName + ".randomizeopt");
        }

        public string GetPairingPresetPath(string presetFileName)
        {
            return ResolveContentPath(_basePath, "Data", "Pairings", presetFileName);
        }

        public List<string> LoadArenaPresetIds(string presetFileName)
        {
            var path = GetArenaPresetPath(presetFileName);
            return LoadIdList(path);
        }

        public List<string> LoadBossPresetIds(string presetFileName)
        {
            var path = GetBossPresetPath(presetFileName);
            return LoadIdList(path);
        }

        public void SaveArenaPreset(string filePath, IEnumerable<string> selectedArenaIds)
        {
            SaveIdList(filePath, selectedArenaIds);
        }

        public void SaveBossPreset(string filePath, IEnumerable<string> selectedBossIds)
        {
            SaveIdList(filePath, selectedBossIds);
        }

        public bool ArenaPresetExists(string presetFileName)
        {
            return File.Exists(GetArenaPresetPath(presetFileName));
        }

        public bool BossPresetExists(string presetFileName)
        {
            return File.Exists(GetBossPresetPath(presetFileName));
        }

        public bool OptionsPresetExists(string presetName)
        {
            return File.Exists(GetOptionsPresetPath(presetName));
        }

        private static List<string> LoadIdList(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Preset file not found.", path);

            var json = File.ReadAllText(path);
            var ids = JsonSerializer.Deserialize<List<string>>(json);

            return ids ?? new List<string>();
        }

        private static void SaveIdList(string filePath, IEnumerable<string> ids)
        {
            var safeIds = ids?.Distinct().ToList() ?? new List<string>();
            var json = JsonSerializer.Serialize(safeIds, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(filePath, json);
        }

        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        private IEnumerable<string> GetFilesFromContentDirectories(string relativeDirectory, string searchPattern)
        {
            foreach (var root in GetContentRoots(_basePath))
            {
                string directory = Path.Combine(root, relativeDirectory);
                if (!Directory.Exists(directory))
                    continue;

                foreach (var file in Directory.GetFiles(directory, searchPattern))
                    yield return file;
            }
        }

        private static IEnumerable<string> GetContentRoots(string basePath)
        {
            yield return basePath;

            var current = new DirectoryInfo(basePath);
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
