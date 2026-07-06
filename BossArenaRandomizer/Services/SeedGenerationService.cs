using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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

    public sealed class SeedGenerationService
    {
        private readonly GenerationService _generationService;

        public SeedGenerationService()
            : this(new GenerationService())
        {
        }

        public SeedGenerationService(GenerationService generationService)
        {
            _generationService = generationService ?? throw new ArgumentNullException(nameof(generationService));
        }

        public GenerationResult Generate(GenerationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (_generationService != null)
                return _generationService.Generate(request);

            if (string.IsNullOrWhiteSpace(request.SelectedOptionsPreset))
                return Fail("Please load an options preset.");

            if (request.WriteOutputFiles && string.IsNullOrWhiteSpace(request.OutputPath))
                return Fail("Please select an output path first.");

            if (string.IsNullOrWhiteSpace(request.SelectedPairingPreset))
                return Fail("Please select a boss/arena pairing preset.");

            if (request.SeedCount <= 0)
                return Fail("Seed count must be at least 1.");

            var optionsFilePath = PresetService.ResolveContentPath(
                request.BasePath,
                "Options",
                request.SelectedOptionsPreset + ".randomizeopt");

            if (!File.Exists(optionsFilePath))
                return Fail("Options preset file not found.");

            var pairingPresetPath = PresetService.ResolveContentPath(
                request.BasePath,
                "Data",
                "Pairings",
                request.SelectedPairingPreset);

            PairingPresetValidator validator;

            try
            {
                validator = PairingPresetValidator.Load(pairingPresetPath);
            }
            catch (Exception ex)
            {
                return Fail($"Pairing preset could not be loaded: {ex.Message}");
            }

            var pairingCache = IndexedPairingCache.Build(request.Arenas, request.Bosses, validator);
            var debugLog = BuildDebugHeader(request);
            var validation = validator.ValidatePreset(
                request.Arenas,
                request.Bosses,
                request.SelectedArenaIds,
                request.SelectedBossIds);

            var validationLines = validation.Issues
                .Select(x => $"[{x.Severity}] {x.Message}")
                .ToList();

            AppendSection(debugLog, "Pairing Validation");
            foreach (var line in validationLines)
                debugLog.AppendLine(line);
            debugLog.AppendLine();

            if (!validation.IsValid)
            {
                return new GenerationResult
                {
                    Success = false,
                    ErrorMessage = "Pairing preset validation failed. Export the debug log for details.",
                    ValidationLines = validationLines,
                    DebugLog = debugLog.ToString()
                };
            }

            var result = new GenerationResult
            {
                Success = true,
                ValidationLines = validationLines
            };
            var pairingFrequency = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
            int successfulAssignments = 0;

            for (int i = 1; i <= request.SeedCount; i++)
            {
                int seed = request.ReplaySeed.HasValue
                    ? request.ReplaySeed.Value + i - 1
                    : Random.Shared.Next(1, int.MaxValue);

                var rng = new Random(seed);
                AppendSection(debugLog, $"Seed {i} of {request.SeedCount}");
                debugLog.AppendLine($"Seed: {seed}");

                bool ok = ArenaBossAssigner.TryAssign(
                    cache: pairingCache,
                    selectedArenaIds: request.SelectedArenaIds,
                    selectedBossIds: request.SelectedBossIds,
                    maxAttempts: request.MaxAttempts,
                    rng: rng,
                    result: out var assignResult,
                    warnDupeMode: msg =>
                    {
                        Debug.WriteLine(msg);
                        debugLog.AppendLine($"Warning: {msg}");
                    },
                    debugLog: msg =>
                    {
                        Debug.WriteLine(msg);
                        debugLog.AppendLine(msg);
                    }
                );

                if (!ok || assignResult == null)
                {
                    debugLog.AppendLine("Status: Failed due to constraints.");
                    result.BatchResults.Add(new BatchSeedResult
                    {
                        Index = i,
                        Success = false,
                        Seed = seed,
                        Message = "Failed due to constraints."
                    });
                    continue;
                }

                Dictionary<string, string> finalAssignments = assignResult.Assignments;
                successfulAssignments++;
                AddPairingFrequency(pairingFrequency, finalAssignments);
                string outputPath = string.Empty;

                if (request.WriteOutputFiles)
                {
                    outputPath = BuildBatchOutputPath(
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
                }

                result.LastSeed = seed;
                result.FinalAssignments = finalAssignments;

                result.BatchResults.Add(new BatchSeedResult
                {
                    Index = i,
                    Success = true,
                    Seed = seed,
                    OutputPath = outputPath,
                    Message = request.WriteOutputFiles ? "Generated" : "Dry run passed"
                });

                debugLog.AppendLine("Status: Success");
                if (!string.IsNullOrWhiteSpace(outputPath))
                    debugLog.AppendLine($"Output: {outputPath}");

                debugLog.AppendLine();
                debugLog.AppendLine("Assignments");
                foreach (var assignment in finalAssignments.OrderBy(x => x.Key))
                    debugLog.AppendLine($"  {assignment.Key} -> {assignment.Value}");

                var uniformityLines = BuildUniformityReport(
                    finalAssignments,
                    request.Bosses,
                    request.SelectedBossIds);

                debugLog.AppendLine();
                debugLog.AppendLine("Uniformity");
                foreach (var line in uniformityLines)
                    debugLog.AppendLine($"  {line}");
                debugLog.AppendLine();

                if (i == request.SeedCount)
                {
                    result.UniformityLines = uniformityLines;
                    result.DisplayGroups = BuildDisplayGroups(finalAssignments, request);
                }
            }

            if (result.BatchResults.All(x => !x.Success))
            {
                result.Success = false;
                result.ErrorMessage = "All batch generations failed due to constraints.";
            }

            result.PairingFrequencyLines = BuildPairingFrequencyReport(
                pairingFrequency,
                successfulAssignments);

            if (result.PairingFrequencyLines.Count > 0)
            {
                AppendSection(debugLog, "Pairing Frequency");
                foreach (var line in result.PairingFrequencyLines)
                    debugLog.AppendLine(line);
                debugLog.AppendLine();
            }

            result.DebugLog = debugLog.ToString();

            return result;
        }

        private static GenerationResult Fail(string message)
        {
            return new GenerationResult
            {
                Success = false,
                ErrorMessage = message
            };
        }

        private static StringBuilder BuildDebugHeader(GenerationRequest request)
        {
            var debugLog = new StringBuilder();
            debugLog.AppendLine("Boss Arena Randomizer Debug Log");
            debugLog.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            debugLog.AppendLine();
            AppendSection(debugLog, "Run Settings");
            debugLog.AppendLine($"Options preset: {request.SelectedOptionsPreset}");
            debugLog.AppendLine($"Pairing preset: {request.SelectedPairingPreset}");
            debugLog.AppendLine($"Selected arenas: {request.SelectedArenaIds.Count}");
            debugLog.AppendLine($"Selected bosses: {request.SelectedBossIds.Count}");
            debugLog.AppendLine($"Seed count: {request.SeedCount}");
            debugLog.AppendLine($"Replay seed: {(request.ReplaySeed.HasValue ? request.ReplaySeed.Value.ToString() : "None")}");
            debugLog.AppendLine($"Mode: {(request.WriteOutputFiles ? "Generate files" : "Dry run")}");
            debugLog.AppendLine();
            return debugLog;
        }

        private static void AppendSection(StringBuilder debugLog, string title)
        {
            debugLog.AppendLine(new string('=', 72));
            debugLog.AppendLine(title);
            debugLog.AppendLine(new string('=', 72));
        }

        private static List<GenerationDisplayGroup> BuildDisplayGroups(
            Dictionary<string, string> finalAssignments,
            GenerationRequest request)
        {
            return finalAssignments
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

        private static List<string> BuildUniformityReport(
            Dictionary<string, string> assignments,
            Dictionary<string, BossInfo> bosses,
            IReadOnlyCollection<string> selectedBossIds)
        {
            var selectedBossIdSet = new HashSet<string>(selectedBossIds, StringComparer.OrdinalIgnoreCase);
            var usageByBossName = assignments.Values
                .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            int duplicateAssignments = usageByBossName.Values.Sum(count => Math.Max(0, count - 1));
            int minUse = usageByBossName.Count == 0 ? 0 : usageByBossName.Values.Min();
            int maxUse = usageByBossName.Count == 0 ? 0 : usageByBossName.Values.Max();
            int unusedSelectedBosses = bosses
                .Where(boss => selectedBossIdSet.Contains(boss.Value.id))
                .Count(boss => !usageByBossName.ContainsKey(boss.Key));

            var lines = new List<string>
            {
                $"Assignments: {assignments.Count}",
                $"Unique bosses used: {usageByBossName.Count}",
                $"Duplicate assignments: {duplicateAssignments}",
                $"Unused selected bosses: {unusedSelectedBosses}",
                $"Boss usage range: {minUse} to {maxUse}",
                duplicateAssignments == 0
                    ? "Result type: unique boss assignment"
                    : "Result type: duplicate-balanced assignment"
            };

            var mostUsed = usageByBossName
                .OrderByDescending(x => x.Value)
                .ThenBy(x => x.Key)
                .Take(5)
                .Select(x => $"{x.Key} ({x.Value})")
                .ToList();

            if (mostUsed.Count > 0)
                lines.Add("Most used bosses: " + string.Join(", ", mostUsed));

            return lines;
        }

        private static void AddPairingFrequency(
            Dictionary<string, Dictionary<string, int>> pairingFrequency,
            Dictionary<string, string> assignments)
        {
            foreach (var assignment in assignments)
            {
                if (!pairingFrequency.TryGetValue(assignment.Key, out var bossCounts))
                {
                    bossCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    pairingFrequency[assignment.Key] = bossCounts;
                }

                bossCounts[assignment.Value] = bossCounts.TryGetValue(assignment.Value, out int count)
                    ? count + 1
                    : 1;
            }
        }

        private static List<string> BuildPairingFrequencyReport(
            Dictionary<string, Dictionary<string, int>> pairingFrequency,
            int successfulAssignments)
        {
            var lines = new List<string>();
            if (successfulAssignments <= 0)
                return lines;

            lines.Add($"Based on {successfulAssignments} successful seed(s).");

            foreach (var arena in pairingFrequency.OrderBy(x => x.Key))
            {
                lines.Add($"{arena.Key}:");

                foreach (var boss in arena.Value.OrderByDescending(x => x.Value).ThenBy(x => x.Key))
                {
                    double percentage = boss.Value * 100.0 / successfulAssignments;
                    lines.Add($"  {boss.Key}: {percentage:0.##}% ({boss.Value}/{successfulAssignments})");
                }
            }

            return lines;
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
