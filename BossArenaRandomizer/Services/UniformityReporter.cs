using System;
using System.Collections.Generic;
using System.Linq;
using BossArenaRandomizer.Core;

namespace BossArenaRandomizer.Services
{
    public sealed class UniformityReporter
    {
        public List<string> BuildReport(
            IReadOnlyCollection<AssignmentPair> assignments,
            Dictionary<string, BossInfo> bosses,
            IReadOnlyCollection<string> selectedBossIds)
        {
            var selectedBossIdSet = new HashSet<string>(selectedBossIds, StringComparer.OrdinalIgnoreCase);
            var usageByBossName = assignments
                .GroupBy(x => x.BossName, StringComparer.OrdinalIgnoreCase)
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
    }
}
