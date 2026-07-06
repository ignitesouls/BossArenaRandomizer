using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BossArenaRandomizer.Core;

namespace BossArenaRandomizer.Services
{
    public sealed class DataRepository
    {
        private readonly string _basePath;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true
        };

        public DataRepository(string basePath)
        {
            _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
        }

        public string AllArenaBossesJsonPath => Path.Combine(_basePath, "Data", "AllArenaBossesDatabase.json");
        public string DataDirectory => Path.Combine(_basePath, "Data");
        public string PairingPresetDirectory => Path.Combine(DataDirectory, "Pairings");

        public Dictionary<string, List<string>> LoadPairingPreset(string presetFileName)
        {
            var path = PresetService.ResolveContentPath(_basePath, "Data", "Pairings", presetFileName);
            if (!File.Exists(path))
                throw new FileNotFoundException("Pairing preset not found.", path);

            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json)
                ?? new Dictionary<string, List<string>>();
        }

        public void SavePairingPreset(string presetFileName, Dictionary<string, List<string>> preset)
        {
            Directory.CreateDirectory(PairingPresetDirectory);
            var path = Path.Combine(PairingPresetDirectory, presetFileName);
            BackupSingleFile(path);
            string json = JsonSerializer.Serialize(preset, _jsonOptions);
            File.WriteAllText(path, json);
        }

        public bool PairingPresetBackupExists(string presetFileName)
        {
            var path = Path.Combine(PairingPresetDirectory, presetFileName);
            return File.Exists(GetBackupPath(path));
        }

        public void RestorePairingPresetBackup(string presetFileName)
        {
            var path = Path.Combine(PairingPresetDirectory, presetFileName);
            var backupPath = GetBackupPath(path);

            if (!File.Exists(backupPath))
                throw new FileNotFoundException("Pairing preset backup not found.", backupPath);

            File.Copy(backupPath, path, overwrite: true);
        }

        public (Dictionary<string, ArenaInfo> Arenas, Dictionary<string, BossInfo> Bosses) LoadAllArenaBossDatabase()
        {
            if (!File.Exists(AllArenaBossesJsonPath))
                return (new Dictionary<string, ArenaInfo>(), new Dictionary<string, BossInfo>());

            return InitialDataRead.LoadAllArenaBosses(AllArenaBossesJsonPath);
        }

        public void SaveAllArenaBossDatabase(Dictionary<string, ArenaInfo> entries)
        {
            Directory.CreateDirectory(DataDirectory);

            var orderedEntries = entries
                .OrderBy(x => x.Key)
                .ToDictionary(
                    x => x.Key,
                    x => x.Value,
                    StringComparer.OrdinalIgnoreCase);

            string json = JsonSerializer.Serialize(orderedEntries, _jsonOptions);
            BackupSingleFile(AllArenaBossesJsonPath);
            File.WriteAllText(AllArenaBossesJsonPath, json);
        }

        public void ReplaceAllArenaBossDatabase(string sourcePath)
        {
            if (!File.Exists(sourcePath))
                throw new FileNotFoundException("Database file not found.", sourcePath);

            ValidateAllArenaBossDatabase(sourcePath);
            Directory.CreateDirectory(DataDirectory);
            BackupSingleFile(AllArenaBossesJsonPath);
            File.Copy(sourcePath, AllArenaBossesJsonPath, overwrite: true);
        }

        public bool MainDatabaseBackupExists()
        {
            return File.Exists(GetBackupPath(AllArenaBossesJsonPath));
        }

        public void RestoreMainDatabaseBackup()
        {
            var backupPath = GetBackupPath(AllArenaBossesJsonPath);
            if (!File.Exists(backupPath))
                throw new FileNotFoundException("Main database backup not found.", backupPath);

            File.Copy(backupPath, AllArenaBossesJsonPath, overwrite: true);
        }

        private static void BackupSingleFile(string path)
        {
            if (!File.Exists(path))
                return;

            File.Copy(path, GetBackupPath(path), overwrite: true);
        }

        private static string GetBackupPath(string path)
        {
            return path + ".backup";
        }

        public void ValidateAllArenaBossDatabase(string sourcePath)
        {
            if (!File.Exists(sourcePath))
                throw new FileNotFoundException("Database file not found.", sourcePath);

            string json = File.ReadAllText(sourcePath);
            using var document = JsonDocument.Parse(json);

            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("Database JSON must be an object keyed by boss or arena name.");

            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var errors = new List<string>();

            foreach (var entry in document.RootElement.EnumerateObject())
            {
                string name = entry.Name.Trim();
                if (string.IsNullOrWhiteSpace(name))
                    errors.Add("An entry has an empty name.");
                else if (!names.Add(name))
                    errors.Add($"Duplicate name found: {name}.");

                if (entry.Value.ValueKind != JsonValueKind.Object)
                {
                    errors.Add($"{name} must be a JSON object.");
                    continue;
                }

                string id = ReadStringLike(entry.Value, "id");
                if (string.IsNullOrWhiteSpace(id))
                    errors.Add($"{name} is missing a valid id.");
                else if (!ids.Add(id))
                    errors.Add($"Duplicate id found: {id}.");

                ValidateInteger(entry.Value, "type", name, errors);
                ValidateInteger(entry.Value, "nightBoss", name, errors);
                ValidateInteger(entry.Value, "region", name, errors);
                ValidateInteger(entry.Value, "scaling", name, errors);
                ValidateBoolean(entry.Value, "dlc", name, errors);
            }

            if (names.Count == 0)
                errors.Add("Database must contain at least one entry.");

            if (errors.Count > 0)
                throw new InvalidDataException("Database validation failed:\n" + string.Join("\n", errors.Take(20)));

            InitialDataRead.LoadAllArenaBosses(sourcePath);
        }

        private static string ReadStringLike(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var property))
                return string.Empty;

            return property.ValueKind switch
            {
                JsonValueKind.String => property.GetString() ?? string.Empty,
                JsonValueKind.Number => property.GetRawText(),
                _ => string.Empty
            };
        }

        private static void ValidateInteger(JsonElement element, string propertyName, string entryName, List<string> errors)
        {
            if (!element.TryGetProperty(propertyName, out var property))
            {
                errors.Add($"{entryName} is missing {propertyName}.");
                return;
            }

            if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out _))
                return;

            if (property.ValueKind == JsonValueKind.String && int.TryParse(property.GetString(), out _))
                return;

            errors.Add($"{entryName} has an invalid {propertyName}; it must be a whole number.");
        }

        private static void ValidateBoolean(JsonElement element, string propertyName, string entryName, List<string> errors)
        {
            if (!element.TryGetProperty(propertyName, out var property))
            {
                errors.Add($"{entryName} is missing {propertyName}.");
                return;
            }

            if (property.ValueKind is JsonValueKind.True or JsonValueKind.False)
                return;

            if (property.ValueKind == JsonValueKind.String && bool.TryParse(property.GetString(), out _))
                return;

            errors.Add($"{entryName} has an invalid {propertyName}; it must be true or false.");
        }
    }
}
