using System;
using System.Collections.Generic;
using System.IO;
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

        public string ArenasJsonPath => Path.Combine(_basePath, "Data", "arenas.json");
        public string BossesJsonPath => Path.Combine(_basePath, "Data", "bosses.json");

        public Dictionary<string, ArenaInfo> LoadArenaDictionary()
        {
            if (!File.Exists(ArenasJsonPath))
                throw new FileNotFoundException("arenas.json not found.", ArenasJsonPath);

            string json = File.ReadAllText(ArenasJsonPath);
            var result = JsonSerializer.Deserialize<Dictionary<string, ArenaInfo>>(json);

            return result ?? new Dictionary<string, ArenaInfo>();
        }

        public void SaveArenaDictionary(Dictionary<string, ArenaInfo> arenas)
        {
            if (arenas == null)
                throw new ArgumentNullException(nameof(arenas));

            Directory.CreateDirectory(Path.GetDirectoryName(ArenasJsonPath)!);

            string json = JsonSerializer.Serialize(arenas, _jsonOptions);
            File.WriteAllText(ArenasJsonPath, json);
        }

        public Dictionary<string, BossInfo> LoadBossDictionary()
        {
            if (!File.Exists(BossesJsonPath))
                throw new FileNotFoundException("bosses.json not found.", BossesJsonPath);

            string json = File.ReadAllText(BossesJsonPath);
            var result = JsonSerializer.Deserialize<Dictionary<string, BossInfo>>(json);

            return result ?? new Dictionary<string, BossInfo>();
        }

        public void SaveBossDictionary(Dictionary<string, BossInfo> bosses)
        {
            if (bosses == null)
                throw new ArgumentNullException(nameof(bosses));

            Directory.CreateDirectory(Path.GetDirectoryName(BossesJsonPath)!);

            string json = JsonSerializer.Serialize(bosses, _jsonOptions);
            File.WriteAllText(BossesJsonPath, json);
        }

        public void BackupArenaJson()
        {
            if (!File.Exists(ArenasJsonPath))
                return;

            string backupPath = Path.Combine(
                Path.GetDirectoryName(ArenasJsonPath)!,
                $"arenas.backup.{DateTime.Now:yyyyMMdd_HHmmss}.json");

            File.Copy(ArenasJsonPath, backupPath, overwrite: false);
        }

        public void BackupBossJson()
        {
            if (!File.Exists(BossesJsonPath))
                return;

            string backupPath = Path.Combine(
                Path.GetDirectoryName(BossesJsonPath)!,
                $"bosses.backup.{DateTime.Now:yyyyMMdd_HHmmss}.json");

            File.Copy(BossesJsonPath, backupPath, overwrite: false);
        }
    }
}