using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BossArenaRandomizer.Core;

namespace BossArenaRandomizer.Services
{
    public sealed class GenerationService
    {
        private readonly Func<string, IProjectPaths> _pathsFactory;
        private readonly IPairingPresetLoader _pairingPresetLoader;
        private readonly IAssignmentWriter _assignmentWriter;
        private readonly UniformityReporter _uniformityReporter;
        private readonly GenerationDisplayBuilder _displayBuilder;

        public GenerationService()
            : this(
                basePath => new ProjectPaths(basePath),
                new PairingPresetFileLoader(),
                new RandomizeOptionsAssignmentWriter(),
                new UniformityReporter(),
                new GenerationDisplayBuilder())
        {
        }

        public GenerationService(
            Func<string, IProjectPaths> pathsFactory,
            IPairingPresetLoader pairingPresetLoader,
            IAssignmentWriter assignmentWriter,
            UniformityReporter uniformityReporter,
            GenerationDisplayBuilder displayBuilder)
        {
            _pathsFactory = pathsFactory ?? throw new ArgumentNullException(nameof(pathsFactory));
            _pairingPresetLoader = pairingPresetLoader ?? throw new ArgumentNullException(nameof(pairingPresetLoader));
            _assignmentWriter = assignmentWriter ?? throw new ArgumentNullException(nameof(assignmentWriter));
            _uniformityReporter = uniformityReporter ?? throw new ArgumentNullException(nameof(uniformityReporter));
            _displayBuilder = displayBuilder ?? throw new ArgumentNullException(nameof(displayBuilder));
        }

        public GenerationResult Generate(GenerationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var initialError = ValidateInitialRequest(request);
            if (!string.IsNullOrWhiteSpace(initialError))
                return Fail(initialError);

            var paths = _pathsFactory(request.BasePath);
            string optionsFilePath = paths.OptionsPresetPath(request.SelectedOptionsPreset);
            if (!File.Exists(optionsFilePath))
                return Fail("Options preset file not found.");

            PairingPresetValidator validator;
            try
            {
                validator = _pairingPresetLoader.Load(paths.PairingPresetPath(request.SelectedPairingPreset));
            }
            catch (Exception ex)
            {
                return Fail($"Pairing preset could not be loaded: {ex.Message}");
            }

            var pairingCache = IndexedPairingCache.Build(request.Arenas, request.Bosses, validator);
            var selectedGraph = SelectedPairingGraph.Build(pairingCache, request.SelectedArenaIds, request.SelectedBossIds);
            var debugLog = new DebugLogBuilder(request);
            var validation = validator.ValidatePreset(
                pairingCache,
                selectedGraph,
                request.SelectedArenaIds,
                request.SelectedBossIds);

            var validationLines = validation.Issues
                .Select(x => $"[{x.Severity}] {x.Message}")
                .ToList();

            debugLog.AppendValidation(validationLines);

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
            var pairingFrequencyReporter = new PairingFrequencyReporter();
            int successfulAssignments = 0;

            for (int i = 1; i <= request.SeedCount; i++)
            {
                int seed = request.ReplaySeed.HasValue
                    ? request.ReplaySeed.Value + i - 1
                    : Random.Shared.Next(1, int.MaxValue);

                var rng = new Random(seed);
                debugLog.BeginSeed(i, seed, request.SeedCount);

                bool ok = ArenaBossAssigner.TryAssign(
                    selectedGraph,
                    request.MaxAttempts,
                    rng,
                    out var assignResult,
                    warnDupeMode: msg =>
                    {
                        Debug.WriteLine(msg);
                        debugLog.AppendLine($"Warning: {msg}");
                    },
                    debugLog: msg =>
                    {
                        Debug.WriteLine(msg);
                        debugLog.AppendLine(msg);
                    });

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

                var finalAssignments = assignResult.AssignmentPairs;
                string outputPath = string.Empty;

                if (request.WriteOutputFiles)
                {
                    try
                    {
                        outputPath = paths.BuildBatchOutputPath(
                            request.OutputFolderPath,
                            request.FileNamePattern,
                            request.SelectedOptionsPreset,
                            i,
                            seed);

                        _assignmentWriter.Write(
                            finalAssignments,
                            outputPath,
                            optionsFilePath,
                            seed,
                            request.ClearArenasEnabled);
                    }
                    catch (Exception ex)
                    {
                        string message = $"Output failed: {ex.Message}";
                        debugLog.AppendLine($"Status: {message}");
                        if (!string.IsNullOrWhiteSpace(outputPath))
                            debugLog.AppendLine($"Output: {outputPath}");

                        result.BatchResults.Add(new BatchSeedResult
                        {
                            Index = i,
                            Success = false,
                            Seed = seed,
                            OutputPath = outputPath,
                            Message = message
                        });
                        continue;
                    }
                }

                successfulAssignments++;
                pairingFrequencyReporter.Add(finalAssignments);

                result.LastSeed = seed;
                result.FinalAssignmentPairs = finalAssignments.ToList();
                result.FinalAssignments = assignResult.Assignments;

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

                debugLog.AppendAssignments(finalAssignments);

                var uniformityLines = _uniformityReporter.BuildReport(
                    finalAssignments,
                    request.Bosses,
                    request.SelectedBossIds);

                debugLog.AppendUniformity(uniformityLines);

                result.UniformityLines = uniformityLines;
                result.DisplayGroups = _displayBuilder.Build(finalAssignments);
            }

            if (result.BatchResults.All(x => !x.Success))
            {
                result.Success = false;
                result.ErrorMessage = "All batch generations failed. See the batch results or debug log for details.";
            }

            result.PairingFrequencyLines = pairingFrequencyReporter.BuildReport(successfulAssignments);

            if (result.PairingFrequencyLines.Count > 0)
            {
                debugLog.AppendSection("Pairing Frequency");
                foreach (var line in result.PairingFrequencyLines)
                    debugLog.AppendLine(line);
                debugLog.AppendLine();
            }

            result.DebugLog = debugLog.ToString();
            return result;
        }

        private static string ValidateInitialRequest(GenerationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SelectedOptionsPreset))
                return "Please load an options preset.";

            if (request.WriteOutputFiles && string.IsNullOrWhiteSpace(request.OutputFolderPath))
                return "Please select an output folder first.";

            if (string.IsNullOrWhiteSpace(request.SelectedPairingPreset))
                return "Please select a boss/arena pairing preset.";

            if (request.SeedCount <= 0)
                return "Seed count must be at least 1.";

            return string.Empty;
        }

        private static GenerationResult Fail(string message)
        {
            return new GenerationResult
            {
                Success = false,
                ErrorMessage = message
            };
        }
    }
}
