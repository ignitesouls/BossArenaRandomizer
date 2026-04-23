using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BossArenaRandomizer.Core;

namespace BossArenaRandomizer.Services
{
    public sealed class SeedAnalysisService
    {
        private readonly SeedCheckRunner _seedCheckRunner;

        public SeedAnalysisService()
        {
            _seedCheckRunner = new SeedCheckRunner(new ISeedCheck[]
            {
                new DoubleGreatRuneCheck(),
                new OMotherCheck()
                // Add new seed checks here.
            });
        }

        public List<CheckOption> GetAvailableCheckOptions()
        {
            return _seedCheckRunner.AvailableChecks
                .Select(c => new CheckOption
                {
                    Id = c.Id,
                    Name = c.DisplayName,
                    Description = c.Description,
                    IsSelected = false
                })
                .ToList();
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

            return _seedCheckRunner.RunSelected(seedText, ids).ToList();
        }

        public string BuildResultsText(List<SeedCheckResult> results, List<CheckOption> checkOptions)
        {
            if (results == null || results.Count == 0)
                return "No results.";

            return string.Join(
                "\n\n",
                results.Select(r =>
                {
                    var name = checkOptions.FirstOrDefault(o => o.Id == r.CheckId)?.Name ?? r.CheckId;
                    return $"[{name}]\n{r.Message}";
                }));
        }

        public string BuildStatusText(List<SeedCheckResult> results)
        {
            if (results == null || results.Count == 0)
                return "No checks run";

            return results.All(r => r.Passed)
                ? "Done (All Passed)"
                : "Done (Some Failed)";
        }
    }
}