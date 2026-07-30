using System.Collections.Generic;
using BossArenaRandomizer.Core;

namespace BossArenaRandomizer.Services
{
    public sealed class GenerationDisplayGroup
    {
        public string RegionName { get; set; } = string.Empty;
        public List<string> Lines { get; set; } = new();
    }

    public sealed class BatchSeedResult
    {
        public int Index { get; set; }
        public bool Success { get; set; }
        public int Seed { get; set; }
        public string OutputPath { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public sealed class GenerationRequest
    {
        public required Dictionary<string, ArenaInfo> Arenas { get; init; }
        public required Dictionary<string, BossInfo> Bosses { get; init; }

        public required List<string> SelectedArenaIds { get; init; }
        public required List<string> SelectedBossIds { get; init; }

        public required string BasePath { get; init; }
        public required string OutputFolderPath { get; init; }
        public required string SelectedOptionsPreset { get; init; }
        public required string SelectedPairingPreset { get; init; }

        public bool ClearArenasEnabled { get; init; }
        public bool WriteOutputFiles { get; init; } = true;

        public int MaxAttempts { get; init; } = 1500;
        public int SeedCount { get; init; } = 1;
        public int? ReplaySeed { get; init; }

        public string FileNamePattern { get; init; } = "BAR_{index}_{seed}.randomizeopt";
    }

    public sealed class GenerationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;

        public int LastSeed { get; set; }
        public Dictionary<string, string> FinalAssignments { get; set; } = new();
        public List<AssignmentPair> FinalAssignmentPairs { get; set; } = new();
        public List<GenerationDisplayGroup> DisplayGroups { get; set; } = new();

        public List<BatchSeedResult> BatchResults { get; set; } = new();
        public List<string> ValidationLines { get; set; } = new();
        public List<string> UniformityLines { get; set; } = new();
        public List<string> PairingFrequencyLines { get; set; } = new();
        public string DebugLog { get; set; } = string.Empty;
    }
}
