using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        public required string OutputPath { get; init; }
        public required string SelectedOptionsPreset { get; init; }

        public bool ClearArenasEnabled { get; init; }
        public bool ArenaSizeRestrictionEnabled { get; init; }
        public bool BossRushDifficultyCurveEnabled { get; init; }
        public bool LooseDifficultyEnabled { get; init; }

        public int MaxAttempts { get; init; } = 1500;

        public int SeedCount { get; init; } = 1;

        public string FileNamePattern { get; init; } = "BAR_{index}_{seed}.randomizeopt";
    }

    public sealed class GenerationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;

        public int LastSeed { get; set; }
        public Dictionary<string, string> FinalAssignments { get; set; } = new();
        public List<GenerationDisplayGroup> DisplayGroups { get; set; } = new();

        public List<BatchSeedResult> BatchResults { get; set; } = new();
    }

    public sealed class SeedGenerationService
    {
        public GenerationResult Generate(GenerationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.SelectedOptionsPreset))
            {
                return new GenerationResult
                {
                    Success = false,
                    ErrorMessage = "Please load an options preset."
                };
            }

            if (string.IsNullOrWhiteSpace(request.OutputPath))
            {
                return new GenerationResult
                {
                    Success = false,
                    ErrorMessage = "Please select an output path first."
                };
            }

            if (request.SeedCount <= 0)
            {
                return new GenerationResult
                {
                    Success = false,
                    ErrorMessage = "Seed count must be at least 1."
                };
            }

            var optionsFilePath = Path.Combine(
                request.BasePath,
                "Options",
                request.SelectedOptionsPreset + ".randomizeopt");

            if (!File.Exists(optionsFilePath))
            {
                return new GenerationResult
                {
                    Success = false,
                    ErrorMessage = "Options preset file not found."
                };
            }

            var validator = Randomization.LoadBitmapsFromCsv(
                Path.Combine(request.BasePath, "ArenaBossData.csv"),
                request.ArenaSizeRestrictionEnabled,
                request.BossRushDifficultyCurveEnabled,
                request.LooseDifficultyEnabled
            );

            var result = new GenerationResult
            {
                Success = true
            };

            for (int i = 1; i <= request.SeedCount; i++)
            {
                var rng = new Random();

                bool ok = ArenaBossAssigner.TryAssign(
                    arenas: request.Arenas,
                    bosses: request.Bosses,
                    selectedArenaIds: request.SelectedArenaIds,
                    selectedBossIds: request.SelectedBossIds,
                    validator: validator,
                    maxAttempts: request.MaxAttempts,
                    rng: rng,
                    result: out var assignResult,
                    warnDupeMode: msg => Debug.WriteLine(msg),
                    debugLog: msg => Debug.WriteLine(msg)
                );

                if (!ok || assignResult == null)
                {
                    result.BatchResults.Add(new BatchSeedResult
                    {
                        Index = i,
                        Success = false,
                        Message = "Failed due to constraints."
                    });
                    continue;
                }

                Dictionary<string, string> finalAssignments = assignResult.Assignments;

                var randomizer = new UniversalReplacementRandomizer.SeedManager();
                int seed = randomizer.GetBaseSeed();

                string outputPath = BuildBatchOutputPath(
                    request.OutputPath,
                    request.FileNamePattern,
                    request.SelectedOptionsPreset,
                    i,
                    seed);

                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

                FinalizeTextFile.WriteFinalAssignments(
                    finalAssignments,
                    request.Arenas,
                    request.Bosses,
                    outputPath,
                    optionsFilePath,
                    seed,
                    request.ClearArenasEnabled
                );

                result.LastSeed = seed;
                result.FinalAssignments = finalAssignments;

                result.BatchResults.Add(new BatchSeedResult
                {
                    Index = i,
                    Success = true,
                    Seed = seed,
                    OutputPath = outputPath,
                    Message = "Generated"
                });

                if (i == request.SeedCount)
                {
                    result.DisplayGroups = finalAssignments
                        .GroupBy(kvp => request.Arenas[kvp.Key].region)
                        .OrderBy(g => g.Key)
                        .Select(regionGroup =>
                        {
                            string regionName = HCData.RegionNames.ContainsKey(regionGroup.Key)
                                ? HCData.RegionNames[regionGroup.Key]
                                : $"Region {regionGroup.Key}";

                            var lines = regionGroup
                                .Select(kvp =>
                                {
                                    string arenaName = kvp.Key;
                                    string bossName = kvp.Value;

                                    string arenaId = request.Arenas[arenaName].id;
                                    string bossId = request.Bosses[bossName].id;

                                    return $"{arenaName} (ID: {arenaId}) -> {bossName} (ID: {bossId})";
                                })
                                .ToList();

                            return new GenerationDisplayGroup
                            {
                                RegionName = regionName,
                                Lines = lines
                            };
                        })
                        .ToList();
                }
            }

            if (result.BatchResults.All(x => !x.Success))
            {
                result.Success = false;
                result.ErrorMessage = "All batch generations failed due to constraints.";
            }

            return result;
        }

        private static string BuildBatchOutputPath(
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
                selectedOptionsPreset.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch));

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
}