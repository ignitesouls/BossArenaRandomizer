using System;

namespace BossArenaRandomizer.Services
{
    public sealed class SettingsService
    {
        public string GetOutputFolderPath()
        {
            string savedPath = Properties.Settings.Default.OutputFilePath;
            if (savedPath.EndsWith(".randomizeopt", StringComparison.OrdinalIgnoreCase))
                return System.IO.Path.GetDirectoryName(savedPath) ?? string.Empty;

            return savedPath;
        }

        public void SaveOutputFolderPath(string path)
        {
            Properties.Settings.Default.OutputFilePath = path ?? string.Empty;
            Properties.Settings.Default.Save();
        }

        public string GetLastUsedArenaPreset()
        {
            return Properties.Settings.Default.LastUsedArenaPreset;
        }

        public void SaveLastUsedArenaPreset(string presetFileName)
        {
            Properties.Settings.Default.LastUsedArenaPreset = presetFileName ?? string.Empty;
            Properties.Settings.Default.Save();
        }

        public string GetLastUsedBossPreset()
        {
            return Properties.Settings.Default.LastUsedBossPreset;
        }

        public void SaveLastUsedBossPreset(string presetFileName)
        {
            Properties.Settings.Default.LastUsedBossPreset = presetFileName ?? string.Empty;
            Properties.Settings.Default.Save();
        }

        public string GetSelectedOptionsPreset()
        {
            return Properties.Settings.Default.SelectedOptionsPreset;
        }

        public void SaveSelectedOptionsPreset(string presetName)
        {
            Properties.Settings.Default.SelectedOptionsPreset = presetName ?? string.Empty;
            Properties.Settings.Default.Save();
        }

        public string GetSelectedPairingPreset()
        {
            return Properties.Settings.Default.SelectedPairingPreset;
        }

        public void SaveSelectedPairingPreset(string presetFileName)
        {
            Properties.Settings.Default.SelectedPairingPreset = presetFileName ?? string.Empty;
            Properties.Settings.Default.Save();
        }

        public string GetSelectedConfiguration()
        {
            return Properties.Settings.Default.SelectedConfiguration;
        }

        public void SaveSelectedConfiguration(string configurationFileName)
        {
            Properties.Settings.Default.SelectedConfiguration = configurationFileName ?? string.Empty;
            Properties.Settings.Default.Save();
        }

        public int GetSeedCount()
        {
            return Math.Max(1, Properties.Settings.Default.SeedCount);
        }

        public string GetFileNamePattern()
        {
            string pattern = Properties.Settings.Default.FileNamePattern;
            return string.IsNullOrWhiteSpace(pattern)
                ? "BAR_{index}_{seed}.randomizeopt"
                : pattern;
        }

        public void SaveGenerateSettings(int seedCount, string fileNamePattern)
        {
            Properties.Settings.Default.SeedCount = Math.Max(1, seedCount);
            Properties.Settings.Default.FileNamePattern = string.IsNullOrWhiteSpace(fileNamePattern)
                ? "BAR_{index}_{seed}.randomizeopt"
                : fileNamePattern;
            Properties.Settings.Default.Save();
        }
        public bool GetUseClearArenas()
        {
            return Properties.Settings.Default.UseClearArenas;
        }

        public bool GetUseBossRushDifficultyCurve()
        {
            return Properties.Settings.Default.UseBossRushDifficultyCurve;
        }

        public bool GetUseLooseDifficulty()
        {
            return Properties.Settings.Default.UseLooseDifficulty;
        }

        public void SaveGenerationFlags(
            bool useClearArenas,
            bool useBossRushDifficultyCurve,
            bool useLooseDifficulty)
        {
            Properties.Settings.Default.UseClearArenas = useClearArenas;
            Properties.Settings.Default.UseBossRushDifficultyCurve = useBossRushDifficultyCurve;
            Properties.Settings.Default.UseLooseDifficulty = useLooseDifficulty;
            Properties.Settings.Default.Save();
        }
    }
}
