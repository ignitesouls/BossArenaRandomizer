using System;

namespace BossArenaRandomizer.Services
{
    public sealed class SettingsService
    {
        public string GetOutputPath()
        {
            return Properties.Settings.Default.OutputFilePath;
        }

        public void SaveOutputPath(string path)
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

        public bool GetUseClearArenas()
        {
            return Properties.Settings.Default.UseClearArenas;
        }

        public bool GetUseArenaSizeRestriction()
        {
            return Properties.Settings.Default.UseArenaSizeRestriction;
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
            bool useArenaSizeRestriction,
            bool useBossRushDifficultyCurve,
            bool useLooseDifficulty)
        {
            Properties.Settings.Default.UseClearArenas = useClearArenas;
            Properties.Settings.Default.UseArenaSizeRestriction = useArenaSizeRestriction;
            Properties.Settings.Default.UseBossRushDifficultyCurve = useBossRushDifficultyCurve;
            Properties.Settings.Default.UseLooseDifficulty = useLooseDifficulty;
            Properties.Settings.Default.Save();
        }
    }
}