using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace BossArenaRandomizer.Core
{
    public sealed class PairingValidationIssue
    {
        public string Severity { get; init; } = "Info";
        public string Message { get; init; } = string.Empty;
    }

    public sealed class PairingValidationResult
    {
        public List<PairingValidationIssue> Issues { get; } = new();

        public bool HasErrors => Issues.Any(x => string.Equals(x.Severity, "Error", StringComparison.OrdinalIgnoreCase));
        public bool IsValid => !HasErrors;
    }

    public sealed class PairingPresetValidator
    {
        private readonly Dictionary<string, HashSet<string>> _allowedBossIdsByArenaId;

        public PairingPresetValidator(Dictionary<string, HashSet<string>> allowedBossIdsByArenaId)
        {
            _allowedBossIdsByArenaId = allowedBossIdsByArenaId
                ?? throw new ArgumentNullException(nameof(allowedBossIdsByArenaId));
        }

        public IReadOnlyDictionary<string, HashSet<string>> AllowedBossIdsByArenaId => _allowedBossIdsByArenaId;

        public static PairingPresetValidator Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Pairing preset file not found.", path);

            string json = File.ReadAllText(path);
            var raw = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json)
                ?? new Dictionary<string, List<string>>();

            var allowed = raw.ToDictionary(
                kvp => kvp.Key,
                kvp => new HashSet<string>(kvp.Value ?? new List<string>(), StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);

            return new PairingPresetValidator(allowed);
        }

        public bool ValidatePairing(string arenaId, string bossId)
        {
            return _allowedBossIdsByArenaId.TryGetValue(arenaId, out var allowedBossIds)
                && allowedBossIds.Contains(bossId);
        }

        public PairingValidationResult ValidatePreset(
            Dictionary<string, ArenaInfo> arenas,
            Dictionary<string, BossInfo> bosses,
            IReadOnlyCollection<string> selectedArenaIds,
            IReadOnlyCollection<string> selectedBossIds)
        {
            var cache = IndexedPairingCache.Build(arenas, bosses, this);
            var graph = SelectedPairingGraph.Build(cache, selectedArenaIds, selectedBossIds);
            return ValidatePreset(cache, graph, selectedArenaIds, selectedBossIds);
        }

        public PairingValidationResult ValidatePreset(
            IndexedPairingCache cache,
            SelectedPairingGraph graph,
            IReadOnlyCollection<string> selectedArenaIds,
            IReadOnlyCollection<string> selectedBossIds)
        {
            var result = new PairingValidationResult();

            var selectedArenaIdSet = new HashSet<string>(selectedArenaIds, StringComparer.OrdinalIgnoreCase);
            var selectedBossIdSet = new HashSet<string>(selectedBossIds, StringComparer.OrdinalIgnoreCase);

            foreach (var arenaId in _allowedBossIdsByArenaId.Keys.Where(id => !cache.ArenaIndexById.ContainsKey(id)).OrderBy(x => x))
                result.Issues.Add(new PairingValidationIssue { Severity = "Warning", Message = $"Preset contains unknown arena ID {arenaId}." });

            foreach (var bossId in _allowedBossIdsByArenaId.Values.SelectMany(x => x).Distinct(StringComparer.OrdinalIgnoreCase).Where(id => !cache.BossIndexById.ContainsKey(id)).OrderBy(x => x))
                result.Issues.Add(new PairingValidationIssue { Severity = "Warning", Message = $"Preset contains unknown boss ID {bossId}." });

            foreach (var arenaId in selectedArenaIdSet.Where(id => !cache.ArenaIndexById.ContainsKey(id)).OrderBy(x => x))
                result.Issues.Add(new PairingValidationIssue { Severity = "Error", Message = $"Selected arena ID {arenaId} is not in the loaded database." });

            foreach (var bossId in selectedBossIdSet.Where(id => !cache.BossIndexById.ContainsKey(id)).OrderBy(x => x))
                result.Issues.Add(new PairingValidationIssue { Severity = "Warning", Message = $"Selected boss ID {bossId} is not in the loaded database." });

            for (int arenaIndex = 0; arenaIndex < graph.ArenaIds.Length; arenaIndex++)
            {
                string arenaId = graph.ArenaIds[arenaIndex];
                string arenaName = graph.ArenaNames[arenaIndex];

                if (!_allowedBossIdsByArenaId.TryGetValue(arenaId, out var allowedBossIds))
                {
                    result.Issues.Add(new PairingValidationIssue { Severity = "Error", Message = $"{arenaName} has no entry in the selected pairing preset." });
                    continue;
                }

                if (graph.BossIndexesByArena[arenaIndex].Length == 0)
                    result.Issues.Add(new PairingValidationIssue { Severity = "Error", Message = $"{arenaName} has no allowed bosses from the current boss selection." });
            }

            var activeBossIds = new HashSet<string>(
                graph.BossIndexesByArena
                    .SelectMany(x => x)
                    .Select(bossIndex => graph.BossIds[bossIndex]),
                StringComparer.OrdinalIgnoreCase);

            foreach (var bossId in selectedBossIdSet.Where(id => cache.BossIndexById.ContainsKey(id) && !activeBossIds.Contains(id)).OrderBy(id => cache.BossNames[cache.BossIndexById[id]]))
            {
                string bossName = cache.BossNames[cache.BossIndexById[bossId]];
                result.Issues.Add(new PairingValidationIssue { Severity = "Warning", Message = $"{bossName} is selected but is not allowed in any selected arena." });
            }

            if (!result.Issues.Any())
                result.Issues.Add(new PairingValidationIssue { Severity = "Info", Message = "Pairing preset validation passed." });

            return result;
        }

        public static string FormatValidationResult(PairingValidationResult validation)
        {
            var sb = new StringBuilder();
            foreach (var issue in validation.Issues)
                sb.AppendLine($"[{issue.Severity}] {issue.Message}");

            return sb.ToString().TrimEnd();
        }
    }
}
