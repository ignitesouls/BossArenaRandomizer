using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace BossArenaRandomizer.Core
{
    public static class ArenaBossAssigner
    {
        public sealed class AssignResult
        {
            public Dictionary<string, string> Assignments { get; }
            public int AttemptsUsed { get; }

            public AssignResult(Dictionary<string, string> assignments, int attemptsUsed)
            {
                Assignments = assignments;
                AttemptsUsed = attemptsUsed;
            }
        }

        public static bool TryAssign(
            Dictionary<string, ArenaInfo> arenas,
            Dictionary<string, BossInfo> bosses,
            IReadOnlyCollection<string> selectedArenaIds,
            IReadOnlyCollection<string> selectedBossIds,
            dynamic validator,
            int maxAttempts,
            Random rng,
            out AssignResult? result,
            Action<string>? warnDupeMode = null,
            Action<string>? debugLog = null)
        {
            result = null;

            // Preselect arenas once (stable across attempts)
            var selectedArenaEntries = arenas
                .Where(a => selectedArenaIds.Contains(a.Value.id))
                .ToList();

            if (selectedArenaEntries.Count == 0)
                return false;

            bool allowDupesAfterExhaustingPool = selectedArenaIds.Count > selectedBossIds.Count;
            bool warned = false;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                // Build boss pool each attempt
                var selectedBossPool = bosses
                    .Where(b => selectedBossIds.Contains(b.Value.id))
                    .ToDictionary(b => b.Key, b => b.Value);

                if (selectedBossPool.Count == 0)
                    return false;

                var usedBosses = new HashSet<string>(); // bossName keys used
                var tempAssignments = new Dictionary<string, string>();
                bool allValid = true;

                foreach (var arenaEntry in selectedArenaEntries)
                {
                    string arenaName = arenaEntry.Key;

                    if (!int.TryParse(arenaEntry.Value.id, out int arenaId))
                    {
                        allValid = false;
                        break;
                    }

                    string? selectedBossName = null;

                    // Shuffle boss pool for this arena pick
                    var shuffledBosses = selectedBossPool
                        .OrderBy(_ => rng.Next())
                        .ToList();

                    if (allowDupesAfterExhaustingPool)
                    {
                        if (!warned)
                        {
                            warned = true;
                            warnDupeMode?.Invoke(
                                "Selecting more arenas than bosses will allow for duplicates. " +
                                "Due to the BAR's constraints, the same boss can appear multiple times. " +
                                "This is because not every boss can go into every arena."
                            );
                        }

                        // Pass 1: unused bosses only
                        var unusedBosses = shuffledBosses
                            .Where(b => !usedBosses.Contains(b.Key))
                            .ToList();

                        selectedBossName = FindFirstValid(arenaId, unusedBosses, validator, usedBosses);

                        // Pass 2: allow dupes
                        if (selectedBossName == null)
                            selectedBossName = FindFirstValid(arenaId, shuffledBosses, validator, usedBosses);

                        if (selectedBossName == null)
                        {
                            allValid = false;
                            break;
                        }

                        tempAssignments[arenaName] = selectedBossName;
                        continue;
                    }

                    // No-dupe mode
                    foreach (var bossEntry in shuffledBosses)
                    {
                        string bossName = bossEntry.Key;

                        if (usedBosses.Contains(bossName))
                            continue;

                        if (!int.TryParse(bossEntry.Value.id, out int bossId))
                            continue;

                        if (validator.Validate(arenaId, bossId))
                        {
                            selectedBossName = bossName;
                            usedBosses.Add(bossName);
                            break;
                        }
                    }

                    if (selectedBossName == null)
                    {
                        allValid = false;
                        break;
                    }

                    tempAssignments[arenaName] = selectedBossName;
                }

                if (allValid)
                {
                    debugLog?.Invoke($"Number of iterations before success: {attempt}");
                    result = new AssignResult(tempAssignments, attempt);
                    return true;
                }
            }

            return false;
        }

        private static string? FindFirstValid(
            int arenaId,
            List<KeyValuePair<string, BossInfo>> bossEntries,
            dynamic validator,
            HashSet<string> usedBosses)
        {
            foreach (var bossEntry in bossEntries)
            {
                string bossName = bossEntry.Key;

                if (!int.TryParse(bossEntry.Value.id, out int bossId))
                    continue;

                if (validator.Validate(arenaId, bossId))
                {
                    usedBosses.Add(bossName);
                    return bossName;
                }
            }

            return null;
        }
    }
}
