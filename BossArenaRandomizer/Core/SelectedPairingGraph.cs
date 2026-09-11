using System;
using System.Collections.Generic;
using System.Linq;

namespace BossArenaRandomizer.Core
{
    public sealed class SelectedPairingGraph
    {
        public IndexedPairingCache Cache { get; }
        public int[] ArenaCacheIndexes { get; }
        public int[] BossCacheIndexes { get; }
        public string[] ArenaNames { get; }
        public string[] ArenaIds { get; }
        public int[] ArenaRegions { get; }
        public string[] BossNames { get; }
        public string[] BossIds { get; }
        public int[][] BossIndexesByArena { get; }

        private SelectedPairingGraph(
            IndexedPairingCache cache,
            int[] arenaCacheIndexes,
            int[] bossCacheIndexes,
            string[] arenaNames,
            string[] arenaIds,
            int[] arenaRegions,
            string[] bossNames,
            string[] bossIds,
            int[][] bossIndexesByArena)
        {
            Cache = cache;
            ArenaCacheIndexes = arenaCacheIndexes;
            BossCacheIndexes = bossCacheIndexes;
            ArenaNames = arenaNames;
            ArenaIds = arenaIds;
            ArenaRegions = arenaRegions;
            BossNames = bossNames;
            BossIds = bossIds;
            BossIndexesByArena = bossIndexesByArena;
        }

        public static SelectedPairingGraph Build(
            IndexedPairingCache cache,
            IReadOnlyCollection<string> selectedArenaIds,
            IReadOnlyCollection<string> selectedBossIds)
        {
            if (cache == null)
                throw new ArgumentNullException(nameof(cache));

            var selectedArenaIndexes = selectedArenaIds
                .Select(id => cache.ArenaIndexById.TryGetValue(id, out int index) ? index : -1)
                .Where(index => index >= 0)
                .Distinct()
                .OrderBy(index => cache.ArenaNames[index], StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var selectedBossIndexes = selectedBossIds
                .Select(id => cache.BossIndexById.TryGetValue(id, out int index) ? index : -1)
                .Where(index => index >= 0)
                .Distinct()
                .OrderBy(index => cache.BossNames[index], StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var selectedBossLocalIndexByCacheIndex = new Dictionary<int, int>();
            for (int localBossIndex = 0; localBossIndex < selectedBossIndexes.Length; localBossIndex++)
                selectedBossLocalIndexByCacheIndex[selectedBossIndexes[localBossIndex]] = localBossIndex;

            var bossIndexesByArena = new int[selectedArenaIndexes.Length][];
            for (int localArenaIndex = 0; localArenaIndex < selectedArenaIndexes.Length; localArenaIndex++)
            {
                int arenaIndex = selectedArenaIndexes[localArenaIndex];
                var validBossIndexes = new List<int>();

                foreach (int bossIndex in selectedBossIndexes)
                {
                    if (cache.IsAllowed(arenaIndex, bossIndex))
                        validBossIndexes.Add(selectedBossLocalIndexByCacheIndex[bossIndex]);
                }

                bossIndexesByArena[localArenaIndex] = validBossIndexes.ToArray();
            }

            return new SelectedPairingGraph(
                cache,
                selectedArenaIndexes,
                selectedBossIndexes,
                selectedArenaIndexes.Select(index => cache.ArenaNames[index]).ToArray(),
                selectedArenaIndexes.Select(index => cache.ArenaIds[index]).ToArray(),
                selectedArenaIndexes.Select(index => cache.ArenaRegions[index]).ToArray(),
                selectedBossIndexes.Select(index => cache.BossNames[index]).ToArray(),
                selectedBossIndexes.Select(index => cache.BossIds[index]).ToArray(),
                bossIndexesByArena);
        }

        public int[][] CreateShuffledCandidateIndexes(Random rng)
        {
            var shuffled = new int[BossIndexesByArena.Length][];
            for (int arenaIndex = 0; arenaIndex < BossIndexesByArena.Length; arenaIndex++)
            {
                shuffled[arenaIndex] = (int[])BossIndexesByArena[arenaIndex].Clone();
                Shuffle(shuffled[arenaIndex], rng);
            }

            return shuffled;
        }

        private static void Shuffle<T>(IList<T> items, Random rng)
        {
            for (int i = items.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (items[i], items[j]) = (items[j], items[i]);
            }
        }
    }
}
