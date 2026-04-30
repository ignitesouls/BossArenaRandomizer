using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using BossArenaRandomizer.Core;

namespace BossArenaRandomizer.Services;

public sealed class DataRepository
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public Dictionary<string, ArenaInfo> LoadArenaDictionary()
    {
        if (!File.Exists(Constants.ArenasJsonPath))
            throw new FileNotFoundException("arenas.json not found.", Constants.ArenasJsonPath);

        string json = File.ReadAllText(Constants.ArenasJsonPath);
        var result = JsonSerializer.Deserialize<Dictionary<string, ArenaInfo>>(json);

        return result ?? new Dictionary<string, ArenaInfo>();
    }

    public void SaveArenaDictionary(Dictionary<string, ArenaInfo> arenas)
    {
        if (arenas == null)
            throw new ArgumentNullException(nameof(arenas));

        Directory.CreateDirectory(Path.GetDirectoryName(Constants.ArenasJsonPath)!);

        string json = JsonSerializer.Serialize(arenas, _jsonOptions);
        File.WriteAllText(Constants.ArenasJsonPath, json);
    }

    public Dictionary<string, BossInfo> LoadBossDictionary()
    {
        if (!File.Exists(Constants.BossesJsonPath))
            throw new FileNotFoundException("bosses.json not found.", Constants.BossesJsonPath);

        string json = File.ReadAllText(Constants.BossesJsonPath);
        var result = JsonSerializer.Deserialize<Dictionary<string, BossInfo>>(json);

        return result ?? new Dictionary<string, BossInfo>();
    }

    public void SaveBossDictionary(Dictionary<string, BossInfo> bosses)
    {
        if (bosses == null)
            throw new ArgumentNullException(nameof(bosses));

        Directory.CreateDirectory(Path.GetDirectoryName(Constants.BossesJsonPath)!);

        string json = JsonSerializer.Serialize(bosses, _jsonOptions);
        File.WriteAllText(Constants.BossesJsonPath, json);
    }

    public void BackupArenaJson()
    {
        if (!File.Exists(Constants.ArenasJsonPath))
            return;

        string backupPath = Path.Combine(
            Path.GetDirectoryName(Constants.ArenasJsonPath)!,
            $"arenas.backup.{DateTime.Now:yyyyMMdd_HHmmss}.json");

        File.Copy(Constants.ArenasJsonPath, backupPath, overwrite: false);
    }

    public void BackupBossJson()
    {
        if (!File.Exists(Constants.BossesJsonPath))
            return;

        string backupPath = Path.Combine(
            Path.GetDirectoryName(Constants.BossesJsonPath)!,
            $"bosses.backup.{DateTime.Now:yyyyMMdd_HHmmss}.json");

        File.Copy(Constants.BossesJsonPath, backupPath, overwrite: false);
    }
}
