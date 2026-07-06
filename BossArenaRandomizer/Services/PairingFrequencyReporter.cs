using System;
using System.Collections.Generic;
using System.Linq;
using BossArenaRandomizer.Core;

namespace BossArenaRandomizer.Services
{
    public sealed class PairingFrequencyReporter
    {
        private readonly Dictionary<string, Dictionary<string, int>> _pairingFrequency = new(StringComparer.OrdinalIgnoreCase);

        public void Add(IReadOnlyCollection<AssignmentPair> assignments)
        {
            foreach (var assignment in assignments)
            {
                if (!_pairingFrequency.TryGetValue(assignment.ArenaName, out var bossCounts))
                {
                    bossCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    _pairingFrequency[assignment.ArenaName] = bossCounts;
                }

                bossCounts[assignment.BossName] = bossCounts.TryGetValue(assignment.BossName, out int count)
                    ? count + 1
                    : 1;
            }
        }

        public List<string> BuildReport(int successfulAssignments)
        {
            var lines = new List<string>();
            if (successfulAssignments <= 0)
                return lines;

            lines.Add($"Based on {successfulAssignments} successful seed(s).");

            foreach (var arena in _pairingFrequency.OrderBy(x => x.Key))
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
    }
}
