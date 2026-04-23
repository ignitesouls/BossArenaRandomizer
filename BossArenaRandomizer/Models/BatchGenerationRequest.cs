using System;
using System.Collections.Generic;

namespace BossArenaRandomizer
{
    public sealed class BatchGenerationRequest
    {
        public string OptionsPresetName { get; set; } = string.Empty;

        public string OptionsPresetPath { get; set; } = string.Empty;

        public string OutputFolder { get; set; } = string.Empty;

        public int SeedCount { get; set; } = 1;

        /// <summary>
        /// Naming pattern for generated files.
        /// Supported tokens:
        /// {index}      -> 1-based seed number in the batch
        /// {seed}       -> RNG seed value
        /// {timestamp}  -> yyyyMMdd_HHmmss
        /// {preset}     -> sanitized preset name
        /// </summary>
        public string FileNamePattern { get; set; } = "BAR_{preset}_{index}_{seed}.randomizeopt";

        public bool ClearArenas { get; set; }

        public bool UseArenaSizeRestriction { get; set; } = true;

        public bool UseArenaDifficultyRestriction { get; set; } = true;

        public int MaxAttemptsPerSeed { get; set; } = 1500;

        public List<string> SelectedArenaIds { get; set; } = new();

        public List<string> SelectedBossIds { get; set; } = new();

        public int? StartingSeed { get; set; }

        public bool OverwriteExistingFiles { get; set; } = false;

        public void Validate()
        {
            if (SeedCount <= 0)
                throw new InvalidOperationException("SeedCount must be greater than 0.");

            if (string.IsNullOrWhiteSpace(OutputFolder))
                throw new InvalidOperationException("OutputFolder is required.");

            if (string.IsNullOrWhiteSpace(OptionsPresetPath))
                throw new InvalidOperationException("OptionsPresetPath is required.");

            if (SelectedArenaIds.Count == 0)
                throw new InvalidOperationException("At least one arena must be selected.");

            if (SelectedBossIds.Count == 0)
                throw new InvalidOperationException("At least one boss must be selected.");

            if (MaxAttemptsPerSeed <= 0)
                throw new InvalidOperationException("MaxAttemptsPerSeed must be greater than 0.");

            if (string.IsNullOrWhiteSpace(FileNamePattern))
                throw new InvalidOperationException("FileNamePattern is required.");
        }
    }
}