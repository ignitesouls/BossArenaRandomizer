using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BossArenaRandomizer.Core;

namespace BossArenaRandomizer.Services;

public sealed class PresetService
{
    public List<string> GetArenaPresetFiles()
    {
        EnsureDirectory(Constants.ArenaPresetDirectory);
        return Directory.GetFiles(Constants.ArenaPresetDirectory, "*.json")
            .Select(Path.GetFileName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList()!;
    }

    public List<string> GetBossPresetFiles()
    {
        EnsureDirectory(Constants.BossPresetDirectory);
        return Directory.GetFiles(Constants.BossPresetDirectory, "*.json")
            .Select(Path.GetFileName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList()!;
    }

    public List<string> GetOptionsPresetNames()
    {
        EnsureDirectory(Constants.OptionsPresetDirectory);
        return Directory.GetFiles(Constants.OptionsPresetDirectory, "*.randomizeopt")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList()!;
    }

    public string GetArenaPresetPath(string presetFileName)
    {
        return Path.Combine(Constants.ArenaPresetDirectory, presetFileName);
    }

    public string GetBossPresetPath(string presetFileName)
    {
        return Path.Combine(Constants.BossPresetDirectory, presetFileName);
    }

    public string GetOptionsPresetPath(string presetName)
    {
        return Path.Combine(Constants.OptionsPresetDirectory, presetName + ".randomizeopt");
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
}