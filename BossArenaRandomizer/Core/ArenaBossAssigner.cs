using System;
using System.Collections.Generic;
using System.Linq;

namespace BossArenaRandomizer.Core
{
    public static class ArenaBossAssigner
    {
        public sealed class AssignResult
        {
            public Dictionary<string, string> Assignments { get; }
            public IReadOnlyList<AssignmentPair> AssignmentPairs { get; }
            public int AttemptsUsed { get; }

            public AssignResult(IReadOnlyList<AssignmentPair> assignmentPairs, int attemptsUsed)
            {
                AssignmentPairs = assignmentPairs;
                Assignments = assignmentPairs.ToDictionary(
                    assignment => assignment.ArenaName,
                    assignment => assignment.BossName,
                    StringComparer.OrdinalIgnoreCase);
                AttemptsUsed = attemptsUsed;
            }

            public AssignResult(Dictionary<string, string> assignments, int attemptsUsed)
            {
                AssignmentPairs = Array.Empty<AssignmentPair>();
                Assignments = assignments;
                AttemptsUsed = attemptsUsed;
            }
        }

        private sealed class IndexedCandidates
        {
            public required string[] ArenaNames { get; init; }
            public required string[] ArenaIds { get; init; }
            public required int[] ArenaRegions { get; init; }
            public required string[] BossNames { get; init; }
            public required string[] BossIds { get; init; }
            public required int[][] BossIndexesByArena { get; init; }
        }

        public static bool TryAssign(
            Dictionary<string, ArenaInfo> arenas,
            Dictionary<string, BossInfo> bosses,
            IReadOnlyCollection<string> selectedArenaIds,
            IReadOnlyCollection<string> selectedBossIds,
            PairingPresetValidator validator,
            int maxAttempts,
            Random rng,
            out AssignResult? result,
            Action<string>? warnDupeMode = null,
            Action<string>? debugLog = null)
        {
            var cache = IndexedPairingCache.Build(arenas, bosses, validator);
            var graph = SelectedPairingGraph.Build(cache, selectedArenaIds, selectedBossIds);
            return TryAssign(
                graph,
                maxAttempts,
                rng,
                out result,
                warnDupeMode,
                debugLog);
        }

        public static bool TryAssign(
            IndexedPairingCache cache,
            IReadOnlyCollection<string> selectedArenaIds,
            IReadOnlyCollection<string> selectedBossIds,
            int maxAttempts,
            Random rng,
            out AssignResult? result,
            Action<string>? warnDupeMode = null,
            Action<string>? debugLog = null)
        {
            var graph = SelectedPairingGraph.Build(cache, selectedArenaIds, selectedBossIds);
            return TryAssign(graph, maxAttempts, rng, out result, warnDupeMode, debugLog);
        }

        public static bool TryAssign(
            SelectedPairingGraph graph,
            int maxAttempts,
            Random rng,
            out AssignResult? result,
            Action<string>? warnDupeMode = null,
            Action<string>? debugLog = null)
        {
            result = null;

            var candidates = new IndexedCandidates
            {
                ArenaNames = graph.ArenaNames,
                ArenaIds = graph.ArenaIds,
                ArenaRegions = graph.ArenaRegions,
                BossNames = graph.BossNames,
                BossIds = graph.BossIds,
                BossIndexesByArena = graph.CreateShuffledCandidateIndexes(rng)
            };

            if (candidates.ArenaNames.Length == 0 || candidates.BossNames.Length == 0)
                return false;

            if (candidates.BossIndexesByArena.Any(c => c.Length == 0))
                return false;

            bool duplicateModeRequired = candidates.ArenaNames.Length > candidates.BossNames.Length;

            if (!duplicateModeRequired)
            {
                if (TryBuildUniqueAssignment(candidates, rng, out var uniqueAssignments))
                {
                    debugLog?.Invoke("Unique assignment solved with bipartite matching.");
                    result = new AssignResult(uniqueAssignments, 1);
                    return true;
                }

                return false;
            }

            warnDupeMode?.Invoke(
                "Selecting more arenas than bosses will allow for duplicates. " +
                "Duplicate boss usage is balanced as evenly as the active preset allows.");

            for (int attempt = 1; attempt <= Math.Max(1, maxAttempts); attempt++)
            {
                if (TryBuildDuplicateAssignment(candidates, rng, out var duplicateAssignments))
                {
                    debugLog?.Invoke($"Number of iterations before success: {attempt}");
                    result = new AssignResult(duplicateAssignments, attempt);
                    return true;
                }
            }

            return false;
        }

        private static bool TryBuildUniqueAssignment(
            IndexedCandidates candidates,
            Random rng,
            out IReadOnlyList<AssignmentPair> assignments)
        {
            assignments = Array.Empty<AssignmentPair>();

            if (candidates.ArenaNames.Length > candidates.BossNames.Length)
                return false;

            if (CountReachableBosses(candidates.BossIndexesByArena, candidates.BossNames.Length) < candidates.ArenaNames.Length)
                return false;

            var matcher = new HopcroftKarpMatcher(candidates.BossIndexesByArena, candidates.BossNames.Length, rng);
            if (!matcher.TryFindPerfectMatching(out var matchedBossByArena))
                return false;

            var assignmentPairs = new List<AssignmentPair>(candidates.ArenaNames.Length);
            for (int arenaIndex = 0; arenaIndex < candidates.ArenaNames.Length; arenaIndex++)
                assignmentPairs.Add(BuildAssignmentPair(candidates, arenaIndex, matchedBossByArena[arenaIndex]));

            assignments = assignmentPairs;
            return true;
        }

        private static bool TryBuildDuplicateAssignment(
            IndexedCandidates candidates,
            Random rng,
            out IReadOnlyList<AssignmentPair> assignments)
        {
            assignments = Array.Empty<AssignmentPair>();
            var usedCounts = new int[candidates.BossNames.Length];
            var assignmentIndexes = Enumerable.Repeat(-1, candidates.ArenaNames.Length).ToArray();
            var remainingArenaIndexes = Enumerable.Range(0, candidates.ArenaNames.Length).ToList();

            if (!TryAssignDuplicatesRecursive(candidates, remainingArenaIndexes, assignmentIndexes, usedCounts, rng))
                return false;

            var assignmentPairs = new List<AssignmentPair>(assignmentIndexes.Length);
            for (int arenaIndex = 0; arenaIndex < assignmentIndexes.Length; arenaIndex++)
                assignmentPairs.Add(BuildAssignmentPair(candidates, arenaIndex, assignmentIndexes[arenaIndex]));

            assignments = assignmentPairs;
            return true;
        }

        private static AssignmentPair BuildAssignmentPair(IndexedCandidates candidates, int arenaIndex, int bossIndex)
        {
            return new AssignmentPair
            {
                ArenaId = new ArenaId(candidates.ArenaIds[arenaIndex]),
                ArenaName = candidates.ArenaNames[arenaIndex],
                ArenaRegion = candidates.ArenaRegions[arenaIndex],
                BossId = new BossId(candidates.BossIds[bossIndex]),
                BossName = candidates.BossNames[bossIndex]
            };
        }

        private static bool TryAssignDuplicatesRecursive(
            IndexedCandidates candidates,
            List<int> remainingArenaIndexes,
            int[] assignmentIndexes,
            int[] usedCounts,
            Random rng)
        {
            if (remainingArenaIndexes.Count == 0)
                return true;

            int remainingListIndex = PickMostConstrainedArenaListIndex(candidates, remainingArenaIndexes, rng);
            if (remainingListIndex < 0)
                return false;

            int arenaIndex = remainingArenaIndexes[remainingListIndex];
            remainingArenaIndexes.RemoveAt(remainingListIndex);

            foreach (int bossIndex in GetBalancedBossOrder(candidates.BossIndexesByArena[arenaIndex], usedCounts, rng))
            {
                assignmentIndexes[arenaIndex] = bossIndex;
                usedCounts[bossIndex]++;

                if (TryAssignDuplicatesRecursive(candidates, remainingArenaIndexes, assignmentIndexes, usedCounts, rng))
                    return true;

                usedCounts[bossIndex]--;
                assignmentIndexes[arenaIndex] = -1;
            }

            remainingArenaIndexes.Add(arenaIndex);
            return false;
        }

        private static int PickMostConstrainedArenaListIndex(
            IndexedCandidates candidates,
            List<int> remainingArenaIndexes,
            Random rng)
        {
            int bestCandidateCount = int.MaxValue;
            var tiedListIndexes = new List<int>();

            for (int listIndex = 0; listIndex < remainingArenaIndexes.Count; listIndex++)
            {
                int arenaIndex = remainingArenaIndexes[listIndex];
                int candidateCount = candidates.BossIndexesByArena[arenaIndex].Length;
                if (candidateCount == 0)
                    continue;

                if (candidateCount < bestCandidateCount)
                {
                    bestCandidateCount = candidateCount;
                    tiedListIndexes.Clear();
                    tiedListIndexes.Add(listIndex);
                }
                else if (candidateCount == bestCandidateCount)
                {
                    tiedListIndexes.Add(listIndex);
                }
            }

            if (tiedListIndexes.Count == 0)
                return -1;

            return tiedListIndexes[rng.Next(tiedListIndexes.Count)];
        }

        private static IEnumerable<int> GetBalancedBossOrder(
            int[] bossIndexes,
            int[] usedCounts,
            Random rng)
        {
            var orderedBosses = bossIndexes.ToList();
            Shuffle(orderedBosses, rng);
            orderedBosses.Sort((left, right) => usedCounts[left].CompareTo(usedCounts[right]));

            foreach (int bossIndex in orderedBosses)
                yield return bossIndex;
        }

        private static int CountReachableBosses(int[][] bossIndexesByArena, int bossCount)
        {
            var reachable = new bool[bossCount];
            int count = 0;

            foreach (var bossIndexes in bossIndexesByArena)
            {
                foreach (int bossIndex in bossIndexes)
                {
                    if (reachable[bossIndex])
                        continue;

                    reachable[bossIndex] = true;
                    count++;
                }
            }

            return count;
        }

        private static void Shuffle<T>(IList<T> items, Random rng)
        {
            for (int i = items.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (items[i], items[j]) = (items[j], items[i]);
            }
        }

        private sealed class HopcroftKarpMatcher
        {
            private readonly int[][] _bossIndexesByArena;
            private readonly int _bossCount;
            private readonly int[] _arenaOrder;
            private readonly int[] _matchedBossByArena;
            private readonly int[] _matchedArenaByBoss;
            private readonly int[] _distance;

            public HopcroftKarpMatcher(int[][] bossIndexesByArena, int bossCount, Random rng)
            {
                _bossIndexesByArena = bossIndexesByArena;
                _bossCount = bossCount;
                _arenaOrder = Enumerable.Range(0, bossIndexesByArena.Length).ToArray();
                Shuffle(_arenaOrder, rng);
                _matchedBossByArena = Enumerable.Repeat(-1, bossIndexesByArena.Length).ToArray();
                _matchedArenaByBoss = Enumerable.Repeat(-1, bossCount).ToArray();
                _distance = new int[bossIndexesByArena.Length];
            }

            public bool TryFindPerfectMatching(out int[] matchedBossByArena)
            {
                int matchingSize = 0;
                while (BuildDistanceLayers())
                {
                    foreach (int arenaIndex in _arenaOrder)
                    {
                        if (_matchedBossByArena[arenaIndex] == -1 && FindAugmentingPath(arenaIndex))
                            matchingSize++;
                    }
                }

                matchedBossByArena = _matchedBossByArena;
                return matchingSize == _bossIndexesByArena.Length;
            }

            private bool BuildDistanceLayers()
            {
                var queue = new Queue<int>();
                bool canReachFreeBoss = false;

                foreach (int arenaIndex in _arenaOrder)
                {
                    if (_matchedBossByArena[arenaIndex] == -1)
                    {
                        _distance[arenaIndex] = 0;
                        queue.Enqueue(arenaIndex);
                    }
                    else
                    {
                        _distance[arenaIndex] = -1;
                    }
                }

                while (queue.Count > 0)
                {
                    int arenaIndex = queue.Dequeue();
                    foreach (int bossIndex in _bossIndexesByArena[arenaIndex])
                    {
                        int matchedArena = _matchedArenaByBoss[bossIndex];
                        if (matchedArena == -1)
                        {
                            canReachFreeBoss = true;
                            continue;
                        }

                        if (_distance[matchedArena] != -1)
                            continue;

                        _distance[matchedArena] = _distance[arenaIndex] + 1;
                        queue.Enqueue(matchedArena);
                    }
                }

                return canReachFreeBoss;
            }

            private bool FindAugmentingPath(int arenaIndex)
            {
                foreach (int bossIndex in _bossIndexesByArena[arenaIndex])
                {
                    int matchedArena = _matchedArenaByBoss[bossIndex];
                    if (matchedArena == -1 ||
                        (_distance[matchedArena] == _distance[arenaIndex] + 1 && FindAugmentingPath(matchedArena)))
                    {
                        _matchedBossByArena[arenaIndex] = bossIndex;
                        _matchedArenaByBoss[bossIndex] = arenaIndex;
                        return true;
                    }
                }

                _distance[arenaIndex] = -1;
                return false;
            }
        }
    }
}
