using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BossArenaRandomizer.Core;

namespace BossArenaRandomizer.Services
{
    public sealed class SeedAnalysisService
    {
        private readonly string _analysisLinesPath;
        private readonly string _customChecksPath;
        private List<string> _analysisLines;
        private List<CustomSeedCheckDefinition> _customChecks;

        public SeedAnalysisService(string basePath)
        {
            if (string.IsNullOrWhiteSpace(basePath))
                throw new ArgumentException("Base path is required.", nameof(basePath));

            _analysisLinesPath = Path.Combine(basePath, "Data", "AnalyzeSeedLines.json");
            _customChecksPath = Path.Combine(basePath, "Data", "CustomSeedChecks.json");
            _analysisLines = LoadAnalysisLines();
            _customChecks = LoadCustomChecks();
        }

        public string AnalysisLinesPath => _analysisLinesPath;
        public string CustomChecksPath => _customChecksPath;

        public List<CheckOption> GetAvailableCheckOptions()
        {
            return CreateRunner().AvailableChecks
                .Select(check => new CheckOption
                {
                    Id = check.Id,
                    Name = check.DisplayName,
                    Description = check.Description,
                    IsSelected = false
                })
                .ToList();
        }

        public List<string> GetAnalysisLines()
        {
            return _analysisLines.ToList();
        }

        public List<string> GetDefaultAnalysisLines()
        {
            return OMotherCheck.DefaultInvalidPhrases.ToList();
        }

        public List<string> SaveAnalysisLines(IEnumerable<string> lines)
        {
            _analysisLines = NormalizeLines(lines);

            string? directory = Path.GetDirectoryName(_analysisLinesPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            string json = JsonSerializer.Serialize(
                _analysisLines,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_analysisLinesPath, json);

            return GetAnalysisLines();
        }

        public List<CustomSeedCheckDefinition> GetCustomChecks()
        {
            return _customChecks.Select(CloneCustomCheck).ToList();
        }

        public List<CustomSeedCheckDefinition> SaveCustomChecks(IEnumerable<CustomSeedCheckDefinition> checks)
        {
            _customChecks = NormalizeCustomChecks(checks);

            string? directory = Path.GetDirectoryName(_customChecksPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            string json = JsonSerializer.Serialize(
                _customChecks,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_customChecksPath, json);

            return GetCustomChecks();
        }

        public string ReadSeedText(string seedPath)
        {
            if (string.IsNullOrWhiteSpace(seedPath))
                throw new InvalidOperationException("Seed path is required.");

            if (!File.Exists(seedPath))
                throw new FileNotFoundException("Seed file not found.", seedPath);

            return File.ReadAllText(seedPath);
        }

        public List<SeedCheckResult> RunSelectedChecks(string seedText, IEnumerable<string> selectedIds)
        {
            var ids = selectedIds?.ToList() ?? new List<string>();
            if (ids.Count == 0)
                throw new InvalidOperationException("At least one check must be selected.");

            return CreateRunner().RunSelected(seedText, ids).ToList();
        }

        public string BuildResultsText(List<SeedCheckResult> results, List<CheckOption> checkOptions)
        {
            if (results == null || results.Count == 0)
                return "No results.";

            return string.Join(
                "\n\n",
                results.Select(result =>
                {
                    string name = checkOptions.FirstOrDefault(option => option.Id == result.CheckId)?.Name
                        ?? result.CheckId;
                    return $"[{name}]\n{result.Message}";
                }));
        }

        public string BuildStatusText(List<SeedCheckResult> results)
        {
            if (results == null || results.Count == 0)
                return "No checks run";

            return results.All(result => result.Passed)
                ? "Done (All Passed)"
                : "Done (Some Failed)";
        }

        private SeedCheckRunner CreateRunner()
        {
            var checks = new List<ISeedCheck>
            {
                new DoubleGreatRuneCheck(),
                new OMotherCheck(_analysisLines)
            };
            checks.AddRange(_customChecks.Select(check => new CustomSpoilerLineCheck(check)));
            return new SeedCheckRunner(checks);
        }

        private List<string> LoadAnalysisLines()
        {
            if (!File.Exists(_analysisLinesPath))
                return GetDefaultAnalysisLines();

            try
            {
                string json = File.ReadAllText(_analysisLinesPath);
                return NormalizeLines(JsonSerializer.Deserialize<List<string>>(json));
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException("AnalyzeSeedLines.json is not valid JSON.", ex);
            }
        }

        private List<CustomSeedCheckDefinition> LoadCustomChecks()
        {
            if (!File.Exists(_customChecksPath))
                return new List<CustomSeedCheckDefinition>();

            try
            {
                string json = File.ReadAllText(_customChecksPath);
                return NormalizeCustomChecks(JsonSerializer.Deserialize<List<CustomSeedCheckDefinition>>(json));
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException("CustomSeedChecks.json is not valid JSON.", ex);
            }
        }

        private static List<string> NormalizeLines(IEnumerable<string>? lines)
        {
            return lines?
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();
        }

        private static List<CustomSeedCheckDefinition> NormalizeCustomChecks(
            IEnumerable<CustomSeedCheckDefinition>? checks)
        {
            var normalized = new List<CustomSeedCheckDefinition>();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (CustomSeedCheckDefinition check in checks ?? Enumerable.Empty<CustomSeedCheckDefinition>())
            {
                string name = check.Name.Trim();
                string searchText = check.SearchText.Trim();
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(searchText))
                    continue;

                string id = (check.Id ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(id) || ids.Contains(id))
                    id = Guid.NewGuid().ToString("N");
                ids.Add(id);

                normalized.Add(new CustomSeedCheckDefinition
                {
                    Id = id,
                    Name = name,
                    SearchText = searchText,
                    MatchOutcome = CustomSeedCheckOutcomes.Normalize(check.MatchOutcome)
                });
            }

            return normalized;
        }

        private static CustomSeedCheckDefinition CloneCustomCheck(CustomSeedCheckDefinition check)
        {
            return new CustomSeedCheckDefinition
            {
                Id = check.Id,
                Name = check.Name,
                SearchText = check.SearchText,
                MatchOutcome = check.MatchOutcome
            };
        }
    }
}
