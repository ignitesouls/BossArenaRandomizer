using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BossArenaRandomizer.Services
{
    public interface IProjectPaths
    {
        string OptionsPresetPath(string presetName);
        string PairingPresetPath(string presetFileName);
        string BuildBatchOutputPath(string baseOutputPath, string fileNamePattern, string selectedOptionsPreset, int index, int seed);
    }

    public sealed class ProjectPaths : IProjectPaths
    {
        private readonly string _basePath;

        public ProjectPaths(string basePath)
        {
            _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
        }

        public string OptionsPresetPath(string presetName)
        {
            return PresetService.ResolveContentPath(_basePath, "Options", presetName + ".randomizeopt");
        }

        public string PairingPresetPath(string presetFileName)
        {
            return PresetService.ResolveContentPath(_basePath, "Data", "Pairings", presetFileName);
        }

        public string BuildBatchOutputPath(
            string baseOutputPath,
            string fileNamePattern,
            string selectedOptionsPreset,
            int index,
            int seed)
        {
            string directory = Path.GetDirectoryName(baseOutputPath) ?? "";
            string originalName = Path.GetFileNameWithoutExtension(baseOutputPath);
            string extension = Path.GetExtension(baseOutputPath);

            if (string.IsNullOrWhiteSpace(extension))
                extension = ".randomizeopt";

            string safePreset = string.Concat(
                selectedOptionsPreset.SelectFileNameSafeCharacters());

            string safePattern = string.IsNullOrWhiteSpace(fileNamePattern)
                ? $"{originalName}_{{index}}_{{seed}}{extension}"
                : fileNamePattern;

            string fileName = safePattern
                .Replace("{index}", index.ToString())
                .Replace("{seed}", seed.ToString())
                .Replace("{preset}", safePreset);

            if (!fileName.EndsWith(".randomizeopt", StringComparison.OrdinalIgnoreCase))
                fileName += extension;

            return Path.Combine(directory, fileName);
        }
    }

    internal static class ProjectPathExtensions
    {
        public static IEnumerable<char> SelectFileNameSafeCharacters(this string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            foreach (char ch in value)
                yield return invalid.Contains(ch) ? '_' : ch;
        }
    }
}
